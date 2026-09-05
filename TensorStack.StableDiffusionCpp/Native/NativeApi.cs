using TensorStack.StableDiffusionCpp.Common;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace TensorStack.StableDiffusionCpp.Native
{
    public static unsafe partial class NativeApi
    {
        public const string LibraryVersion = "6b3edaa"; // https://github.com/leejet/stable-diffusion.cpp/blob/master-841-6b3edaa/include/stable-diffusion.h
        internal const string LibraryName = "stable-diffusion";
        private static nint _libraryHandle;

        internal static bool LoadNativeLibrary(out BackendInfo backendInfo, string libraryPath = null)
        {
            backendInfo = null;
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

            backendInfo = GetBackendInfo(deviceSize);
            return true;
        }


        private static BackendInfo GetBackendInfo(nuint deviceSize)
        {
            byte[] buffer = new byte[(int)deviceSize];
            fixed (byte* pBuffer = buffer)
            {
                sd_list_devices(pBuffer, (nuint)buffer.Length);
            }
            var deviceInfos = Encoding.UTF8.GetString(buffer).TrimEnd('\0').Split("\n", StringSplitOptions.TrimEntries);
            var devices = new BackendDevice[deviceInfos.Length];
            for (int i = 0; i < deviceInfos.Length; i++)
            {
                devices[i] = new BackendDevice(deviceInfos[i]);
            }
            return new BackendInfo
            {
                Devices = devices,
                Commit = AnsiStringMarshaller.ConvertToManaged(sd_commit()),
                Version = AnsiStringMarshaller.ConvertToManaged(sd_version()),
                SystemInfo = AnsiStringMarshaller.ConvertToManaged(sd_get_system_info()),
                NumPhysicalCores = sd_get_num_physical_cores(),
            };
        }

        internal enum sample_method_t
        {
            EULER_SAMPLE_METHOD,
            EULER_A_SAMPLE_METHOD,
            HEUN_SAMPLE_METHOD,
            DPM2_SAMPLE_METHOD,
            DPMPP2S_A_SAMPLE_METHOD,
            DPMPP2M_SAMPLE_METHOD,
            DPMPP2Mv2_SAMPLE_METHOD,
            IPNDM_SAMPLE_METHOD,
            IPNDM_V_SAMPLE_METHOD,
            LCM_SAMPLE_METHOD,
            DDIM_TRAILING_SAMPLE_METHOD,
            TCD_SAMPLE_METHOD,
            RES_MULTISTEP_SAMPLE_METHOD,
            RES_2S_SAMPLE_METHOD,
            ER_SDE_SAMPLE_METHOD,
            EULER_CFG_PP_SAMPLE_METHOD,
            EULER_A_CFG_PP_SAMPLE_METHOD,
            EULER_GE_SAMPLE_METHOD,
            DPMPP2M_SDE_SAMPLE_METHOD,
            DPMPP2M_SDE_BT_SAMPLE_METHOD,
            LMS_SAMPLE_METHOD,
            SAMPLE_METHOD_COUNT
        }

        internal enum scheduler_t
        {
            DISCRETE_SCHEDULER,
            KARRAS_SCHEDULER,
            EXPONENTIAL_SCHEDULER,
            AYS_SCHEDULER,
            GITS_SCHEDULER,
            SGM_UNIFORM_SCHEDULER,
            SIMPLE_SCHEDULER,
            SMOOTHSTEP_SCHEDULER,
            KL_OPTIMAL_SCHEDULER,
            LCM_SCHEDULER,
            BONG_TANGENT_SCHEDULER,
            LTX2_SCHEDULER,
            LOGIT_NORMAL_SCHEDULER,
            FLUX2_SCHEDULER,
            FLUX_SCHEDULER,
            BETA_SCHEDULER,
            SCHEDULER_COUNT
        }

        internal enum prediction_t
        {
            EPS_PRED,
            V_PRED,
            EDM_V_PRED,
            FLOW_PRED,
            FLUX_FLOW_PRED,
            SEFI_FLOW_PRED,
            MINIT2I_FLOW_PRED,
            PREDICTION_COUNT
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
            SD_LOG_INFO,
            SD_LOG_WARN,
            SD_LOG_ERROR
        }

        internal enum preview_t
        {
            PREVIEW_NONE,
            PREVIEW_PROJ,
            PREVIEW_TAE,
            PREVIEW_VAE,
            PREVIEW_COUNT
        }

        internal enum lora_apply_mode_t
        {
            LORA_APPLY_AUTO,
            LORA_APPLY_IMMEDIATELY,
            LORA_APPLY_AT_RUNTIME,
            LORA_APPLY_MODE_COUNT
        }

        internal enum sd_vae_format_t
        {
            SD_VAE_FORMAT_AUTO = -1,
            SD_VAE_FORMAT_FLUX,
            SD_VAE_FORMAT_SD3,
            SD_VAE_FORMAT_FLUX2,
            SD_VAE_FORMAT_WAN,
            SD_VAE_FORMAT_COUNT
        }

        internal enum sd_cache_mode_t
        {
            SD_CACHE_DISABLED = 0,
            SD_CACHE_EASYCACHE,
            SD_CACHE_UCACHE,
            SD_CACHE_DBCACHE,
            SD_CACHE_TAYLORSEER,
            SD_CACHE_CACHE_DIT,
            SD_CACHE_SPECTRUM,
        }

        internal enum sd_hires_upscaler_t
        {
            SD_HIRES_UPSCALER_NONE,
            SD_HIRES_UPSCALER_LATENT,
            SD_HIRES_UPSCALER_LATENT_NEAREST,
            SD_HIRES_UPSCALER_LATENT_NEAREST_EXACT,
            SD_HIRES_UPSCALER_LATENT_ANTIALIASED,
            SD_HIRES_UPSCALER_LATENT_BICUBIC,
            SD_HIRES_UPSCALER_LATENT_BICUBIC_ANTIALIASED,
            SD_HIRES_UPSCALER_LANCZOS,
            SD_HIRES_UPSCALER_NEAREST,
            SD_HIRES_UPSCALER_MODEL,
            SD_HIRES_UPSCALER_COUNT,
        }

        internal enum sd_cancel_mode_t
        {
            SD_CANCEL_ALL,
            SD_CANCEL_NEW_LATENTS,
            SD_CANCEL_RESET
        }

        internal enum rng_type_t
        {
            STD_DEFAULT_RNG,
            CUDA_RNG,
            CPU_RNG,
            RNG_TYPE_COUNT
        }


        // ============================================================
        // Structs
        // ============================================================

        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_tiling_params_t
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool enabled;

            [MarshalAs(UnmanagedType.I1)]
            public bool temporal_tiling;

            public int tile_size_x;
            public int tile_size_y;
            public float target_overlap;
            public float rel_size_x;
            public float rel_size_y;

            public byte* extra_tiling_args;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_embedding_t
        {
            public byte* name;
            public byte* path;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_ctx_params_t
        {
            public byte* model_path;
            public byte* clip_l_path;
            public byte* clip_g_path;
            public byte* clip_vision_path;
            public byte* t5xxl_path;
            public byte* llm_path;
            public byte* llm_vision_path;
            public byte* diffusion_model_path;
            public byte* high_noise_diffusion_model_path;
            public byte* uncond_diffusion_model_path;
            public byte* embeddings_connectors_path;
            public byte* vae_path;
            public byte* audio_vae_path;
            public byte* taesd_path;
            public byte* control_net_path;
            public byte* ip_adapter_path;
            public byte* motion_module_path;
            public sd_embedding_t* embeddings;
            public uint embedding_count;
            public byte* photo_maker_path;
            public byte* pulid_weights_path;
            public byte* tensor_type_rules;
            public int n_threads;
            public sd_type_t wtype;
            public rng_type_t rng_type;
            public rng_type_t sampler_rng_type;
            public prediction_t prediction;
            public lora_apply_mode_t lora_apply_mode;

            [MarshalAs(UnmanagedType.I1)]
            public bool enable_mmap;

            [MarshalAs(UnmanagedType.I1)]
            public bool flash_attn;

            [MarshalAs(UnmanagedType.I1)]
            public bool diffusion_flash_attn;

            [MarshalAs(UnmanagedType.I1)]
            public bool tae_preview_only;

            [MarshalAs(UnmanagedType.I1)]
            public bool diffusion_conv_direct;

            [MarshalAs(UnmanagedType.I1)]
            public bool vae_conv_direct;

            [MarshalAs(UnmanagedType.I1)]
            public bool force_sdxl_vae_conv_scale;

            public sd_vae_format_t vae_format;
            public byte* max_vram;

            [MarshalAs(UnmanagedType.I1)]
            public bool stream_layers;

            [MarshalAs(UnmanagedType.I1)]
            public bool eager_load;

            public byte* backend;
            public byte* params_backend;
            public byte* split_mode;

            [MarshalAs(UnmanagedType.I1)]
            public bool auto_fit;

            public byte* rpc_servers;
            public byte* model_args;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_audio_t
        {
            public uint sample_rate;
            public uint channels;
            public ulong sample_count;
            public float* data;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_image_t
        {
            public uint width;
            public uint height;
            public uint channel;
            public byte* data;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_ref_video_t
        {
            public sd_image_t* frames;
            public int frame_count;
            public int fps;
            public sd_audio_t audio;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_slg_params_t
        {
            public int* layers;
            public nuint layer_count;
            public float layer_start;
            public float layer_end;
            public float scale;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_guidance_params_t
        {
            public float txt_cfg;
            public float img_cfg;
            public float distilled_guidance;
            public sd_slg_params_t slg;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_sample_params_t
        {
            public sd_guidance_params_t guidance;
            public scheduler_t scheduler;
            public sample_method_t sample_method;
            public int sample_steps;
            public float eta;
            public int shifted_timestep;
            public float* custom_sigmas;
            public int custom_sigmas_count;
            public float flow_shift;
            public byte* extra_sample_args;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_pm_params_t
        {
            public sd_image_t* id_images;
            public int id_images_count;
            public byte* id_embed_path;
            public float style_strength;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_pulid_params_t
        {
            public byte* id_embedding_path;
            public float id_weight;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_cache_params_t
        {
            public sd_cache_mode_t mode;
            public float reuse_threshold;
            public float start_percent;
            public float end_percent;
            public float error_decay_rate;

            [MarshalAs(UnmanagedType.I1)]
            public bool use_relative_threshold;

            [MarshalAs(UnmanagedType.I1)]
            public bool reset_error_on_compute;

            public int Fn_compute_blocks;
            public int Bn_compute_blocks;
            public float residual_diff_threshold;
            public int max_warmup_steps;
            public int max_cached_steps;
            public int max_continuous_cached_steps;
            public int taylorseer_n_derivatives;
            public int taylorseer_skip_interval;
            public byte* scm_mask;

            [MarshalAs(UnmanagedType.I1)]
            public bool scm_policy_dynamic;

            public float spectrum_w;
            public int spectrum_m;
            public float spectrum_lam;
            public int spectrum_window_size;
            public float spectrum_flex_window;
            public int spectrum_warmup_steps;
            public float spectrum_stop_percent;
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
        internal struct sd_hires_params_t
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool enabled;

            public sd_hires_upscaler_t upscaler;
            public byte* model_path;
            public float scale;
            public int target_width;
            public int target_height;
            public int steps;
            public float denoising_strength;
            public int upscale_tile_size;
            public float* custom_sigmas;
            public int custom_sigmas_count;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_img_gen_params_t
        {
            public sd_lora_t* loras;
            public uint lora_count;
            public byte* prompt;
            public byte* negative_prompt;
            public int clip_skip;
            public sd_image_t init_image;
            public sd_image_t* ref_images;
            public int ref_images_count;
            public byte* ref_image_args;
            public sd_image_t mask_image;
            public int width;
            public int height;
            public sd_sample_params_t sample_params;
            public float strength;
            public long seed;
            public int batch_count;
            public sd_image_t control_image;
            public float control_strength;
            public sd_image_t ip_adapter_image;
            public float ip_adapter_strength;
            public sd_pm_params_t pm_params;
            public sd_pulid_params_t pulid_params;
            public sd_tiling_params_t vae_tiling_params;
            public sd_cache_params_t cache;
            public sd_hires_params_t hires;
            public int qwen_image_layers;

            [MarshalAs(UnmanagedType.I1)]
            public bool circular_x;

            [MarshalAs(UnmanagedType.I1)]
            public bool circular_y;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_vid_gen_params_t
        {
            public sd_lora_t* loras;
            public uint lora_count;
            public byte* prompt;
            public byte* negative_prompt;
            public int clip_skip;
            public sd_image_t init_image;
            public sd_image_t end_image;
            public sd_image_t* ref_images;
            public int ref_images_count;
            public sd_ref_video_t* ref_videos;
            public int ref_videos_count;
            public sd_audio_t* ref_audios;
            public int ref_audios_count;
            public sd_image_t* control_frames;
            public int control_frames_size;
            public int width;
            public int height;
            public sd_sample_params_t sample_params;
            public sd_sample_params_t high_noise_sample_params;
            public float moe_boundary;
            public float strength;
            public long seed;
            public int video_frames;
            public int fps;
            public float vace_strength;
            public sd_tiling_params_t vae_tiling_params;
            public sd_cache_params_t cache;
            public sd_hires_params_t hires;

            [MarshalAs(UnmanagedType.I1)]
            public bool circular_x;

            [MarshalAs(UnmanagedType.I1)]
            public bool circular_y;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct sd_adetailer_params_t
        {
            public byte* prompt;
            public byte* negative_prompt;
            public byte* extra_ad_args;
        }


        // ============================================================
        // Opaque native types
        // ============================================================
        internal struct sd_ctx_t { }
        internal struct upscaler_ctx_t { }
        internal struct adetailer_ctx_t { }
        internal struct ggml_tensor { }


        // ============================================================
        // Callback delegates
        // ============================================================

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_log_cb_t(sd_log_level_t level, byte* text, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_progress_cb_t(int step, int steps, float time, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void sd_preview_cb_t(int step, int frame_count, sd_image_t* frames, [MarshalAs(UnmanagedType.I1)] bool is_noisy, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal delegate bool sd_graph_eval_callback_t(ggml_tensor* t, [MarshalAs(UnmanagedType.I1)] bool ask, void* user_data);


        // ============================================================
        // API
        // ============================================================

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_log_callback))]
        internal static partial void sd_set_log_callback(sd_log_cb_t sd_log_cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_progress_callback))]
        internal static partial void sd_set_progress_callback(sd_progress_cb_t cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_preview_callback))]
        internal static partial void sd_set_preview_callback(sd_preview_cb_t cb, preview_t mode, int interval, [MarshalAs(UnmanagedType.I1)] bool denoised, [MarshalAs(UnmanagedType.I1)] bool noisy, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_set_backend_eval_callback))]
        internal static partial void sd_set_backend_eval_callback(sd_graph_eval_callback_t cb, void* data);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_num_physical_cores))]
        internal static partial int sd_get_num_physical_cores();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_system_info))]
        internal static partial byte* sd_get_system_info();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_supports_image_generation))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_supports_image_generation(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_supports_video_generation))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_supports_video_generation(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_load_control_net))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_load_control_net(sd_ctx_t* sd_ctx, byte* path);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_unload_control_net))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_unload_control_net(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_has_control_net))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool sd_ctx_has_control_net(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_type_name))]
        internal static partial byte* sd_type_name(sd_type_t type);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_sd_type))]
        internal static partial sd_type_t str_to_sd_type(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_rng_type_name))]
        internal static partial byte* sd_rng_type_name(rng_type_t rng_type);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_rng_type))]
        internal static partial rng_type_t str_to_rng_type(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_sample_method_name))]
        internal static partial byte* sd_sample_method_name(sample_method_t sample_method);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_sample_method))]
        internal static partial sample_method_t str_to_sample_method(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_scheduler_name))]
        internal static partial byte* sd_scheduler_name(scheduler_t scheduler);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_scheduler))]
        internal static partial scheduler_t str_to_scheduler(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_prediction_name))]
        internal static partial byte* sd_prediction_name(prediction_t prediction);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_prediction))]
        internal static partial prediction_t str_to_prediction(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_preview_name))]
        internal static partial byte* sd_preview_name(preview_t preview);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_preview))]
        internal static partial preview_t str_to_preview(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_lora_apply_mode_name))]
        internal static partial byte* sd_lora_apply_mode_name(lora_apply_mode_t mode);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_lora_apply_mode))]
        internal static partial lora_apply_mode_t str_to_lora_apply_mode(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_hires_upscaler_name))]
        internal static partial byte* sd_hires_upscaler_name(sd_hires_upscaler_t upscaler);

        [LibraryImport(LibraryName, EntryPoint = nameof(str_to_sd_hires_upscaler))]
        internal static partial sd_hires_upscaler_t str_to_sd_hires_upscaler(byte* str);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_cache_params_init))]
        internal static partial void sd_cache_params_init(sd_cache_params_t* cache_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_hires_params_init))]
        internal static partial void sd_hires_params_init(sd_hires_params_t* hires_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_params_init))]
        internal static partial void sd_ctx_params_init(sd_ctx_params_t* sd_ctx_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_ctx_params_to_str))]
        internal static partial byte* sd_ctx_params_to_str(sd_ctx_params_t* sd_ctx_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(new_sd_ctx))]
        internal static partial sd_ctx_t* new_sd_ctx(sd_ctx_params_t* sd_ctx_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(free_sd_ctx))]
        internal static partial void free_sd_ctx(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(free_sd_audio))]
        internal static partial void free_sd_audio(sd_audio_t* audio);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_sample_params_init))]
        internal static partial void sd_sample_params_init(sd_sample_params_t* sample_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_sample_params_to_str))]
        internal static partial byte* sd_sample_params_to_str(sd_sample_params_t* sample_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_default_sample_method))]
        internal static partial sample_method_t sd_get_default_sample_method(sd_ctx_t* sd_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_get_default_scheduler))]
        internal static partial scheduler_t sd_get_default_scheduler(sd_ctx_t* sd_ctx, sample_method_t sample_method);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_img_gen_params_init))]
        internal static partial void sd_img_gen_params_init(sd_img_gen_params_t* sd_img_gen_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_img_gen_params_to_str))]
        internal static partial byte* sd_img_gen_params_to_str(sd_img_gen_params_t* sd_img_gen_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(generate_image))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool generate_image(sd_ctx_t* sd_ctx, sd_img_gen_params_t* sd_img_gen_params, out sd_image_t* images_out, out int num_images_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_cancel_generation))]
        internal static partial void sd_cancel_generation(sd_ctx_t* sd_ctx, sd_cancel_mode_t mode);

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_vid_gen_params_init))]
        internal static partial void sd_vid_gen_params_init(sd_vid_gen_params_t* sd_vid_gen_params);

        [LibraryImport(LibraryName, EntryPoint = nameof(generate_video))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool generate_video(sd_ctx_t* sd_ctx, sd_vid_gen_params_t* sd_vid_gen_params, out sd_image_t* frames_out, out int num_frames_out, out sd_audio_t* audio_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(new_upscaler_ctx))]
        internal static partial upscaler_ctx_t* new_upscaler_ctx(byte* esrgan_path, [MarshalAs(UnmanagedType.I1)] bool direct, int n_threads, int tile_size, byte* backend, byte* params_backend);

        [LibraryImport(LibraryName, EntryPoint = nameof(free_upscaler_ctx))]
        internal static partial void free_upscaler_ctx(upscaler_ctx_t* upscaler_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(upscale))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool upscale(upscaler_ctx_t* upscaler_ctx, sd_image_t input_image, uint upscale_factor, out sd_image_t* images_out, out int num_images_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(get_upscale_factor))]
        internal static partial int get_upscale_factor(upscaler_ctx_t* upscaler_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(new_adetailer_ctx))]
        internal static partial adetailer_ctx_t* new_adetailer_ctx(byte* detector_path, int n_threads, byte* backend, byte* params_backend);

        [LibraryImport(LibraryName, EntryPoint = nameof(free_adetailer_ctx))]
        internal static partial void free_adetailer_ctx(adetailer_ctx_t* adetailer_ctx);

        [LibraryImport(LibraryName, EntryPoint = nameof(adetail_image))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool adetail_image(adetailer_ctx_t* adetailer_ctx, sd_ctx_t* sd_ctx, sd_image_t input_image, sd_adetailer_params_t* adetailer_params, sd_img_gen_params_t* inpaint_params, out sd_image_t* images_out, out int num_images_out);

        [LibraryImport(LibraryName, EntryPoint = nameof(convert))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool convert(byte* input_path, byte* vae_path, byte* output_path, sd_type_t output_type, byte* tensor_type_rules, [MarshalAs(UnmanagedType.I1)] bool convert_name);

        [LibraryImport(LibraryName, EntryPoint = nameof(convert_with_components))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool convert_with_components(byte* model_path, byte* clip_l_path, byte* clip_g_path, byte* t5xxl_path, byte* diffusion_model_path, byte* vae_path, byte* output_path, sd_type_t output_type, byte* tensor_type_rules, [MarshalAs(UnmanagedType.I1)] bool convert_name, int n_threads);

        [LibraryImport(LibraryName, EntryPoint = nameof(preprocess_canny))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool preprocess_canny(sd_image_t image, float high_threshold, float low_threshold, float weak, float strong, [MarshalAs(UnmanagedType.I1)] bool inverse);

        [LibraryImport(LibraryName, EntryPoint = nameof(load_imatrix))]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static partial bool load_imatrix(byte* imatrix_path);

        [LibraryImport(LibraryName, EntryPoint = nameof(save_imatrix))]
        internal static partial void save_imatrix(byte* imatrix_path);

        [LibraryImport(LibraryName, EntryPoint = nameof(enable_imatrix_collection))]
        internal static partial void enable_imatrix_collection();

        [LibraryImport(LibraryName, EntryPoint = nameof(disable_imatrix_collection))]
        internal static partial void disable_imatrix_collection();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_commit))]
        internal static partial byte* sd_commit();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_version))]
        internal static partial byte* sd_version();

        [LibraryImport(LibraryName, EntryPoint = nameof(sd_list_devices))]
        internal static partial nuint sd_list_devices(byte* buffer, nuint buffer_size);

        [LibraryImport(LibraryName, EntryPoint = nameof(free_sd_images))]
        internal static partial void free_sd_images(sd_image_t* result_images, int num_images);
    }
}