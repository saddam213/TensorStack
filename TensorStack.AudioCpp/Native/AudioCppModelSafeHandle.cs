using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TensorStack.AudioCpp.Common;

namespace TensorStack.AudioCpp.Native
{
    internal unsafe sealed class AudioCppModelSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppModelSafeHandle"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.Exception">Failed to load audio.cpp model</exception>
        public AudioCppModelSafeHandle(AudioCppOptions options) : base(true)
        {
            var registry = nint.Zero;
            var loadOptions = nint.Zero;
            var pipelineHint = Marshal.StringToCoTaskMemUTF8(options.Pipeline);
            try
            {
                // Registry, the model keeps it alive internally so it is released once the model is loaded
                NativeApi.ThrowIfFailed(NativeApi.audiocpp_registry_create(null, out registry), "registry create");

                // Model
                loadOptions = options.LoadOptions.ToUnmanaged();
                var modelConfig = new NativeApi.ModelConfig { FamilyHint = (byte*)pipelineHint };
                NativeApi.ThrowIfFailed(NativeApi.audiocpp_model_load(registry, options.ModelPath, &modelConfig, loadOptions, out var model), "model load");
                SetHandle(model);

                // Model Info
                Pipeline = NativeApi.PtrToString(NativeApi.audiocpp_model_family(handle));
                RequestOptions = GetDeclaredOptions(NativeApi.OptionScopeType.Request);
                SessionOptions = GetDeclaredOptions(NativeApi.OptionScopeType.Session);
            }
            finally
            {
                NativeApi.audiocpp_options_free(loadOptions);
                NativeApi.audiocpp_registry_free(registry);
                Marshal.FreeCoTaskMem(pipelineHint);
            }
        }

        /// <summary>
        /// Gets the audio.cpp pipeline (model family) of the model.
        /// </summary>
        public string Pipeline { get; }

        /// <summary>
        /// Gets the request option names the model declares.
        /// </summary>
        public IReadOnlySet<string> RequestOptions { get; }

        /// <summary>
        /// Gets the session option names the model declares.
        /// </summary>
        public IReadOnlySet<string> SessionOptions { get; }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.audiocpp_model_free(handle);
            return true;
        }


        /// <summary>
        /// Gets the model.
        /// </summary>
        public nint GetModel()
        {
            return DangerousGetHandle();
        }


        /// <summary>
        /// Determines whether the model supports the task.
        /// </summary>
        /// <param name="task">The task (gen, tts, clon, asr etc).</param>
        /// <param name="mode">The mode (offline, streaming).</param>
        public bool IsTaskSupported(string task, string mode)
        {
            return NativeApi.audiocpp_model_supports(GetModel(), task, mode) == 1;
        }


        /// <summary>
        /// Gets the option names the model declares for the scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        private HashSet<string> GetDeclaredOptions(NativeApi.OptionScopeType scope)
        {
            var options = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var count = NativeApi.audiocpp_model_option_count(GetModel(), scope);
            for (nuint i = 0; i < count; i++)
            {
                if (NativeApi.audiocpp_model_option(GetModel(), scope, i, out var name, null, null, null, null, null, null) == NativeApi.StatusType.Ok)
                    options.Add(NativeApi.PtrToString(name));
            }
            return options;
        }
    }
}
