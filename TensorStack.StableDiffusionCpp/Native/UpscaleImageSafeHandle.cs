using Microsoft.Win32.SafeHandles;
using System;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe sealed class UpscaleImageSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public UpscaleImageSafeHandle() : base(true) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpscaleImageSafeHandle"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.Exception">Failed to create Upscale Image context</exception>
        public UpscaleImageSafeHandle(UpscaleOptions options) : base(true)
        {
            using (var parameters = options.ToUnmanagedImageUpscaler())
            {
                var status = NativeApi.sd_upscaler_create(parameters.Handle, out NativeApi.sd_upscaler_t* context);
                NativeApi.ThrowIfFailed(status, "Failed to create Upscale Image context");

                SetHandle((nint)context);
                DefaultOptions = GetDefaultOptions();
            }
        }

        /// <summary>
        /// Gets the default options.
        /// </summary>
        public UpscaleImageOptions DefaultOptions { get; }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.sd_upscaler_free((NativeApi.sd_upscaler_t*)handle);
            return true;
        }


        /// <summary>
        /// Gets the context.
        /// </summary>
        public NativeApi.sd_upscaler_t* GetContext()
        {
            return (NativeApi.sd_upscaler_t*)DangerousGetHandle();
        }


        /// <summary>
        /// Gets the default options.
        /// </summary>
        private UpscaleImageOptions GetDefaultOptions()
        {
            return new UpscaleImageOptions
            {
                ScaleFactor = Math.Max(1, NativeApi.sd_upscaler_get_scale(GetContext()))
            };
        }
    }
}
