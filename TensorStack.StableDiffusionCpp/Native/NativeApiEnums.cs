namespace TensorStack.StableDiffusionCpp.Native
{
    public static class NativeApiEnums
    {
        internal static NativeApi.sample_method_t ToUnmanaged(this SamplerType managed)
        {
            switch (managed)
            {
                case SamplerType.Euler:
                    return NativeApi.sample_method_t.EULER_SAMPLE_METHOD;
                case SamplerType.Euler_A:
                    return NativeApi.sample_method_t.EULER_A_SAMPLE_METHOD;
                case SamplerType.Heun:
                    return NativeApi.sample_method_t.HEUN_SAMPLE_METHOD;
                case SamplerType.DPM2:
                    return NativeApi.sample_method_t.DPM2_SAMPLE_METHOD;
                case SamplerType.DPMPP2S_A:
                    return NativeApi.sample_method_t.DPMPP2S_A_SAMPLE_METHOD;
                case SamplerType.DPMPP2M:
                    return NativeApi.sample_method_t.DPMPP2M_SAMPLE_METHOD;
                case SamplerType.DPMPP2Mv2:
                    return NativeApi.sample_method_t.DPMPP2Mv2_SAMPLE_METHOD;
                case SamplerType.IPNDM:
                    return NativeApi.sample_method_t.IPNDM_SAMPLE_METHOD;
                case SamplerType.IPNDM_V:
                    return NativeApi.sample_method_t.IPNDM_V_SAMPLE_METHOD;
                case SamplerType.LCM:
                    return NativeApi.sample_method_t.LCM_SAMPLE_METHOD;
                case SamplerType.DDIM:
                    return NativeApi.sample_method_t.DDIM_TRAILING_SAMPLE_METHOD;
                case SamplerType.TCD:
                    return NativeApi.sample_method_t.TCD_SAMPLE_METHOD;
                case SamplerType.ResidualMultiStep:
                    return NativeApi.sample_method_t.RES_MULTISTEP_SAMPLE_METHOD;
                case SamplerType.Residual2S:
                    return NativeApi.sample_method_t.RES_2S_SAMPLE_METHOD;
                case SamplerType.ER_SDE:
                    return NativeApi.sample_method_t.ER_SDE_SAMPLE_METHOD;
                case SamplerType.Euler_CFG_PP:
                    return NativeApi.sample_method_t.EULER_CFG_PP_SAMPLE_METHOD;
                case SamplerType.Euler_A_CFG_PP:
                    return NativeApi.sample_method_t.EULER_A_CFG_PP_SAMPLE_METHOD;
                case SamplerType.Euler_GE:
                    return NativeApi.sample_method_t.EULER_GE_SAMPLE_METHOD;
                case SamplerType.DPMPP2M_SDE:
                    return NativeApi.sample_method_t.DPMPP2M_SDE_SAMPLE_METHOD;
                case SamplerType.DPMPP2M_SDE_BT:
                    return NativeApi.sample_method_t.DPMPP2M_SDE_BT_SAMPLE_METHOD;
                case SamplerType.LMS:
                    return NativeApi.sample_method_t.LMS_SAMPLE_METHOD;
                case SamplerType.Default:
                    return NativeApi.sample_method_t.SAMPLE_METHOD_COUNT;
                default:
                    return NativeApi.sample_method_t.SAMPLE_METHOD_COUNT;
            }
        }

        internal static SamplerType ToManaged(this NativeApi.sample_method_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sample_method_t.EULER_SAMPLE_METHOD:
                    return SamplerType.Euler;
                case NativeApi.sample_method_t.EULER_A_SAMPLE_METHOD:
                    return SamplerType.Euler_A;
                case NativeApi.sample_method_t.HEUN_SAMPLE_METHOD:
                    return SamplerType.Heun;
                case NativeApi.sample_method_t.DPM2_SAMPLE_METHOD:
                    return SamplerType.DPM2;
                case NativeApi.sample_method_t.DPMPP2S_A_SAMPLE_METHOD:
                    return SamplerType.DPMPP2S_A;
                case NativeApi.sample_method_t.DPMPP2M_SAMPLE_METHOD:
                    return SamplerType.DPMPP2M;
                case NativeApi.sample_method_t.DPMPP2Mv2_SAMPLE_METHOD:
                    return SamplerType.DPMPP2Mv2;
                case NativeApi.sample_method_t.IPNDM_SAMPLE_METHOD:
                    return SamplerType.IPNDM;
                case NativeApi.sample_method_t.IPNDM_V_SAMPLE_METHOD:
                    return SamplerType.IPNDM_V;
                case NativeApi.sample_method_t.LCM_SAMPLE_METHOD:
                    return SamplerType.LCM;
                case NativeApi.sample_method_t.DDIM_TRAILING_SAMPLE_METHOD:
                    return SamplerType.DDIM;
                case NativeApi.sample_method_t.TCD_SAMPLE_METHOD:
                    return SamplerType.TCD;
                case NativeApi.sample_method_t.RES_MULTISTEP_SAMPLE_METHOD:
                    return SamplerType.ResidualMultiStep;
                case NativeApi.sample_method_t.RES_2S_SAMPLE_METHOD:
                    return SamplerType.Residual2S;
                case NativeApi.sample_method_t.ER_SDE_SAMPLE_METHOD:
                    return SamplerType.ER_SDE;
                case NativeApi.sample_method_t.EULER_CFG_PP_SAMPLE_METHOD:
                    return SamplerType.Euler_CFG_PP;
                case NativeApi.sample_method_t.EULER_A_CFG_PP_SAMPLE_METHOD:
                    return SamplerType.Euler_A_CFG_PP;
                case NativeApi.sample_method_t.EULER_GE_SAMPLE_METHOD:
                    return SamplerType.Euler_GE;
                case NativeApi.sample_method_t.DPMPP2M_SDE_SAMPLE_METHOD:
                    return SamplerType.DPMPP2M_SDE;
                case NativeApi.sample_method_t.DPMPP2M_SDE_BT_SAMPLE_METHOD:
                    return SamplerType.DPMPP2M_SDE_BT;
                case NativeApi.sample_method_t.LMS_SAMPLE_METHOD:
                    return SamplerType.LMS;
                case NativeApi.sample_method_t.SAMPLE_METHOD_COUNT:
                    return SamplerType.Default;
                default:
                    return SamplerType.Default;
            }
        }

        internal static NativeApi.scheduler_t ToUnmanaged(this SchedulerType managed)
        {
            switch (managed)
            {
                case SchedulerType.Discrete:
                    return NativeApi.scheduler_t.DISCRETE_SCHEDULER;
                case SchedulerType.Karras:
                    return NativeApi.scheduler_t.KARRAS_SCHEDULER;
                case SchedulerType.Exponential:
                    return NativeApi.scheduler_t.EXPONENTIAL_SCHEDULER;
                case SchedulerType.AYS:
                    return NativeApi.scheduler_t.AYS_SCHEDULER;
                case SchedulerType.GITS:
                    return NativeApi.scheduler_t.GITS_SCHEDULER;
                case SchedulerType.SGMUniform:
                    return NativeApi.scheduler_t.SGM_UNIFORM_SCHEDULER;
                case SchedulerType.Simple:
                    return NativeApi.scheduler_t.SIMPLE_SCHEDULER;
                case SchedulerType.Smoothstep:
                    return NativeApi.scheduler_t.SMOOTHSTEP_SCHEDULER;
                case SchedulerType.KlOptimal:
                    return NativeApi.scheduler_t.KL_OPTIMAL_SCHEDULER;
                case SchedulerType.LCM:
                    return NativeApi.scheduler_t.LCM_SCHEDULER;
                case SchedulerType.BongTangent:
                    return NativeApi.scheduler_t.BONG_TANGENT_SCHEDULER;
                case SchedulerType.LTX2:
                    return NativeApi.scheduler_t.LTX2_SCHEDULER;
                case SchedulerType.LogitNormal:
                    return NativeApi.scheduler_t.LOGIT_NORMAL_SCHEDULER;
                case SchedulerType.FLUX2:
                    return NativeApi.scheduler_t.FLUX2_SCHEDULER;
                case SchedulerType.FLUX:
                    return NativeApi.scheduler_t.FLUX_SCHEDULER;
                case SchedulerType.Beta:
                    return NativeApi.scheduler_t.BETA_SCHEDULER;
                case SchedulerType.Default:
                    return NativeApi.scheduler_t.SCHEDULER_COUNT;
                default:
                    return NativeApi.scheduler_t.SCHEDULER_COUNT;
            }
        }

        internal static SchedulerType ToManaged(this NativeApi.scheduler_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.scheduler_t.DISCRETE_SCHEDULER:
                    return SchedulerType.Discrete;
                case NativeApi.scheduler_t.KARRAS_SCHEDULER:
                    return SchedulerType.Karras;
                case NativeApi.scheduler_t.EXPONENTIAL_SCHEDULER:
                    return SchedulerType.Exponential;
                case NativeApi.scheduler_t.AYS_SCHEDULER:
                    return SchedulerType.AYS;
                case NativeApi.scheduler_t.GITS_SCHEDULER:
                    return SchedulerType.GITS;
                case NativeApi.scheduler_t.SGM_UNIFORM_SCHEDULER:
                    return SchedulerType.SGMUniform;
                case NativeApi.scheduler_t.SIMPLE_SCHEDULER:
                    return SchedulerType.Simple;
                case NativeApi.scheduler_t.SMOOTHSTEP_SCHEDULER:
                    return SchedulerType.Smoothstep;
                case NativeApi.scheduler_t.KL_OPTIMAL_SCHEDULER:
                    return SchedulerType.KlOptimal;
                case NativeApi.scheduler_t.LCM_SCHEDULER:
                    return SchedulerType.LCM;
                case NativeApi.scheduler_t.BONG_TANGENT_SCHEDULER:
                    return SchedulerType.BongTangent;
                case NativeApi.scheduler_t.LTX2_SCHEDULER:
                    return SchedulerType.LTX2;
                case NativeApi.scheduler_t.LOGIT_NORMAL_SCHEDULER:
                    return SchedulerType.LogitNormal;
                case NativeApi.scheduler_t.FLUX2_SCHEDULER:
                    return SchedulerType.FLUX2;
                case NativeApi.scheduler_t.FLUX_SCHEDULER:
                    return SchedulerType.FLUX;
                case NativeApi.scheduler_t.BETA_SCHEDULER:
                    return SchedulerType.Beta;
                case NativeApi.scheduler_t.SCHEDULER_COUNT:
                    return SchedulerType.Default;
                default:
                    return SchedulerType.Default;
            }
        }

        internal static NativeApi.prediction_t ToUnmanaged(this PredictionType managed)
        {
            switch (managed)
            {
                case PredictionType.EPS:
                    return NativeApi.prediction_t.EPS_PRED;
                case PredictionType.Variable:
                    return NativeApi.prediction_t.V_PRED;
                case PredictionType.EDMVariable:
                    return NativeApi.prediction_t.EDM_V_PRED;
                case PredictionType.Flow:
                    return NativeApi.prediction_t.FLOW_PRED;
                case PredictionType.FluxFlow:
                    return NativeApi.prediction_t.FLUX_FLOW_PRED;
                case PredictionType.SefiFlow:
                    return NativeApi.prediction_t.SEFI_FLOW_PRED;
                case PredictionType.MiniT2IFlow:
                    return NativeApi.prediction_t.MINIT2I_FLOW_PRED;
                case PredictionType.Default:
                    return NativeApi.prediction_t.PREDICTION_COUNT;
                default:
                    return NativeApi.prediction_t.PREDICTION_COUNT;
            }
        }

        internal static PredictionType ToManaged(this NativeApi.prediction_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.prediction_t.EPS_PRED:
                    return PredictionType.EPS;
                case NativeApi.prediction_t.V_PRED:
                    return PredictionType.Variable;
                case NativeApi.prediction_t.EDM_V_PRED:
                    return PredictionType.EDMVariable;
                case NativeApi.prediction_t.FLOW_PRED:
                    return PredictionType.Flow;
                case NativeApi.prediction_t.FLUX_FLOW_PRED:
                    return PredictionType.FluxFlow;
                case NativeApi.prediction_t.SEFI_FLOW_PRED:
                    return PredictionType.SefiFlow;
                case NativeApi.prediction_t.MINIT2I_FLOW_PRED:
                    return PredictionType.MiniT2IFlow;
                case NativeApi.prediction_t.PREDICTION_COUNT:
                    return PredictionType.Default;
                default:
                    return PredictionType.Default;
            }
        }

        internal static NativeApi.sd_type_t ToUnmanaged(this DataType managed)
        {
            switch (managed)
            {
                case DataType.F32:
                    return NativeApi.sd_type_t.SD_TYPE_F32;
                case DataType.F16:
                    return NativeApi.sd_type_t.SD_TYPE_F16;
                case DataType.Q4_0:
                    return NativeApi.sd_type_t.SD_TYPE_Q4_0;
                case DataType.Q4_1:
                    return NativeApi.sd_type_t.SD_TYPE_Q4_1;
                case DataType.Q5_0:
                    return NativeApi.sd_type_t.SD_TYPE_Q5_0;
                case DataType.Q5_1:
                    return NativeApi.sd_type_t.SD_TYPE_Q5_1;
                case DataType.Q8_0:
                    return NativeApi.sd_type_t.SD_TYPE_Q8_0;
                case DataType.Q8_1:
                    return NativeApi.sd_type_t.SD_TYPE_Q8_1;
                case DataType.Q2_K:
                    return NativeApi.sd_type_t.SD_TYPE_Q2_K;
                case DataType.Q3_K:
                    return NativeApi.sd_type_t.SD_TYPE_Q3_K;
                case DataType.Q4_K:
                    return NativeApi.sd_type_t.SD_TYPE_Q4_K;
                case DataType.Q5_K:
                    return NativeApi.sd_type_t.SD_TYPE_Q5_K;
                case DataType.Q6_K:
                    return NativeApi.sd_type_t.SD_TYPE_Q6_K;
                case DataType.Q8_K:
                    return NativeApi.sd_type_t.SD_TYPE_Q8_K;
                case DataType.IQ2_XXS:
                    return NativeApi.sd_type_t.SD_TYPE_IQ2_XXS;
                case DataType.IQ2_XS:
                    return NativeApi.sd_type_t.SD_TYPE_IQ2_XS;
                case DataType.IQ3_XXS:
                    return NativeApi.sd_type_t.SD_TYPE_IQ3_XXS;
                case DataType.IQ1_S:
                    return NativeApi.sd_type_t.SD_TYPE_IQ1_S;
                case DataType.IQ4_NL:
                    return NativeApi.sd_type_t.SD_TYPE_IQ4_NL;
                case DataType.IQ3_S:
                    return NativeApi.sd_type_t.SD_TYPE_IQ3_S;
                case DataType.IQ2_S:
                    return NativeApi.sd_type_t.SD_TYPE_IQ2_S;
                case DataType.IQ4_XS:
                    return NativeApi.sd_type_t.SD_TYPE_IQ4_XS;
                case DataType.I8:
                    return NativeApi.sd_type_t.SD_TYPE_I8;
                case DataType.I16:
                    return NativeApi.sd_type_t.SD_TYPE_I16;
                case DataType.I32:
                    return NativeApi.sd_type_t.SD_TYPE_I32;
                case DataType.I64:
                    return NativeApi.sd_type_t.SD_TYPE_I64;
                case DataType.F64:
                    return NativeApi.sd_type_t.SD_TYPE_F64;
                case DataType.IQ1_M:
                    return NativeApi.sd_type_t.SD_TYPE_IQ1_M;
                case DataType.BF16:
                    return NativeApi.sd_type_t.SD_TYPE_BF16;
                case DataType.TQ1_0:
                    return NativeApi.sd_type_t.SD_TYPE_TQ1_0;
                case DataType.TQ2_0:
                    return NativeApi.sd_type_t.SD_TYPE_TQ2_0;
                case DataType.MXFP4:
                    return NativeApi.sd_type_t.SD_TYPE_MXFP4;
                case DataType.NVFP4:
                    return NativeApi.sd_type_t.SD_TYPE_NVFP4;
                case DataType.Q1_0:
                    return NativeApi.sd_type_t.SD_TYPE_Q1_0;
                case DataType.Q2_0:
                    return NativeApi.sd_type_t.SD_TYPE_Q2_0;
                case DataType.F8_E4M3:
                    return NativeApi.sd_type_t.SD_TYPE_F8_E4M3;
                case DataType.F8_E5M2:
                    return NativeApi.sd_type_t.SD_TYPE_F8_E5M2;
                case DataType.Default:
                    return NativeApi.sd_type_t.SD_TYPE_COUNT;
                default:
                    return NativeApi.sd_type_t.SD_TYPE_COUNT;
            }
        }

        internal static DataType ToManaged(this NativeApi.sd_type_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sd_type_t.SD_TYPE_F32:
                    return DataType.F32;
                case NativeApi.sd_type_t.SD_TYPE_F16:
                    return DataType.F16;
                case NativeApi.sd_type_t.SD_TYPE_Q4_0:
                    return DataType.Q4_0;
                case NativeApi.sd_type_t.SD_TYPE_Q4_1:
                    return DataType.Q4_1;
                case NativeApi.sd_type_t.SD_TYPE_Q5_0:
                    return DataType.Q5_0;
                case NativeApi.sd_type_t.SD_TYPE_Q5_1:
                    return DataType.Q5_1;
                case NativeApi.sd_type_t.SD_TYPE_Q8_0:
                    return DataType.Q8_0;
                case NativeApi.sd_type_t.SD_TYPE_Q8_1:
                    return DataType.Q8_1;
                case NativeApi.sd_type_t.SD_TYPE_Q2_K:
                    return DataType.Q2_K;
                case NativeApi.sd_type_t.SD_TYPE_Q3_K:
                    return DataType.Q3_K;
                case NativeApi.sd_type_t.SD_TYPE_Q4_K:
                    return DataType.Q4_K;
                case NativeApi.sd_type_t.SD_TYPE_Q5_K:
                    return DataType.Q5_K;
                case NativeApi.sd_type_t.SD_TYPE_Q6_K:
                    return DataType.Q6_K;
                case NativeApi.sd_type_t.SD_TYPE_Q8_K:
                    return DataType.Q8_K;
                case NativeApi.sd_type_t.SD_TYPE_IQ2_XXS:
                    return DataType.IQ2_XXS;
                case NativeApi.sd_type_t.SD_TYPE_IQ2_XS:
                    return DataType.IQ2_XS;
                case NativeApi.sd_type_t.SD_TYPE_IQ3_XXS:
                    return DataType.IQ3_XXS;
                case NativeApi.sd_type_t.SD_TYPE_IQ1_S:
                    return DataType.IQ1_S;
                case NativeApi.sd_type_t.SD_TYPE_IQ4_NL:
                    return DataType.IQ4_NL;
                case NativeApi.sd_type_t.SD_TYPE_IQ3_S:
                    return DataType.IQ3_S;
                case NativeApi.sd_type_t.SD_TYPE_IQ2_S:
                    return DataType.IQ2_S;
                case NativeApi.sd_type_t.SD_TYPE_IQ4_XS:
                    return DataType.IQ4_XS;
                case NativeApi.sd_type_t.SD_TYPE_I8:
                    return DataType.I8;
                case NativeApi.sd_type_t.SD_TYPE_I16:
                    return DataType.I16;
                case NativeApi.sd_type_t.SD_TYPE_I32:
                    return DataType.I32;
                case NativeApi.sd_type_t.SD_TYPE_I64:
                    return DataType.I64;
                case NativeApi.sd_type_t.SD_TYPE_F64:
                    return DataType.F64;
                case NativeApi.sd_type_t.SD_TYPE_IQ1_M:
                    return DataType.IQ1_M;
                case NativeApi.sd_type_t.SD_TYPE_BF16:
                    return DataType.BF16;
                case NativeApi.sd_type_t.SD_TYPE_TQ1_0:
                    return DataType.TQ1_0;
                case NativeApi.sd_type_t.SD_TYPE_TQ2_0:
                    return DataType.TQ2_0;
                case NativeApi.sd_type_t.SD_TYPE_MXFP4:
                    return DataType.MXFP4;
                case NativeApi.sd_type_t.SD_TYPE_NVFP4:
                    return DataType.NVFP4;
                case NativeApi.sd_type_t.SD_TYPE_Q1_0:
                    return DataType.Q1_0;
                case NativeApi.sd_type_t.SD_TYPE_COUNT:
                    return DataType.Default;
                default:
                    return DataType.Default;
            }
        }

        internal static NativeApi.sd_log_level_t ToUnmanaged(this LogLevelType managed)
        {
            switch (managed)
            {
                case LogLevelType.Debug:
                    return NativeApi.sd_log_level_t.SD_LOG_DEBUG;
                case LogLevelType.Verbose:
                    return NativeApi.sd_log_level_t.SD_LOG_VERBOSE;
                case LogLevelType.Info:
                    return NativeApi.sd_log_level_t.SD_LOG_INFO;
                case LogLevelType.Warn:
                    return NativeApi.sd_log_level_t.SD_LOG_WARN;
                case LogLevelType.Error:
                    return NativeApi.sd_log_level_t.SD_LOG_ERROR;
                default:
                    return NativeApi.sd_log_level_t.SD_LOG_ERROR;
            }
        }

        internal static LogLevelType ToManaged(this NativeApi.sd_log_level_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sd_log_level_t.SD_LOG_DEBUG:
                    return LogLevelType.Debug;
                case NativeApi.sd_log_level_t.SD_LOG_INFO:
                    return LogLevelType.Info;
                case NativeApi.sd_log_level_t.SD_LOG_WARN:
                    return LogLevelType.Warn;
                case NativeApi.sd_log_level_t.SD_LOG_ERROR:
                    return LogLevelType.Error;
                default:
                    return LogLevelType.Error;
            }
        }

        internal static NativeApi.preview_t ToUnmanaged(this PreviewType managed)
        {
            switch (managed)
            {
                case PreviewType.Disabled:
                    return NativeApi.preview_t.PREVIEW_NONE;
                case PreviewType.Projection:
                    return NativeApi.preview_t.PREVIEW_PROJ;
                case PreviewType.TAE:
                    return NativeApi.preview_t.PREVIEW_TAE;
                case PreviewType.VAE:
                    return NativeApi.preview_t.PREVIEW_VAE;
                case PreviewType.Default:
                    return NativeApi.preview_t.PREVIEW_COUNT;
                default:
                    return NativeApi.preview_t.PREVIEW_NONE;
            }
        }

        internal static PreviewType ToManaged(this NativeApi.preview_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.preview_t.PREVIEW_NONE:
                    return PreviewType.Disabled;
                case NativeApi.preview_t.PREVIEW_PROJ:
                    return PreviewType.Projection;
                case NativeApi.preview_t.PREVIEW_TAE:
                    return PreviewType.TAE;
                case NativeApi.preview_t.PREVIEW_VAE:
                    return PreviewType.VAE;
                case NativeApi.preview_t.PREVIEW_COUNT:
                    return PreviewType.Default;
                default:
                    return PreviewType.Disabled;
            }
        }

        internal static NativeApi.lora_apply_mode_t ToUnmanaged(this LoraApplyType managed)
        {
            switch (managed)
            {
                case LoraApplyType.Auto:
                    return NativeApi.lora_apply_mode_t.LORA_APPLY_AUTO;
                case LoraApplyType.Immediately:
                    return NativeApi.lora_apply_mode_t.LORA_APPLY_IMMEDIATELY;
                case LoraApplyType.AtRuntime:
                    return NativeApi.lora_apply_mode_t.LORA_APPLY_AT_RUNTIME;
                case LoraApplyType.Default:
                    return NativeApi.lora_apply_mode_t.LORA_APPLY_MODE_COUNT;
                default:
                    return NativeApi.lora_apply_mode_t.LORA_APPLY_MODE_COUNT;
            }
        }

        internal static LoraApplyType ToManaged(this NativeApi.lora_apply_mode_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.lora_apply_mode_t.LORA_APPLY_AUTO:
                    return LoraApplyType.Auto;
                case NativeApi.lora_apply_mode_t.LORA_APPLY_IMMEDIATELY:
                    return LoraApplyType.Immediately;
                case NativeApi.lora_apply_mode_t.LORA_APPLY_AT_RUNTIME:
                    return LoraApplyType.AtRuntime;
                case NativeApi.lora_apply_mode_t.LORA_APPLY_MODE_COUNT:
                    return LoraApplyType.Default;
                default:
                    return LoraApplyType.Default;
            }
        }

        internal static NativeApi.sd_vae_format_t ToUnmanaged(this VaeFormatType managed)
        {
            switch (managed)
            {
                case VaeFormatType.Auto:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_AUTO;
                case VaeFormatType.FLUX:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_FLUX;
                case VaeFormatType.SD3:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_SD3;
                case VaeFormatType.FLUX2:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_FLUX2;
                case VaeFormatType.WAN:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_WAN;
                case VaeFormatType.Default:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_COUNT;
                default:
                    return NativeApi.sd_vae_format_t.SD_VAE_FORMAT_AUTO;
            }
        }

        internal static VaeFormatType ToManaged(this NativeApi.sd_vae_format_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sd_vae_format_t.SD_VAE_FORMAT_AUTO:
                    return VaeFormatType.Auto;
                case NativeApi.sd_vae_format_t.SD_VAE_FORMAT_FLUX:
                    return VaeFormatType.FLUX;
                case NativeApi.sd_vae_format_t.SD_VAE_FORMAT_SD3:
                    return VaeFormatType.SD3;
                case NativeApi.sd_vae_format_t.SD_VAE_FORMAT_FLUX2:
                    return VaeFormatType.FLUX2;
                case NativeApi.sd_vae_format_t.SD_VAE_FORMAT_WAN:
                    return VaeFormatType.WAN;
                case NativeApi.sd_vae_format_t.SD_VAE_FORMAT_COUNT:
                    return VaeFormatType.Default;
                default:
                    return VaeFormatType.Auto;
            }
        }

        internal static NativeApi.sd_cache_mode_t ToUnmanaged(this SDCacheType managed)
        {
            switch (managed)
            {
                case SDCacheType.Disabled:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_DISABLED;
                case SDCacheType.EasyCache:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_EASYCACHE;
                case SDCacheType.UCache:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_UCACHE;
                case SDCacheType.DBCache:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_DBCACHE;
                case SDCacheType.TaylorSeer:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_TAYLORSEER;
                case SDCacheType.DitCache:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_CACHE_DIT;
                case SDCacheType.Spectrum:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_SPECTRUM;
                default:
                    return NativeApi.sd_cache_mode_t.SD_CACHE_DISABLED;
            }
        }

        internal static SDCacheType ToManaged(this NativeApi.sd_cache_mode_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sd_cache_mode_t.SD_CACHE_DISABLED:
                    return SDCacheType.Disabled;
                case NativeApi.sd_cache_mode_t.SD_CACHE_EASYCACHE:
                    return SDCacheType.EasyCache;
                case NativeApi.sd_cache_mode_t.SD_CACHE_UCACHE:
                    return SDCacheType.UCache;
                case NativeApi.sd_cache_mode_t.SD_CACHE_DBCACHE:
                    return SDCacheType.DBCache;
                case NativeApi.sd_cache_mode_t.SD_CACHE_TAYLORSEER:
                    return SDCacheType.TaylorSeer;
                case NativeApi.sd_cache_mode_t.SD_CACHE_CACHE_DIT:
                    return SDCacheType.DitCache;
                case NativeApi.sd_cache_mode_t.SD_CACHE_SPECTRUM:
                    return SDCacheType.Spectrum;
                default:
                    return SDCacheType.Disabled;
            }
        }

        internal static NativeApi.sd_hires_upscaler_t ToUnmanaged(this HiresUpscaleType managed)
        {
            switch (managed)
            {
                case HiresUpscaleType.None:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_NONE;
                case HiresUpscaleType.Latent:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT;
                case HiresUpscaleType.LatentNearest:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_NEAREST;
                case HiresUpscaleType.LatentNearestExact:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_NEAREST_EXACT;
                case HiresUpscaleType.LatentAntialiased:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_ANTIALIASED;
                case HiresUpscaleType.LatentBicubic:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_BICUBIC;
                case HiresUpscaleType.LatentBicubicAntialiased:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_BICUBIC_ANTIALIASED;
                case HiresUpscaleType.Lanczos:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LANCZOS;
                case HiresUpscaleType.Nearest:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_NEAREST;
                case HiresUpscaleType.Model:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_MODEL;
                case HiresUpscaleType.Default:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_COUNT;
                default:
                    return NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_NONE;
            }
        }

        internal static HiresUpscaleType ToManaged(this NativeApi.sd_hires_upscaler_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_NONE:
                    return HiresUpscaleType.None;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT:
                    return HiresUpscaleType.Latent;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_NEAREST:
                    return HiresUpscaleType.LatentNearest;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_NEAREST_EXACT:
                    return HiresUpscaleType.LatentNearestExact;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_ANTIALIASED:
                    return HiresUpscaleType.LatentAntialiased;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_BICUBIC:
                    return HiresUpscaleType.LatentBicubic;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LATENT_BICUBIC_ANTIALIASED:
                    return HiresUpscaleType.LatentBicubicAntialiased;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_LANCZOS:
                    return HiresUpscaleType.Lanczos;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_NEAREST:
                    return HiresUpscaleType.Nearest;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_MODEL:
                    return HiresUpscaleType.Model;
                case NativeApi.sd_hires_upscaler_t.SD_HIRES_UPSCALER_COUNT:
                    return HiresUpscaleType.Default;
                default:
                    return HiresUpscaleType.None;
            }
        }

        internal static NativeApi.sd_cancel_mode_t ToUnmanaged(this CancelType managed)
        {
            switch (managed)
            {
                case CancelType.Immediate:
                    return NativeApi.sd_cancel_mode_t.SD_CANCEL_ALL;
                case CancelType.NextStep:
                    return NativeApi.sd_cancel_mode_t.SD_CANCEL_NEW_LATENTS;
                case CancelType.Reset:
                    return NativeApi.sd_cancel_mode_t.SD_CANCEL_RESET;
                default:
                    return NativeApi.sd_cancel_mode_t.SD_CANCEL_ALL;
            }
        }

        internal static CancelType ToManaged(this NativeApi.sd_cancel_mode_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.sd_cancel_mode_t.SD_CANCEL_ALL:
                    return CancelType.Immediate;
                case NativeApi.sd_cancel_mode_t.SD_CANCEL_NEW_LATENTS:
                    return CancelType.NextStep;
                case NativeApi.sd_cancel_mode_t.SD_CANCEL_RESET:
                    return CancelType.Reset;
                default:
                    return CancelType.Immediate;
            }
        }

        internal static NativeApi.rng_type_t ToUnmanaged(this RngType managed)
        {
            switch (managed)
            {
                case RngType.Standard:
                    return NativeApi.rng_type_t.STD_DEFAULT_RNG;
                case RngType.CUDA:
                    return NativeApi.rng_type_t.CUDA_RNG;
                case RngType.CPU:
                    return NativeApi.rng_type_t.CPU_RNG;
                case RngType.Default:
                    return NativeApi.rng_type_t.RNG_TYPE_COUNT;
                default:
                    return NativeApi.rng_type_t.RNG_TYPE_COUNT;
            }
        }

        internal static RngType ToManaged(this NativeApi.rng_type_t unmanaged)
        {
            switch (unmanaged)
            {
                case NativeApi.rng_type_t.STD_DEFAULT_RNG:
                    return RngType.Standard;
                case NativeApi.rng_type_t.CUDA_RNG:
                    return RngType.CUDA;
                case NativeApi.rng_type_t.CPU_RNG:
                    return RngType.CPU;
                case NativeApi.rng_type_t.RNG_TYPE_COUNT:
                    return RngType.Default;
                default:
                    return RngType.Default;
            }
        }
    }
}
