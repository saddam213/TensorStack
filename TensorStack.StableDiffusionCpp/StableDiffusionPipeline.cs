using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;
using TensorStack.StableDiffusionCpp.Native;

namespace TensorStack.StableDiffusionCpp
{
    public sealed class StableDiffusionPipeline : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IProgress<PipelineProgress> _progresssCallback;
        private StableDiffusionContext _stableDiffusionContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionPipeline"/> class.
        /// </summary>
        /// <param name="backendPath">The backend path.</param>
        /// <param name="progresssCallback">The progresss callback.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.Exception">Failed to load 'StableDiffusion.cpp' native library: {backendPath}</exception>
        public StableDiffusionPipeline(string backendPath, IProgress<PipelineProgress> progresssCallback = null, ILogger logger = null)
        {
            _logger = logger;
            _progresssCallback = progresssCallback;
            if (!NativeApi.LoadNativeLibrary(out var backendInfo, backendPath))
                throw new Exception($"Failed to load 'StableDiffusion.cpp' native library: {backendPath}");
            if (!NativeApi.LibraryVersion.Equals(backendInfo.Commit, StringComparison.OrdinalIgnoreCase))
                logger?.LogWarning($"[StableDiffusion.cpp] Native library version mismatch: loaded '{backendInfo.Commit}', expected '{NativeApi.LibraryVersion}'.");

            BackendInfo = backendInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionPipeline"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="progresssCallback">The progresss callback.</param>
        /// <param name="logCallback">The log callback.</param>
        public StableDiffusionPipeline(BackendConfig configuration, IProgress<PipelineProgress> progresssCallback = null, ILogger logger = null)
            : this(configuration.Directory, progresssCallback, logger) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionPipeline"/> class.
        /// </summary>
        /// <param name="backend">The backend environment.</param>
        /// <param name="progresssCallback">The progresss callback.</param>
        /// <param name="logger">The logger.</param>
        public StableDiffusionPipeline(StableDiffusionBackend backend, IProgress<PipelineProgress> progresssCallback = null, ILogger logger = null)
            : this(backend.Config, progresssCallback, logger) { }

        /// <summary>
        /// Gets the backend information.
        /// </summary>
        public BackendInfo BackendInfo { get; }

        /// <summary>
        /// Gets the devices.
        /// </summary>
        public IReadOnlyList<BackendDevice> Devices => BackendInfo?.Devices;

        /// <summary>
        /// Gets the default image options for the current context.
        /// </summary>
        public GenerateImageOptions DefaultImageOptions => _stableDiffusionContext?.DefaultImageOptions;

        /// <summary>
        /// Gets the default video options for the current context.
        /// </summary>
        public GenerateVideoOptions DefaultVideoOptions => _stableDiffusionContext?.DefaultVideoOptions;


        /// <summary>
        /// Loads the StableDiffusion pipeline with the specified options.
        /// </summary>
        /// <param name="options">The StableDiffusion options.</param>
        public void Load(StableDiffusionOptions options)
        {
            Unload();
            _stableDiffusionContext = new StableDiffusionContext(options, OnProgressCallback, OnPreviewCallback, OnNativeLogCallback);
        }


        /// <summary>
        /// Loads the Upscale pipeline with the specified options.
        /// </summary>
        /// <param name="options">The Upscale options.</param>
        public void Load(UpscaleOptions options)
        {
            Unload();
            _stableDiffusionContext = new StableDiffusionContext(options, OnProgressCallback, OnNativeLogCallback);
        }


        /// <summary>
        /// Loads the StableDiffusion + Upscale pipeline with the specified options.
        /// </summary>
        /// <param name="options">The StableDiffusion options.</param>
        /// <param name="upscaleOptions">The upscale options.</param>
        public void Load(StableDiffusionOptions options, UpscaleOptions upscaleOptions)
        {
            Unload();
            _stableDiffusionContext = new StableDiffusionContext(options, upscaleOptions, OnProgressCallback, OnPreviewCallback, OnNativeLogCallback);
        }


        /// <summary>
        /// Loads the StableDiffusion pipeline with the specified options.
        /// </summary>
        /// <param name="options">The StableDiffusion options.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public Task LoadAsync(StableDiffusionOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                Load(options);
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken);
        }


        /// <summary>
        /// Loads the Upscale pipeline with the specified options.
        /// </summary>
        /// <param name="options">The Upscale options.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public Task LoadAsync(UpscaleOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                Load(options);
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken);
        }


        /// <summary>
        /// Loads the StableDiffusion + Upscale pipeline with the specified options.
        /// </summary>
        /// <param name="stableDiffusionOptions">The StableDiffusion options.</param>
        /// <param name="upscaleOptions">The Upscale options.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public Task LoadAsync(StableDiffusionOptions stableDiffusionOptions, UpscaleOptions upscaleOptions, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                Load(stableDiffusionOptions, upscaleOptions);
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken);
        }


        /// <summary>
        /// Generates an image with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public ImageTensor GenerateImage(GenerateImageOptions options, CancellationToken cancellationToken = default)
        {
            return _stableDiffusionContext.Generate(options, cancellationToken);
        }


        /// <summary>
        /// Generates an image with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<ImageTensor> GenerateImageAsync(GenerateImageOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var result = GenerateImage(options, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }


        /// <summary>
        /// Generates a video with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public VideoSequence GenerateVideo(GenerateVideoOptions options, CancellationToken cancellationToken = default)
        {
            return _stableDiffusionContext.Generate(options, cancellationToken);
        }


        /// <summary>
        /// Generates a video with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<VideoSequence> GenerateVideoAsync(GenerateVideoOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var result = GenerateVideo(options, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }


        /// <summary>
        /// Upscales the image
        /// </summary>
        /// <param name="inputImage">The input image.</param>
        /// <param name="options">The upscale options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ImageTensor UpscaleImage(ImageTensor inputImage, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            return _stableDiffusionContext.Upscale(inputImage, options, cancellationToken);
        }


        /// <summary>
        /// Upscales the image
        /// </summary>
        /// <param name="inputImage">The input image.</param>
        /// <param name="options">The upscale options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<ImageTensor> UpscaleImageAsync(ImageTensor inputImage, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var result = UpscaleImage(inputImage, options, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }


        /// <summary>
        /// Upscales the video frames
        /// </summary>
        /// <param name="videoFrames">The video frames.</param>
        /// <param name="options">The upscale options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ImageTensor[] UpscaleFrames(ImageTensor[] videoFrames, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            return _stableDiffusionContext.Upscale(videoFrames, options, cancellationToken);
        }


        /// <summary>
        /// Upscales the video frames
        /// </summary>
        /// <param name="videoFrames">The video frames.</param>
        /// <param name="options">The upscale options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<ImageTensor[]> UpscaleFramesAsync(ImageTensor[] videoFrames, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var result = UpscaleFrames(videoFrames, options, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }


        /// <summary>
        /// Upscales the video
        /// </summary>
        /// <param name="inputVideo">The input video.</param>
        /// <param name="options">The upscale options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public VideoSequence UpscaleVideo(VideoSequence inputVideo, UpscaleVideoOptions options = default, CancellationToken cancellationToken = default)
        {
            return _stableDiffusionContext.Upscale(inputVideo, options, cancellationToken);
        }


        /// <summary>
        /// Upscales the video
        /// </summary>
        /// <param name="inputVideo">The input video.</param>
        /// <param name="options">The upscale options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<VideoSequence> UpscaleVideoAsync(VideoSequence inputImage, UpscaleVideoOptions options = default, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var result = UpscaleVideo(inputImage, options, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken);
        }


        /// <summary>
        /// Unloads the context.
        /// </summary>
        public void Unload()
        {
            _stableDiffusionContext?.Dispose();
            _stableDiffusionContext = null;
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Unload();
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Called when progress is emitted.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="timeSeconds">The time seconds.</param>
        private void OnProgressCallback(int value, int maximum, float timeSeconds)
        {
            var elapsedTime = GetProgresTime(timeSeconds);
            _progresssCallback?.Report(new PipelineProgress
            {
                Key = "Component",
                Value = value,
                Maximum = maximum,
                Elapsed = (float)elapsedTime.TotalMilliseconds,
            });
            _logger?.LogDebug($"[StableDiffusion.cpp] Component: {value}/{maximum}, {elapsedTime}");
        }


        /// <summary>
        /// Called when preview is emitted
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="steps">The steps.</param>
        /// <param name="timeSeconds">The time seconds.</param>
        /// <param name="frames">The frames.</param>
        private void OnPreviewCallback(int step, int steps, float timeSeconds, ImageTensor[] frames)
        {
            var elapsedTime = GetProgresTime(timeSeconds);
            _progresssCallback?.Report(new PipelineProgress
            {
                Key = "Generate",
                Subkey = "Step",
                Value = step,
                Maximum = steps,
                Tensors = frames,
                Elapsed = (float)elapsedTime.TotalMilliseconds,
            });
            _logger?.LogDebug($"[StableDiffusion.cpp] Step: {step}/{steps}, {elapsedTime}, PreviewFrames: {frames?.Length ?? 0}");
        }


        /// <summary>
        /// Called when logs are emitted
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        private void OnNativeLogCallback(LogLevelType level, string message)
        {
            var logLevel = level switch
            {
                LogLevelType.Info => LogLevel.Information,
                LogLevelType.Debug => LogLevel.Debug,
                LogLevelType.Warn => LogLevel.Warning,
                LogLevelType.Error => LogLevel.Error,
                LogLevelType.Verbose => LogLevel.Trace,
                _ => LogLevel.Trace
            };
            _logger?.Log(logLevel, "[StableDiffusion.cpp] {message}", message);
        }


        /// <summary>
        /// Gets the progres time.
        /// </summary>
        /// <param name="time">The time.</param>
        private static TimeSpan GetProgresTime(float time)
        {
            if ((int)time == 2000) // No idea?
                return TimeSpan.Zero;

            return TimeSpan.FromSeconds(time);
        }
    }
}
