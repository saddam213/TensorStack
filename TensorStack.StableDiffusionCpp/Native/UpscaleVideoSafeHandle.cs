using Microsoft.Win32.SafeHandles;
using System;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe sealed class UpscaleVideoSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public UpscaleVideoSafeHandle() : base(true) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpscaleVideoSafeHandle"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.Exception">Failed to create Upscale Video context</exception>
        public UpscaleVideoSafeHandle(UpscaleOptions options) : base(true)
        {
            var unmanaged = options.ToUnmanaged();
            try
            {
                var context = NativeApi.new_video_upscaler_ctx(&unmanaged);
                if (context == null)
                    throw new Exception("Failed to create Upscale Video context");

                SetHandle((nint)context);
                DefaultOptions = GetDefaultOptions();
            }
            finally
            {
                unmanaged.FreeUnmanaged();
            }
        }

        /// <summary>
        /// Gets the default options.
        /// </summary>
        public UpscaleVideoOptions DefaultOptions { get; }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.free_video_upscaler_ctx((NativeApi.video_upscaler_ctx_t*)handle);
            return true;
        }


        /// <summary>
        /// Gets the context.
        /// </summary>
        public NativeApi.video_upscaler_ctx_t* GetContext()
        {
            return (NativeApi.video_upscaler_ctx_t*)DangerousGetHandle();
        }


        /// <summary>
        /// Gets the default options.
        /// </summary>
        /// <returns>UpscaleVideoOptions.</returns>
        private UpscaleVideoOptions GetDefaultOptions()
        {
            var options = new NativeApi.sd_video_upscale_params_t();
            NativeApi.sd_video_upscale_params_init(&options);
            return options.ToManaged();
        }
    }
}