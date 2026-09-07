// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
namespace TensorStack.OnnxRuntime.LLM.Common
{
    public record SearchOptions : GenerateOptions
    {
        public SearchOptions(){}
        public SearchOptions(GenerateOptions options) : base(options) { }
    }
}
