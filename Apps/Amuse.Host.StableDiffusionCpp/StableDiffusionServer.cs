using Amuse.Common;
using Amuse.Host.StableDiffusionCpp.Common;
using Amuse.Host.StableDiffusionCpp.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Amuse.Host.StableDiffusionCpp
{
    public sealed class StableDiffusionServer : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly ServerConfig _configuration;
        private readonly ProcessHandler _processHandler;
        private readonly Channel<string> _consoleChannel = Channel.CreateUnbounded<string>();
        private readonly StableDiffusionClient _stableDiffusionClient;
        private readonly IProgress<PipelineProgress> _progressCallback;
        private CancellationTokenSource _cancellationTokenSource;
        private Process _serverProcess;
        private Task _consoleOutputTask;
        private JobModel _currentJob;
        private CapabilitiesModel _modelCapabilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionServer"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public StableDiffusionServer(ServerConfig configuration, IProgress<PipelineProgress> progressCallback, ILogger logger)
        {
            _logger = logger;
            _configuration = configuration;
            _progressCallback = progressCallback;
            _processHandler = new ProcessHandler();
            _stableDiffusionClient = new StableDiffusionClient(configuration);
        }

        /// <summary>
        /// Gets the current job.
        /// </summary>
        public JobModel CurrentJob => _currentJob;

        /// <summary>
        /// Gets the model capabilities.
        /// </summary>
        public CapabilitiesModel ModelCapabilities => _modelCapabilities;


        /// <summary>
        /// Start the server
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [StartAsync] Starting StableDiffusion.cpp server...");
            _consoleOutputTask = ProcessConsoleOutput(_cancellationTokenSource.Token);
            var serverPath = Path.Combine(_configuration.Directory, "sd-server.exe");
            var serverArguments = GetServerArguments(_configuration);
            _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [StartAsync] Server Path: {serverPath}", serverPath);
            _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [StartAsync] Server Arguments: {serverArguments}", serverArguments);
            var processInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                Arguments = GetServerArguments(_configuration),
                WorkingDirectory = Path.GetDirectoryName(serverPath),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            AddServerVariables(processInfo, _configuration);
            _serverProcess = new Process { StartInfo = processInfo };
            _serverProcess.OutputDataReceived += OnDataReceived;
            _serverProcess.ErrorDataReceived += OnDataReceived;
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
            _processHandler.AddProcess(_serverProcess);
            _modelCapabilities = await WaitForServerStartup(cancellationToken);
            _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [StartAsync] StableDiffusion.cpp server started.");
        }


        /// <summary>
        /// Stop the server
        /// </summary>
        /// <param name="timeout">The timeout before calling kill-process</param>
        public async Task StopAsync(int timeout = 5000)
        {
            if (_serverProcess is null)
                return;

            try
            {
                _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [StopAsync] Stopping StableDiffusion.cpp server...");
                _consoleChannel.Writer.TryComplete();
                _cancellationTokenSource.Cancel();
                if (_consoleOutputTask is not null)
                    await _consoleOutputTask;

                _serverProcess.Kill(true);
                using (var cancelation = new CancellationTokenSource(timeout))
                {
                    await _serverProcess.WaitForExitAsync(cancelation.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AmuseHost] [StableDiffusionServer] [StopAsync] Exception stopping StableDiffusion.cpp server.");
            }
            finally
            {
                _serverProcess?.Dispose();
                _serverProcess = null;
                _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [StopAsync] StableDiffusion.cpp server stopped.");
            }
        }


        /// <summary>
        /// Generate image
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.Exception">Failed To Create Image Job</exception>
        public async Task<byte[]> GenerateImageAsync(ImageParams request, CancellationToken cancellationToken = default)
        {
            try
            {
                _currentJob = await _stableDiffusionClient.CreateJobAsync(request, cancellationToken);
                if (_currentJob == null)
                    throw new Exception("Failed to create image job");

                var completed = await WaitForCompletionAsync(cancellationToken: cancellationToken);
                return completed.Result?.GetImageBytes();
            }
            catch (OperationCanceledException)
            {
                await CancelGenerateAsync();
                throw;
            }
            finally
            {
                _currentJob = null;
            }
        }


        /// <summary>
        /// Generate video
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.Exception">Failed To Create Image Job</exception>
        public async Task<byte[]> GenerateVideoAsync(VideoParams request, CancellationToken cancellationToken = default)
        {
            try
            {
                _currentJob = await _stableDiffusionClient.CreateJobAsync(request, cancellationToken);
                if (_currentJob == null)
                    throw new Exception("Failed to create video job");

                var completed = await WaitForCompletionAsync(cancellationToken: cancellationToken);
                return completed.Result?.GetVideoBytes();
            }
            catch (OperationCanceledException)
            {
                await CancelGenerateAsync();
                throw;
            }
            finally
            {
                _currentJob = null;
            }
        }


        /// <summary>
        /// Cancel the active generation
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<bool> CancelGenerateAsync()
        {
            if (_currentJob == null)
                return true;

            return await _stableDiffusionClient.CancelJobAsync(_currentJob);
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _stableDiffusionClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }


        /// <summary>
        /// Waits for server startup.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.InvalidOperationException">Stable Diffusion server failed to start.</exception>
        private async Task<CapabilitiesModel> WaitForServerStartup(CancellationToken cancellationToken = default)
        {
            var capabilities = default(CapabilitiesModel);
            using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500)))
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    if (_serverProcess.HasExited)
                        break;

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        capabilities = await _stableDiffusionClient.GetCapabilitiesAsync(cancellationToken);
                        if (capabilities != null)
                            break;
                    }
                    catch (Exception)
                    {
                        _logger.LogInformation("[AmuseHost] [StableDiffusionServer] [WaitForServerStartup] Waiting for server startup...");
                    }
                }
            }

            return capabilities ?? throw new InvalidOperationException("StableDiffusion.cpp server failed to start");
        }


        /// <summary>
        /// Wait for generation completion as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;JobModel&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.InvalidOperationException">Generation Job</exception>
        /// <exception cref="System.OperationCanceledException"></exception>
        /// <exception cref="System.Exception">Generation Failed</exception>
        private async Task<JobModel> WaitForCompletionAsync(CancellationToken cancellationToken = default)
        {
            using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500)))
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var generationJob = await _stableDiffusionClient.GetJobAsync(_currentJob, cancellationToken)
                                     ?? throw new InvalidOperationException($"Generation Job not found, Id: {_currentJob?.Id}");
                    if (generationJob.Status == JobStatus.Completed)
                        return generationJob;
                    if (generationJob.Status == JobStatus.Cancelled)
                        throw new OperationCanceledException();
                    if (generationJob.Status == JobStatus.Failed)
                        break;

                }
                cancellationToken.ThrowIfCancellationRequested();
                throw new Exception($"Generation Job failed, Id: {_currentJob?.Id}");
            }
        }


        /// <summary>
        /// Handles the <see cref="E:DataReceived" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> instance containing the event data.</param>
        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            _consoleChannel.Writer.TryWrite(e.Data);
        }


        /// <summary>
        /// Processes the console output.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task ProcessConsoleOutput(CancellationToken cancellationToken)
        {
            await foreach (var consoleLine in _consoleChannel.Reader.ReadAllAsync(cancellationToken))
            {
                LogConsoleOutput(consoleLine);
                if (TryParseStep(consoleLine, out int step, out int steps, out float elapsed))
                {
                    _progressCallback?.Report(new PipelineProgress
                    {
                        Value = step,
                        Maximum = steps,
                        Key = "Generate",
                        Subkey = "Step",
                        Elapsed = elapsed
                    });
                }
            }
        }


        /// <summary>
        /// Loga the console output
        /// </summary>
        /// <param name="consoleLine">The console line.</param>
        private void LogConsoleOutput(string consoleLine)
        {
            if (string.IsNullOrEmpty(consoleLine))
                return;

            const string logFormat = "[StableDiffusion.Cpp] {LogLine}";
            if (consoleLine.Length >= 7)
            {
                if (consoleLine.StartsWith("[INFO ]"))
                {
                    _logger.LogInformation(logFormat, consoleLine[7..].TrimStart());
                    return;
                }
                if (consoleLine.StartsWith("[DEBUG]"))
                {
                    _logger.LogDebug(logFormat, consoleLine[7..].TrimStart());
                    return;
                }
                if (consoleLine.StartsWith("[ERROR]"))
                {
                    _logger.LogError(logFormat, consoleLine[7..].TrimStart());
                    return;
                }
            }
            _logger.LogInformation(logFormat, consoleLine);
        }


        /// <summary>
        /// Parse step progress from output
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="step">The step.</param>
        /// <param name="steps">The steps.</param>
        /// <returns><c>true</c> if progress was parsed, <c>false</c> otherwise.</returns>
        private static bool TryParseStep(ReadOnlySpan<char> line, out int step, out int steps, out float elapsed)
        {
            elapsed = 0;
            step = steps = 0;
            line = line.TrimStart();
            if (!line.StartsWith("|="))
                return false;

            var end = line.IndexOf('-');
            var begin = line.LastIndexOf('|') + 1;
            var substring = line[begin..end].Trim();
            var slashIndex = substring.IndexOf('/');
            if (slashIndex < 1)
                return false;

            var itStart = end + 1;
            var itsEnd = line.IndexOf("it/s");
            var sitEnd = line.IndexOf("s/it");
            if (sitEnd > 0)
            {
                if (float.TryParse(line[itStart..sitEnd].Trim(), out var interations))
                    elapsed = interations * 1000;
            }
            else if (itsEnd > 0)
            {
                if (float.TryParse(line[itStart..itsEnd].Trim(), out var interations))
                    elapsed = (1.0f / interations) * 1000;
            }

            var stepValue = substring[0..slashIndex];
            var stepsValue = substring[(slashIndex + 1)..];
            return int.TryParse(stepValue, out step) && int.TryParse(stepsValue, out steps);
        }


        /// <summary>
        /// Adds the server variables.
        /// </summary>
        /// <param name="processInfo">The process information.</param>
        /// <param name="serverConfig">The server configuration.</param>
        private static void AddServerVariables(ProcessStartInfo processInfo, ServerConfig serverConfig)
        {
            if (serverConfig.ServerVariables?.Count > 0)
            {
                foreach (var variable in serverConfig.ServerVariables)
                {
                    processInfo.Environment[variable.Key] = variable.Value;
                }
            }
        }


        /// <summary>
        /// Gets the server arguments.
        /// </summary>
        /// <param name="serverConfig">The server configuration.</param>
        private static string GetServerArguments(ServerConfig serverConfig)
        {
            var argumentBuilder = new StringBuilder(GetModelArguments(serverConfig.ModelConfig));
            argumentBuilder.Append($"--listen-ip {serverConfig.Address} ");
            argumentBuilder.Append($"--listen-port {serverConfig.Port} ");
            argumentBuilder.Append($"--backend {serverConfig.Backend.GetShortName()}{serverConfig.DeviceId} ");
            if (serverConfig.MemoryMode == MemoryModeType.Balanced)
            {
                argumentBuilder.Append("--auto-fit ");
            }
            else if (serverConfig.MemoryMode == MemoryModeType.OffloadCPU)
            {
                argumentBuilder.Append("--offload-to-cpu ");
                argumentBuilder.Append("--params-backend te=disk,vae=disk,clip_vision=disk ");
                argumentBuilder.Append("--stream-layers ");
            }
            else if (serverConfig.MemoryMode == MemoryModeType.OffloadModel)
            {
                argumentBuilder.Append("--offload-to-cpu ");
                argumentBuilder.Append("--params-backend te=disk,vae=disk,clip_vision=disk ");
            }

            if (serverConfig.MemoryMode == MemoryModeType.Device)
            {
                var QuantizationType = GetQuantizationType(serverConfig.QuantizationType);
                argumentBuilder.Append("--eager-load ");
                argumentBuilder.Append($"--type {QuantizationType} ");
            }
            else
            {
                if (serverConfig.MemoryReserve > 0)
                    argumentBuilder.Append($"--max-vram -{serverConfig.MemoryReserve} ");
            }

            if (serverConfig.Backend == BackendType.Vulkan)
            {
                argumentBuilder.Append("--vae-conv-direct ");
                argumentBuilder.Append("--diffusion-conv-direct ");
            }

            if (serverConfig.IsFlashAttentionEnabled)
                argumentBuilder.Append("--fa ");

            argumentBuilder.Append("--rng cpu ");
            argumentBuilder.Append("--lora-apply-mode at_runtime ");
            if (serverConfig.IsDebug)
                argumentBuilder.Append("-v ");
            return argumentBuilder.ToString();
        }


        /// <summary>
        /// Gets the model arguments.
        /// </summary>
        /// <param name="modelConfig">The model configuration.</param>
        private static string GetModelArguments(ModelConfig modelConfig)
        {
            var argumentBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(modelConfig.Full))
            {
                argumentBuilder.Append($"--model {modelConfig.Full} ");
            }
            else
            {
                if (!string.IsNullOrEmpty(modelConfig.ClipL))
                    argumentBuilder.Append($"--clip_l {modelConfig.ClipL} ");
                if (!string.IsNullOrEmpty(modelConfig.ClipG))
                    argumentBuilder.Append($"--clip_g {modelConfig.ClipG} ");
                if (!string.IsNullOrEmpty(modelConfig.ClipVison))
                    argumentBuilder.Append($"--clip_vision {modelConfig.ClipVison} ");
                if (!string.IsNullOrEmpty(modelConfig.T5XXL))
                    argumentBuilder.Append($"--t5xxl {modelConfig.T5XXL} ");
                if (!string.IsNullOrEmpty(modelConfig.LLM))
                    argumentBuilder.Append($"--llm {modelConfig.LLM} ");
                if (!string.IsNullOrEmpty(modelConfig.VisionLLM))
                    argumentBuilder.Append($"--llm_vision {modelConfig.VisionLLM} ");
                if (!string.IsNullOrEmpty(modelConfig.Diffusion))
                    argumentBuilder.Append($"--diffusion-model {modelConfig.Diffusion} ");
                if (!string.IsNullOrEmpty(modelConfig.DiffusionHighNoise))
                    argumentBuilder.Append($"--high-noise-diffusion-model {modelConfig.DiffusionHighNoise} ");
                if (!string.IsNullOrEmpty(modelConfig.DiffusionUncond))
                    argumentBuilder.Append($"--uncond-diffusion-model {modelConfig.DiffusionUncond} ");
                if (!string.IsNullOrEmpty(modelConfig.Connectors))
                    argumentBuilder.Append($"--embeddings-connectors {modelConfig.Connectors} ");
                if (!string.IsNullOrEmpty(modelConfig.Vae))
                    argumentBuilder.Append($"--vae {modelConfig.Vae} ");
                if (!string.IsNullOrEmpty(modelConfig.VaeAudio))
                    argumentBuilder.Append($"--audio-vae {modelConfig.VaeAudio} ");
            }
            if (!string.IsNullOrEmpty(modelConfig.Tased))
                argumentBuilder.Append($"--taesd {modelConfig.Tased} ");
            if (!string.IsNullOrEmpty(modelConfig.ControlNet))
                argumentBuilder.Append($"--control-net {modelConfig.ControlNet} ");
            if (!string.IsNullOrEmpty(modelConfig.LoraModelDirectory))
                argumentBuilder.Append($"--lora-model-dir {modelConfig.LoraModelDirectory} ");
            if (!string.IsNullOrEmpty(modelConfig.EmbeddingsDirectory))
                argumentBuilder.Append($"--embd-dir {modelConfig.EmbeddingsDirectory} ");
            if (!string.IsNullOrEmpty(modelConfig.ExtraModelArgs))
                argumentBuilder.Append($"--model-args {modelConfig.ExtraModelArgs} ");

            return argumentBuilder.ToString();
        }


        /// <summary>
        /// Gets the type of the quantization.
        /// </summary>
        /// <param name="quantizationType">Type of the quantization.</param>
        private static string GetQuantizationType(QuantizationType quantizationType)
        {
            return quantizationType switch
            {
                QuantizationType.Q4Bit => "q4_0",
                QuantizationType.Q8Bit => "q8_0",
                _ => "bf16"
            };
        }
    }
}
