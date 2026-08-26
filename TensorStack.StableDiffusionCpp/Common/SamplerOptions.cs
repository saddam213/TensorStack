namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record SamplerOptions
    {
        public SchedulerType Scheduler { get; set; }
        public SamplerType SampleMethod { get; set; }
        public int SampleSteps { get; set; }
        public float Eta { get; set; }
        public int ShiftedTimestep { get; set; }
        public float[] CustomSigmas { get; set; }
        public float FlowShift { get; set; }
        public string ExtraSampleArgs { get; set; }

        public float TxtCfg { get; set; }
        public float ImgCfg { get; set; }
        public float DistilledGuidance { get; set; }

        public int[] SlgLayers { get; set; }
        public float SlgLayerStart { get; set; }
        public float SlgLayerEnd { get; set; }
        public float SlgScale { get; set; }
    }
}