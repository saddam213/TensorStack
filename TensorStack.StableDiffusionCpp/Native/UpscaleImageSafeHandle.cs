using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices.Marshalling;
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
            var unmanagedBackend = Utf8StringMarshaller.ConvertToUnmanaged(options.Backend);
            var unmanagedModelPath = Utf8StringMarshaller.ConvertToUnmanaged(options.ModelPath);
            var unmanagedParamsbackend = Utf8StringMarshaller.ConvertToUnmanaged(options.ParamsBackend);
            var unmanagedIsConvDirect = !string.IsNullOrEmpty(options.ConvDirectMode) && options.ConvDirectMode.Equals("on");

            try
            {
                var context = NativeApi.new_upscaler_ctx(unmanagedModelPath, unmanagedIsConvDirect, options.Threads, options.TileSize, unmanagedBackend, unmanagedParamsbackend);
                if (context == null)
                    throw new Exception("Failed to create Upscale Image context");

                SetHandle((nint)context);
                DefaultOptions = GetDefaultOptions();
            }
            finally
            {
                Utf8StringMarshaller.Free(unmanagedBackend);
                Utf8StringMarshaller.Free(unmanagedModelPath);
                Utf8StringMarshaller.Free(unmanagedParamsbackend);
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
            NativeApi.free_upscaler_ctx((NativeApi.upscaler_ctx_t*)handle);
            return true;
        }


        /// <summary>
        /// Gets the context.
        /// </summary>
        public NativeApi.upscaler_ctx_t* GetContext()
        {
            return (NativeApi.upscaler_ctx_t*)DangerousGetHandle();
        }


        /// <summary>
        /// Gets the default options.
        /// </summary>
        private UpscaleImageOptions GetDefaultOptions()
        {
            return new UpscaleImageOptions
            {
                ScaleFactor = Math.Max(1, NativeApi.get_upscale_factor(GetContext()))
            };
        }
    }
}