// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using System;

namespace TensorStack.Common.Audio
{
    public record AudioSegment(string Source, TimeSpan Start, TimeSpan Duration, TimeSpan Position)
    {
        public bool IsFirst { get; set; }
        public bool IsLast { get; set; }
        public string FileName { get; set; }
    }
}
