namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// Sampler options, the defaults mirror StableDiffusion.cpp.
    /// <see cref="float.PositiveInfinity"/> and the <c>Default</c> enum values select the model default.
    /// </summary>
    public sealed record SamplerOptions
    {
        public SchedulerType Scheduler { get; set; } = SchedulerType.Default;
        public SamplerType SampleMethod { get; set; } = SamplerType.Default;
        public int SampleSteps { get; set; } = 20;
        public float Eta { get; set; } = float.PositiveInfinity;
        public int ShiftedTimestep { get; set; }
        public float[] CustomSigmas { get; set; } = [];
        public float FlowShift { get; set; } = float.PositiveInfinity;
        public string ExtraSampleArgs { get; set; }

        public float TxtCfg { get; set; } = 7f;
        public float ImgCfg { get; set; } = float.PositiveInfinity;
        public float DistilledGuidance { get; set; } = 3.5f;

        public int[] SlgLayers { get; set; } = [];
        public float SlgLayerStart { get; set; } = 0.01f;
        public float SlgLayerEnd { get; set; } = 0.2f;
        public float SlgScale { get; set; }
    }
}
