namespace Amuse.Host.StableDiffusionCpp.Config
{
    public class ModelConfig
    {
        public string Full { get; set; }
        public string ClipL { get; set; }
        public string ClipG { get; set; }
        public string ClipVison { get; set; }
        public string T5XXL { get; set; }
        public string LLM { get; set; }
        public string VisionLLM { get; set; }
        public string Diffusion { get; set; }
        public string DiffusionHighNoise { get; set; }
        public string DiffusionUncond { get; set; }
        public string Connectors { get; set; }
        public string Vae { get; set; }
        public string VaeAudio { get; set; }
        public string Tased { get; set; }
        public string ControlNet { get; set; }
        public string EmbeddingsDirectory { get; set; }
        public string LoraModelDirectory { get; set; }
        public string UpscaleModelDirectory { get; set; }
        public string ExtraModelArgs { get; set; }

    }

}
