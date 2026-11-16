namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// Inference cache options, the defaults mirror StableDiffusion.cpp.
    /// </summary>
    public sealed record CacheOptions
    {
        public SDCacheType Mode { get; set; } = SDCacheType.Disabled;
        public float ReuseThreshold { get; set; } = float.PositiveInfinity;
        public float StartPercent { get; set; } = 0.15f;
        public float EndPercent { get; set; } = 0.95f;
        public float ErrorDecayRate { get; set; } = 1f;
        public bool UseRelativeThreshold { get; set; } = true;
        public bool ResetErrorOnCompute { get; set; } = true;
        public int FnComputeBlocks { get; set; } = 8;
        public int BnComputeBlocks { get; set; }
        public float ResidualDiffThreshold { get; set; } = 0.08f;
        public int MaxWarmupSteps { get; set; } = 8;
        public int MaxCachedSteps { get; set; } = -1;
        public int MaxContinuousCachedSteps { get; set; } = -1;
        public int TaylorseerNDerivatives { get; set; } = 1;
        public int TaylorseerSkipInterval { get; set; } = 1;
        public string ScmMask { get; set; }
        public bool ScmPolicyDynamic { get; set; } = true;
        public float SpectrumW { get; set; } = 0.4f;
        public int SpectrumM { get; set; } = 3;
        public float SpectrumLam { get; set; } = 1f;
        public int SpectrumWindowSize { get; set; } = 2;
        public float SpectrumFlexWindow { get; set; } = 0.5f;
        public int SpectrumWarmupSteps { get; set; } = 4;
        public float SpectrumStopPercent { get; set; } = 0.9f;
    }
}
