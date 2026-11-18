using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TensorStack.LlamaCpp.Common;

namespace TensorStack.LlamaCpp.Native
{
    internal static unsafe partial class NativeApi
    {
        internal const string LibraryVersion = "b12359"; // https://github.com/ggml-org/llama.cpp/blob/b12359/include/llama.h
        internal const string LibraryApiVersion = "1.0.0"; // llama_version() of the pinned release (v1.0.0)
        internal const string LibraryName = "llama";
        internal const string LibraryNameMtmd = "mtmd";
        internal const string LibraryNameGgml = "ggml";
        internal const string LibraryNameGgmlBase = "ggml-base";
        internal const uint DefaultSeed = 0xFFFFFFFF;

        private static readonly string[] _libraryNames = [LibraryNameGgmlBase, LibraryNameGgml, LibraryName, LibraryNameMtmd];
        private static readonly object _logLock = new object();
        private static readonly StringBuilder _logBuffer = new StringBuilder();
        private static Action<LogLevelType, string> _logCallback;
        private static LogLevelType _logBufferLevel;
        private static BackendInfo _backendInfo;
        private static volatile bool _isAbortRequested;
        private static bool _isLibraryLoaded;

        /// <summary>
        /// Loads the native libraries.
        /// </summary>
        /// <param name="backendInfo">The backend information.</param>
        /// <param name="libraryPath">The environment directory.</param>
        /// <returns><c>true</c> if loaded, <c>false</c> otherwise.</returns>
        internal static bool LoadNativeLibrary(out BackendInfo backendInfo, string libraryPath = null)
        {
            backendInfo = _backendInfo;
            if (_isLibraryLoaded)
                return true;

            var currentDirectory = Environment.CurrentDirectory;
            var workingDirectory = string.IsNullOrEmpty(libraryPath)
                ? AppContext.BaseDirectory
                : Path.GetFullPath(libraryPath);
            foreach (var libraryName in _libraryNames)
            {
                if (!File.Exists(Path.Combine(workingDirectory, $"{libraryName}.dll")))
                    return false;
            }

            try
            {
                Environment.CurrentDirectory = workingDirectory;
                foreach (var libraryName in _libraryNames)
                {
                    if (!NativeLibrary.TryLoad(Path.Combine(workingDirectory, $"{libraryName}.dll"), out _))
                        return false;
                }

                // Logging
                llama_log_set(&OnNativeLog, null);
                mtmd_helper_log_set(&OnNativeLog, null);

                // backend
                llama_backend_init();
                ggml_backend_load_all_from_path(workingDirectory);
                _isLibraryLoaded = true;
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }

            _backendInfo = backendInfo = GetBackendInfo();
            return true;
        }


