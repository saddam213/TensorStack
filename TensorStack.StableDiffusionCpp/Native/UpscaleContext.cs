using System;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe sealed class UpscaleContext : IDisposable
    {
        private readonly UpscaleImageSafeHandle _upscaleImageHandle;
        private readonly UpscaleVideoSafeHandle _upscaleVideoHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpscaleContext"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public UpscaleContext(UpscaleOptions options)
        {
            if (!string.IsNullOrEmpty(options.ModelPath))
            {
                _upscaleImageHandle = new UpscaleImageSafeHandle(options);
            }
            else
            {
                _upscaleVideoHandle = new UpscaleVideoSafeHandle(options);
            }
        }

        /// <summary>
        /// Gets the default image upscale options.
        /// </summary>
        public UpscaleImageOptions DefaultImageOptions => _upscaleImageHandle?.DefaultOptions;

        /// <summary>
        /// Gets the default video upscale options.
        /// </summary>
        public UpscaleVideoOptions DefaultVideoOptions => _upscaleVideoHandle?.DefaultOptions;

        /// <summary>
        /// Determines whether image upscale is supported.
        /// </summary>
        public bool IsImageUpscaleSupported()
        {
            return DefaultImageOptions != null;
        }

        /// <summary>
        /// Determines whether video upscale is supported.
        /// </summary>
        public bool IsVideoUpscaleSupported()
        {
            return DefaultVideoOptions != null;
        }


        /// <summary>
        /// Upscales managed images.
        /// </summary>
        /// <param name="inputImage">The input image.</param>
        /// <param name="options">The options.</param>
        public ImageTensor UpscaleImage(ImageTensor inputImage, UpscaleImageOptions options = default)
        {
            var unmanaged = inputImage.ToUnmanaged();
            try
            {
                return UpscaleImage(unmanaged, options);
            }
            finally
            {
                unmanaged.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Upscales unmanaged images.
        /// </summary>
        /// <param name="inputImage">The input image.</param>
        /// <param name="options">The options.</param>
        internal ImageTensor UpscaleImage(NativeApi.sd_image_t inputImage, UpscaleImageOptions options = default)
        {
            return UpscaleImageInternal(inputImage, options);
        }


        /// <summary>
        /// Upscales managed frames.
        /// </summary>
        /// <param name="videoFrames">The video frames.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ImageTensor[] UpscaleFrames(ImageTensor[] videoFrames, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            var managed = new ImageTensor[videoFrames.Length];
            for (int i = 0; i < videoFrames.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                managed[i] = UpscaleImage(videoFrames[i], options);
            }
            return managed;
        }


        /// <summary>
        /// Upscales unmanaged frames.
        /// </summary>
        /// <param name="videoFrames">The video frames.</param>
        /// <param name="frameCount">The frame count.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal ImageTensor[] UpscaleFrames(NativeApi.sd_image_t* videoFrames, int frameCount, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            var managed = new ImageTensor[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                managed[i] = UpscaleImage(videoFrames[i], options);
            }
            return managed;
        }


        /// <summary>
        /// Upscales managed video.
        /// </summary>
        /// <param name="videoSequence">The video sequence.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public VideoSequence UpscaleVideo(VideoSequence videoSequence, UpscaleVideoOptions options = default, CancellationToken cancellationToken = default)
        {
            var videoFrames = UpscaleVideoInternal(videoSequence.Frames, (int)videoSequence.FrameRate, options, cancellationToken);
            return new VideoSequence(videoFrames, videoSequence.FrameRate, videoSequence.Audio);
        }


        /// <summary>
        /// Upscales unmanaged video.
        /// </summary>
        /// <param name="videoResult">The video result.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ImageTensor[] UpscaleVideo(NativeApi.sd_video_generation_result_t videoResult, UpscaleVideoOptions options = default, CancellationToken cancellationToken = default)
        {
            return UpscaleVideoInternal(videoResult, options, cancellationToken);
        }


        /// <summary>
        /// Upscales unmanaged image.
        /// </summary>
        /// <param name="inputImage">The input image.</param>
        /// <param name="options">The options.</param>
        private ImageTensor UpscaleImageInternal(NativeApi.sd_image_t inputImage, UpscaleImageOptions options)
        {
            options = options ?? DefaultImageOptions;
            if (!NativeApi.upscale(_upscaleImageHandle.GetContext(), inputImage, (uint)options.ScaleFactor, out NativeApi.sd_image_generation_result_t* unmanagedResults) || unmanagedResults == null)
                return null;

            try
            {
                var unmanagedImage = unmanagedResults[0];
                var unmanagedImages = unmanagedImage.items;
                var unmanagedImageCount = unmanagedImage.count;
                if (unmanagedImages == null || unmanagedImageCount <= 0)
                    return null;
                return unmanagedImages[0].image.ToManaged();
            }
            finally
            {
                NativeApi.free_sd_image_generation_result(unmanagedResults);
            }
        }


        /// <summary>
        /// Upscales managed video.
        /// </summary>
        /// <param name="videoFrames">The video frames.</param>
        /// <param name="frameRate">The frame rate.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private ImageTensor[] UpscaleVideoInternal(ImageTensor[] videoFrames, int frameRate, UpscaleVideoOptions options, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultVideoOptions;
            var parameters = options.ToUnmanaged(videoFrames, frameRate);
            try
            {
                return UpscaleVideoInternal(&parameters, cancellationToken);
            }
            finally
            {
                parameters.FreeUnmanaged();
            }
        }


        /// <summary>
        /// Upscales unmanaged video.
        /// </summary>
        /// <param name="videoResult">The sd video result.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private ImageTensor[] UpscaleVideoInternal(NativeApi.sd_video_generation_result_t videoResult, UpscaleVideoOptions options, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultVideoOptions;
            var parameters = options.ToUnmanaged(videoResult.frames, videoResult.frame_count, videoResult.fps);
            return UpscaleVideoInternal(&parameters, cancellationToken);
        }


        /// <summary>
        /// Upscales unmanaged video.
        /// </summary>
        /// <param name="parameters">The upscale parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private ImageTensor[] UpscaleVideoInternal(NativeApi.sd_video_upscale_params_t* parameters, CancellationToken cancellationToken = default)
        {
            if (!NativeApi.upscale_video(_upscaleVideoHandle.GetContext(), parameters, out NativeApi.sd_video_generation_result_t* unmanagedResults) || unmanagedResults == null)
                return null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var unmanagedResult = unmanagedResults[0];
                var unmanagedFrames = unmanagedResult.frames;
                var unmanagedFrameCount = unmanagedResult.frame_count;
                if (unmanagedFrames == null || unmanagedFrameCount <= 0)
                    return null;

                var managedVideo = new ImageTensor[unmanagedFrameCount];
                Parallel.For(0, unmanagedFrameCount, i =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    managedVideo[i] = unmanagedFrames[i].ToManaged();
                });
                return managedVideo;
            }
            finally
            {
                NativeApi.free_sd_video_generation_result(unmanagedResults);
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _upscaleImageHandle?.Dispose();
            _upscaleVideoHandle?.Dispose();
        }
    }
}