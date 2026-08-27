namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record ContextOptions
    {
        public string ModelPath { get; set; }
        public string ClipLPath { get; set; }
        public string ClipGPath { get; set; }
        public string ClipVisionPath { get; set; }
        public string T5xxlPath { get; set; }
        public string LlmPath { get; set; }
        public string LlmVisionPath { get; set; }
        public string DiffusionModelPath { get; set; }
        public string HighNoiseDiffusionModelPath { get; set; }
        public string UncondDiffusionModelPath { get; set; }
        public string EmbeddingsConnectorsPath { get; set; }
        public string VaePath { get; set; }
        public string AudioVaePath { get; set; }
        public string TaesdPath { get; set; }
        public string ControlNetPath { get; set; }
        public string IpAdapterPath { get; set; }
        public string MotionModulePath { get; set; }
        public string PhotoMakerPath { get; set; }
        public string PulidWeightsPath { get; set; }

        public string Backend { get; set; }
        public string ParamsBackend { get; set; }
        public RngType RngType { get; set; }= RngType.Default;
        public RngType SamplerRngType { get; set; } = RngType.Default;
        public PredictionType Prediction { get; set; } = PredictionType.Default;
        public LoraApplyType LoraApplyMode { get; set; } = LoraApplyType.AtRuntime;

        public int Threads { get; set; } = -1;
        public DataType DataType { get; set; } = DataType.Default;
        public string MaxVram { get; set; } = "-1";
        public bool AutoFit { get; set; }
        public bool StreamLayers { get; set; }
        public bool EagerLoad { get; set; }
        public string SplitMode { get; set; }
        public string TensorTypeRules { get; set; }
        public bool EnableMmap { get; set; }
        public bool FlashAttn { get; set; }
        public bool DiffusionFlashAttn { get; set; }
        public bool TaePreviewOnly { get; set; }
        public bool DiffusionConvDirect { get; set; }
        public bool VaeConvDirect { get; set; }
        public bool ForceSdxlVaeConvScale { get; set; }
        public VaeFormatType VaeFormat { get; set; } = VaeFormatType.Auto;

        public string RpcServers { get; set; }
        public string ModelArgs { get; set; }
        public EmbeddingOptions[] Embeddings { get; set; }

        public PreviewType PreviewType { get; set; } = PreviewType.Disabled;
        public int PreviewInterval { get; set; } = 1;
        public bool IsPreviewNoisy { get; set; }
    }
}