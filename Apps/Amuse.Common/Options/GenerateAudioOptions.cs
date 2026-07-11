using Amuse.Common.Message;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace Amuse.Common
{
    public sealed record GenerateAudioOptions
    {
        public int Seed { get; set; }
        public string Prompt { get; set; }
        public string Prompt2 { get; set; }
        public string NegativePrompt { get; set; }
        public float GuidanceScale { get; set; } = 1;
        public float GuidanceScale2 { get; set; } = 1;
        public int Steps { get; set; } = 50;
        public int Steps2 { get; set; } = 20;
        public float Strength { get; set; } = 1;
        public string TempFileName { get; set; }
        public bool EnableVaeTiling { get; set; }
        public bool EnableVaeSlicing { get; set; }
        public float Duration { get; set; } = 5f;
        public LanguageType Language { get; set; }
        public string Instruction { get; set; }
        public string Task { get; set; }
        public int MaxLength { get; set; }
        public int MaxLength2 { get; set; }
        public int Bpm { get; set; }
        public string Keyscale { get; set; }
        public string TrackName { get; set; }
        public string TimeSignature { get; set; }
        public float Speed { get; set; }
        public float SilenceDuration { get; set; }
        public int SampleRate { get; set; }
        public SchedulerOptions SchedulerOptions { get; set; }
        public List<LoraOptions> LoraOptions { get; set; }


        [JsonIgnore]
        public List<AudioTensor> InputAudios { get; set; } = [];


        public void PackTensors(PipelineRequest request)
        {
            request.AudioTensorCount = InputAudios?.Count ?? 0;
            if (request.AudioTensorCount > 0)
            {
                var index = 0;
                var validTensors = new Tensor<float>[request.AudioTensorCount];
                if (!InputAudios.IsNullOrEmpty())
                {
                    foreach (var tensor in InputAudios)
                        validTensors[index++] = tensor;
                }
                request.Tensors = validTensors;
            }
        }


        public void UnpackTensors(PipelineRequest request)
        {
            if (request?.Tensors == null)
                return;

            if (request.AudioTensorCount > 0)
            {
                InputAudios = request.Tensors
                    .Take(request.AudioTensorCount)
                    .Select(x => x.AsAudioTensor(SampleRate))
                    .ToList();
            }
        }
    }
}
