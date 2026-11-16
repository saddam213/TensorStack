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
            // Upscaling requires an RGB image
            var unmanaged = inputImage.ToUnmanaged(3);
            try
            {
                return UpscaleImage(unmanaged, options);
            }
            finally
            {
                NativeApi.sd_tensor_free(unmanaged);
            }
        }


        /// <summary>
        /// Upscales unmanaged images.
        /// </summary>
        /// <param name="inputImage">The input image, borrowed.</param>
        /// <param name="options">The options.</param>
        internal ImageTensor UpscaleImage(NativeApi.sd_tensor_t* inputImage, UpscaleImageOptions options = default)
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
        /// Upscales the frames of an unmanaged result.
        /// </summary>
        /// <param name="result">The result, borrowed.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal ImageTensor[] UpscaleFrames(NativeApi.sd_result_t* result, UpscaleImageOptions options = default, CancellationToken cancellationToken = default)
        {
            var frameCount = checked((int)NativeApi.sd_result_get_image_count(result));
            var managed = new ImageTensor[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                managed[i] = UpscaleImage(NativeApi.sd_result_get_image(result, (nuint)i), options);
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
            // Upscaling requires RGB frames
            var unmanaged = videoSequence.Frames.ToUnmanaged(3);
            try
            {
                var videoFrames = UpscaleVideoInternal(unmanaged, (int)videoSequence.FrameRate, options, cancellationToken);
                return new VideoSequence(videoFrames, videoSequence.FrameRate, videoSequence.Audio);
            }
            finally
            {
                NativeApi.sd_tensor_free(unmanaged);
            }
        }


        /// <summary>
        /// Upscales the video of an unmanaged result.
        /// </summary>
        /// <param name="result">The video result, borrowed.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.Exception">Failed to read video result</exception>
        internal ImageTensor[] UpscaleVideo(NativeApi.sd_result_t* result, UpscaleVideoOptions options = default, CancellationToken cancellationToken = default)
        {
            // Packs the frames into a single [C,W,H,T] tensor, owned by the result
            var video = NativeApi.sd_result_get_video(result);
            if (video == null || video->data == null)
                throw new Exception($"Failed to read video result: {NativeApi.GetLastError()}");

            return UpscaleVideoInternal(video->data, video->fps, options, cancellationToken);
        }


        /// <summary>
        /// Upscales unmanaged image.
        /// </summary>
        /// <param name="inputImage">The input image, borrowed.</param>
        /// <param name="options">The options, the scale is fixed by the loaded model.</param>
        private ImageTensor UpscaleImageInternal(NativeApi.sd_tensor_t* inputImage, UpscaleImageOptions options)
        {
            using (var parameters = NativeApiExtensions.ToUnmanagedUpscale(inputImage))
            {
                var status = NativeApi.sd_upscale(_upscaleImageHandle.GetContext(), parameters.Handle, out NativeApi.sd_result_t* unmanagedResult);
                NativeApi.ThrowIfFailed(status, "Failed to upscale image");

                try
                {
                    if (NativeApi.sd_result_get_image_count(unmanagedResult) == 0)
                        return null;

                    return NativeApiExtensions.ToImageTensor(NativeApi.sd_result_get_image(unmanagedResult, 0));
                }
                finally
                {
                    NativeApi.sd_result_free(unmanagedResult);
                }
            }
        }


        /// <summary>
        /// Upscales unmanaged video.
        /// </summary>
        /// <param name="videoFrames">The video frames [C,W,H,T], borrowed.</param>
        /// <param name="frameRate">The frame rate.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private ImageTensor[] UpscaleVideoInternal(NativeApi.sd_tensor_t* videoFrames, int frameRate, UpscaleVideoOptions options, CancellationToken cancellationToken = default)
        {
            options = options ?? DefaultVideoOptions;
            using (var parameters = options.ToUnmanaged(videoFrames, frameRate))
            {
                var status = NativeApi.sd_upscale_video(_upscaleVideoHandle.GetContext(), parameters.Handle, out NativeApi.sd_result_t* unmanagedResult);
                NativeApi.ThrowIfFailed(status, "Failed to upscale video");

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = (nint)unmanagedResult;
                    var frameCount = checked((int)NativeApi.sd_result_get_image_count(unmanagedResult));
                    if (frameCount <= 0)
                        return null;

                    var managedVideo = new ImageTensor[frameCount];
                    Parallel.For(0, frameCount, i =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        managedVideo[i] = NativeApiExtensions.ToImageTensor(NativeApi.sd_result_get_image((NativeApi.sd_result_t*)result, (nuint)i));
                    });
                    return managedVideo;
                }
                finally
                {
                    NativeApi.sd_result_free(unmanagedResult);
                }
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
