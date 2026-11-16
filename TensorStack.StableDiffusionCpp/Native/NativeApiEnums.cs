using System.Runtime.InteropServices.Marshalling;

namespace TensorStack.StableDiffusionCpp.Native
{
    /// <summary>
    /// Managed enum to native value conversions.
    /// StableDiffusion.cpp takes its modes as strings, a <c>null</c> name means the option is left unset.
    /// </summary>
    public static unsafe class NativeApiEnums
    {
        internal const string AutoName = "auto";

        /// <summary>
        /// Gets the native sampler name, "auto" selects the model default.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this SamplerType managed)
        {
            return managed switch
            {
                SamplerType.Euler => "euler",
                SamplerType.Euler_A => "euler_a",
                SamplerType.Heun => "heun",
                SamplerType.DPM2 => "dpm2",
                SamplerType.DPMPP2S_A => "dpm++2s_a",
                SamplerType.DPMPP2M => "dpm++2m",
                SamplerType.DPMPP2Mv2 => "dpm++2mv2",
                SamplerType.IPNDM => "ipndm",
                SamplerType.IPNDM_V => "ipndm_v",
                SamplerType.LCM => "lcm",
                SamplerType.DDIM => "ddim_trailing",
                SamplerType.TCD => "tcd",
                SamplerType.ResidualMultiStep => "res_multistep",
                SamplerType.Residual2S => "res_2s",
                SamplerType.ER_SDE => "er_sde",
                SamplerType.Euler_CFG_PP => "euler_cfg_pp",
                SamplerType.Euler_A_CFG_PP => "euler_a_cfg_pp",
                SamplerType.Euler_GE => "euler_ge",
                SamplerType.DPMPP2M_SDE => "dpm++2m_sde",
                SamplerType.DPMPP2M_SDE_BT => "dpm++2m_sde_bt",
                SamplerType.LMS => "lms",
                _ => AutoName
            };
        }


        /// <summary>
        /// Gets the native scheduler name, "auto" selects the model default.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this SchedulerType managed)
        {
            return managed switch
            {
                SchedulerType.Discrete => "discrete",
                SchedulerType.Karras => "karras",
                SchedulerType.Exponential => "exponential",
                SchedulerType.AYS => "ays",
                SchedulerType.GITS => "gits",
                SchedulerType.SGMUniform => "sgm_uniform",
                SchedulerType.Simple => "simple",
                SchedulerType.Smoothstep => "smoothstep",
                SchedulerType.KlOptimal => "kl_optimal",
                SchedulerType.LCM => "lcm",
                SchedulerType.BongTangent => "bong_tangent",
                SchedulerType.LTX2 => "ltx2",
                SchedulerType.LogitNormal => "logit_normal",
                SchedulerType.FLUX2 => "flux2",
                SchedulerType.FLUX => "flux",
                SchedulerType.Beta => "beta",
                SchedulerType.LLADA => "llada_image",
                _ => AutoName
            };
        }


        /// <summary>
        /// Gets the native prediction name, "auto" selects the model default.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this PredictionType managed)
        {
            return managed switch
            {
                PredictionType.EPS => "eps",
                PredictionType.Variable => "v",
                PredictionType.EDMVariable => "edm_v",
                PredictionType.Flow => "sd3_flow",
                PredictionType.FluxFlow => "flux_flow",
                PredictionType.SefiFlow => "sefi_flow",
                PredictionType.MiniT2IFlow => "minit2i_flow",
                PredictionType.SenseNovaFlow => "sensenova_u1_flow",
                PredictionType.Flux3Flow => "flux3_flow",
                PredictionType.H3AVFlow => "h3_av_flow",
                PredictionType.MingFlow => "ming_image_flow",
                _ => AutoName
            };
        }


        /// <summary>
        /// Gets the native weight type name, "auto" selects the model default.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this DataType managed)
        {
            if (managed == DataType.Default)
                return AutoName;

            // Managed values match sd_type_t, the library owns the names
            var name = Utf8StringMarshaller.ConvertToManaged(NativeApi.sd_type_name((NativeApi.sd_type_t)(int)managed));
            if (string.IsNullOrEmpty(name) || name.Equals("NONE"))
                return AutoName;

            return name;
        }


        /// <summary>
        /// Gets the native RNG name, "auto" selects the model default.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this RngType managed)
        {
            return managed switch
            {
                RngType.Standard => "std_default",
                RngType.CUDA => "cuda",
                RngType.CPU => "cpu",
                _ => AutoName
            };
        }


