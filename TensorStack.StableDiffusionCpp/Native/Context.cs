using TensorStack.StableDiffusionCpp.Common;
using System;
using System.Runtime.InteropServices.Marshalling;
using TensorStack.Common.Tensor;
using TensorStack.Media.Video;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal sealed unsafe class Context : IDisposable
    {
        private readonly NativeApi.sd_log_cb_t _unmanagedLogCallback;
        private readonly NativeApi.sd_preview_cb_t _unmanagedPreviewCallback;
        private readonly NativeApi.sd_progress_cb_t _unmanagedProgressCallback;
        private readonly Action<LogLevelType, string> _logCallback;
        private readonly Action<int, int, float> _progressCallback;
        private readonly Action<int, ImageTensor[]> _previewCallback;
        private readonly ContextSafeHandle _contextHandle;
        private readonly GenerateImageOptions _defaultImageOptions;
        private readonly GenerateVideoOptions _defaultVideoOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="previewCallback">The preview callback.</param>
        /// <param name="logCallback">The log callback.</param>
        private Context(Action<int, int, float> progressCallback = null, Action<int, ImageTensor[]> previewCallback = null, Action<LogLevelType, string> logCallback = null)
        {
            _logCallback = logCallback;
            _previewCallback = previewCallback;
            _progressCallback = progressCallback;
            _unmanagedLogCallback = OnLogCallback;
            _unmanagedPreviewCallback = OnPreviewCallback;
            _unmanagedProgressCallback = OnProgressCallback;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <param name="previewCallback">The preview callback.</param>
        /// <param name="logCallback">The log callback.</param>
        /// <exception cref="System.Exception">Failed to create StableDiffusion.cpp context</exception>
        public Context(ContextOptions options, Action<int, int, float> progressCallback = null, Action<int, ImageTensor[]> previewCallback = null, Action<LogLevelType, string> logCallback = null)
            : this(progressCallback, previewCallback, logCallback)
        {
            var native = options.ToUnmanaged();
            try
            {
                InitializeCallbacks(options);
                var context = NativeApi.new_sd_ctx(&native);
                if (context == null)
                    throw new Exception("Failed to create StableDiffusion.cpp context");

                _contextHandle = new ContextSafeHandle(context);
                _defaultImageOptions = GetDefaultImageOptions();
                _defaultVideoOptions = GetDefaultVideoOptions();
            }
            finally
            {
                native.FreeUnmanaged();
            }
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
            return NativeApi.sd_ctx_supports_image_generation(_contextHandle.GetContext());
        }


        /// <summary>
        /// Determines whether the context supports video generation.
        /// </summary>
        public bool IsVideoGenerationSupported()
        {
            return NativeApi.sd_ctx_supports_video_generation(_contextHandle.GetContext());
        }


        /// <summary>
        /// Generates an image with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public ImageTensor[] Generate(GenerateImageOptions options)
        {
            var parameters = options.ToUnmanaged();
            try
            {
                if (!NativeApi.generate_image(_contextHandle.GetContext(), &parameters, out NativeApi.sd_image_t* unmanagedImages, out int unmanagedImageCount))
                    return null;

                if (unmanagedImages == null || unmanagedImageCount <= 0)
                    return null;

                try
                {
                    var managedImages = new ImageTensor[unmanagedImageCount];
                    for (int i = 0; i < unmanagedImageCount; i++)
                    {
                        managedImages[i] = unmanagedImages[i].ToManaged();
                    }
                    return managedImages;
                }
                finally
                {
                    NativeApi.free_sd_images(unmanagedImages, unmanagedImageCount);
                }
            }
            finally
            {
                parameters.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Generates a video with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public VideoSequence Generate(GenerateVideoOptions options)
        {
            var parameters = options.ToUnmanaged();
            try
            {
                if (!NativeApi.generate_video(_contextHandle.GetContext(), &parameters, out NativeApi.sd_image_t* unmanagedFrames, out int unmanagedFrameCount, out NativeApi.sd_audio_t* unmanagedAudio))
                    return null;
                if (unmanagedFrames == null || unmanagedFrameCount <= 0)
                    return null;

                try
                {
                    var managedAudio = unmanagedAudio == null ? default : unmanagedAudio[0].ToManaged();
                    var managedVideo = new ImageTensor[unmanagedFrameCount];
                    for (int i = 0; i < unmanagedFrameCount; i++)
                    {
                        managedVideo[i] = unmanagedFrames[i].ToManaged();
                    }
                    return new VideoSequence(managedVideo, options.Fps, managedAudio);
                }
                finally
                {
                    NativeApi.free_sd_audio(unmanagedAudio);
                    NativeApi.free_sd_images(unmanagedFrames, unmanagedFrameCount);
                }
            }
            finally
            {
                parameters.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Cancels the active generation.
        /// </summary>
        /// <param name="cancelType">Type of the cancel.</param>
        public void Cancel(CancelType cancelType)
        {
            NativeApi.sd_cancel_generation(_contextHandle.GetContext(), cancelType.ToUnmanaged());
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            NativeApi.sd_set_log_callback(null, null);
            NativeApi.sd_set_progress_callback(null, null);
            if (_unmanagedPreviewCallback != null)
                NativeApi.sd_set_preview_callback(null, NativeApi.preview_t.PREVIEW_COUNT, 0, false, false, null);

            _contextHandle?.Dispose();
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
        /// Initializes the callbacks.
        /// </summary>
        /// <param name="options">The options.</param>
        private void InitializeCallbacks(ContextOptions options)
        {
            NativeApi.sd_set_log_callback(_unmanagedLogCallback, null);
            NativeApi.sd_set_progress_callback(_unmanagedProgressCallback, null);
            if (options.PreviewType != PreviewType.Disabled && options.PreviewType == PreviewType.Default)
                NativeApi.sd_set_preview_callback(_unmanagedPreviewCallback, options.PreviewType.ToUnmanaged(), options.PreviewInterval, !options.IsPreviewNoisy, options.IsPreviewNoisy, null);
        }


        /// <summary>
        /// Called when unmanaged log is emitted.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="text">The text.</param>
        /// <param name="data">The data.</param>
        private void OnLogCallback(NativeApi.sd_log_level_t level, byte* text, void* data)
        {
            _logCallback?.Invoke(level.ToManaged(), AnsiStringMarshaller.ConvertToManaged(text)?.Trim('\r', '\n'));
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
            if (frames == null || frame_count == 0 || _previewCallback == null)
                return;

            var managedFrames = new ImageTensor[frame_count];
            for (int i = 0; i < frame_count; i++)
            {
                managedFrames[i] = frames[i].ToManaged();
            }
            _previewCallback?.Invoke(step, managedFrames);
        }
    }
}