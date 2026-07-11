using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace Amuse.Common.Message
{
    public sealed class PipelineResponse : IPipelineMessage
    {
        public PipelineResponse() { }
        public PipelineResponse(Exception ex) : this(ex.Message)
        {
            IsCanceled = ex is OperationCanceledException;
        }
        public PipelineResponse(string errorMessage)
        {
            Error = errorMessage;
            Type = ResponseType.Error;
        }

        public PipelineResponse(params IReadOnlyList<Tensor<float>> tensors)
        {
            Tensors = tensors;
            Type = ResponseType.Tensor;
        }


        public PipelineResponse(params TextInput[] textResults)
        {
            Type = ResponseType.Object;
            TextResponse = textResults;
        }

        public ResponseType Type { get; init; }
        public TextInput[] TextResponse { get; set; }

        public string Error { get; init; }
        public bool IsCanceled { get; init; }

        [JsonIgnore]
        public IReadOnlyList<Tensor<float>> Tensors { get; set; }


        [JsonIgnore]
        public bool IsError => !string.IsNullOrEmpty(Error);
    }
}
