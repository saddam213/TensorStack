using Amuse.Common;
using Amuse.Common.Config;
using Amuse.Common.Message;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;

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
                _serverConfig = _pipelineLoadOptions.ToServerConfig(_pipelineCreateOptions);
                _pipeline = new StableDiffusionServer(_serverConfig, _progressRelayCallback, Logger);
                await _pipeline.StartAsync(cancellationToken);
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
                    RegisterCancellation(PipelineCancellation.Token);

                    request.RunOptions.UnpackTensors(request);
                    var modelConfig = _serverConfig.ModelConfig;
                    if (request.RunOptions.ImageOptions != null)
                    {
                        var options = request.RunOptions.ImageOptions;
                        var defaultsParams = _pipeline.ModelCapabilities.DefaultParams.ImageParams;
                        var generateParams = options.ToServerParams(modelConfig, _pipelineLoadOptions, defaultsParams);
                        var result = await _pipeline.GenerateImageAsync(generateParams, cancellationToken);
                        await File.WriteAllBytesAsync(options.TempFileName, result, cancellationToken);
                    }
                    else if (request.RunOptions.VideoOptions != null)
                    {
                        var options = request.RunOptions.VideoOptions;
                        var defaultsParams = _pipeline.ModelCapabilities.DefaultParams.VideoParams;
                        var generateParams = options.ToServerParams(modelConfig, _pipelineLoadOptions, defaultsParams);
                        var result = await _pipeline.GenerateVideoAsync(generateParams, cancellationToken);
                        await File.WriteAllBytesAsync(options.TempFileName, result, cancellationToken);
                    }
                    await SendMessage(new PipelineResponse(default(Tensor<float>[])), cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
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
        /// Registers the pipeline cancellation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to register callback</param>
        private void RegisterCancellation(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var cancelSuccess = await _pipeline.CancelGenerateAsync();
                        if (!cancelSuccess)
                        {
                            // SD-CPP Server cannot cancel running jobs, so we have to close the server
                            // Throw a normal exception so the frontend will close the server gracefully
                            var ex = new Exception("Unable to cancel running task, Closing server");
                            Logger.LogError(ex, "[AmuseHost] [PipelineServer] [RunPipeline] Unable to cancel running task");
                            await SendException(ex, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[AmuseHost] [PipelineServer] [RunPipeline] Exception occurred while executing CancelGenerateAsync.");
                        await SendException(ex, CancellationToken.None);
                    }
                });
            });
        }


        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        private async Task UpdateProgress(PipelineProgress progress)
        {
            await QueueProgress(progress);
        }

    }
}