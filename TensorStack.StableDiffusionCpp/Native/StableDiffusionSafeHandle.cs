using Microsoft.Win32.SafeHandles;
using System;
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
            var parameters = options.ToUnmanaged();
            try
            {
                var context = NativeApi.new_sd_ctx(&parameters);
                if (context == null)
                    throw new Exception("Failed to create Diffusion context");

                SetHandle((nint)context);
                DefaultImageOptions = GetDefaultImageOptions();
                DefaultVideoOptions = GetDefaultVideoOptions();
            }
            finally
            {
                parameters.FreeUnmanaged();
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
            NativeApi.free_sd_ctx((NativeApi.sd_ctx_t*)handle);
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
        /// </summary>
        private GenerateImageOptions GetDefaultImageOptions()
        {
            if (!NativeApi.sd_ctx_supports_image_generation(GetContext()))
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
            if (!NativeApi.sd_ctx_supports_video_generation(GetContext()))
                return null;

            var options = new NativeApi.sd_vid_gen_params_t();
            NativeApi.sd_vid_gen_params_init(&options);
            return options.ToManaged();
        }
    }
}