        /// <summary>
        /// Gets the native preview mode name.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this PreviewType managed)
        {
            return managed switch
            {
                PreviewType.Projection => "proj",
                PreviewType.TAE => "tae",
                PreviewType.VAE => "vae",
                _ => "none"
            };
        }


        /// <summary>
        /// Gets the native LoRA apply mode name, <c>null</c> leaves the option unset.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this LoraApplyType managed)
        {
            return managed switch
            {
                LoraApplyType.Auto => "auto",
                LoraApplyType.Immediately => "immediately",
                LoraApplyType.AtRuntime => "at_runtime",
                _ => null
            };
        }


        /// <summary>
        /// Gets the native VAE format name, <c>null</c> leaves the option unset.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this VaeFormatType managed)
        {
            return managed switch
            {
                VaeFormatType.Auto => "auto",
                VaeFormatType.FLUX => "flux",
                VaeFormatType.SD3 => "sd3",
                VaeFormatType.FLUX2 => "flux2",
                VaeFormatType.WAN => "wan",
                _ => null
            };
        }


        /// <summary>
        /// Gets the native cache mode name.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this SDCacheType managed)
        {
            return managed switch
            {
                SDCacheType.EasyCache => "easycache",
                SDCacheType.UCache => "ucache",
                SDCacheType.DBCache => "dbcache",
                SDCacheType.TaylorSeer => "taylorseer",
                SDCacheType.DitCache => "cache_dit",
                SDCacheType.Spectrum => "spectrum",
                _ => "disabled"
            };
        }


        /// <summary>
        /// Gets the native hires upscaler name, <c>null</c> leaves the option unset.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this HiresUpscaleType managed)
        {
            return managed switch
            {
                HiresUpscaleType.None => "none",
                HiresUpscaleType.Latent => "latent",
                HiresUpscaleType.LatentNearest => "latent_nearest",
                HiresUpscaleType.LatentNearestExact => "latent_nearest_exact",
                HiresUpscaleType.LatentAntialiased => "latent_antialiased",
                HiresUpscaleType.LatentBicubic => "latent_bicubic",
                HiresUpscaleType.LatentBicubicAntialiased => "latent_bicubic_antialiased",
                HiresUpscaleType.Lanczos => "lanczos",
                HiresUpscaleType.Nearest => "nearest",
                HiresUpscaleType.Model => "model",
                _ => null
            };
        }


        /// <summary>
        /// Gets the native control type name, <c>null</c> leaves the option unset.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static string ToUnmanaged(this ControlNetType managed)
        {
            return managed switch
            {
                ControlNetType.None => "none",
                ControlNetType.OpenPose => "openpose",
                ControlNetType.Depth => "depth",
                ControlNetType.Scribble => "scribble",
                ControlNetType.Canny => "canny",
                ControlNetType.Normal => "normal",
                ControlNetType.Segment => "segment",
                ControlNetType.Tile => "tile",
                ControlNetType.Repaint => "repaint",
                _ => null
            };
        }


        /// <summary>
        /// Gets the native cancel mode.
        /// </summary>
        /// <param name="managed">The managed value.</param>
        internal static NativeApi.sd_cancel_mode_t ToUnmanaged(this CancelType managed)
        {
            return managed switch
            {
                CancelType.NextStep => NativeApi.sd_cancel_mode_t.SD_CANCEL_NEW_LATENTS,
                CancelType.Reset => NativeApi.sd_cancel_mode_t.SD_CANCEL_RESET,
                _ => NativeApi.sd_cancel_mode_t.SD_CANCEL_ALL
            };
        }


        /// <summary>
        /// Gets the managed log level.
        /// </summary>
        /// <param name="unmanaged">The unmanaged value.</param>
        internal static LogLevelType ToManaged(this NativeApi.sd_log_level_t unmanaged)
        {
            return unmanaged switch
            {
                NativeApi.sd_log_level_t.SD_LOG_DEBUG => LogLevelType.Debug,
                NativeApi.sd_log_level_t.SD_LOG_VERBOSE => LogLevelType.Verbose,
                NativeApi.sd_log_level_t.SD_LOG_INFO => LogLevelType.Info,
                NativeApi.sd_log_level_t.SD_LOG_WARN => LogLevelType.Warn,
                NativeApi.sd_log_level_t.SD_LOG_ERROR => LogLevelType.Error,
                _ => LogLevelType.Info
            };
        }
    }
}
