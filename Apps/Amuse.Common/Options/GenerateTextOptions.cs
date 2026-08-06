using Amuse.Common.Message;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace Amuse.Common
{
    public sealed record GenerateTextOptions
    {
        public int Seed { get; set; }
        public string Prompt { get; set; }
        public ConversationMessage[] Conversation { get; set; }
        public string TempFileName { get; set; }
        public LanguageType Language { get; set; }
        public string Instruction { get; set; }
        public string Task { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public bool IsSamplingEnabled { get; set; }
        public int Beams { get; set; }
        public float Temperature { get; set; }
        public int TopK { get; set; }
        public float TopP { get; set; }
        public float TopH { get; set; }
        public float TypicalP { get; set; }
        public float RepetitionPenalty { get; set; }
        public float LengthPenalty { get; set; }
        public int NoRepeatNgramSize { get; set; }
        public string EarlyStopping { get; set; }
        public int ChunkSize { get; set; }
        public bool IsThinkingEnabled { get; set; }
        public int SampleRate { get; set; }
        public CacheType CacheType { get; set; }

        [JsonIgnore]
        public List<ImageTensor> InputImages { get; set; } = [];

        [JsonIgnore]
        public List<AudioTensor> InputAudios { get; set; } = [];


        public void PackTensors(PipelineRequest request)
        {
            request.ImageTensorCount = InputImages?.Count ?? 0;
            request.AudioTensorCount = InputAudios?.Count ?? 0;
            var totalCount = request.ImageTensorCount + request.AudioTensorCount;
            if (totalCount > 0)
            {
                var index = 0;
                var validTensors = new Tensor<float>[totalCount];
                if (!InputImages.IsNullOrEmpty())
                {
                    foreach (var tensor in InputImages)
                        validTensors[index++] = tensor;
                }

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

            if (request.ImageTensorCount > 0)
            {
                InputImages = request.Tensors
                    .Take(request.ImageTensorCount)
                    .Select(x => x.AsImageTensor())
                    .ToList();
            }

            if (request.AudioTensorCount > 0)
            {
                InputAudios = request.Tensors
                    .Skip(request.ImageTensorCount)
                    .Take(request.AudioTensorCount)
                    .Select(x => x.AsAudioTensor(SampleRate))
                    .ToList();
            }
        }
    }
}