        /// <summary>
        /// Gets the backend information.
        /// </summary>
        private static BackendInfo GetBackendInfo()
        {
            var version = PtrToString(llama_version());
            return new BackendInfo
            {
                Commit = version,
                Version = version,
                SystemInfo = PtrToString(llama_print_system_info())
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
        /// Requests or clears the abort of the evaluation that is in progress, only honoured by CPU execution.
        /// </summary>
        /// <param name="isAbortRequested">if set to <c>true</c> the evaluation is aborted.</param>
        internal static void SetAbortRequested(bool isAbortRequested)
        {
            _isAbortRequested = isAbortRequested;
        }


        /// <summary>
        /// Gets the native abort callback.
        /// </summary>
        internal static delegate* unmanaged[Cdecl]<void*, byte> GetAbortCallback()
        {
            return &OnNativeAbort;
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
        /// Called by the native library to check if the evaluation should be aborted.
        /// </summary>
        /// <param name="userData">The user data.</param>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte OnNativeAbort(void* userData)
        {
            return _isAbortRequested ? (byte)1 : (byte)0;
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
                    // A new message ends the unterminated line before it, a continuation keeps the level of the line it continues
                    if (level != LogLevelType.Continue)
                    {
                        FlushLogBuffer();
                        _logBufferLevel = level;
                    }
                    else if (_logBufferLevel == LogLevelType.None)
                    {
                        _logBufferLevel = LogLevelType.Info;
                    }

                    _logBuffer.Append(message);
                    if (message.EndsWith('\n'))
                        FlushLogBuffer();
                }
            }
            catch (Exception)
            {
                // Exceptions must never cross the native boundary
            }
        }


        /// <summary>
        /// Logs the buffered line, the caller must hold the log lock.
        /// </summary>
        private static void FlushLogBuffer()
        {
            var line = _logBuffer.ToString().TrimEnd();
            _logBuffer.Clear();
            if (line.Length == 0)
                return;

            _logCallback?.Invoke(_logBufferLevel, line);
        }


        // ggml: Backends and devices
        [LibraryImport(LibraryNameGgml, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ggml_backend_load_all_from_path(string directoryPath);

        [LibraryImport(LibraryNameGgml)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint ggml_backend_dev_count();

        [LibraryImport(LibraryNameGgml)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint ggml_backend_dev_get(nuint index);

        [LibraryImport(LibraryNameGgmlBase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint ggml_backend_dev_name(nint device);

        [LibraryImport(LibraryNameGgmlBase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint ggml_backend_dev_description(nint device);

        [LibraryImport(LibraryNameGgmlBase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial DeviceType ggml_backend_dev_type(nint device);

        [LibraryImport(LibraryNameGgmlBase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void ggml_backend_dev_memory(nint device, out nuint free, out nuint total);


        // llama: Versioning and logging
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_version();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_print_system_info();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_log_set(delegate* unmanaged[Cdecl]<LogLevelType, byte*, void*, void> logCallback, void* userData);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_backend_init();


        // llama: Default parameters
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ModelParams llama_model_default_params();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ContextParams llama_context_default_params();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial SamplerChainParams llama_sampler_chain_default_params();


        // llama: Model
        [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_model_load_from_file(string modelPath, ModelParams parameters);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_model_free(nint model);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_model_get_vocab(nint model);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_model_n_ctx_train(nint model);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_model_n_swa(nint model);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_model_desc(nint model, byte* buffer, nuint bufferSize);


        // llama: Context
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_init_from_model(nint model, ContextParams parameters);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_free(nint context);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial uint llama_n_ctx(nint context);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial uint llama_n_batch(nint context);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_get_memory(nint context);


        // llama: Memory
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_memory_clear(nint memory, [MarshalAs(UnmanagedType.I1)] bool data);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool llama_memory_seq_rm(nint memory, int sequenceId, int positionStart, int positionEnd);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_memory_seq_pos_min(nint memory, int sequenceId);


        // llama: Batch
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_batch_ext_init(nint context);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_batch_ext_free(nint batch);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_batch_ext_clear(nint batch);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_batch_ext_add_token(nint batch, int sequenceId, int token);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool llama_batch_ext_set_pos(nint batch, int index, int* position);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool llama_batch_ext_set_output_logits(nint batch, int index, [MarshalAs(UnmanagedType.I1)] bool value);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_process(nint context, ProcessType type, nint batch);


        // llama: Vocabulary
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_vocab_n_tokens(nint vocab);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_vocab_get_text(nint vocab, int token);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial TokenAttribute llama_vocab_get_attr(nint vocab, int token);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool llama_vocab_is_eog(nint vocab, int token);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_tokenize(nint vocab, byte* text, int textLength, int* tokens, int tokensMaximum, [MarshalAs(UnmanagedType.I1)] bool addSpecial, [MarshalAs(UnmanagedType.I1)] bool parseSpecial);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_token_to_piece(nint vocab, int token, byte* buffer, int length, int leftStrip, [MarshalAs(UnmanagedType.I1)] bool special);


        // llama: Sampling
        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_chain_init(SamplerChainParams parameters);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_sampler_chain_add(nint chain, nint sampler);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void llama_sampler_free(nint sampler);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int llama_sampler_sample(nint sampler, nint context, int index);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_greedy();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_dist(uint seed);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_top_k(int k);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_top_p(float p, nuint minKeep);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_min_p(float p, nuint minKeep);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_typical(float p, nuint minKeep);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_temp(float temperature);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint llama_sampler_init_penalties(int vocabSize, int penaltyLastN, float penaltyRepeat, float penaltyFrequency, float penaltyPresent);


        // mtmd: Context
        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void mtmd_helper_log_set(delegate* unmanaged[Cdecl]<LogLevelType, byte*, void*, void> logCallback, void* userData);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_default_marker();

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial MtmdContextParams mtmd_context_params_default();

        [LibraryImport(LibraryNameMtmd, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_init_from_file(string projectorPath, nint model, MtmdContextParams parameters);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void mtmd_free(nint context);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool mtmd_support_vision(nint context);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool mtmd_support_audio(nint context);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int mtmd_get_audio_sample_rate(nint context);


        // mtmd: Bitmaps (image or audio input)
        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_bitmap_init(uint width, uint height, byte* data);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_bitmap_init_from_audio(nuint samples, float* data);

        [LibraryImport(LibraryNameMtmd, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void mtmd_bitmap_set_id(nint bitmap, string id);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void mtmd_bitmap_free(nint bitmap);


        // mtmd: Input chunks
        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_input_chunks_init();

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint mtmd_input_chunks_size(nint chunks);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_input_chunks_get(nint chunks, nuint index);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void mtmd_input_chunks_free(nint chunks);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial ChunkType mtmd_input_chunk_get_type(nint chunk);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int* mtmd_input_chunk_get_tokens_text(nint chunk, out nuint tokenCount);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nuint mtmd_input_chunk_get_n_tokens(nint chunk);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int mtmd_input_chunk_get_n_pos(nint chunk);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial nint mtmd_input_chunk_get_id(nint chunk);


        // mtmd: Tokenize and evaluate
        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int mtmd_tokenize(nint context, nint output, MtmdInputText* text, nint* bitmaps, nuint bitmapCount);

        [LibraryImport(LibraryNameMtmd)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int mtmd_helper_eval_chunk_single(nint context, nint llamaContext, nint chunk, int positionPast, int sequenceId, int batchSize, [MarshalAs(UnmanagedType.I1)] bool logitsLast, out int positionNew);
    }
}
