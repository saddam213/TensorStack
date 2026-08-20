using Amuse.Common;
using Amuse.Common.Config;
using Amuse.Common.Message;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;
using TensorStack.Media.Image;

namespace Amuse.Host.StableDiffusionCpp
{
    public sealed class HostServer : PipelineServer
    {
        private readonly IProgress<PipelineProgress> _progressRelayCallback;
        private StableDiffusionServer _pipeline;
        private PipelineCreateOptions _pipelineCreateOptions;
        private PipelineLoadOptions _pipelineLoadOptions;
        private Config.ServerConfig _serverConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostServer"/> class.
        /// </summary>
        /// <param name="channelConfig">The channel configuration.</param>
        /// <param name="logger">The logger.</param>
        public HostServer(ServerConfig channelConfig, ILogger logger)
            : base(channelConfig, logger)
        {
            _progressRelayCallback = new Progress<PipelineProgress>(async (p) => await UpdateProgress(p));
        }


        /// <summary>
        /// Called when the Channel is opened.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task ChannelOpenedAsync()
        {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Called when the Channel is closed.
        /// </summary>
        protected override async Task ChannelClosedAsync()
        {
            if (_pipeline != null)
                await _pipeline.DisposeAsync();
        }


        /// <summary>
        /// Create Pipeline
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected override async Task CreatePipelineAsync(PipelineRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var timestamp = Stopwatch.GetTimestamp();
                if (!await EnvironmentManager.InitializeAsync(request.CreateOptions, _progressRelayCallback))
                    throw new Exception($"Failed to Install StableDiffusion.Cpp {request.CreateOptions.HostVersion}");

                _pipelineCreateOptions = request.CreateOptions;
                Logger.LogInformation($"[AmuseHost] [PipelineServer] [CreatePipeline] Environment created, Elapsed: {Stopwatch.GetElapsedTime(timestamp)}");
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[AmuseHost] [PipelineServer] [CreatePipeline] An exception occurred creating environment.");
                await SendException(ex, cancellationToken);
            }
        }


        /// <summary>
        /// Loads the pipeline
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected override async Task LoadPipelineAsync(PipelineRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await UpdateProgress(new PipelineProgress { Key = "Load", Subkey = "Pipeline", Message = "Loading Pipeline Components..." });

                _pipelineLoadOptions = request.LoadOptions;
                await StartStableDiffusionServerAsync(cancellationToken);
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[AmuseHost] [PipelineServer] [LoadPipeline] An exception occurred loading pipeline.");
                await SendException(ex, cancellationToken);
            }
        }


