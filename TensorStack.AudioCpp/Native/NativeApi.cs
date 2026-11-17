using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TensorStack.AudioCpp.Common;

namespace TensorStack.AudioCpp.Native
{
    /// <summary>
    /// Native bindings for the audio.cpp C ABI (include/audiocpp.h)
    /// </summary>
    internal static unsafe partial class NativeApi
    {
        internal const string LibraryVersion = "0.9.5"; // https://github.com/0xShug0/audio.cpp/blob/v0.9.5/include/audiocpp.h
        internal const string LibraryName = "audiocpp";
        private static nint _libraryHandle;
        private static BackendInfo _backendInfo;

        private static readonly object _logLock = new object();
        private static readonly StringBuilder _logBuffer = new StringBuilder();
        private static Action<LogLevelType, string> _logCallback;
        private static LogLevelType _logBufferLevel;

        /// <summary>
        /// Loads the native library.
        /// </summary>
        /// <param name="directory">The environment directory.</param>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        internal static bool LoadNativeLibrary(out BackendInfo backendInfo, string libraryPath = null)
        {
            backendInfo = _backendInfo;
            if (_libraryHandle != nint.Zero)
                return true;

            var currentDirectory = Environment.CurrentDirectory;
            var workingDirectory = string.IsNullOrEmpty(libraryPath)
                ? AppContext.BaseDirectory
                : Path.GetFullPath(libraryPath);
            var fullLibraryPath = Path.Combine(workingDirectory, $"{LibraryName}.dll");
            if (!File.Exists(fullLibraryPath))
                return false;

            try
            {
                Environment.CurrentDirectory = workingDirectory;
                if (!NativeLibrary.TryLoad(fullLibraryPath, out nint handle))
                    return false;

                _libraryHandle = handle;
                audiocpp_abi_version();
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }

            _backendInfo = backendInfo = GetBackendInfo();

            // Logging
            audiocpp_log_set(&OnNativeLog, null);
            return true;
        }


        /// <summary>
        /// Gets the backend information.
        /// </summary>
        private static BackendInfo GetBackendInfo()
        {
            var abiVersion = audiocpp_abi_version();
            var abiMajor = abiVersion >> 16;
            var abiMinor = (abiVersion >> 8) & 0xff;
            return new BackendInfo
            {
                Commit = $"{abiMajor}.{abiMinor}",
                Version = PtrToString(audiocpp_build_version()),
            };
        }


        /// <summary>
        /// Sets the callback that receives native log messages, the native log sink is process wide.
        /// </summary>
        /// <param name="logCallback">The log callback, null to stop receiving messages.</param>
        internal static void SetLogCallback(Action<LogLevelType, string> logCallback)
        {
            lock (_logLock)
            {
                _logCallback = logCallback;
            }
        }


        /// <summary>
        /// Throws if the status is not OK, must be called on the thread that made the native call.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="operation">The operation.</param>
        internal static void ThrowIfFailed(StatusType status, string operation)
        {
            if (status == StatusType.Ok)
                return;

            var error = PtrToString(audiocpp_last_error());
            if (string.IsNullOrWhiteSpace(error))
                error = PtrToString(audiocpp_status_string(status));

            throw new Exception($"audio.cpp {operation} failed ({status}): {error}");
        }


        /// <summary>
        /// Converts a borrowed UTF8 pointer to a managed string.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        internal static string PtrToString(nint pointer)
        {
            return pointer == nint.Zero ? string.Empty : Marshal.PtrToStringUTF8(pointer);
        }


        /// <summary>
        /// Called when the native library emits a log message, messages can arrive in fragments.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="text">The text.</param>
        /// <param name="userData">The user data.</param>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnNativeLog(LogLevelType level, byte* text, void* userData)
        {
            try
            {
                var message = Marshal.PtrToStringUTF8((nint)text);
                if (string.IsNullOrEmpty(message))
                    return;

                lock (_logLock)
                {
                    if (_logBuffer.Length == 0 || level != LogLevelType.Continue)
                        _logBufferLevel = level == LogLevelType.Continue ? LogLevelType.Info : level;

                    _logBuffer.Append(message);
                    if (!message.EndsWith('\n'))
                        return;

                    var line = _logBuffer.ToString().TrimEnd();
                    _logBuffer.Clear();
                    if (line.Length == 0)
                        return;

                    _logCallback?.Invoke(_logBufferLevel, line);
                }
            }
            catch (Exception)
            {
                // Exceptions must never cross the native boundary
            }
        }


        internal enum StatusType
        {
            Ok = 0,
            InvalidArgument = 1,
            UnsupportedFamily = 2,
            LoadFailed = 3,
            Runtime = 4,
            OutOfMemory = 5,
            OutOfRange = 6,
            NotAvailable = 7
        }

        internal enum OptionScopeType
        {
            Request = 0,
            Session = 1,
            Load = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ModelConfig
        {
            public byte* FamilyHint;
            public byte* ConfigId;
            public byte* WeightId;
            public byte* ModelSpecOverride;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BackendConfig
        {
            public byte* Backend;
            public int Device;
            public int Threads;
        }


        // Versioning
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial uint audiocpp_abi_version();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint audiocpp_build_version();


        // Status and errors
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint audiocpp_last_error();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint audiocpp_status_string(StatusType status);


        // Logging
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_log_set(delegate* unmanaged[Cdecl]<LogLevelType, byte*, void*, void> logCallback, void* userData);


        // Options
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint audiocpp_options_create();

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_options_set(nint options, string key, string value);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_options_free(nint options);


        // Registry
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_registry_create(string configPath, out nint registry);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_registry_free(nint registry);


        // Model
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_model_load(nint registry, string modelPath, ModelConfig* config, nint options, out nint model);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_model_free(nint model);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint audiocpp_model_family(nint model);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int audiocpp_model_supports(nint model, string task, string mode);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint audiocpp_model_option_count(nint model, OptionScopeType scope);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_model_option(nint model, OptionScopeType scope, nuint index, out nint name, nint* valueName, nint* description, nint* defaultValue, nint* minValue, nint* maxValue, int* required);


        // Session
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_session_create(nint model, string task, string mode, BackendConfig* backendConfig, nint options, out nint session);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_session_free(nint session);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_session_run(nint session, nint request, out nint result);


        // Request
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint audiocpp_request_create();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_request_free(nint request);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_text(nint request, string text, string language);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_text_language(nint request, string language);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_audio(nint request, float* samples, nuint frames, int sampleRate, int channels);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_voice_audio(nint request, float* samples, nuint frames, int sampleRate, int channels);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_voice_id(nint request, string cachedVoiceId);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_speaking_rate(nint request, float speakingRate);

        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_request_set_option(nint request, string key, string value);


        // Result
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void audiocpp_result_free(nint result);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_result_audio(nint result, out nint samples, out nuint frames, out int sampleRate, out int channels);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial StatusType audiocpp_result_text(nint result, out nint text, out nint language);
    }
}
