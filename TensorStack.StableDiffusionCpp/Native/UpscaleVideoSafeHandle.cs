using Microsoft.Win32.SafeHandles;
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
            using (var parameters = options.ToUnmanagedVideoUpscaler())
            {
                var status = NativeApi.sd_vid_upscaler_create(parameters.Handle, out NativeApi.sd_vid_upscaler_t* context);
                NativeApi.ThrowIfFailed(status, "Failed to create Upscale Video context");

                SetHandle((nint)context);
                DefaultOptions = GetDefaultOptions();
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
            NativeApi.sd_vid_upscaler_free((NativeApi.sd_vid_upscaler_t*)handle);
            return true;
        }


        /// <summary>
        /// Gets the context.
        /// </summary>
        public NativeApi.sd_vid_upscaler_t* GetContext()
        {
            return (NativeApi.sd_vid_upscaler_t*)DangerousGetHandle();
        }


        /// <summary>
        /// Gets the default options.
        /// The library no longer exposes its defaults, the managed options mirror them.
        /// </summary>
        /// <returns>UpscaleVideoOptions.</returns>
        private UpscaleVideoOptions GetDefaultOptions()
        {
            return new UpscaleVideoOptions();
        }
    }
}
