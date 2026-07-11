using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.Common.Tensor;

namespace Amuse.Common.Message
{
    public sealed class PipelineRequest : IPipelineMessage
    {
        public PipelineRequest() { }
        public PipelineRequest(RequestType type)
        {
            Type = type;
        }

        public PipelineRequest(PipelineLoadOptions options, RequestType type)
        {
            LoadOptions = options;
            Type = type;
        }

        public PipelineRequest(PipelineReloadOptions options)
        {
            ReloadOptions = options;
            Type = RequestType.Reload;
        }

        public PipelineRequest(PipelineCreateOptions options)
        {
            CreateOptions = options;
            Type = RequestType.Create;
        }

        public PipelineRequest(PipelineRunOptions options)
        {
            RunOptions = options;
            Type = RequestType.Run;
            options.PackTensors(this);
        }

        public RequestType Type { get; init; }
        public PipelineCreateOptions CreateOptions { get; init; }
        public PipelineLoadOptions LoadOptions { get; init; }
        public PipelineReloadOptions ReloadOptions { get; init; }
        public PipelineRunOptions RunOptions { get; init; }

        public int ImageTensorCount { get; set; }
        public int ControlNetTensorCount { get; set; }
        public int AudioTensorCount { get; set; }

        [JsonIgnore]
        public IReadOnlyList<Tensor<float>> Tensors { get; set; }
    }
}
