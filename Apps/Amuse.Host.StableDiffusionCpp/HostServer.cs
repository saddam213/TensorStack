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
                Logger.LogInformation($"[PipelineServer] [CreatePipeline] Environment created, Elapsed: {Stopwatch.GetElapsedTime(timestamp)}");
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PipelineServer] [CreatePipeline] An exception occurred creating environment.");
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
                _pipelineLoadOptions = request.LoadOptions;
                var serverConfig = _pipelineLoadOptions.ToServerConfig(_pipelineCreateOptions);
                _pipeline = new StableDiffusionServer(serverConfig, _progressRelayCallback, Logger);
                await _pipeline.StartAsync(cancellationToken);
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PipelineServer] [LoadPipeline] An exception occurred loading pipeline.");
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
                // TODO: Reload Pipeline
                await SendResponse(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PipelineServer] [ReloadPipeline] An exception occurred reloading pipeline.");
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
                Logger.LogError(ex, "[PipelineServer] [UnloadPipeline] An exception occurred unloading pipeline.");
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
                request.RunOptions.UnpackTensors(request);
                if (request.RunOptions.ImageOptions != null)
                {
                    var defaults = _pipeline.ModelCapabilities.DefaultParams.ImageParams;
                    var options = request.RunOptions.ImageOptions.ToServerParams(_pipelineLoadOptions, defaults);
                    var imageResult = await _pipeline.GenerateImageAsync(options, cancellationToken);
                    var tempFilename = request.RunOptions.ImageOptions.TempFileName;
                    await File.WriteAllBytesAsync(tempFilename, imageResult, cancellationToken);
                    await SendMessage(new PipelineResponse(default(Tensor<float>[])), cancellationToken);
                }
                else if (request.RunOptions.VideoOptions != null)
                {
                    var defaults = _pipeline.ModelCapabilities.DefaultParams.VideoParams;
                    var options = request.RunOptions.VideoOptions.ToServerParams(_pipelineLoadOptions, defaults);
                    var videoResult = await _pipeline.GenerateVideoAsync(options, cancellationToken);
                    var tempFilename = request.RunOptions.VideoOptions.TempFileName;
                    await File.WriteAllBytesAsync(tempFilename, videoResult, cancellationToken);
                    await SendMessage(new PipelineResponse(default(Tensor<float>[])), cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogError("[PipelineServer] [RunPipeline] {Message}", ex.Message);
                await SendException(ex, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PipelineServer] [RunPipeline] An exception occurred running pipeline.");
                await SendException(ex, cancellationToken);
            }
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