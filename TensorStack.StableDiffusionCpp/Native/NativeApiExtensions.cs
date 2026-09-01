using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe static class NativeApiExtensions
    {
        #region ContextOptions

        internal static ContextOptions ToManaged(this NativeApi.sd_ctx_params_t unmanaged)
        {
            return new ContextOptions
            {
                ModelPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.model_path),
                ClipLPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.clip_l_path),
                ClipGPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.clip_g_path),
                ClipVisionPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.clip_vision_path),
                T5xxlPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.t5xxl_path),
                LlmPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.llm_path),
                LlmVisionPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.llm_vision_path),
                DiffusionModelPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.diffusion_model_path),
                HighNoiseDiffusionModelPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.high_noise_diffusion_model_path),
                UncondDiffusionModelPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.uncond_diffusion_model_path),
                EmbeddingsConnectorsPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.embeddings_connectors_path),
                VaePath = AnsiStringMarshaller.ConvertToManaged(unmanaged.vae_path),
                AudioVaePath = AnsiStringMarshaller.ConvertToManaged(unmanaged.audio_vae_path),
                TaesdPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.taesd_path),
                ControlNetPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.control_net_path),
                IpAdapterPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.ip_adapter_path),
                MotionModulePath = AnsiStringMarshaller.ConvertToManaged(unmanaged.motion_module_path),
                // Embeddings = "",
                PhotoMakerPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.photo_maker_path),
                PulidWeightsPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.pulid_weights_path),
                TensorTypeRules = AnsiStringMarshaller.ConvertToManaged(unmanaged.tensor_type_rules),
                Threads = unmanaged.n_threads,
                DataType = unmanaged.wtype.ToManaged(),
                RngType = unmanaged.rng_type.ToManaged(),
                SamplerRngType = unmanaged.sampler_rng_type.ToManaged(),
                Prediction = unmanaged.prediction.ToManaged(),
                LoraApplyMode = unmanaged.lora_apply_mode.ToManaged(),
                EnableMmap = unmanaged.enable_mmap,
                FlashAttn = unmanaged.flash_attn,
                DiffusionFlashAttn = unmanaged.diffusion_flash_attn,
                TaePreviewOnly = unmanaged.tae_preview_only,
                DiffusionConvDirect = unmanaged.diffusion_conv_direct,
                VaeConvDirect = unmanaged.vae_conv_direct,
                ForceSdxlVaeConvScale = unmanaged.force_sdxl_vae_conv_scale,
                VaeFormat = unmanaged.vae_format.ToManaged(),
                MaxVram = AnsiStringMarshaller.ConvertToManaged(unmanaged.max_vram),
                StreamLayers = unmanaged.stream_layers,
                EagerLoad = unmanaged.eager_load,
                Backend = AnsiStringMarshaller.ConvertToManaged(unmanaged.backend),
                ParamsBackend = AnsiStringMarshaller.ConvertToManaged(unmanaged.params_backend),
                SplitMode = AnsiStringMarshaller.ConvertToManaged(unmanaged.split_mode),
                AutoFit = unmanaged.auto_fit,
                RpcServers = AnsiStringMarshaller.ConvertToManaged(unmanaged.rpc_servers),
                ModelArgs = AnsiStringMarshaller.ConvertToManaged(unmanaged.model_args),
            };
        }


        internal static NativeApi.sd_ctx_params_t ToUnmanaged(this ContextOptions managed)
        {
            NativeApi.sd_embedding_t* embeddings = null;
            if (managed.Embeddings is { Length: > 0 })
            {
                embeddings = (NativeApi.sd_embedding_t*)NativeMemory.Alloc((nuint)managed.Embeddings.Length, (nuint)sizeof(NativeApi.sd_embedding_t));
                for (int i = 0; i < managed.Embeddings.Length; i++)
                {
                    embeddings[i] = managed.Embeddings[i].ToUnmanaged();
                }
            }

            return new NativeApi.sd_ctx_params_t
            {
                model_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.ModelPath),
                clip_l_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.ClipLPath),
                clip_g_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.ClipGPath),
                clip_vision_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.ClipVisionPath),
                t5xxl_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.T5xxlPath),
                llm_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.LlmPath),
                llm_vision_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.LlmVisionPath),
                diffusion_model_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.DiffusionModelPath),
                high_noise_diffusion_model_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.HighNoiseDiffusionModelPath),
                uncond_diffusion_model_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.UncondDiffusionModelPath),
                embeddings_connectors_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.EmbeddingsConnectorsPath),
                vae_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.VaePath),
                audio_vae_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.AudioVaePath),
                taesd_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.TaesdPath),
                control_net_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.ControlNetPath),
                ip_adapter_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.IpAdapterPath),
                motion_module_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.MotionModulePath),
                embeddings = embeddings,
                embedding_count = (uint)(managed.Embeddings?.Length ?? 0),
                photo_maker_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.PhotoMakerPath),
                pulid_weights_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.PulidWeightsPath),
                tensor_type_rules = AnsiStringMarshaller.ConvertToUnmanaged(managed.TensorTypeRules),
                n_threads = managed.Threads,
                wtype = managed.DataType.ToUnmanaged(),
                rng_type = managed.RngType.ToUnmanaged(),
                sampler_rng_type = managed.SamplerRngType.ToUnmanaged(),
                prediction = managed.Prediction.ToUnmanaged(),
                lora_apply_mode = managed.LoraApplyMode.ToUnmanaged(),
                enable_mmap = managed.EnableMmap,
                flash_attn = managed.FlashAttn,
                diffusion_flash_attn = managed.DiffusionFlashAttn,
                tae_preview_only = managed.TaePreviewOnly,
                diffusion_conv_direct = managed.DiffusionConvDirect,
                vae_conv_direct = managed.VaeConvDirect,
                force_sdxl_vae_conv_scale = managed.ForceSdxlVaeConvScale,
                vae_format = managed.VaeFormat.ToUnmanaged(),
                max_vram = AnsiStringMarshaller.ConvertToUnmanaged(managed.MaxVram),
                stream_layers = managed.StreamLayers,
                eager_load = managed.EagerLoad,
                backend = AnsiStringMarshaller.ConvertToUnmanaged(managed.Backend),
                params_backend = AnsiStringMarshaller.ConvertToUnmanaged(managed.ParamsBackend),
                split_mode = AnsiStringMarshaller.ConvertToUnmanaged(managed.SplitMode),
                auto_fit = managed.AutoFit,
                rpc_servers = AnsiStringMarshaller.ConvertToUnmanaged(managed.RpcServers),
                model_args = AnsiStringMarshaller.ConvertToUnmanaged(managed.ModelArgs)
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_ctx_params_t native)
        {
            AnsiStringMarshaller.Free(native.model_path);
            AnsiStringMarshaller.Free(native.clip_l_path);
            AnsiStringMarshaller.Free(native.clip_g_path);
            AnsiStringMarshaller.Free(native.clip_vision_path);
            AnsiStringMarshaller.Free(native.t5xxl_path);
            AnsiStringMarshaller.Free(native.llm_path);
            AnsiStringMarshaller.Free(native.llm_vision_path);
            AnsiStringMarshaller.Free(native.diffusion_model_path);
            AnsiStringMarshaller.Free(native.high_noise_diffusion_model_path);
            AnsiStringMarshaller.Free(native.uncond_diffusion_model_path);
            AnsiStringMarshaller.Free(native.embeddings_connectors_path);
            AnsiStringMarshaller.Free(native.vae_path);
            AnsiStringMarshaller.Free(native.audio_vae_path);
            AnsiStringMarshaller.Free(native.taesd_path);
            AnsiStringMarshaller.Free(native.control_net_path);
            AnsiStringMarshaller.Free(native.ip_adapter_path);
            AnsiStringMarshaller.Free(native.motion_module_path);
            AnsiStringMarshaller.Free(native.photo_maker_path);
            AnsiStringMarshaller.Free(native.pulid_weights_path);
            AnsiStringMarshaller.Free(native.tensor_type_rules);
            AnsiStringMarshaller.Free(native.max_vram);
            AnsiStringMarshaller.Free(native.backend);
            AnsiStringMarshaller.Free(native.params_backend);
            AnsiStringMarshaller.Free(native.split_mode);
            AnsiStringMarshaller.Free(native.rpc_servers);
            AnsiStringMarshaller.Free(native.model_args);
            if (native.embeddings != null)
            {
                for (uint i = 0; i < native.embedding_count; i++)
                {
                    native.embeddings[i].FreeUnmanaged();
                }
                NativeMemory.Free(native.embeddings);
            }

        }

        #endregion

        #region SamplerOptions

        internal static SamplerOptions ToManaged(this NativeApi.sd_sample_params_t unmanaged)
        {
            var unmangedGuidance = unmanaged.guidance;
            var unmangedGuidanceSlg = unmanaged.guidance.slg;

            int[] layers = [];
            if (unmangedGuidanceSlg.layer_count > 0)
                layers = new ReadOnlySpan<int>(unmangedGuidanceSlg.layers, checked((int)unmangedGuidanceSlg.layer_count)).ToArray();

            return new SamplerOptions
            {
                Scheduler = unmanaged.scheduler.ToManaged(),
                SampleMethod = unmanaged.sample_method.ToManaged(),
                SampleSteps = unmanaged.sample_steps,
                Eta = unmanaged.eta,
                ShiftedTimestep = unmanaged.shifted_timestep,
                CustomSigmas = [],
                FlowShift = unmanaged.flow_shift,
                ExtraSampleArgs = AnsiStringMarshaller.ConvertToManaged(unmanaged.extra_sample_args),
                TxtCfg = unmangedGuidance.txt_cfg,
                ImgCfg = unmangedGuidance.img_cfg,
                DistilledGuidance = unmangedGuidance.distilled_guidance,
                SlgLayers = layers,
                SlgLayerEnd = unmangedGuidanceSlg.layer_end,
                SlgLayerStart = unmangedGuidanceSlg.layer_start,
                SlgScale = unmangedGuidanceSlg.scale,
            };
        }


        internal static unsafe NativeApi.sd_sample_params_t ToUnmanaged(this SamplerOptions managed)
        {
            float* customSigmas = null;
            if (!managed.CustomSigmas.IsNullOrEmpty())
            {
                customSigmas = (float*)NativeMemory.Alloc((nuint)managed.CustomSigmas.Length, sizeof(float));
                managed.CustomSigmas.AsSpan().CopyTo(new Span<float>(customSigmas, managed.CustomSigmas.Length));
            }

            int* layers = null;
            var layerCount = managed?.SlgLayers?.Length ?? 0;
            if (layerCount > 0)
            {
                layers = (int*)NativeMemory.Alloc((nuint)managed.SlgLayers.Length, sizeof(int));
                managed.SlgLayers.AsSpan().CopyTo(new Span<int>(layers, managed.SlgLayers.Length));
            }

            return new NativeApi.sd_sample_params_t
            {
                guidance = new NativeApi.sd_guidance_params_t
                {
                    txt_cfg = managed.TxtCfg,
                    img_cfg = managed.ImgCfg,
                    distilled_guidance = managed.DistilledGuidance,
                    slg = new NativeApi.sd_slg_params_t
                    {
                        layers = layers,
                        layer_count = (nuint)layerCount,
                        layer_start = managed.SlgLayerStart,
                        layer_end = managed.SlgLayerEnd,
                        scale = managed.SlgScale
                    }
                },
                scheduler = managed.Scheduler.ToUnmanaged(),
                sample_method = managed.SampleMethod.ToUnmanaged(),
                sample_steps = managed.SampleSteps,
                eta = managed.Eta,
                shifted_timestep = managed.ShiftedTimestep,
                custom_sigmas = customSigmas,
                custom_sigmas_count = managed.CustomSigmas?.Length ?? 0,
                flow_shift = managed.FlowShift,
                extra_sample_args = AnsiStringMarshaller.ConvertToUnmanaged(managed.ExtraSampleArgs)
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_sample_params_t native)
        {
            if (native.guidance.slg.layers != null)
            {
                NativeMemory.Free(native.guidance.slg.layers);
            }

            AnsiStringMarshaller.Free(native.extra_sample_args);
            if (native.custom_sigmas != null)
                NativeMemory.Free(native.custom_sigmas);
        }

        #endregion

        #region ImageGenerationParameters

        internal static GenerateImageOptions ToManaged(this NativeApi.sd_img_gen_params_t unmanaged)
        {
            return new GenerateImageOptions
            {
                Prompt = AnsiStringMarshaller.ConvertToManaged(unmanaged.prompt),
                NegativePrompt = AnsiStringMarshaller.ConvertToManaged(unmanaged.negative_prompt),
                ClipSkip = unmanaged.clip_skip,
                RefImageArgs = AnsiStringMarshaller.ConvertToManaged(unmanaged.ref_image_args),
                Width = unmanaged.width,
                Height = unmanaged.height,
                SampleParameters = unmanaged.sample_params.ToManaged(),
                Strength = unmanaged.strength,
                Seed = unmanaged.seed,
                BatchCount = unmanaged.batch_count,
                ControlStrength = unmanaged.control_strength,
                IpAdapterStrength = unmanaged.ip_adapter_strength,
                PmParameters = unmanaged.pm_params.ToManaged(),
                PulidParameters = unmanaged.pulid_params.ToManaged(),
                VaeTilingParameters = unmanaged.vae_tiling_params.ToManaged(),
                Cache = unmanaged.cache.ToManaged(),
                Hires = unmanaged.hires.ToManaged(),
                QwenImageLayers = unmanaged.qwen_image_layers,
                CircularX = unmanaged.circular_x,
                CircularY = unmanaged.circular_y,
                // Loras = "",
                // InitImage = "",
                // RefImages = "",
                // MaskImage = "",
                // ControlImage = "",
                // IpAdapterImage = "",
            };
        }


        internal static NativeApi.sd_img_gen_params_t ToUnmanaged(this GenerateImageOptions managed)
        {
            NativeApi.sd_lora_t* loras = null;
            if (managed.Loras is { Length: > 0 })
            {
                loras = (NativeApi.sd_lora_t*)NativeMemory.Alloc((nuint)managed.Loras.Length, (nuint)sizeof(NativeApi.sd_lora_t));
                for (int i = 0; i < managed.Loras.Length; i++)
                {
                    loras[i] = managed.Loras[i].ToUnmanaged();
                }
            }

            NativeApi.sd_image_t* refImages = null;
            if (managed.RefImages is { Length: > 0 })
            {
                refImages = (NativeApi.sd_image_t*)NativeMemory.Alloc((nuint)managed.RefImages.Length, (nuint)sizeof(NativeApi.sd_image_t));
                for (int i = 0; i < managed.RefImages.Length; i++)
                {
                    refImages[i] = managed.RefImages[i].ToUnmanaged();
                }
            }

            return new NativeApi.sd_img_gen_params_t
            {
                loras = loras,
                lora_count = (uint)(managed.Loras?.Length ?? 0),
                prompt = AnsiStringMarshaller.ConvertToUnmanaged(managed.Prompt),
                negative_prompt = AnsiStringMarshaller.ConvertToUnmanaged(managed.NegativePrompt),
                clip_skip = managed.ClipSkip,
                init_image = managed.InitImage != null ? managed.InitImage.ToUnmanaged() : default,
                ref_images = refImages,
                ref_images_count = managed.RefImages?.Length ?? 0,
                ref_image_args = AnsiStringMarshaller.ConvertToUnmanaged(managed.RefImageArgs),
                mask_image = managed.MaskImage != null ? managed.MaskImage.ToUnmanaged(1) : default,
                width = managed.Width,
                height = managed.Height,
                sample_params = managed.SampleParameters.ToUnmanaged(),
                strength = managed.Strength,
                seed = managed.Seed,
                batch_count = managed.BatchCount,
                control_image = managed.ControlImage != null ? managed.ControlImage.ToUnmanaged() : default,
                control_strength = managed.ControlStrength,
                ip_adapter_image = managed.IpAdapterImage != null ? managed.IpAdapterImage.ToUnmanaged() : default,
                ip_adapter_strength = managed.IpAdapterStrength,
                pm_params = managed.PmParameters.ToUnmanaged(),
                pulid_params = managed.PulidParameters.ToUnmanaged(),
                vae_tiling_params = managed.VaeTilingParameters.ToUnmanaged(),
                cache = managed.Cache.ToUnmanaged(),
                hires = managed.Hires.ToUnmanaged(),
                qwen_image_layers = managed.QwenImageLayers,
                circular_x = managed.CircularX,
                circular_y = managed.CircularY
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_img_gen_params_t native)
        {
            if (native.prompt != null)
                AnsiStringMarshaller.Free(native.prompt);
            if (native.negative_prompt != null)
                AnsiStringMarshaller.Free(native.negative_prompt);
            if (native.ref_image_args != null)
                AnsiStringMarshaller.Free(native.ref_image_args);

            if (native.loras != null)
            {
                for (uint i = 0; i < native.lora_count; i++)
                {
                    native.loras[i].FreeUnmanaged();
                }
                NativeMemory.Free(native.loras);
            }

            if (native.ref_images != null)
            {
                for (int i = 0; i < native.ref_images_count; i++)
                {
                    native.ref_images[i].FreeUnmanaged();
                }
                NativeMemory.Free(native.ref_images);
            }

            native.init_image.FreeUnmanaged();
            native.mask_image.FreeUnmanaged();
            native.sample_params.FreeUnmanaged();
            native.control_image.FreeUnmanaged();
            native.ip_adapter_image.FreeUnmanaged();
            native.pm_params.FreeUnmanaged();
            native.pulid_params.FreeUnmanaged();
            native.vae_tiling_params.FreeUnmanaged();
            native.cache.FreeUnmanaged();
            native.hires.FreeUnmanaged();
        }

        #endregion

        #region VideoGenerationParameters

        internal static GenerateVideoOptions ToManaged(this NativeApi.sd_vid_gen_params_t unmanaged)
        {
            return new GenerateVideoOptions
            {
                Prompt = AnsiStringMarshaller.ConvertToManaged(unmanaged.prompt),
                NegativePrompt = AnsiStringMarshaller.ConvertToManaged(unmanaged.negative_prompt),
                ClipSkip = unmanaged.clip_skip,
                Fps = unmanaged.fps,
                MoeBoundary = unmanaged.moe_boundary,
                VaceStrength = unmanaged.vace_strength,
                VideoFrames = unmanaged.video_frames,
                Width = unmanaged.width,
                Height = unmanaged.height,
                SampleParameters = unmanaged.sample_params.ToManaged(),
                HighNoiseSampleParameters = unmanaged.high_noise_sample_params.ToManaged(),
                Strength = unmanaged.strength,
                Seed = unmanaged.seed,
                VaeTilingParameters = unmanaged.vae_tiling_params.ToManaged(),
                Cache = unmanaged.cache.ToManaged(),
                Hires = unmanaged.hires.ToManaged(),
                CircularX = unmanaged.circular_x,
                CircularY = unmanaged.circular_y,
                //ControlFrames,
                //EndImage,
                //InitImage,
                //Loras,
                //RefAudios,
                //RefImages,
                //RefVideos
            };
        }


        internal static NativeApi.sd_vid_gen_params_t ToUnmanaged(this GenerateVideoOptions managed)
        {
            NativeApi.sd_lora_t* loras = null;
            if (managed.Loras is { Length: > 0 })
            {
                loras = (NativeApi.sd_lora_t*)NativeMemory.Alloc((nuint)managed.Loras.Length, (nuint)sizeof(NativeApi.sd_lora_t));
                for (int i = 0; i < managed.Loras.Length; i++)
                {
                    loras[i] = managed.Loras[i].ToUnmanaged();
                }
            }

            NativeApi.sd_image_t* refImages = null;
            if (managed.RefImages is { Length: > 0 })
            {
                refImages = (NativeApi.sd_image_t*)NativeMemory.Alloc((nuint)managed.RefImages.Length, (nuint)sizeof(NativeApi.sd_image_t));
                for (int i = 0; i < managed.RefImages.Length; i++)
                {
                    refImages[i] = managed.RefImages[i].ToUnmanaged();
                }
            }


            NativeApi.sd_ref_video_t* refVideos = null;
            if (managed.RefVideos is { Length: > 0 })
            {
                refVideos = (NativeApi.sd_ref_video_t*)NativeMemory.Alloc((nuint)managed.RefVideos.Length, (nuint)sizeof(NativeApi.sd_ref_video_t));
                for (int i = 0; i < managed.RefVideos.Length; i++)
                {
                    refVideos[i] = managed.RefVideos[i].ToUnmanaged();
                }
            }

            NativeApi.sd_audio_t* refAudios = null;
            if (managed.RefAudios is { Length: > 0 })
            {
                refAudios = (NativeApi.sd_audio_t*)NativeMemory.Alloc((nuint)managed.RefAudios.Length, (nuint)sizeof(NativeApi.sd_audio_t));
                for (int i = 0; i < managed.RefAudios.Length; i++)
                {
                    refAudios[i] = managed.RefAudios[i].ToUnmanaged();
                }
            }

            NativeApi.sd_image_t* controlFrames = null;
            if (managed.ControlFrames is { Length: > 0 })
            {
                controlFrames = (NativeApi.sd_image_t*)NativeMemory.Alloc((nuint)managed.ControlFrames.Length, (nuint)sizeof(NativeApi.sd_image_t));
                for (int i = 0; i < managed.ControlFrames.Length; i++)
                {
                    controlFrames[i] = managed.ControlFrames[i].ToUnmanaged();
                }
            }

            return new NativeApi.sd_vid_gen_params_t
            {
                loras = loras,
                lora_count = (uint)(managed.Loras?.Length ?? 0),
                prompt = AnsiStringMarshaller.ConvertToUnmanaged(managed.Prompt),
                negative_prompt = AnsiStringMarshaller.ConvertToUnmanaged(managed.NegativePrompt),
                clip_skip = managed.ClipSkip,
                init_image = managed.InitImage != null ? managed.InitImage.ToUnmanaged() : default,
                end_image = managed.EndImage != null ? managed.EndImage.ToUnmanaged() : default,
                ref_images = refImages,
                ref_images_count = managed.RefImages?.Length ?? 0,
                ref_videos = refVideos,
                ref_videos_count = managed.RefVideos?.Length ?? 0,
                ref_audios = refAudios,
                ref_audios_count = managed.RefAudios?.Length ?? 0,
                control_frames = controlFrames,
                control_frames_size = managed.ControlFrames?.Length ?? 0,
                width = managed.Width,
                height = managed.Height,
                sample_params = managed.SampleParameters.ToUnmanaged(),
                high_noise_sample_params = managed.HighNoiseSampleParameters.ToUnmanaged(),
                moe_boundary = managed.MoeBoundary,
                strength = managed.Strength,
                seed = managed.Seed,
                video_frames = managed.VideoFrames,
                fps = managed.Fps,
                vace_strength = managed.VaceStrength,
                vae_tiling_params = managed.VaeTilingParameters.ToUnmanaged(),
                cache = managed.Cache.ToUnmanaged(),
                hires = managed.Hires.ToUnmanaged(),
                circular_x = managed.CircularX,
                circular_y = managed.CircularY
            };
        }

        internal static void FreeUnmanaged(this NativeApi.sd_vid_gen_params_t native)
        {
            // LoRAs
            if (native.loras != null)
            {
                for (uint i = 0; i < native.lora_count; i++)
                {
                    native.loras[i].FreeUnmanaged();
                }

                NativeMemory.Free(native.loras);
            }

            AnsiStringMarshaller.Free(native.prompt);
            AnsiStringMarshaller.Free(native.negative_prompt);

            // Images
            native.init_image.FreeUnmanaged();
            native.end_image.FreeUnmanaged();

            if (native.ref_images != null)
            {
                for (int i = 0; i < native.ref_images_count; i++)
                {
                    native.ref_images[i].FreeUnmanaged();
                }

                NativeMemory.Free(native.ref_images);
            }

            // Videos
            if (native.ref_videos != null)
            {
                for (int i = 0; i < native.ref_videos_count; i++)
                {
                    native.ref_videos[i].FreeUnmanaged();
                }

                NativeMemory.Free(native.ref_videos);
            }

            // Audio
            if (native.ref_audios != null)
            {
                for (int i = 0; i < native.ref_audios_count; i++)
                {
                    native.ref_audios[i].FreeUnmanaged();
                }

                NativeMemory.Free(native.ref_audios);
            }

            // Control frames
            if (native.control_frames != null)
            {
                for (int i = 0; i < native.control_frames_size; i++)
                {
                    native.control_frames[i].FreeUnmanaged();
                }

                NativeMemory.Free(native.control_frames);
            }

            native.sample_params.FreeUnmanaged();
            native.high_noise_sample_params.FreeUnmanaged();
            native.vae_tiling_params.FreeUnmanaged();
            native.cache.FreeUnmanaged();
            native.hires.FreeUnmanaged();
        }

        #endregion

        #region LoraOptions

        internal static NativeApi.sd_lora_t ToUnmanaged(this LoraOptions managed)
        {
            return new NativeApi.sd_lora_t
            {
                is_high_noise = managed.IsHighNoise,
                multiplier = managed.Multiplier,
                path = AnsiStringMarshaller.ConvertToUnmanaged(managed.Path)
            };
        }

        internal static void FreeUnmanaged(this NativeApi.sd_lora_t native)
        {
            AnsiStringMarshaller.Free(native.path);
        }

        #endregion

        #region TilingOptions

        internal static TilingOptions ToManaged(this NativeApi.sd_tiling_params_t unmanaged)
        {
            return new TilingOptions
            {
                Enabled = unmanaged.enabled,
                TileSizeX = unmanaged.tile_size_x,
                TileSizeY = unmanaged.tile_size_y,
                TargetOverlap = unmanaged.target_overlap,
                RelSizeX = unmanaged.rel_size_x,
                RelSizeY = unmanaged.rel_size_y,
                TemporalTiling = unmanaged.temporal_tiling,
                ExtraTilingArgs = AnsiStringMarshaller.ConvertToManaged(unmanaged.extra_tiling_args)
            };
        }


        internal static NativeApi.sd_tiling_params_t ToUnmanaged(this TilingOptions managed)
        {
            return new NativeApi.sd_tiling_params_t
            {
                enabled = managed.Enabled,
                temporal_tiling = managed.TemporalTiling,
                tile_size_x = managed.TileSizeX,
                tile_size_y = managed.TileSizeY,
                target_overlap = managed.TargetOverlap,
                rel_size_x = managed.RelSizeX,
                rel_size_y = managed.RelSizeY,
                extra_tiling_args = AnsiStringMarshaller.ConvertToUnmanaged(managed.ExtraTilingArgs)
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_tiling_params_t native)
        {
            AnsiStringMarshaller.Free(native.extra_tiling_args);
        }

        #endregion

        #region HiresOptions

        internal static HiresOptions ToManaged(this NativeApi.sd_hires_params_t unmanaged)
        {
            return new HiresOptions
            {
                Enabled = unmanaged.enabled,
                Upscaler = unmanaged.upscaler.ToManaged(),
                DenoisingStrength = unmanaged.denoising_strength,
                ModelPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.model_path),
                Scale = unmanaged.scale,
                Steps = unmanaged.steps,
                TargetHeight = unmanaged.target_height,
                TargetWidth = unmanaged.target_width,
                UpscaleTileSize = unmanaged.upscale_tile_size,
                CustomSigmas = null
            };
        }


        internal static NativeApi.sd_hires_params_t ToUnmanaged(this HiresOptions managed)
        {
            float* customSigmas = null;
            if (!managed.CustomSigmas.IsNullOrEmpty())
            {
                customSigmas = (float*)NativeMemory.Alloc((nuint)managed.CustomSigmas.Length, sizeof(float));
                managed.CustomSigmas.AsSpan().CopyTo(new Span<float>(customSigmas, managed.CustomSigmas.Length));
            }

            return new NativeApi.sd_hires_params_t
            {
                enabled = managed.Enabled,
                upscaler = managed.Upscaler.ToUnmanaged(),
                model_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.ModelPath),
                scale = managed.Scale,
                target_width = managed.TargetWidth,
                target_height = managed.TargetHeight,
                steps = managed.Steps,
                denoising_strength = managed.DenoisingStrength,
                upscale_tile_size = managed.UpscaleTileSize,
                custom_sigmas = customSigmas,
                custom_sigmas_count = managed.CustomSigmas?.Length ?? 0
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_hires_params_t native)
        {
            AnsiStringMarshaller.Free(native.model_path);
            if (native.custom_sigmas != null)
                NativeMemory.Free(native.custom_sigmas);
        }

        #endregion

        #region AdetailerOptions

        internal static NativeApi.sd_adetailer_params_t ToUnmanaged(this AdetailerOptions managed)
        {
            return new NativeApi.sd_adetailer_params_t
            {
                prompt = AnsiStringMarshaller.ConvertToUnmanaged(managed.Prompt),
                negative_prompt = AnsiStringMarshaller.ConvertToUnmanaged(managed.NegativePrompt),
                extra_ad_args = AnsiStringMarshaller.ConvertToUnmanaged(managed.ExtraAdArgs)
            };
        }

        internal static void FreeUnmanaged(this NativeApi.sd_adetailer_params_t native)
        {
            AnsiStringMarshaller.Free(native.prompt);
            AnsiStringMarshaller.Free(native.negative_prompt);
            AnsiStringMarshaller.Free(native.extra_ad_args);
        }

        #endregion

        #region CacheOptions

        internal static CacheOptions ToManaged(this NativeApi.sd_cache_params_t unmanaged)
        {
            return new CacheOptions
            {
                Mode = unmanaged.mode.ToManaged(),
                BnComputeBlocks = unmanaged.Bn_compute_blocks,
                EndPercent = unmanaged.end_percent,
                ErrorDecayRate = unmanaged.error_decay_rate,
                FnComputeBlocks = unmanaged.Fn_compute_blocks,
                MaxCachedSteps = unmanaged.max_cached_steps,
                MaxContinuousCachedSteps = unmanaged.max_continuous_cached_steps,
                MaxWarmupSteps = unmanaged.max_warmup_steps,
                ResetErrorOnCompute = unmanaged.reset_error_on_compute,
                ResidualDiffThreshold = unmanaged.residual_diff_threshold,
                ReuseThreshold = unmanaged.reuse_threshold,
                ScmMask = AnsiStringMarshaller.ConvertToManaged(unmanaged.scm_mask),
                ScmPolicyDynamic = unmanaged.scm_policy_dynamic,
                SpectrumFlexWindow = unmanaged.spectrum_flex_window,
                SpectrumLam = unmanaged.spectrum_lam,
                SpectrumM = unmanaged.spectrum_m,
                SpectrumStopPercent = unmanaged.spectrum_stop_percent,
                SpectrumW = unmanaged.spectrum_w,
                SpectrumWarmupSteps = unmanaged.spectrum_warmup_steps,
                SpectrumWindowSize = unmanaged.spectrum_window_size,
                StartPercent = unmanaged.start_percent,
                TaylorseerNDerivatives = unmanaged.taylorseer_n_derivatives,
                TaylorseerSkipInterval = unmanaged.taylorseer_skip_interval,
                UseRelativeThreshold = unmanaged.use_relative_threshold,
            };
        }


        internal static NativeApi.sd_cache_params_t ToUnmanaged(this CacheOptions managed)
        {
            return new NativeApi.sd_cache_params_t
            {
                mode = managed.Mode.ToUnmanaged(),
                reuse_threshold = managed.ReuseThreshold,
                start_percent = managed.StartPercent,
                end_percent = managed.EndPercent,
                error_decay_rate = managed.ErrorDecayRate,

                use_relative_threshold = managed.UseRelativeThreshold,
                reset_error_on_compute = managed.ResetErrorOnCompute,

                Fn_compute_blocks = managed.FnComputeBlocks,
                Bn_compute_blocks = managed.BnComputeBlocks,
                residual_diff_threshold = managed.ResidualDiffThreshold,
                max_warmup_steps = managed.MaxWarmupSteps,
                max_cached_steps = managed.MaxCachedSteps,
                max_continuous_cached_steps = managed.MaxContinuousCachedSteps,
                taylorseer_n_derivatives = managed.TaylorseerNDerivatives,
                taylorseer_skip_interval = managed.TaylorseerSkipInterval,

                scm_mask = AnsiStringMarshaller.ConvertToUnmanaged(managed.ScmMask),

                scm_policy_dynamic = managed.ScmPolicyDynamic,

                spectrum_w = managed.SpectrumW,
                spectrum_m = managed.SpectrumM,
                spectrum_lam = managed.SpectrumLam,
                spectrum_window_size = managed.SpectrumWindowSize,
                spectrum_flex_window = managed.SpectrumFlexWindow,
                spectrum_warmup_steps = managed.SpectrumWarmupSteps,
                spectrum_stop_percent = managed.SpectrumStopPercent
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_cache_params_t native)
        {
            AnsiStringMarshaller.Free(native.scm_mask);
        }


        #endregion

        #region EmbeddingOptions

        internal static NativeApi.sd_embedding_t ToUnmanaged(this EmbeddingOptions managed)
        {
            return new NativeApi.sd_embedding_t
            {
                name = AnsiStringMarshaller.ConvertToUnmanaged(managed.Name),
                path = AnsiStringMarshaller.ConvertToUnmanaged(managed.Path)
            };
        }

        internal static void FreeUnmanaged(this NativeApi.sd_embedding_t native)
        {
            AnsiStringMarshaller.Free(native.name);
            AnsiStringMarshaller.Free(native.path);
        }

        #endregion

        #region PhotoMakerOptions

        internal static PhotoMakerOptions ToManaged(this NativeApi.sd_pm_params_t unmanaged)
        {
            return new PhotoMakerOptions
            {
                IdEmbedPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.id_embed_path),
                StyleStrength = unmanaged.style_strength
            };
        }


        internal static NativeApi.sd_pm_params_t ToUnmanaged(this PhotoMakerOptions managed)
        {
            NativeApi.sd_image_t* images = null;
            if (managed.IdImages is { Length: > 0 })
            {
                images = (NativeApi.sd_image_t*)NativeMemory.Alloc((nuint)managed.IdImages.Length, (nuint)sizeof(NativeApi.sd_image_t));
                for (int i = 0; i < managed.IdImages.Length; i++)
                {
                    images[i] = managed.IdImages[i].ToUnmanaged();
                }
            }

            return new NativeApi.sd_pm_params_t
            {
                id_images = images,
                id_images_count = managed.IdImages?.Length ?? 0,
                id_embed_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.IdEmbedPath),
                style_strength = managed.StyleStrength
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_pm_params_t native)
        {
            if (native.id_images != null)
            {
                for (int i = 0; i < native.id_images_count; i++)
                {
                    native.id_images[i].FreeUnmanaged();
                }
                NativeMemory.Free(native.id_images);
            }
            AnsiStringMarshaller.Free(native.id_embed_path);
        }

        #endregion

        #region PulidOptions

        internal static PulidOptions ToManaged(this NativeApi.sd_pulid_params_t unmanaged)
        {
            return new PulidOptions
            {
                IdWeight = unmanaged.id_weight,
                IdEmbeddingPath = AnsiStringMarshaller.ConvertToManaged(unmanaged.id_embedding_path),
            };
        }

        internal static NativeApi.sd_pulid_params_t ToUnmanaged(this PulidOptions managed)
        {
            return new NativeApi.sd_pulid_params_t
            {
                id_embedding_path = AnsiStringMarshaller.ConvertToUnmanaged(managed.IdEmbeddingPath),
                id_weight = managed.IdWeight
            };
        }

        internal static void FreeUnmanaged(this NativeApi.sd_pulid_params_t native)
        {
            AnsiStringMarshaller.Free(native.id_embedding_path);
        }

        #endregion

        #region VideoData

        internal static NativeApi.sd_ref_video_t ToUnmanaged(this VideoData managed)
        {
            NativeApi.sd_image_t* frames = null;
            if (managed.Frames is { Length: > 0 })
            {
                frames = (NativeApi.sd_image_t*)NativeMemory.Alloc((nuint)managed.Frames.Length, (nuint)sizeof(NativeApi.sd_image_t));
                for (int i = 0; i < managed.Frames.Length; i++)
                {
                    frames[i] = managed.Frames[i].ToUnmanaged();
                }
            }

            NativeApi.sd_audio_t audio = default;
            if (managed.Audio != null)
            {
                audio = managed.Audio.ToUnmanaged();
            }

            return new NativeApi.sd_ref_video_t
            {
                frames = frames,
                frame_count = managed.Frames?.Length ?? 0,
                fps = managed.Fps,
                audio = audio
            };
        }


        internal static void FreeUnmanaged(this NativeApi.sd_ref_video_t native)
        {
            if (native.frames != null)
            {
                for (int i = 0; i < native.frame_count; i++)
                {
                    native.frames[i].FreeUnmanaged();
                }
                NativeMemory.Free(native.frames);
            }

            native.audio.FreeUnmanaged();
            if (native.audio.data != null)
            {
                NativeMemory.Free(native.audio.data);
            }
        }

        #endregion

        #region ImageData

        internal static void FreeUnmanaged(this NativeApi.sd_image_t image)
        {
            if (image.data != null)
            {
                NativeMemory.Free(image.data);
            }
        }

        #endregion

        #region AudioData

        internal static void FreeUnmanaged(this NativeApi.sd_audio_t native)
        {
            if (native.data != null)
                NativeMemory.Free(native.data);
        }

        #endregion

        #region Tensors

        public static unsafe ImageTensor ToManaged(this NativeApi.sd_image_t image)
        {
            var width = checked((int)image.width);
            var height = checked((int)image.height);
            var channels = checked((int)image.channel);
            if (image.data == null)
                throw new ArgumentNullException(nameof(image.data));
            if (channels != 3 && channels != 4)
                throw new NotSupportedException($"Unsupported channel count: {channels}. Expected 3 (RGB) or 4 (RGBA).");

            var ptr = image.data;
            var rowBytes = width * channels;
            var tensor = new ImageTensor(height, width);
            for (int y = 0; y < height; y++)
            {
                byte* row = ptr + y * rowBytes;
                for (int x = 0; x < width; x++)
                {
                    byte* pixel = row + x * channels;
                    tensor[0, 0, y, x] = pixel[0].NormalizeToFloat(); //R
                    tensor[0, 1, y, x] = pixel[1].NormalizeToFloat(); //G
                    tensor[0, 2, y, x] = pixel[2].NormalizeToFloat(); //B
                    tensor[0, 3, y, x] = channels == 4 ? pixel[3].NormalizeToFloat() : 1.0f; //A
                }
            }
            return tensor;
        }


        public static unsafe NativeApi.sd_image_t ToUnmanaged(this ImageTensor tensor, int channels = 3)
        {
            var height = tensor.Dimensions[2];
            var width = tensor.Dimensions[3];
            var byteCount = checked(width * height * channels);
            var data = (byte*)NativeMemory.Alloc((nuint)byteCount);

            try
            {
                for (int y = 0; y < height; y++)
                {
                    var row = data + y * width * channels;
                    for (int x = 0; x < width; x++)
                    {
                        byte* pixel = row + x * channels;
                        switch (channels)
                        {
                            case 1:
                                pixel[0] = tensor[0, 0, y, x].DenormalizeToByte();
                                break;
                            case 3:
                                pixel[0] = tensor[0, 0, y, x].DenormalizeToByte(); // R
                                pixel[1] = tensor[0, 1, y, x].DenormalizeToByte(); // G
                                pixel[2] = tensor[0, 2, y, x].DenormalizeToByte(); // B
                                break;
                            case 4:
                                pixel[0] = tensor[0, 0, y, x].DenormalizeToByte(); // R
                                pixel[1] = tensor[0, 1, y, x].DenormalizeToByte(); // G
                                pixel[2] = tensor[0, 2, y, x].DenormalizeToByte(); // B
                                pixel[3] = tensor[0, 3, y, x].DenormalizeToByte(); // A
                                break;
                        }
                    }
                }
                return new NativeApi.sd_image_t
                {
                    width = (uint)width,
                    height = (uint)height,
                    channel = (uint)channels,
                    data = data
                };
            }
            catch
            {
                NativeMemory.Free(data);
                throw;
            }
        }


        public static unsafe AudioTensor ToManaged(this NativeApi.sd_audio_t audio)
        {
            var sampleRate = checked((int)audio.sample_rate);
            var channels = checked((int)audio.channels);
            var samples = checked((int)audio.sample_count);
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));
            if (samples <= 0)
                throw new ArgumentOutOfRangeException(nameof(samples));
            if (audio.data == null)
                throw new ArgumentNullException(nameof(audio.data));

            var tensor = new Tensor<float>([channels, samples]);
            var totalSamples = checked(channels * samples);
            for (int i = 0; i < totalSamples; i++)
            {
                tensor.Memory.Span[i] = audio.data[i];
            }
            return tensor.AsAudioTensor(sampleRate);
        }


        public static unsafe NativeApi.sd_audio_t ToUnmanaged(this AudioTensor audio)
        {
            var sampleRate = audio.SampleRate;
            var channels = audio.Channels;
            var samples = audio.Samples;
            if (channels <= 0)
                throw new InvalidOperationException("Audio has no channels.");
            if (samples <= 0)
                throw new InvalidOperationException("Audio has no samples.");

            var totalSamples = checked(channels * samples);
            var byteCount = checked((nuint)totalSamples * sizeof(float));
            var data = (float*)NativeMemory.Alloc(byteCount);

            try
            {
                for (int channel = 0; channel < channels; channel++)
                {
                    for (int sample = 0; sample < samples; sample++)
                    {
                        data[channel * samples + sample] = audio[channel, sample];
                    }
                }

                return new NativeApi.sd_audio_t
                {
                    sample_rate = (uint)sampleRate,
                    channels = (uint)channels,
                    sample_count = (ulong)samples,
                    data = data
                };
            }
            catch
            {
                NativeMemory.Free(data);
                throw;
            }
        }

        #endregion
    }
}