        /// <summary>
        /// Reload the pipeline
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected override async Task ReloadPipelineAsync(PipelineRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var reloadOptions = request.ReloadOptions;
                _pipelineLoadOptions.LoraAdapters = reloadOptions.LoraAdapters;
                _pipelineLoadOptions.ProcessType = reloadOptions.ProcessType;
                _pipelineLoadOptions.ControlNet = reloadOptions.ControlNet;
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[AmuseHost] [PipelineServer] [ReloadPipeline] An exception occurred reloading pipeline.");
                await SendException(ex, cancellationToken);
            }
        }


        /// <summary>
        /// Unloads the pipeline
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected override async Task UnloadPipelineAsync(PipelineRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _pipeline.StopAsync();
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[AmuseHost] [PipelineServer] [UnloadPipeline] An exception occurred unloading pipeline.");
                await SendException(ex, cancellationToken);
            }
        }


        /// <summary>
        /// Runs the pipeline
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected override async Task RunPipelineAsync(PipelineRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await UpdateProgress(new PipelineProgress { Key = "Load", Subkey = "Pipeline", Message = "Loading Pipeline Components..." });
                using (PipelineCancellation = new CancellationTokenSource())
                {
                    request.RunOptions.UnpackTensors(request);
                    var modelConfig = _serverConfig.ModelConfig;
                    if (request.RunOptions.ImageOptions != null)
                    {
                        var options = request.RunOptions.ImageOptions;
                        var defaultsParams = _pipeline.ModelCapabilities.DefaultParams.ImageParams;
                        var generateParams = options.ToServerParams(modelConfig, _pipelineLoadOptions, defaultsParams);
                        var result = await _pipeline.GenerateImageAsync(generateParams, PipelineCancellation.Token);
                        await SendMessage(new PipelineResponse(GetImageTensor(result)), cancellationToken);
                    }
                    else if (request.RunOptions.VideoOptions != null)
                    {
                        var options = request.RunOptions.VideoOptions;
                        var defaultsParams = _pipeline.ModelCapabilities.DefaultParams.VideoParams;
                        var generateParams = options.ToServerParams(modelConfig, _pipelineLoadOptions, defaultsParams);
                        var result = await _pipeline.GenerateVideoAsync(generateParams, PipelineCancellation.Token);
                        await SaveVideoAsync(result, options.TempFileName, PipelineCancellation.Token);
                        await SendMessage(new PipelineResponse(default(Tensor<float>[])), cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                await RestartStableDiffusionServerAsync();
                Logger.LogError("[AmuseHost] [PipelineServer] [RunPipeline] {Message}", ex.Message);
                await SendException(ex, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[AmuseHost] [PipelineServer] [RunPipeline] An exception occurred running pipeline.");
                await SendException(ex, cancellationToken);
            }
        }


        /// <summary>
        /// Start StableDiffusion.cpp server 
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task StartStableDiffusionServerAsync(CancellationToken cancellationToken = default)
        {
            _serverConfig = _pipelineLoadOptions.ToServerConfig(_pipelineCreateOptions);
            _pipeline = new StableDiffusionServer(_serverConfig, _progressRelayCallback, Logger);
            await _pipeline.StartAsync(cancellationToken);
        }


        /// <summary>
        /// Restart StableDiffusion.cpp server 
        /// </summary>
        private async Task RestartStableDiffusionServerAsync()
        {
            await _pipeline.StopAsync();
            await StartStableDiffusionServerAsync();
        }


        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        private async Task UpdateProgress(PipelineProgress progress)
        {
            await QueueProgress(progress);
        }


        /// <summary>
        /// Save video converted from WEBM to MP4
        /// </summary>
        /// <param name="webmBytes">The webm bytes.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task SaveVideoAsync(byte[] webmBytes, string outputFile, CancellationToken cancellationToken = default)
        {
            var encoders = new[]
            {
                "h264_nvenc",      // NVIDIA
                "h264_amf",        // AMD
                "h264_qsv",        // Intel
                "h264_d3d12va",    // Windows D3D12
                "libopenh264",     // CPU fallback
            };
            Logger.LogInformation("[AmuseHost] [PipelineServer] [SaveVideoAsync] Saving output video...");
            foreach (var encoder in encoders)
            {
                try
                {
                    if (await EncodeVideoAsync(webmBytes, outputFile, encoder, cancellationToken))
                    {
                        Logger.LogInformation($"[AmuseHost] [PipelineServer] [SaveVideoAsync] Successfully saved output video, Codec: {encoder}...", encoder);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception) { /* Codec not avaliable */ }
            }

            Logger.LogInformation($"[AmuseHost] [PipelineServer] [SaveVideoAsync] Successfully saved output video using fallback");
            await File.WriteAllBytesAsync(outputFile, webmBytes, cancellationToken);
        }


        /// <summary>
        /// Encode video
        /// </summary>
        /// <param name="webmBytes">The webm bytes.</param>
        /// <param name="outputFile">The output file.</param>
        /// <param name="encoder">The encoder.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A Task&lt;System.Boolean&gt; representing the asynchronous operation.</returns>
        private static async Task<bool> EncodeVideoAsync(byte[] webmBytes, string outputFile, string encoder, CancellationToken cancellationToken = default)
        {
            var arguments = $"-hide_banner -f webm -i pipe:0 -c:v {encoder} -pix_fmt yuv420p -c:a aac -b:a 192k -movflags +faststart -y \"{outputFile}\"";
            using (var process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg.exe";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                var errorTask = process.StandardError.ReadToEndAsync();
                await process.StandardInput.BaseStream.WriteAsync(webmBytes, cancellationToken);
                process.StandardInput.Close();
                await process.WaitForExitAsync(cancellationToken);
                var error = await errorTask;
                return process.ExitCode == 0;
            }
        }


        /// <summary>
        /// Gets the image tensor.
        /// </summary>
        /// <param name="imageBytes">The image bytes.</param>
        private static ImageTensor GetImageTensor(byte[] imageBytes)
        {
            using (var bitmap = SKBitmap.Decode(imageBytes))
            {
                return bitmap.ToTensor();
            }
        }
    }
}