using System.ComponentModel.DataAnnotations;

namespace TensorStack.StableDiffusionCpp
{
    public enum BackendType
    {
        [Display(Name = "", ShortName = "cpu")]
        CPU = 0,

        [Display(Name = "", ShortName = "cuda")]
        CUDA = 1,

        [Display(Name = "", ShortName = "vulkan")]
        Vulkan = 2,

        [Display(Name = "", ShortName = "metal")]
        Metal = 3,

        [Display(Name = "", ShortName = "rocm")]
        ROCM = 4
    }

    public enum SamplerType
    {
        Euler,
        Euler_A,
        Heun,
        DPM2,
        DPMPP2S_A,
        DPMPP2M,
        DPMPP2Mv2,
        IPNDM,
        IPNDM_V,
        LCM,
        DDIM,
        TCD,
        ResidualMultiStep,
        Residual2S,
        ER_SDE,
        Euler_CFG_PP,
        Euler_A_CFG_PP,
        Euler_GE,
        DPMPP2M_SDE,
        DPMPP2M_SDE_BT,
        LMS,
        Default
    }

    public enum SchedulerType
    {
        Discrete,
        Karras,
        Exponential,
        AYS,
        GITS,
        SGMUniform,
        Simple,
        Smoothstep,
        KlOptimal,
        LCM,
        BongTangent,
        LTX2,
        LogitNormal,
        FLUX2,
        FLUX,
        Beta,
        Default
    }

    public enum PredictionType
    {
        EPS,
        Variable,
        EDMVariable,
        Flow,
        FluxFlow,
        SefiFlow,
        MiniT2IFlow,
        Default
    }

    public enum DataType
    {
        F32 = 0,
        F16 = 1,
        Q4_0 = 2,
        Q4_1 = 3,
        Q5_0 = 6,
        Q5_1 = 7,
        Q8_0 = 8,
        Q8_1 = 9,
        Q2_K = 10,
        Q3_K = 11,
        Q4_K = 12,
        Q5_K = 13,
        Q6_K = 14,
        Q8_K = 15,
        IQ2_XXS = 16,
        IQ2_XS = 17,
        IQ3_XXS = 18,
        IQ1_S = 19,
        IQ4_NL = 20,
        IQ3_S = 21,
        IQ2_S = 22,
        IQ4_XS = 23,
        I8 = 24,
        I16 = 25,
        I32 = 26,
        I64 = 27,
        F64 = 28,
        IQ1_M = 29,
        BF16 = 30,
        TQ1_0 = 34,
        TQ2_0 = 35,
        MXFP4 = 39,
        NVFP4 = 40,
        Q1_0 = 41,
        Q2_0 = 42,
        F8_E4M3 = 43,
        F8_E5M2 = 44,
        Default = 45,
    }

    public enum LogLevelType
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public enum PreviewType
    {
        Disabled,
        Projection,
        TAE,
        VAE,
        Default
    }

    public enum LoraApplyType
    {
        Auto,
        Immediately,
        AtRuntime,
        Default
    }

    public enum VaeFormatType
    {
        Auto = -1,
        FLUX,
        SD3,
        FLUX2,
        WAN,
        Default
    }

    public enum SDCacheType
    {
        Disabled = 0,
        EasyCache,
        UCache,
        DBCache,
        TaylorSeer,
        DitCache,
        Spectrum,
    }

    public enum HiresUpscaleType
    {
        None,
        Latent,
        LatentNearest,
        LatentNearestExact,
        LatentAntialiased,
        LatentBicubic,
        LatentBicubicAntialiased,
        Lanczos,
        Nearest,
        Model,
        Default,
    }

    public enum CancelType
    {
        [Display(Name = "Cancel All", Description = "Stop the current generation as soon as possible.")]
        Immediate,

        [Display(Name = "Cancel Next", Description = "Finish the current image sample, then skip additional batch latents and return completed images")]
        NextStep,

        [Display(Name = "Reset", Description = "Clear a pending cancellation request.")]
        Reset
    }

    public enum RngType
    {
        Standard,
        CUDA,
        CPU,
        Default
    }
}
