using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    public static unsafe partial class NativeApi
    {
        public const string LibraryVersion = "532d2c1"; // https://github.com/leejet/stable-diffusion.cpp/blob/master-1106-532d2c1/include/stable-diffusion.h
        internal const string LibraryName = "stable-diffusion";
        private static nint _libraryHandle;
        private static BackendInfo _backendInfo;

        internal static bool LoadNativeLibrary(out BackendInfo backendInfo, string libraryPath = null)
        {
            backendInfo = _backendInfo;
            if (_libraryHandle != nint.Zero)
                return true;

            var deviceSize = nuint.Zero;
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
                deviceSize = sd_list_devices(null, 0);
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }

            if (deviceSize == nuint.Zero)
                return false;

            _backendInfo = backendInfo = GetBackendInfo(deviceSize);
            return true;
        }


        private static BackendInfo GetBackendInfo(nuint deviceSize)
        {
            byte[] buffer = new byte[(int)deviceSize];
            fixed (byte* pBuffer = buffer)
            {
                sd_list_devices(pBuffer, (nuint)buffer.Length);
            }
            var deviceInfos = Encoding.UTF8.GetString(buffer).TrimEnd('\0').Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var devices = new BackendDevice[deviceInfos.Length];
            for (int i = 0; i < deviceInfos.Length; i++)
            {
                devices[i] = new BackendDevice(deviceInfos[i]);
            }
            return new BackendInfo
            {
                Devices = devices,
                Commit = Utf8StringMarshaller.ConvertToManaged(sd_commit()),
                Version = Utf8StringMarshaller.ConvertToManaged(sd_version()),
                SystemInfo = Utf8StringMarshaller.ConvertToManaged(sd_get_system_info()),
                NumPhysicalCores = sd_get_num_physical_cores(),
            };
        }


        /// <summary>
        /// Gets the last native error for the calling thread.
        /// </summary>
        internal static string GetLastError()
        {
            return Utf8StringMarshaller.ConvertToManaged(sd_get_last_error());
        }


        /// <summary>
        /// Throws if the native status is not OK.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="System.Exception">The message, status and native error</exception>
        internal static void ThrowIfFailed(sd_status_t status, string message)
        {
            if (status == sd_status_t.SD_STATUS_OK)
                return;

            // Must be read before any other native call, it is thread-local and cleared by the next call
            var error = GetLastError();
            throw new Exception(string.IsNullOrEmpty(error) ? $"{message} ({status})" : $"{message} ({status}): {error}");
        }


        // ============================================================
        // Enums
        // ============================================================

        internal enum sd_status_t
        {
            SD_STATUS_OK = 0,
            SD_STATUS_INVALID_ARGUMENT = 1,
            SD_STATUS_UNKNOWN_KEY = 2,
            SD_STATUS_TYPE_MISMATCH = 3,
            SD_STATUS_OUT_OF_RANGE = 4,
            SD_STATUS_UNSUPPORTED = 5,
            SD_STATUS_OUT_OF_MEMORY = 6,
            SD_STATUS_LOAD_FAILED = 7,
            SD_STATUS_GENERATION_FAILED = 8,
            SD_STATUS_BUSY = 9,
            SD_STATUS_CANCELLED = 10
        }

        internal enum sd_tensor_type_t : int
        {
            SD_TENSOR_U8 = 1,
            SD_TENSOR_I32 = 2,
            SD_TENSOR_I64 = 3,
            SD_TENSOR_F32 = 4,
            SD_TENSOR_F64 = 5
        }

        internal enum sd_result_kind_t
        {
            SD_RESULT_INVALID = 0,
            SD_RESULT_IMAGE = 1,
            SD_RESULT_VIDEO = 2
        }

        internal enum sd_type_t
        {
            SD_TYPE_F32 = 0,
            SD_TYPE_F16 = 1,
            SD_TYPE_Q4_0 = 2,
            SD_TYPE_Q4_1 = 3,
            SD_TYPE_Q5_0 = 6,
            SD_TYPE_Q5_1 = 7,
            SD_TYPE_Q8_0 = 8,
            SD_TYPE_Q8_1 = 9,
            SD_TYPE_Q2_K = 10,
            SD_TYPE_Q3_K = 11,
            SD_TYPE_Q4_K = 12,
            SD_TYPE_Q5_K = 13,
            SD_TYPE_Q6_K = 14,
            SD_TYPE_Q8_K = 15,
            SD_TYPE_IQ2_XXS = 16,
            SD_TYPE_IQ2_XS = 17,
            SD_TYPE_IQ3_XXS = 18,
            SD_TYPE_IQ1_S = 19,
            SD_TYPE_IQ4_NL = 20,
            SD_TYPE_IQ3_S = 21,
            SD_TYPE_IQ2_S = 22,
            SD_TYPE_IQ4_XS = 23,
            SD_TYPE_I8 = 24,
            SD_TYPE_I16 = 25,
            SD_TYPE_I32 = 26,
            SD_TYPE_I64 = 27,
            SD_TYPE_F64 = 28,
            SD_TYPE_IQ1_M = 29,
            SD_TYPE_BF16 = 30,
            SD_TYPE_TQ1_0 = 34,
            SD_TYPE_TQ2_0 = 35,
            SD_TYPE_MXFP4 = 39,
            SD_TYPE_NVFP4 = 40,
            SD_TYPE_Q1_0 = 41,
            SD_TYPE_Q2_0 = 42,
            SD_TYPE_F8_E4M3 = 43,
            SD_TYPE_F8_E5M2 = 44,
            SD_TYPE_COUNT = 45
        }

        internal enum sd_log_level_t
        {
            SD_LOG_DEBUG,
            SD_LOG_VERBOSE,
            SD_LOG_INFO,
            SD_LOG_WARN,
            SD_LOG_ERROR
        }

        internal enum sd_cancel_mode_t
        {
            SD_CANCEL_ALL,
            SD_CANCEL_NEW_LATENTS,
            SD_CANCEL_RESET
        }


        // ============================================================
        // Structs
        // ============================================================

        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_audio_t
        {
            public uint sample_rate;
            public sd_tensor_t* data;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_video_t
        {
            public sd_tensor_t* data;
            public int fps;
            public sd_audio_t audio;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_embedding_t
        {
            public byte* name;
            public byte* path;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_lora_t
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool is_high_noise;

            public float multiplier;
            public byte* path;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_generation_progress_t
        {
            public byte* stage;
            public byte* detail;
            public byte* unit;
            public int sample_pass;
            public long completed;
            public long total;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_preview_info_t
        {
            public int sample_pass;
            public int total_steps;
        }


        // ============================================================
        // Opaque native types
        // ============================================================
        internal struct sd_tensor_t { }
        internal struct sd_ctx_t { }
        internal struct sd_result_t { }
        internal struct sd_opt_t { }
        internal struct sd_upscaler_t { }
        internal struct sd_vid_upscaler_t { }
        internal struct sd_adetailer_t { }
        internal struct ggml_tensor { }


        // ============================================================
        // Callback delegates
        // ============================================================

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_log_cb_t(sd_log_level_t level, byte* text, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_progress_cb_t(int step, int steps, float time, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_generation_progress_cb_t(sd_generation_progress_t* progress, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_preview_cb_t(int step, int frame_count, sd_tensor_t** frames, [MarshalAs(UnmanagedType.I1)] bool is_noisy, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal delegate bool sd_graph_eval_callback_t(ggml_tensor* t, [MarshalAs(UnmanagedType.I1)] bool ask, void* user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal delegate bool sd_prompt_image_cb_t(byte* source, void* data, sd_tensor_t** image_out);


        // ============================================================
        // API: Status and diagnostics
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_last_error))]
        internal static partial byte* sd_get_last_error();


        // ============================================================
        // API: Tensors
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_create))]
        internal static partial sd_tensor_t* sd_tensor_create(sd_tensor_type_t type, nuint rank, long* shape, void* data, nuint data_size);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_retain))]
        internal static partial sd_tensor_t* sd_tensor_retain(sd_tensor_t* tensor);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_free))]
        internal static partial void sd_tensor_free(sd_tensor_t* tensor);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_type))]
        internal static partial sd_tensor_type_t sd_tensor_type(sd_tensor_t* tensor);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_rank))]
        internal static partial nuint sd_tensor_rank(sd_tensor_t* tensor);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_dim))]
        internal static partial long sd_tensor_dim(sd_tensor_t* tensor, nuint axis);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_byte_size))]
        internal static partial nuint sd_tensor_byte_size(sd_tensor_t* tensor);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_tensor_data))]
        internal static partial void* sd_tensor_data(sd_tensor_t* tensor);


        // ============================================================
        // API: Options
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_opt_create))]
        internal static partial sd_opt_t* sd_ctx_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_img_gen_opt_create))]
        internal static partial sd_opt_t* sd_img_gen_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_vid_gen_opt_create))]
        internal static partial sd_opt_t* sd_vid_gen_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscaler_opt_create))]
        internal static partial sd_opt_t* sd_upscaler_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscale_opt_create))]
        internal static partial sd_opt_t* sd_upscale_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_vid_upscaler_opt_create))]
        internal static partial sd_opt_t* sd_vid_upscaler_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_vid_upscale_opt_create))]
        internal static partial sd_opt_t* sd_vid_upscale_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_adetailer_opt_create))]
        internal static partial sd_opt_t* sd_adetailer_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_adetail_opt_create))]
        internal static partial sd_opt_t* sd_adetail_opt_create();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_free))]
        internal static partial void sd_opt_free(sd_opt_t* opt);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_bool), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_bool(sd_opt_t* opt, string key, [MarshalAs(UnmanagedType.I1)] bool value);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_int), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_int(sd_opt_t* opt, string key, long value);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_float), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_float(sd_opt_t* opt, string key, double value);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_string), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_string(sd_opt_t* opt, string key, string value);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_tensor), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_tensor(sd_opt_t* opt, string key, sd_tensor_t* tensor);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_tensors), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_tensors(sd_opt_t* opt, string key, sd_tensor_t** tensors, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_floats), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_floats(sd_opt_t* opt, string key, float* values, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_ints), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_ints(sd_opt_t* opt, string key, int* values, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_loras), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_loras(sd_opt_t* opt, string key, sd_lora_t* values, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_embeddings), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_embeddings(sd_opt_t* opt, string key, sd_embedding_t* values, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_audios), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_audios(sd_opt_t* opt, string key, sd_audio_t* values, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_set_videos), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_set_videos(sd_opt_t* opt, string key, sd_video_t* values, nuint count);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_opt_unset), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_opt_unset(sd_opt_t* opt, string key);


        // ============================================================
        // API: Context and generation
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_create))]
        internal static partial sd_status_t sd_ctx_create(sd_opt_t* opt, out sd_ctx_t* ctx_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_free))]
        internal static partial void sd_ctx_free(sd_ctx_t* ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_generate_image))]
        internal static partial sd_status_t sd_generate_image(sd_ctx_t* ctx, sd_opt_t* opt, out sd_result_t* result_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_generate_video))]
        internal static partial sd_status_t sd_generate_video(sd_ctx_t* ctx, sd_opt_t* opt, out sd_result_t* result_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_supports_image_generation))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_supports_image_generation(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_supports_video_generation))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_supports_video_generation(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_load_control_net), StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_load_control_net(sd_ctx_t* sd_ctx, string path);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_unload_control_net))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_unload_control_net(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_has_control_net))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_has_control_net(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_model_version_name))]
        internal static partial byte* sd_get_model_version_name(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_cancel_generation))]
        internal static partial void sd_cancel_generation(sd_ctx_t* sd_ctx, sd_cancel_mode_t mode);


        // ============================================================
        // API: Results
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_kind))]
        internal static partial sd_result_kind_t sd_result_get_kind(sd_result_t* result);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_image_count))]
        internal static partial nuint sd_result_get_image_count(sd_result_t* result);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_image))]
        internal static partial sd_tensor_t* sd_result_get_image(sd_result_t* result, nuint index);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_video))]
        internal static partial sd_video_t* sd_result_get_video(sd_result_t* result);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_generation_info))]
        internal static partial byte* sd_result_get_generation_info(sd_result_t* result, nuint index);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_audio))]
        internal static partial sd_audio_t* sd_result_get_audio(sd_result_t* result);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_get_fps))]
        internal static partial int sd_result_get_fps(sd_result_t* result);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_result_free))]
        internal static partial void sd_result_free(sd_result_t* result);


        // ============================================================
        // API: Callbacks and progress
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_log_callback))]
        internal static partial void sd_set_log_callback(sd_log_cb_t sd_log_cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_progress_callback))]
        internal static partial void sd_set_progress_callback(sd_progress_cb_t cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_generation_progress_callback))]
        internal static partial void sd_set_generation_progress_callback(sd_ctx_t* ctx, sd_generation_progress_cb_t cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_preview_callback), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_status_t sd_set_preview_callback(sd_preview_cb_t cb, string mode, int interval, [MarshalAs(UnmanagedType.I1)] bool denoised, [MarshalAs(UnmanagedType.I1)] bool noisy, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_preview_info))]
        internal static partial sd_preview_info_t sd_get_preview_info();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_prompt_image_callback))]
        internal static partial void sd_set_prompt_image_callback(sd_ctx_t* ctx, sd_prompt_image_cb_t cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_backend_eval_callback))]
        internal static partial void sd_set_backend_eval_callback(sd_graph_eval_callback_t cb, void* data);


        // ============================================================
        // API: Image upscaling
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscaler_create))]
        internal static partial sd_status_t sd_upscaler_create(sd_opt_t* opt, out sd_upscaler_t* upscaler_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscaler_free))]
        internal static partial void sd_upscaler_free(sd_upscaler_t* upscaler);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscale))]
        internal static partial sd_status_t sd_upscale(sd_upscaler_t* upscaler, sd_opt_t* opt, out sd_result_t* result_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscaler_get_scale))]
        internal static partial int sd_upscaler_get_scale(sd_upscaler_t* upscaler);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscaler_probe_scale), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int sd_upscaler_probe_scale(string model_path);


        // ============================================================
        // API: Video upscaling
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_vid_upscaler_create))]
        internal static partial sd_status_t sd_vid_upscaler_create(sd_opt_t* opt, out sd_vid_upscaler_t* upscaler_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_vid_upscaler_free))]
        internal static partial void sd_vid_upscaler_free(sd_vid_upscaler_t* upscaler);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_upscale_video))]
        internal static partial sd_status_t sd_upscale_video(sd_vid_upscaler_t* upscaler, sd_opt_t* opt, out sd_result_t* result_out);


        // ============================================================
        // API: ADetailer
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_adetailer_create))]
        internal static partial sd_status_t sd_adetailer_create(sd_opt_t* opt, out sd_adetailer_t* adetailer_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_adetailer_free))]
        internal static partial void sd_adetailer_free(sd_adetailer_t* adetailer);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_adetail))]
        internal static partial sd_status_t sd_adetail(sd_adetailer_t* adetailer, sd_ctx_t* ctx, sd_opt_t* opt, out sd_result_t* result_out);


        // ============================================================
        // API: Image preprocessing
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_preprocess_canny))]
        internal static partial sd_status_t sd_preprocess_canny(sd_tensor_t* image, float high_threshold, float low_threshold, float weak, float strong, [MarshalAs(UnmanagedType.I1)] bool inverse, out sd_tensor_t* image_out);


        // ============================================================
        // API: Weight types and model conversion
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_type_name))]
        internal static partial byte* sd_type_name(sd_type_t type);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_sd_type), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial sd_type_t str_to_sd_type(string str);

        [LibraryImport(LibraryName, EntryPoint = nameof(convert))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool convert(byte* input_path, byte* vae_path, byte* output_path, sd_type_t output_type, byte* tensor_type_rules, [MarshalAs(UnmanagedType.I1)] bool convert_name);

        [LibraryImport(LibraryName, EntryPoint = nameof(convert_with_components))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool convert_with_components(byte* model_path, byte* clip_l_path, byte* clip_g_path, byte* t5xxl_path, byte* diffusion_model_path, byte* vae_path, byte* output_path, sd_type_t output_type, byte* tensor_type_rules, [MarshalAs(UnmanagedType.I1)] bool convert_name, int n_threads, sd_lora_t* loras, int lora_count);


        // ============================================================
        // API: Importance matrix
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(load_imatrix))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool load_imatrix(byte* imatrix_path);

        [LibraryImport(LibraryName, EntryPoint = nameof(save_imatrix))]
        internal static partial void save_imatrix(byte* imatrix_path);

        [LibraryImport(LibraryName, EntryPoint = nameof(enable_imatrix_collection))]
        internal static partial void enable_imatrix_collection();

        [LibraryImport(LibraryName, EntryPoint = nameof(disable_imatrix_collection))]
        internal static partial void disable_imatrix_collection();


        // ============================================================
        // API: Devices and system information
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_list_devices))]
        internal static partial nuint sd_list_devices(byte* buffer, nuint buffer_size);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_add_rpc_servers))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_add_rpc_servers(byte* servers);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_num_physical_cores))]
        internal static partial int sd_get_num_physical_cores();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_system_info))]
        internal static partial byte* sd_get_system_info();


        // ============================================================
        // API: Build information
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_commit))]
        internal static partial byte* sd_commit();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_version))]
        internal static partial byte* sd_version();
    }
}
