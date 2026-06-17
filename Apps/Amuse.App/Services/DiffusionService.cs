using Amuse.App.Common;
using Amuse.App.Runtime;
using Amuse.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TensorStack.Audio;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Image;
using TensorStack.Video;

namespace Amuse.App.Services
{

    public sealed class DiffusionService : ServiceBase, IDiffusionService
    {
        private readonly ILogger _logger;
        private readonly Settings _settings;
        private readonly IMediaService _mediaService;
        private readonly IEnvironmentService _environmentService;
        private readonly IPreviewService _previewService;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _isExecuting;
        private bool _isCanceling;
        private IDiffusionRuntime _diffusionRuntime;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiffusionService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public DiffusionService(Settings settings, IEnvironmentService environmentService, IMediaService mediaService, IPreviewService previewService, ILogger<DiffusionService> logger)
        {
            _logger = logger;
            _settings = settings;
            _mediaService = mediaService;
            _environmentService = environmentService;
            _previewService = previewService;
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        public PipelineModel Pipeline => _diffusionRuntime?.Pipeline;

        /// <summary>
        /// Gets the default options.
        /// </summary>
        public DiffusionDefaultOptions DefaultOptions => _diffusionRuntime?.DefaultOptions;

        /// <summary>
        /// Gets a value indicating whether this instance is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { SetProperty(ref _isLoaded, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is loading.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is executing.
        /// </summary>
        public bool IsExecuting
        {
            get { return _isExecuting; }
            private set { SetProperty(ref _isExecuting, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is canceling.
        /// </summary>
        public bool IsCanceling
        {
            get { return _isCanceling; }
            private set { SetProperty(ref _isCanceling, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can cancel.
        /// </summary>
        public bool CanCancel => _isLoading || _isExecuting;


        /// <summary>
        /// Load the pipeline
        /// </summary>
        /// <param name="config">The configuration.</param>
        public async Task LoadAsync(PipelineModel pipeline, IProgress<PipelineProgress> progressCallback)
        {
            IsLoaded = false;
            IsLoading = true;
            IsCanceling = false;
            try
            {
                if (_diffusionRuntime != null)
                {
                    await _previewService.UnloadAsync();
                    await _diffusionRuntime.UnloadAsync();
                    DisposeRuntime();
                }

                _diffusionRuntime = pipeline.DiffusionModel.Backend switch
                {
                    BackendType.PyTorch => new PyTorchDiffusion(_settings, _environmentService, _mediaService, _logger),
                    BackendType.OnnxRuntime => new OnnxDiffusion(_settings, _mediaService, _logger),
                    _ => throw new NotImplementedException()
                };

                await _diffusionRuntime.LoadAsync(pipeline, progressCallback);
                await _previewService.LoadAsync(pipeline);
                IsLoaded = true;
            }
            catch (OperationCanceledException)
            {
                DisposeRuntime();
                throw;
            }
            finally
            {
                IsLoading = false;
                IsCanceling = false;
            }
        }


        /// <summary>
        /// Reload the pipeline
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public async Task ReloadAsync(PipelineModel pipeline, IProgress<PipelineProgress> progressCallback)
        {
            IsLoaded = false;
            IsLoading = true;
            IsCanceling = false;
            try
            {
                await _diffusionRuntime.ReloadAsync(pipeline, progressCallback);
                IsLoaded = true;
            }
            catch (OperationCanceledException)
            {
                await _previewService.UnloadAsync();
                DisposeRuntime();
                throw;
            }
            finally
            {
                IsLoading = false;
                IsCanceling = false;
            }
        }


        public async Task UpdateAsync(PipelineModel pipeline)
        {
            await _diffusionRuntime.UpdateAsync(pipeline);
        }


        /// <summary>
        /// Execute the upscaler
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<ImageTensor> GenerateImageAsync(DiffusionInputOptions options)
        {
            IsExecuting = true;
            IsCanceling = false;
            try
            {
                return await _diffusionRuntime.GenerateImageAsync(options);
            }
            finally
            {
                IsExecuting = false;
                IsCanceling = false;
            }
        }


        public async Task<VideoInputStream> GenerateVideoAsync(DiffusionInputOptions options)
        {
            IsExecuting = true;
            IsCanceling = false;
            try
            {
                return await _diffusionRuntime.GenerateVideoAsync(options);
            }
            finally
            {
                IsExecuting = false;
                IsCanceling = false;
            }
        }


        public async Task<AudioInputStream> GenerateAudioAsync(DiffusionInputOptions options)
        {
            IsExecuting = true;
            IsCanceling = false;
            try
            {
                return await _diffusionRuntime.GenerateAudioAsync(options);
            }

            finally
            {
                IsExecuting = false;
                IsCanceling = false;
            }
        }


        public async Task<TextResult> GenerateTextAsync(DiffusionInputOptions options)
        {
            IsExecuting = true;
            IsCanceling = false;
            try
            {
                return await _diffusionRuntime.GenerateTextAsync(options);
            }

            finally
            {
                IsExecuting = false;
                IsCanceling = false;
            }
        }


        public async Task<ImageInput> GeneratePreviewAsync(PipelineProgress progress)
        {
            if (progress.Tensors.IsNullOrEmpty())
                return default;

            return await _previewService.GenerateAsync(progress.Tensors[0]);
        }


        /// <summary>
        /// Cancel the running task (Load or Execute)
        /// </summary>
        public async Task CancelAsync()
        {
            try
            {
                IsCanceling = true;
                await _diffusionRuntime.CancelAsync();
            }
            catch (Exception) { }
        }


        /// <summary>
        /// Stop/Kill server
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                await _previewService.UnloadAsync();
                await _diffusionRuntime.StopAsync();
            }
            catch (Exception) { }
            finally
            {
                IsLoaded = false;
                IsLoading = false;
                IsExecuting = false;
                IsCanceling = false;
                DisposeRuntime();
            }
        }


        /// <summary>
        /// Unload the pipeline
        /// </summary>
        public async Task UnloadAsync()
        {
            await _previewService.UnloadAsync();
            await _diffusionRuntime.UnloadAsync();
            IsLoaded = false;
            IsLoading = false;
            IsExecuting = false;
            IsCanceling = false;
        }


        private void DisposeRuntime()
        {
            _diffusionRuntime?.Dispose();
            _diffusionRuntime = null;
        }


        public void Dispose()
        {
            _previewService?.Dispose();
            DisposeRuntime();
        }
    }


    public interface IDiffusionService : IDiffusionRuntime
    {
        bool IsLoaded { get; }
        bool IsLoading { get; }
        bool IsExecuting { get; }
        bool IsCanceling { get; }
        bool CanCancel { get; }

        Task<ImageInput> GeneratePreviewAsync(PipelineProgress progress);
    }
}
