using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices.Marshalling;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe sealed class StableDiffusionSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public StableDiffusionSafeHandle() : base(true) { }
        public StableDiffusionSafeHandle(StableDiffusionOptions options) : base(true)
        {
            var parameters = options.ToUnmanaged();
            try
            {
                var context = NativeApi.new_sd_ctx(&parameters);
                if (context == null)
                    throw new Exception("Failed to create Diffusion context");

                SetHandle((nint)context);
            }
            finally
            {
                parameters.FreeUnmanaged();
            }
        }


        protected override bool ReleaseHandle()
        {
            NativeApi.free_sd_ctx((NativeApi.sd_ctx_t*)handle);
            return true;
        }


        public NativeApi.sd_ctx_t* GetContext()
        {
            return (NativeApi.sd_ctx_t*)DangerousGetHandle();
        }
    }


    internal unsafe sealed class UpscaleSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public UpscaleSafeHandle() : base(true) { }
        public UpscaleSafeHandle(UpscaleOptions options) : base(true)
        {
            var unmanagedBackend = Utf8StringMarshaller.ConvertToUnmanaged(options.Backend);
            var unmanagedModelPath = Utf8StringMarshaller.ConvertToUnmanaged(options.ModelPath);
            var unmanagedParamsbackend = Utf8StringMarshaller.ConvertToUnmanaged(options.ParamsBackend);

            try
            {
                var context = NativeApi.new_upscaler_ctx(unmanagedModelPath, options.IsConvDirect, options.Threads, options.TileSize, unmanagedBackend, unmanagedParamsbackend);
                if (context == null)
                    throw new Exception("Failed to create Upscale context");

                SetHandle((nint)context);

            }
            finally
            {
                Utf8StringMarshaller.Free(unmanagedBackend);
                Utf8StringMarshaller.Free(unmanagedModelPath);
                Utf8StringMarshaller.Free(unmanagedParamsbackend);
            }
        }


        protected override bool ReleaseHandle()
        {
            NativeApi.free_upscaler_ctx((NativeApi.upscaler_ctx_t*)handle);
            return true;
        }


        public NativeApi.upscaler_ctx_t* GetContext()
        {
            return (NativeApi.upscaler_ctx_t*)DangerousGetHandle();
        }
    }
}