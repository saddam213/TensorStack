using Microsoft.Win32.SafeHandles;
using System;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe sealed class ContextSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ContextSafeHandle() : base(true) { }
        public ContextSafeHandle(NativeApi.sd_ctx_t* context) : base(true)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            SetHandle((nint)context);
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
}