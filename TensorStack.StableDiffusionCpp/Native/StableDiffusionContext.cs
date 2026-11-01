using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal sealed unsafe class StableDiffusionContext : IDisposable
    {
        private readonly NativeApi.sd_log_cb_t _unmanagedLogCallback;
        private readonly NativeApi.sd_preview_cb_t _unmanagedPreviewCallback;
        private readonly NativeApi.sd_progress_cb_t _unmanagedProgressCallback;
        private readonly Action<LogLevelType, string> _logCallback;
        private readonly Action<int, int, float> _progressCallback;
        private readonly Action<int, int, float, ImageTensor[]> _previewCallback;
        private readonly StableDiffusionSafeHandle _stableDiffusionHandle;
        private readonly UpscaleSafeHandle _upscaleHandle;
        private readonly StableDiffusionOptions _stableDiffusionOptions;
        private readonly GenerateImageOptions _defaultImageOptions;
        private readonly GenerateVideoOptions _defaultVideoOptions;
        private readonly UpscaleOptions _upscaleOptions;
        private readonly uint _upscaleFactor;
        private ImageTensor[] _previewTensors;
        private bool _isGenerating;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionContext"/> class.
        /// </summary>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="previewCallback">The preview callback.</param>
        /// <param name="logCallback">The log callback.</param>
        private StableDiffusionContext(Action<int, int, float> progressCallback = null, Action<int, int, float, ImageTensor[]> previewCallback = null, Action<LogLevelType, string> logCallback = null)
        {
            _logCallback = logCallback;
            _previewCallback = previewCallback;
            _progressCallback = progressCallback;
            _unmanagedLogCallback = OnLogCallback;
            _unmanagedPreviewCallback = OnPreviewCallback;
            _unmanagedProgressCallback = OnProgressCallback;
            InitializeCallbacks();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionContext"/> class.
        /// </summary>
        /// <param name="options">The StableDiffusion options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="previewCallback">The preview callback.</param>
        /// <param name="logCallback">The log callback.</param>
        public StableDiffusionContext(StableDiffusionOptions options, Action<int, int, float> progressCallback = null, Action<int, int, float, ImageTensor[]> previewCallback = null, Action<LogLevelType, string> logCallback = null)
            : this(progressCallback, previewCallback, logCallback)
        {
            _stableDiffusionOptions = options;
            _stableDiffusionHandle = new StableDiffusionSafeHandle(_stableDiffusionOptions);
            _defaultImageOptions = GetDefaultImageOptions();
            _defaultVideoOptions = GetDefaultVideoOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionContext"/> class.
        /// </summary>
        /// <param name="options">The upscale options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="logCallback">The log callback.</param>
        public StableDiffusionContext(UpscaleOptions options, Action<int, int, float> progressCallback = null, Action<LogLevelType, string> logCallback = null)
            : this(progressCallback, null, logCallback)
        {
            _upscaleOptions = options;
            _upscaleHandle = new UpscaleSafeHandle(_upscaleOptions);
            _upscaleFactor = (uint)Math.Max(1, NativeApi.get_upscale_factor(_upscaleHandle.GetContext()));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionContext"/> class.
        /// </summary>
        /// <param name="stableDiffusionOptions">The StableDiffusion options.</param>
        /// <param name="upscaleOptions">The upscale options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="previewCallback">The preview callback.</param>
        /// <param name="logCallback">The log callback.</param>
        public StableDiffusionContext(StableDiffusionOptions stableDiffusionOptions, UpscaleOptions upscaleOptions, Action<int, int, float> progressCallback = null, Action<int, int, float, ImageTensor[]> previewCallback = null, Action<LogLevelType, string> logCallback = null)
            : this(stableDiffusionOptions, progressCallback, previewCallback, logCallback)
        {
            _upscaleOptions = upscaleOptions;
            _upscaleHandle = new UpscaleSafeHandle(_upscaleOptions);
            _upscaleFactor = (uint)Math.Max(1, NativeApi.get_upscale_factor(_upscaleHandle.GetContext()));
        }

        /// <summary>
        /// Gets the default image options.
        /// </summary>
        public GenerateImageOptions DefaultImageOptions => _defaultImageOptions;

        /// <summary>
        /// Gets the default video options.
        /// </summary>
        public GenerateVideoOptions DefaultVideoOptions => _defaultVideoOptions;

        /// <summary>
        /// Determines whether the context supports image generation.
        /// </summary>
        public bool IsImageGenerationSupported()
        {
            if (_stableDiffusionHandle == null)
                return false;
            return NativeApi.sd_ctx_supports_image_generation(_stableDiffusionHandle.GetContext());
        }

        /// <summary>
        /// Determines whether the context supports video generation.
        /// </summary>
        public bool IsVideoGenerationSupported()
        {
            if (_stableDiffusionHandle == null)
                return false;
            return NativeApi.sd_ctx_supports_video_generation(_stableDiffusionHandle.GetContext());
        }

        /// <summary>
        /// Determines whether image upscale is supported.
        /// </summary>
        public bool IsImageUpscaleSupported()
        {
            return _upscaleHandle != null;
        }

        /// <summary>
        /// Determines whether video upscale is supported.
        /// </summary>
        public bool IsVideoUpscaleSupported()
        {
            return _upscaleHandle != null;
        }


        /// <summary>
        /// Generates an image with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public ImageTensor Generate(GenerateImageOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_stableDiffusionHandle);

            var parameters = options.ToUnmanaged();
            try
            {
                cancellationToken.Register(() => GenerateCancel(CancelType.Immediate));
                if (!NativeApi.generate_image(_stableDiffusionHandle.GetContext(), &parameters, out NativeApi.sd_image_generation_result_t* unmanagedResults) || unmanagedResults == null)
                    return null;

                try
                {
                    var unmanagedResult = unmanagedResults[0];
                    var unmanagedImages = unmanagedResult.items;
                    var unmanagedImageCount = unmanagedResult.count;
                    if (unmanagedImages == null || unmanagedImageCount <= 0)
                        return null;

                    var unmanagedImage = unmanagedImages[0].image;
                    if (_upscaleHandle != null)
                        return UpscaleInternal(unmanagedImage);

                    return unmanagedImage.ToManaged();
                }
                finally
                {
                    NativeApi.free_sd_image_generation_result(unmanagedResults);
                }
            }
            finally
            {
                ResetState();
                parameters.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Generates a video with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public VideoSequence Generate(GenerateVideoOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_stableDiffusionHandle);

            var parameters = options.ToUnmanaged();
            try
            {
                cancellationToken.Register(() => GenerateCancel(CancelType.Immediate));
                if (!NativeApi.generate_video(_stableDiffusionHandle.GetContext(), &parameters, out NativeApi.sd_video_generation_result_t* unmanagedResults) || unmanagedResults == null)
                    return null;

                try
                {
                    var unmanagedVideo = unmanagedResults[0];
                    var unmanagedFrames = unmanagedVideo.frames;
                    var unmanagedFrameCount = unmanagedVideo.frame_count;
                    var unmanagedAudio = unmanagedVideo.audio;
                    var unmanagedFps = unmanagedVideo.fps;
                    if (unmanagedFrames == null || unmanagedFrameCount <= 0)
                        return null;

                    var managedVideo = new ImageTensor[unmanagedFrameCount];
                    var managedAudio = unmanagedAudio == null ? default : unmanagedAudio[0].ToManaged();
                    for (int i = 0; i < unmanagedFrameCount; i++)
                    {
                        var unmanagedFrame = unmanagedFrames[i];
                        managedVideo[i] = _upscaleHandle != null
                            ? unmanagedFrame.ToManaged()
                            : UpscaleInternal(unmanagedFrame);
                    }
                    return new VideoSequence(managedVideo, unmanagedFps, managedAudio);
                }
                finally
                {
                    NativeApi.free_sd_video_generation_result(unmanagedResults);
                }
            }
            finally
            {
                ResetState();
                parameters.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Upscales the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        public ImageTensor Upscale(ImageTensor image, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_upscaleHandle);

            var unmanagedImage = image.ToUnmanaged();
            try
            {
                var timestamp = Stopwatch.GetTimestamp();
                var managedImage = UpscaleInternal(unmanagedImage);
                _progressCallback(1, 1, (float)Stopwatch.GetElapsedTime(timestamp).TotalSeconds);
                return managedImage;
            }
            finally
            {
                unmanagedImage.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Upscales the specified video sequence.
        /// </summary>
        /// <param name="videoSequence">The video sequence.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public VideoSequence Upscale(VideoSequence videoSequence, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_upscaleHandle);

            var unmanagedFrames = videoSequence.Frames.Select(x => x.ToUnmanaged()).ToArray();
            try
            {
                var managedFrames = new ImageTensor[videoSequence.FrameCount];
                for (int i = 0; i < unmanagedFrames.Length; i++)
                {
                    var timestamp = Stopwatch.GetTimestamp();
                    cancellationToken.ThrowIfCancellationRequested();
                    managedFrames[i] = UpscaleInternal(unmanagedFrames[i]);
                    _progressCallback(i, unmanagedFrames.Length, (float)Stopwatch.GetElapsedTime(timestamp).TotalSeconds);
                }
                return new VideoSequence(managedFrames, videoSequence.FrameRate, videoSequence.Audio);
            }
            finally
            {
                unmanagedFrames.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Initializes the callbacks.
        /// </summary>
        private void InitializeCallbacks()
        {
            NativeApi.sd_set_log_callback(_unmanagedLogCallback, null);
            NativeApi.sd_set_progress_callback(_unmanagedProgressCallback, null);
            if (_stableDiffusionOptions != null)
            {
                if (_stableDiffusionOptions.PreviewType != PreviewType.Disabled && _stableDiffusionOptions.PreviewType != PreviewType.Default)
                    NativeApi.sd_set_preview_callback(_unmanagedPreviewCallback, _stableDiffusionOptions.PreviewType.ToUnmanaged(), _stableDiffusionOptions.PreviewInterval, !_stableDiffusionOptions.IsPreviewNoisy, _stableDiffusionOptions.IsPreviewNoisy, null);
            }
        }


        /// <summary>
        /// Disposes the callbacks.
        /// </summary>
        private void DisposeCallbacks()
        {
            NativeApi.sd_set_log_callback(null, null);
            NativeApi.sd_set_progress_callback(null, null);
            if (_unmanagedPreviewCallback != null)
                NativeApi.sd_set_preview_callback(null, NativeApi.preview_t.PREVIEW_COUNT, 0, false, false, null);
        }


        /// <summary>
        /// Cancel generation.
        /// </summary>
        /// <param name="cancelType">Type of the cancel.</param>
        private void GenerateCancel(CancelType cancelType)
        {
            ResetState();
            NativeApi.sd_cancel_generation(_stableDiffusionHandle.GetContext(), cancelType.ToUnmanaged());
        }


        /// <summary>
        /// Upscales the internal.
        /// </summary>
        /// <param name="inputImage">The input image.</param>
        /// <returns>ImageTensor.</returns>
        private ImageTensor UpscaleInternal(NativeApi.sd_image_t inputImage)
        {
            if (!NativeApi.upscale(_upscaleHandle.GetContext(), inputImage, _upscaleFactor, out NativeApi.sd_image_generation_result_t* unmanagedResults) || unmanagedResults == null)
                return null;

            var unmanagedImage = unmanagedResults[0];
            var unmanagedImages = unmanagedImage.items;
            var unmanagedImageCount = unmanagedImage.count;
            if (unmanagedImages == null || unmanagedImageCount <= 0)
                return null;
            return unmanagedImages[0].ToManaged();
        }


        /// <summary>
        /// Gets the default image options.
        /// </summary>
        private GenerateImageOptions GetDefaultImageOptions()
        {
            if (!IsImageGenerationSupported())
                return null;

            var options = new NativeApi.sd_img_gen_params_t();
            NativeApi.sd_img_gen_params_init(&options);
            return options.ToManaged();
        }


        /// <summary>
        /// Gets the default video options.
        /// </summary>
        private GenerateVideoOptions GetDefaultVideoOptions()
        {
            if (!IsVideoGenerationSupported())
                return null;

            var options = new NativeApi.sd_vid_gen_params_t();
            NativeApi.sd_vid_gen_params_init(&options);
            return options.ToManaged();
        }


        /// <summary>
        /// Called when unmanaged log is emitted.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="text">The text.</param>
        /// <param name="data">The data.</param>
        private void OnLogCallback(NativeApi.sd_log_level_t level, byte* text, void* data)
        {
            _logCallback?.Invoke(level.ToManaged(), Utf8StringMarshaller.ConvertToManaged(text)?.Trim('\r', '\n'));
        }


        /// <summary>
        /// Called when unmanaged progress is emitted.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="steps">The steps.</param>
        /// <param name="time">The time.</param>
        /// <param name="data">The data.</param>
        private void OnProgressCallback(int step, int steps, float time, void* data)
        {
            if (_isGenerating && _previewTensors is not null)
            {
                _previewCallback?.Invoke(step, steps, time, _previewTensors);
                _previewTensors = null;
                return;
            }
            _progressCallback?.Invoke(step, steps, time);
        }


        /// <summary>
        /// Called when unmanaged preview is emitted.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="frame_count">The frame count.</param>
        /// <param name="frames">The frames.</param>
        /// <param name="is_noisy">if set to <c>true</c> [is noisy].</param>
        /// <param name="data">The data.</param>
        private void OnPreviewCallback(int step, int frame_count, NativeApi.sd_image_t* frames, bool is_noisy, void* data)
        {
            _isGenerating = true;
            if (frames == null || frame_count == 0 || _previewCallback == null)
                return;

            var managedFrames = new ImageTensor[frame_count];
            for (int i = 0; i < frame_count; i++)
            {
                managedFrames[i] = frames[i].ToManaged();
            }
            _previewTensors = managedFrames;
        }


        /// <summary>
        /// Resets the state.
        /// </summary>
        private void ResetState()
        {
            _isGenerating = false;
            _previewTensors = null;
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeCallbacks();
            ResetState();
            _upscaleHandle?.Dispose();
            _stableDiffusionHandle?.Dispose();
        }
    }
}