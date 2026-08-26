namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record CacheOptions
    {
        public SDCacheType Mode { get; set; }
        public float ReuseThreshold { get; set; }
        public float StartPercent { get; set; }
        public float EndPercent { get; set; }
        public float ErrorDecayRate { get; set; }
        public bool UseRelativeThreshold { get; set; }
        public bool ResetErrorOnCompute { get; set; }
        public int FnComputeBlocks { get; set; }
        public int BnComputeBlocks { get; set; }
        public float ResidualDiffThreshold { get; set; }
        public int MaxWarmupSteps { get; set; }
        public int MaxCachedSteps { get; set; }
        public int MaxContinuousCachedSteps { get; set; }
        public int TaylorseerNDerivatives { get; set; }
        public int TaylorseerSkipInterval { get; set; }
        public string ScmMask { get; set; }
        public bool ScmPolicyDynamic { get; set; }
        public float SpectrumW { get; set; }
        public int SpectrumM { get; set; }
        public float SpectrumLam { get; set; }
        public int SpectrumWindowSize { get; set; }
        public float SpectrumFlexWindow { get; set; }
        public int SpectrumWarmupSteps { get; set; }
        public float SpectrumStopPercent { get; set; }
    }
}