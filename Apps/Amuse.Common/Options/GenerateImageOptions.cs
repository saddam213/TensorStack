using Amuse.Common.Message;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace Amuse.Common
{
    public sealed record GenerateImageOptions
    {
        public int Seed { get; set; }
        public string Prompt { get; set; }
        public string Prompt2 { get; set; }
        public string NegativePrompt { get; set; }
        public float GuidanceScale { get; set; } = 1;
        public float GuidanceScale2 { get; set; } = 1;
        public int Steps { get; set; } = 50;
        public int Steps2 { get; set; } = 20;
        public int Height { get; set; }
        public int Width { get; set; }
        public float Strength { get; set; } = 1;
        public float ControlNetScale { get; set; } = 1;
        public string TempFileName { get; set; }
        public bool EnableVaeTiling { get; set; }
        public bool EnableVaeSlicing { get; set; }
        public LanguageType Language { get; set; }
        public string Instruction { get; set; }
        public string Task { get; set; }
        public int MaxLength { get; set; }
        public int MaxLength2 { get; set; }
        public LatentUpscale LatentUpscale { get; set; }
        public int LatentUpscaleSteps { get; set; }
        public float LatentUpscaleStrength { get; set; }
        public int LatentUpscaleTileSize { get; set; }
        public SchedulerOptions SchedulerOptions { get; set; }
        public List<LoraOptions> LoraOptions { get; set; }


        [JsonIgnore]
        public List<ImageTensor> InputImages { get; set; } = [];

        [JsonIgnore]
        public List<ImageTensor> InputControlImages { get; set; } = [];


        public void PackTensors(PipelineRequest request)
        {
            request.ImageTensorCount = InputImages?.Count ?? 0;
            request.ControlNetTensorCount = InputControlImages?.Count ?? 0;
            var totalCount = request.ImageTensorCount + request.ControlNetTensorCount;
            if (totalCount > 0)
            {
                var index = 0;
                var validTensors = new Tensor<float>[totalCount];
                if (!InputImages.IsNullOrEmpty())
                {
                    foreach (var tensor in InputImages)
                        validTensors[index++] = tensor;
                }

                if (!InputControlImages.IsNullOrEmpty())
                {
                    foreach (var tensor in InputControlImages)
                        validTensors[index++] = tensor;
                }
                request.Tensors = validTensors;
            }
        }

        public void UnpackTensors(PipelineRequest request)
        {
            if (request?.Tensors == null)
                return;

            if (request.ImageTensorCount > 0)
            {
                InputImages = request.Tensors
                    .Take(request.ImageTensorCount)
                    .Select(x => x.AsImageTensor())
                    .ToList();
            }

            if (request.ControlNetTensorCount > 0)
            {
                InputControlImages = request.Tensors
                    .Skip(request.ImageTensorCount)
                    .Take(request.ControlNetTensorCount)
                    .Select(x => x.AsImageTensor())
                    .ToList();
            }
        }
    }
}
