using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;

namespace TensorStack.StableDiffusionCpp.Native
{
    internal unsafe static class NativeApiExtensions
    {
        #region ContextOptions

        /// <summary>
        /// Creates the native context options, the caller owns the result.
        /// </summary>
        /// <param name="managed">The managed options.</param>
        internal static NativeOptions ToUnmanaged(this StableDiffusionOptions managed)
        {
            var options = NativeOptions.CreateContext();
            try
            {
                // Models
                options.SetString("model_path", managed.ModelPath);
                options.SetString("clip_l_path", managed.ClipLPath);
                options.SetString("clip_g_path", managed.ClipGPath);
                options.SetString("clip_vision_path", managed.ClipVisionPath);
                options.SetString("t5xxl_path", managed.T5xxlPath);
                options.SetString("llm_path", managed.LlmPath);
                options.SetString("llm_vision_path", managed.LlmVisionPath);
                options.SetString("diffusion_model_path", managed.DiffusionModelPath);
                options.SetString("high_noise_diffusion_model_path", managed.HighNoiseDiffusionModelPath);
                options.SetString("uncond_diffusion_model_path", managed.UncondDiffusionModelPath);
                options.SetString("embeddings_connectors_path", managed.EmbeddingsConnectorsPath);
                options.SetString("vae_path", managed.VaePath);
                options.SetString("audio_vae_path", managed.AudioVaePath);
                options.SetString("audio_encoder_path", managed.AudioEncoderPath);
                options.SetString("taesd_path", managed.TaesdPath);
                options.SetString("control_net_path", managed.ControlNetPath);
                options.SetString("ip_adapter_path", managed.IpAdapterPath);
                options.SetString("motion_module_path", managed.MotionModulePath);
                options.SetString("photo_maker_path", managed.PhotoMakerPath);
                options.SetString("pulid_weights_path", managed.PulidWeightsPath);
                options.SetString("tokenizer", managed.TokenizerPath);
                options.SetEmbeddings("embeddings", managed.Embeddings);

                // Backend
                options.SetString("backend", managed.Backend);
                options.SetString("params_backend", managed.ParamsBackend);
                options.SetString("split_mode", managed.SplitMode);
                options.SetString("rpc_servers", managed.RpcServers);
                options.SetString("max_vram", managed.MaxVram);
                options.SetInt("n_threads", managed.Threads, 1);
                options.SetBool("auto_fit", managed.AutoFit);
                options.SetBool("eager_load", managed.EagerLoad);
                options.SetBool("enable_mmap", managed.EnableMmap);
                options.SetBool("disable_prefetch", !managed.IsPrefetchEnabled);
                options.SetBool("disable_segmented_compute", !managed.IsSegmentedComputeEnabled);
                options.SetBool("batched_cfg", managed.IsBatchedCfgEnabled);
                options.SetBool("sage_attn", managed.SageAttn);
                options.SetString("flash_attn_mode", managed.FlashAttnMode);
                options.SetString("conv_direct_mode", managed.ConvDirectMode);
                options.SetInt("conditioning_cache_size", managed.ConditioningCacheSize, 0);

                // Model
                options.SetString("model_args", managed.ModelArgs);
                options.SetString("tensor_type_rules", managed.TensorTypeRules);
                options.SetString("wtype", managed.DataType.ToUnmanaged());
                options.SetString("rng_type", managed.RngType.ToUnmanaged());
                options.SetString("sampler_rng_type", managed.SamplerRngType.ToUnmanaged());
                options.SetString("prediction", managed.Prediction.ToUnmanaged());
                options.SetString("lora_apply_mode", managed.LoraApplyMode.ToUnmanaged());
                options.SetString("vae_format", managed.VaeFormat.ToUnmanaged());
                options.SetBool("tae_preview_only", managed.TaePreviewOnly);
                options.SetBool("force_sdxl_vae_conv_scale", managed.ForceSdxlVaeConvScale);
                if (managed.ScaleLinear >= 0)
                    options.SetFloat("linear_scale", managed.ScaleLinear);
                if (managed.ScaleAttn >= 0)
                    options.SetFloat("attn_scale", managed.ScaleAttn);

                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }

        #endregion

        #region ImageGenerationParameters

        /// <summary>
        /// Creates the native image generation options, the caller owns the result.
        /// Only values that differ from the defaults are sent, the rest keep the library default.
        /// </summary>
        /// <param name="managed">The managed options.</param>
        internal static NativeOptions ToUnmanaged(this GenerateImageOptions managed)
        {
            var defaults = new GenerateImageOptions();
            var options = NativeOptions.CreateImage();
            try
            {
                // Prompt
                options.SetString("prompt", managed.Prompt);
                options.SetString("negative_prompt", managed.NegativePrompt);
                options.SetInt("clip_skip", managed.ClipSkip, defaults.ClipSkip, int.MinValue);
                options.SetLoras("loras", managed.Loras);

                // Generation
                options.SetInt("width", managed.Width, defaults.Width, 1);
                options.SetInt("height", managed.Height, defaults.Height, 1);
                options.SetInt("seed", managed.Seed, defaults.Seed, long.MinValue);
                options.SetInt("batch_count", managed.BatchCount, defaults.BatchCount, 1);
                options.SetFloat("strength", managed.Strength, defaults.Strength);
                options.SetBool("circular_x", managed.CircularX, defaults.CircularX);
                options.SetBool("circular_y", managed.CircularY, defaults.CircularY);

                // Setting this at all is rejected by every model other than Qwen Image Layered
                options.SetInt("qwen_image_layers", managed.QwenImageLayers, defaults.QwenImageLayers, 0);

                // Images
                options.SetImage("init_image", managed.InitImage);
                options.SetImage("mask_image", managed.MaskImage, 1);
                options.SetImages("ref_images", managed.RefImages);
                options.SetString("ref_image_args", managed.RefImageArgs);
                options.SetString("image_preprocess.rules", managed.ImagePreprocess);

                // ControlNet
                options.SetImage("control_image", managed.ControlImage);
                options.SetFloat("control_strength", managed.ControlStrength, defaults.ControlStrength);
                if (managed.ControlNetType != defaults.ControlNetType)
                    options.SetString("control_type", managed.ControlNetType.ToUnmanaged());

                // IpAdapter
                options.SetImage("ip_adapter_image", managed.IpAdapterImage);
                options.SetFloat("ip_adapter_strength", managed.IpAdapterStrength, defaults.IpAdapterStrength);

                options.SetSampler("sample", managed.SampleParameters, defaults.SampleParameters, false);
                options.SetPhotoMaker(managed.PmParameters, defaults.PmParameters);
                options.SetPulid(managed.PulidParameters, defaults.PulidParameters);
                options.SetTiling(managed.VaeTilingParameters, defaults.VaeTilingParameters);
                options.SetCache(managed.Cache, defaults.Cache);
                options.SetHires(managed.Hires, defaults.Hires);
                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }

        #endregion

        #region VideoGenerationParameters

        /// <summary>
        /// Creates the native video generation options, the caller owns the result.
        /// Only values that differ from the defaults are sent, the rest keep the library default.
        /// </summary>
        /// <param name="managed">The managed options.</param>
        internal static NativeOptions ToUnmanaged(this GenerateVideoOptions managed)
        {
            var defaults = new GenerateVideoOptions();
            var options = NativeOptions.CreateVideo();
            try
            {
                // Prompt
                options.SetString("prompt", managed.Prompt);
                options.SetString("negative_prompt", managed.NegativePrompt);
                options.SetInt("clip_skip", managed.ClipSkip, defaults.ClipSkip, int.MinValue);
                options.SetLoras("loras", managed.Loras);

                // Generation
                options.SetInt("width", managed.Width, defaults.Width, 1);
                options.SetInt("height", managed.Height, defaults.Height, 1);
                options.SetInt("seed", managed.Seed, defaults.Seed, long.MinValue);
                options.SetInt("video_frames", managed.VideoFrames, defaults.VideoFrames, 1);
                options.SetInt("fps", managed.Fps, defaults.Fps, 1);
                options.SetFloat("strength", managed.Strength, defaults.Strength);
                options.SetFloat("moe_boundary", managed.MoeBoundary, defaults.MoeBoundary);
                options.SetFloat("vace_strength", managed.VaceStrength, defaults.VaceStrength);
                options.SetBool("circular_x", managed.CircularX, defaults.CircularX);
                options.SetBool("circular_y", managed.CircularY, defaults.CircularY);

                // Images
                options.SetImage("init_image", managed.InitImage);
                options.SetImage("end_image", managed.EndImage);
                options.SetImages("ref_images", managed.RefImages);
                options.SetString("ref_image_args", managed.RefImageArgs);
                options.SetString("image_preprocess.rules", managed.ImagePreprocess);

                // Video/Audio
                options.SetFrames("control_frames", managed.ControlFrames);
                options.SetVideos("ref_videos", managed.RefVideos);
                options.SetAudios("ref_audios", managed.RefAudios);

                options.SetSampler("sample", managed.SampleParameters, defaults.SampleParameters, false);
                options.SetSampler("high_noise_sample", managed.HighNoiseSampleParameters, defaults.HighNoiseSampleParameters, true);
                options.SetTiling(managed.VaeTilingParameters, defaults.VaeTilingParameters);
                options.SetCache(managed.Cache, defaults.Cache);
                options.SetHires(managed.Hires, defaults.Hires);
                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }

        #endregion

        #region UpscaleParameters

        /// <summary>
        /// Creates the native image upscaler load options, the caller owns the result.
        /// </summary>
        /// <param name="managed">The managed options.</param>
        internal static NativeOptions ToUnmanagedImageUpscaler(this UpscaleOptions managed)
        {
            var options = NativeOptions.CreateUpscaler();
            try
            {
                options.SetString("model_path", managed.ModelPath);
                options.SetString("backend", managed.Backend);
                options.SetString("params_backend", managed.ParamsBackend);
                options.SetInt("n_threads", managed.Threads, 1);
                options.SetInt("tile_size", managed.TileSize, 1);
                options.SetBool("conv_direct", !string.IsNullOrEmpty(managed.ConvDirectMode) && managed.ConvDirectMode.Equals("on"));
                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }


        /// <summary>
        /// Creates the native video upscaler load options, the caller owns the result.
        /// </summary>
        /// <param name="managed">The managed options.</param>
        internal static NativeOptions ToUnmanagedVideoUpscaler(this UpscaleOptions managed)
        {
            var options = NativeOptions.CreateVideoUpscaler();
            try
            {
                options.SetString("diffusion_model_path", managed.DiffusionModelPath);
                options.SetString("vae_path", managed.VaePath);
                options.SetString("backend", managed.Backend);
                options.SetString("params_backend", managed.ParamsBackend);
                options.SetString("flash_attn_mode", managed.FlashAttnMode);
                options.SetString("conv_direct_mode", managed.ConvDirectMode);
                options.SetInt("n_threads", managed.Threads, 1);
                options.SetInt("upscale_factor", managed.ScaleFactor, 1);
                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }


        /// <summary>
        /// Creates the native image upscale options, the caller owns the result.
        /// </summary>
        /// <param name="image">The RGB image, retained by the options.</param>
        internal static NativeOptions ToUnmanagedUpscale(NativeApi.sd_tensor_t* image)
        {
            var options = NativeOptions.CreateUpscale();
            try
            {
                options.SetTensor("image", image);
                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }


        /// <summary>
        /// Creates the native video upscale options, the caller owns the result.
        /// </summary>
        /// <param name="managed">The managed options.</param>
        /// <param name="frames">The RGB frames [C,W,H,T], retained by the options.</param>
        /// <param name="frameRate">The frame rate.</param>
        internal static NativeOptions ToUnmanaged(this UpscaleVideoOptions managed, NativeApi.sd_tensor_t* frames, int frameRate)
        {
            var options = NativeOptions.CreateVideoUpscale();
            try
            {
                options.SetTensor("frames", frames);
                options.SetInt("fps", frameRate, 1);
                options.SetInt("seed", managed.Seed);
                options.SetInt("window_size", managed.WindowSize, 1);
                options.SetInt("temporal_chunk_size", managed.TemporalChunkSize, 1);
                return options;
            }
            catch
            {
                options.Dispose();
                throw;
            }
        }

        #endregion

        #region SamplerOptions

        /// <summary>
        /// Sets the sampler options.
        /// The high-noise pass shares the base scheduler, sigmas, flow shift and timestep shift, the library has no separate keys for them.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="prefix">The key prefix, "sample" or "high_noise_sample".</param>
        /// <param name="managed">The managed options.</param>
        /// <param name="defaults">The default options.</param>
        /// <param name="isHighNoise">if set to <c>true</c> these are the high-noise options.</param>
        private static void SetSampler(this NativeOptions options, string prefix, SamplerOptions managed, SamplerOptions defaults, bool isHighNoise)
        {
            if (managed == null)
                return;

            if (managed.SampleMethod != defaults.SampleMethod)
                options.SetString($"{prefix}.method", managed.SampleMethod.ToUnmanaged());

            options.SetInt($"{prefix}.steps", managed.SampleSteps, defaults.SampleSteps, isHighNoise ? -1 : 1);
            options.SetFloat($"{prefix}.eta", managed.Eta, defaults.Eta);
            options.SetString($"{prefix}.args", managed.ExtraSampleArgs);
            if (!isHighNoise)
            {
                if (managed.Scheduler != defaults.Scheduler)
                    options.SetString($"{prefix}.scheduler", managed.Scheduler.ToUnmanaged());

                options.SetInt($"{prefix}.shifted_timestep", managed.ShiftedTimestep, defaults.ShiftedTimestep, 0);
                options.SetFloat($"{prefix}.flow_shift", managed.FlowShift, defaults.FlowShift);
                options.SetFloats($"{prefix}.sigmas", managed.CustomSigmas);
            }

            // Guidance
            options.SetFloat($"{prefix}.guidance.txt_cfg", managed.TxtCfg, defaults.TxtCfg);
            options.SetFloat($"{prefix}.guidance.img_cfg", managed.ImgCfg, defaults.ImgCfg);
            options.SetFloat($"{prefix}.guidance.distilled_guidance", managed.DistilledGuidance, defaults.DistilledGuidance);

            // Skip Layer Guidance
            options.SetInts($"{prefix}.guidance.slg.layers", managed.SlgLayers);
            options.SetFloat($"{prefix}.guidance.slg.layer_start", managed.SlgLayerStart, defaults.SlgLayerStart);
            options.SetFloat($"{prefix}.guidance.slg.layer_end", managed.SlgLayerEnd, defaults.SlgLayerEnd);
            options.SetFloat($"{prefix}.guidance.slg.scale", managed.SlgScale, defaults.SlgScale);
        }

        #endregion

        #region TilingOptions

        /// <summary>
        /// Sets the VAE tiling options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="managed">The managed options.</param>
        /// <param name="defaults">The default options.</param>
        private static void SetTiling(this NativeOptions options, TilingOptions managed, TilingOptions defaults)
        {
            if (managed == null)
                return;

            options.SetBool("vae_tiling.enabled", managed.Enabled, defaults.Enabled);
            options.SetBool("vae_tiling.temporal_tiling", managed.TemporalTiling, defaults.TemporalTiling);
            options.SetInt("vae_tiling.tile_size_w", managed.TileSizeW, defaults.TileSizeW, 0);
            options.SetInt("vae_tiling.tile_size_h", managed.TileSizeH, defaults.TileSizeH, 0);
            options.SetFloat("vae_tiling.target_overlap", managed.TargetOverlap, defaults.TargetOverlap);
            options.SetFloat("vae_tiling.rel_size_w", managed.RelSizeW, defaults.RelSizeW);
            options.SetFloat("vae_tiling.rel_size_h", managed.RelSizeH, defaults.RelSizeH);
            options.SetString("vae_tiling.extra_tiling_args", managed.ExtraTilingArgs);
        }

        #endregion

        #region HiresOptions

        /// <summary>
        /// Sets the hires options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="managed">The managed options.</param>
        /// <param name="defaults">The default options.</param>
        private static void SetHires(this NativeOptions options, HiresOptions managed, HiresOptions defaults)
        {
            if (managed == null)
                return;

            options.SetBool("hires.enabled", managed.Enabled, defaults.Enabled);
            if (managed.Upscaler != defaults.Upscaler)
                options.SetString("hires.upscaler", managed.Upscaler.ToUnmanaged());

            options.SetString("hires.model_path", managed.ModelPath);
            options.SetFloat("hires.scale", managed.Scale, defaults.Scale);
            options.SetInt("hires.target_width", managed.TargetWidth, defaults.TargetWidth, 0);
            options.SetInt("hires.target_height", managed.TargetHeight, defaults.TargetHeight, 0);
            options.SetInt("hires.steps", managed.Steps, defaults.Steps, 0);
            options.SetFloat("hires.denoising_strength", managed.DenoisingStrength, defaults.DenoisingStrength);
            options.SetInt("hires.upscale_tile_size", managed.UpscaleTileSize, defaults.UpscaleTileSize, 1);
            options.SetFloats("hires.sigmas", managed.CustomSigmas);
        }

        #endregion

        #region CacheOptions

        /// <summary>
        /// Sets the cache options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="managed">The managed options.</param>
        /// <param name="defaults">The default options.</param>
        private static void SetCache(this NativeOptions options, CacheOptions managed, CacheOptions defaults)
        {
            if (managed == null)
                return;

            if (managed.Mode != defaults.Mode)
                options.SetString("cache.mode", managed.Mode.ToUnmanaged());

            options.SetFloat("cache.reuse_threshold", managed.ReuseThreshold, defaults.ReuseThreshold);
            options.SetFloat("cache.start_percent", managed.StartPercent, defaults.StartPercent);
            options.SetFloat("cache.end_percent", managed.EndPercent, defaults.EndPercent);
            options.SetFloat("cache.error_decay_rate", managed.ErrorDecayRate, defaults.ErrorDecayRate);
            options.SetBool("cache.use_relative_threshold", managed.UseRelativeThreshold, defaults.UseRelativeThreshold);
            options.SetBool("cache.reset_error_on_compute", managed.ResetErrorOnCompute, defaults.ResetErrorOnCompute);
            options.SetInt("cache.fn_compute_blocks", managed.FnComputeBlocks, defaults.FnComputeBlocks, 0);
            options.SetInt("cache.bn_compute_blocks", managed.BnComputeBlocks, defaults.BnComputeBlocks, 0);
            options.SetFloat("cache.residual_diff_threshold", managed.ResidualDiffThreshold, defaults.ResidualDiffThreshold);
            options.SetInt("cache.max_warmup_steps", managed.MaxWarmupSteps, defaults.MaxWarmupSteps, 0);
            options.SetInt("cache.max_cached_steps", managed.MaxCachedSteps, defaults.MaxCachedSteps, int.MinValue);
            options.SetInt("cache.max_continuous_cached_steps", managed.MaxContinuousCachedSteps, defaults.MaxContinuousCachedSteps, int.MinValue);
            options.SetInt("cache.taylorseer_n_derivatives", managed.TaylorseerNDerivatives, defaults.TaylorseerNDerivatives, 0);
            options.SetInt("cache.taylorseer_skip_interval", managed.TaylorseerSkipInterval, defaults.TaylorseerSkipInterval, 1);
            options.SetString("cache.scm_mask", managed.ScmMask);
            options.SetBool("cache.scm_policy_dynamic", managed.ScmPolicyDynamic, defaults.ScmPolicyDynamic);
            options.SetFloat("cache.spectrum_w", managed.SpectrumW, defaults.SpectrumW);
            options.SetInt("cache.spectrum_m", managed.SpectrumM, defaults.SpectrumM, 1);
            options.SetFloat("cache.spectrum_lam", managed.SpectrumLam, defaults.SpectrumLam);
            options.SetInt("cache.spectrum_window_size", managed.SpectrumWindowSize, defaults.SpectrumWindowSize, 1);
            options.SetFloat("cache.spectrum_flex_window", managed.SpectrumFlexWindow, defaults.SpectrumFlexWindow);
            options.SetInt("cache.spectrum_warmup_steps", managed.SpectrumWarmupSteps, defaults.SpectrumWarmupSteps, 0);
            options.SetFloat("cache.spectrum_stop_percent", managed.SpectrumStopPercent, defaults.SpectrumStopPercent);
        }

        #endregion

        #region PhotoMakerOptions

        /// <summary>
        /// Sets the PhotoMaker options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="managed">The managed options.</param>
        /// <param name="defaults">The default options.</param>
        private static void SetPhotoMaker(this NativeOptions options, PhotoMakerOptions managed, PhotoMakerOptions defaults)
        {
            if (managed == null)
                return;

            options.SetImages("photomaker.id_images", managed.IdImages);
            options.SetString("photomaker.id_embed_path", managed.IdEmbedPath);
            options.SetFloat("photomaker.style_strength", managed.StyleStrength, defaults.StyleStrength);
        }

        #endregion

        #region PulidOptions

        /// <summary>
        /// Sets the PuLID options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="managed">The managed options.</param>
        /// <param name="defaults">The default options.</param>
        private static void SetPulid(this NativeOptions options, PulidOptions managed, PulidOptions defaults)
        {
            if (managed == null)
                return;

            options.SetString("pulid.id_embedding_path", managed.IdEmbeddingPath);
            options.SetFloat("pulid.id_weight", managed.IdWeight, defaults.IdWeight);
        }

        #endregion

        #region Tensors

        /// <summary>
        /// Creates a native U8 image tensor [C,W,H], the caller must free it with sd_tensor_free.
        /// </summary>
        /// <param name="tensor">The tensor.</param>
        /// <param name="channels">The channels, 1 (mask), 3 (RGB) or 4 (RGBA).</param>
        internal static NativeApi.sd_tensor_t* ToUnmanaged(this ImageTensor tensor, int channels = 4)
        {
            var height = tensor.Dimensions[2];
            var width = tensor.Dimensions[3];
            var byteCount = checked(width * height * channels);
            var data = (byte*)NativeMemory.Alloc((nuint)byteCount);
            try
            {
                WritePixels(tensor, data, channels);

                // The library copies the data
                var shape = stackalloc long[] { channels, width, height };
                var result = NativeApi.sd_tensor_create(NativeApi.sd_tensor_type_t.SD_TENSOR_U8, 3, shape, data, (nuint)byteCount);
                if (result == null)
                    throw new Exception($"Failed to create image tensor: {NativeApi.GetLastError()}");

                return result;
            }
            finally
            {
                NativeMemory.Free(data);
            }
        }


        /// <summary>
        /// Creates a native U8 video tensor [C,W,H,T], the caller must free it with sd_tensor_free.
        /// </summary>
        /// <param name="frames">The frames, all the same size.</param>
        /// <param name="channels">The channels, 1, 3 (RGB) or 4 (RGBA).</param>
        /// <exception cref="System.ArgumentException">Video frames must all be the same size</exception>
        internal static NativeApi.sd_tensor_t* ToUnmanaged(this ImageTensor[] frames, int channels = 4)
        {
            if (frames.IsNullOrEmpty())
                throw new ArgumentException("Video has no frames", nameof(frames));

            var height = frames[0].Dimensions[2];
            var width = frames[0].Dimensions[3];
            foreach (var frame in frames)
            {
                if (frame.Dimensions[2] != height || frame.Dimensions[3] != width)
                    throw new ArgumentException("Video frames must all be the same size", nameof(frames));
            }

            var frameSize = checked(width * height * channels);
            var byteCount = checked((nuint)frameSize * (nuint)frames.Length);
            var data = (nint)NativeMemory.Alloc(byteCount);
            try
            {
                Parallel.For(0, frames.Length, i =>
                {
                    WritePixels(frames[i], (byte*)data + (nint)i * frameSize, channels);
                });

                // The library copies the data
                var shape = stackalloc long[] { channels, width, height, frames.Length };
                var result = NativeApi.sd_tensor_create(NativeApi.sd_tensor_type_t.SD_TENSOR_U8, 4, shape, (void*)data, byteCount);
                if (result == null)
                    throw new Exception($"Failed to create video tensor: {NativeApi.GetLastError()}");

                return result;
            }
            finally
            {
                NativeMemory.Free((void*)data);
            }
        }


        /// <summary>
        /// Creates a native audio descriptor with an F32 tensor [C,N], the caller must free the tensor with sd_tensor_free.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <exception cref="System.InvalidOperationException">Audio has no channels or samples</exception>
        internal static NativeApi.sd_audio_t ToUnmanaged(this AudioTensor audio)
        {
            var sampleRate = audio.SampleRate;
            var channels = audio.Channels;
            var samples = audio.Samples;
            if (channels <= 0)
                throw new InvalidOperationException("Audio has no channels.");
            if (samples <= 0)
                throw new InvalidOperationException("Audio has no samples.");
            if (sampleRate <= 0)
                throw new InvalidOperationException("Audio has no sample rate.");

            var totalSamples = checked(channels * samples);
            fixed (float* data = audio.Span)
            {
                // The library copies the data
                var shape = stackalloc long[] { channels, samples };
                var result = NativeApi.sd_tensor_create(NativeApi.sd_tensor_type_t.SD_TENSOR_F32, 2, shape, data, (nuint)totalSamples * sizeof(float));
                if (result == null)
                    throw new Exception($"Failed to create audio tensor: {NativeApi.GetLastError()}");

                return new NativeApi.sd_audio_t
                {
                    sample_rate = (uint)sampleRate,
                    data = result
                };
            }
        }


        /// <summary>
        /// Copies a native U8 image tensor [C,W,H] to a managed image.
        /// </summary>
        /// <param name="tensor">The tensor, borrowed.</param>
        /// <exception cref="System.ArgumentNullException">tensor</exception>
        /// <exception cref="System.ArgumentException">Expected a U8 [C,W,H] image tensor</exception>
        internal static ImageTensor ToImageTensor(NativeApi.sd_tensor_t* tensor)
        {
            if (tensor == null)
                throw new ArgumentNullException(nameof(tensor));
            if (NativeApi.sd_tensor_type(tensor) != NativeApi.sd_tensor_type_t.SD_TENSOR_U8 || NativeApi.sd_tensor_rank(tensor) != 3)
                throw new ArgumentException("Expected a U8 [C,W,H] image tensor", nameof(tensor));

            var channels = checked((int)NativeApi.sd_tensor_dim(tensor, 0));
            var width = checked((int)NativeApi.sd_tensor_dim(tensor, 1));
            var height = checked((int)NativeApi.sd_tensor_dim(tensor, 2));
            var data = (byte*)NativeApi.sd_tensor_data(tensor);
            if (data == null)
                throw new ArgumentNullException(nameof(tensor));

            return ReadPixels(data, width, height, channels);
        }


        /// <summary>
        /// Copies a native U8 video tensor [C,W,H,T] to managed frames.
        /// </summary>
        /// <param name="tensor">The tensor, borrowed.</param>
        /// <exception cref="System.ArgumentNullException">tensor</exception>
        /// <exception cref="System.ArgumentException">Expected a U8 [C,W,H,T] video tensor</exception>
        internal static ImageTensor[] ToImageTensors(NativeApi.sd_tensor_t* tensor)
        {
            if (tensor == null)
                throw new ArgumentNullException(nameof(tensor));
            if (NativeApi.sd_tensor_type(tensor) != NativeApi.sd_tensor_type_t.SD_TENSOR_U8 || NativeApi.sd_tensor_rank(tensor) != 4)
                throw new ArgumentException("Expected a U8 [C,W,H,T] video tensor", nameof(tensor));

            var channels = checked((int)NativeApi.sd_tensor_dim(tensor, 0));
            var width = checked((int)NativeApi.sd_tensor_dim(tensor, 1));
            var height = checked((int)NativeApi.sd_tensor_dim(tensor, 2));
            var frameCount = checked((int)NativeApi.sd_tensor_dim(tensor, 3));
            var frameSize = checked(width * height * channels);
            var data = (nint)NativeApi.sd_tensor_data(tensor);
            if (data == nint.Zero)
                throw new ArgumentNullException(nameof(tensor));

            var frames = new ImageTensor[frameCount];
            Parallel.For(0, frameCount, i =>
            {
                frames[i] = ReadPixels((byte*)data + (nint)i * frameSize, width, height, channels);
            });
            return frames;
        }


        /// <summary>
        /// Copies a native audio descriptor to a managed audio tensor.
        /// </summary>
        /// <param name="audio">The audio, borrowed.</param>
        /// <exception cref="System.ArgumentException">Expected an F32 [C,N] audio tensor</exception>
        internal static AudioTensor ToManaged(this NativeApi.sd_audio_t audio)
        {
            var tensor = audio.data;
            if (tensor == null)
                throw new ArgumentNullException(nameof(audio));
            if (NativeApi.sd_tensor_type(tensor) != NativeApi.sd_tensor_type_t.SD_TENSOR_F32 || NativeApi.sd_tensor_rank(tensor) != 2)
                throw new ArgumentException("Expected an F32 [C,N] audio tensor", nameof(audio));

            var sampleRate = checked((int)audio.sample_rate);
            var channels = checked((int)NativeApi.sd_tensor_dim(tensor, 0));
            var samples = checked((int)NativeApi.sd_tensor_dim(tensor, 1));
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));
            if (samples <= 0)
                throw new ArgumentOutOfRangeException(nameof(samples));

            var data = (float*)NativeApi.sd_tensor_data(tensor);
            if (data == null)
                throw new ArgumentNullException(nameof(audio));

            var totalSamples = checked(channels * samples);
            var managedTensor = new Tensor<float>([channels, samples]);
            var unmanagedTensor = new ReadOnlySpan<float>(data, totalSamples);
            unmanagedTensor.CopyTo(managedTensor.Memory.Span);
            return managedTensor.AsAudioTensor(sampleRate);
        }


        /// <summary>
        /// Writes an image as interleaved bytes, row by row.
        /// </summary>
        /// <param name="tensor">The tensor.</param>
        /// <param name="data">The destination, width * height * channels bytes.</param>
        /// <param name="channels">The channels.</param>
        private static void WritePixels(ImageTensor tensor, byte* data, int channels)
        {
            var height = tensor.Dimensions[2];
            var width = tensor.Dimensions[3];
            var channelSize = checked(width * height);

            // StableDiffusion.cpp can injest 1,3 and 4 channel tensors depending on input type
            // images can be 3 or 4, masks are 1
            var r = tensor.GetChannel(1);
            var g = tensor.GetChannel(2);
            var b = tensor.GetChannel(3);
            var a = tensor.GetChannel(4);
            if (channels == 1)
            {
                for (int i = 0; i < channelSize; i++)
                    data[i] = r[i].DenormalizeToByte();
            }
            else if (channels == 3)
            {
                for (int i = 0, dst = 0; i < channelSize; i++)
                {
                    data[dst++] = r[i].DenormalizeToByte();
                    data[dst++] = g[i].DenormalizeToByte();
                    data[dst++] = b[i].DenormalizeToByte();
                }
            }
            else
            {
                for (int i = 0, dst = 0; i < channelSize; i++)
                {
                    data[dst++] = r[i].DenormalizeToByte();
                    data[dst++] = g[i].DenormalizeToByte();
                    data[dst++] = b[i].DenormalizeToByte();
                    data[dst++] = a[i].DenormalizeToByte();
                }
            }
        }


        /// <summary>
        /// Reads an image from interleaved bytes, row by row.
        /// </summary>
        /// <param name="data">The source, width * height * channels bytes.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="channels">The channels.</param>
        private static ImageTensor ReadPixels(byte* data, int width, int height, int channels)
        {
            var channelSize = checked(width * height);

            // TensorStack image tensors are strictly 4 channle RGBA
            var tensor = new ImageTensor(height, width, 1f);
            var r = tensor.GetChannel(1);
            var g = tensor.GetChannel(2);
            var b = tensor.GetChannel(3);
            var a = tensor.GetChannel(4);
            if (channels == 1)
            {
                for (int i = 0; i < channelSize; i++)
                {
                    var value = data[i].NormalizeToFloat();
                    r[i] = value;
                    g[i] = value;
                    b[i] = value;
                }
            }
            else if (channels == 3)
            {
                for (int i = 0, src = 0; i < channelSize; i++)
                {
                    r[i] = data[src++].NormalizeToFloat();
                    g[i] = data[src++].NormalizeToFloat();
                    b[i] = data[src++].NormalizeToFloat();
                }
            }
            else if (channels == 4)
            {
                for (int i = 0, src = 0; i < channelSize; i++)
                {
                    r[i] = data[src++].NormalizeToFloat();
                    g[i] = data[src++].NormalizeToFloat();
                    b[i] = data[src++].NormalizeToFloat();
                    a[i] = data[src++].NormalizeToFloat();
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(channels));
            }
            return tensor;
        }

        #endregion
    }
}
