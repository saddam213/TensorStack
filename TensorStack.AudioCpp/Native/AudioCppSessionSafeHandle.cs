using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TensorStack.AudioCpp.Common;

namespace TensorStack.AudioCpp.Native
{
    internal unsafe sealed class AudioCppSessionSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppSessionSafeHandle"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="task">The task, a session is bound to a single task.</param>
        /// <param name="mode">The mode (offline, streaming).</param>
        /// <param name="options">The options.</param>
        /// <param name="sessionOptions">The session options the model declares.</param>
        /// <exception cref="System.Exception">Failed to create audio.cpp session</exception>
        public AudioCppSessionSafeHandle(AudioCppModelSafeHandle model, string task, string mode, AudioCppOptions options, IReadOnlyDictionary<string, string> sessionOptions) : base(true)
        {
            Task = task;
            var nativeOptions = nint.Zero;
            var backend = Marshal.StringToCoTaskMemUTF8(options.Backend);
            try
            {
                nativeOptions = sessionOptions.ToUnmanaged();
                var backendConfig = new NativeApi.BackendConfig
                {
                    Backend = (byte*)backend,
                    Device = options.Device,
                    Threads = options.Threads
                };
                NativeApi.ThrowIfFailed(NativeApi.audiocpp_session_create(model.GetModel(), task, mode, &backendConfig, nativeOptions, out var session), $"'{task}' session create");
                SetHandle(session);
            }
            finally
            {
                NativeApi.audiocpp_options_free(nativeOptions);
                Marshal.FreeCoTaskMem(backend);
            }
        }

        /// <summary>
        /// Gets the task the session is bound to.
        /// </summary>
        public string Task { get; }


        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            NativeApi.audiocpp_session_free(handle);
            return true;
        }


        /// <summary>
        /// Gets the session.
        /// </summary>
        public nint GetSession()
        {
            return DangerousGetHandle();
        }
    }
}
