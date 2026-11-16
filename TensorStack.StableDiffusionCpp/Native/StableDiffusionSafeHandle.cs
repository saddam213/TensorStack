using Microsoft.Win32.SafeHandles;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe sealed class StableDiffusionSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public StableDiffusionSafeHandle() : base(true) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionSafeHandle"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.Exception">Failed to create Diffusion context</exception>
        public StableDiffusionSafeHandle(StableDiffusionOptions options) : base(true)
        {
            using (var parameters = options.ToUnmanaged())
            {
                var status = NativeApi.sd_ctx_create(parameters.Handle, out NativeApi.sd_ctx_t* context);
                NativeApi.ThrowIfFailed(status, "Failed to create Diffusion context");

                SetHandle((nint)context);
                DefaultImageOptions = GetDefaultImageOptions();
                DefaultVideoOptions = GetDefaultVideoOptions();
            }
        }

        /// <summary>
        /// Gets the default image options.
        /// </summary>
        public GenerateImageOptions DefaultImageOptions { get; }

        /// <summary>
        /// Gets the default video options.
        /// </summary>
        public GenerateVideoOptions DefaultVideoOptions { get; }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.sd_ctx_free((NativeApi.sd_ctx_t*)handle);
            return true;
        }


        /// <summary>
        /// Gets the context.
        /// </summary>
        public NativeApi.sd_ctx_t* GetContext()
        {
            return (NativeApi.sd_ctx_t*)DangerousGetHandle();
        }


        /// <summary>
        /// Gets the default image options.
        /// The library no longer exposes its defaults, the managed options mirror them.
        /// </summary>
        private GenerateImageOptions GetDefaultImageOptions()
        {
            if (!NativeApi.sd_ctx_supports_image_generation(GetContext()))
                return null;

            return new GenerateImageOptions();
        }


        /// <summary>
        /// Gets the default video options.
        /// The library no longer exposes its defaults, the managed options mirror them.
        /// </summary>
        private GenerateVideoOptions GetDefaultVideoOptions()
        {
            if (!NativeApi.sd_ctx_supports_video_generation(GetContext()))
                return null;

            return new GenerateVideoOptions();
        }
    }
}
