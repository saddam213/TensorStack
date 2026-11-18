using Microsoft.Win32.SafeHandles;

namespace TensorStack.LlamaCpp.Native
{
    /// <summary>
    /// Owns a native llama_model.
    /// </summary>
    internal sealed class LlamaModelSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaModelSafeHandle"/> class.
        /// </summary>
        /// <param name="model">The native model.</param>
        public LlamaModelSafeHandle(nint model) : base(true)
        {
            SetHandle(model);
        }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.llama_model_free(handle);
            return true;
        }
    }


    /// <summary>
    /// Owns a native llama_context.
    /// </summary>
    internal sealed class LlamaContextSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaContextSafeHandle"/> class.
        /// </summary>
        /// <param name="context">The native context.</param>
        public LlamaContextSafeHandle(nint context) : base(true)
        {
            SetHandle(context);
        }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.llama_free(handle);
            return true;
        }
    }


    /// <summary>
    /// Owns a native llama_batch_ext.
    /// </summary>
    internal sealed class LlamaBatchSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaBatchSafeHandle"/> class.
        /// </summary>
        /// <param name="batch">The native batch.</param>
        public LlamaBatchSafeHandle(nint batch) : base(true)
        {
            SetHandle(batch);
        }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.llama_batch_ext_free(handle);
            return true;
        }
    }


    /// <summary>
    /// Owns a native llama_sampler chain, the samplers added to the chain are freed with it.
    /// </summary>
    internal sealed class LlamaSamplerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaSamplerSafeHandle"/> class.
        /// </summary>
        /// <param name="sampler">The native sampler chain.</param>
        public LlamaSamplerSafeHandle(nint sampler) : base(true)
        {
            SetHandle(sampler);
        }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.llama_sampler_free(handle);
            return true;
        }
    }


    /// <summary>
    /// Owns a native mtmd_context (multimodal projector).
    /// </summary>
    internal sealed class MtmdContextSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MtmdContextSafeHandle"/> class.
        /// </summary>
        /// <param name="context">The native multimodal context.</param>
        public MtmdContextSafeHandle(nint context) : base(true)
        {
            SetHandle(context);
        }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.mtmd_free(handle);
            return true;
        }
    }
}
