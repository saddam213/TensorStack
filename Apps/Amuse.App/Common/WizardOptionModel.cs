// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using System.Text.Json.Serialization;

namespace Amuse.App.Common
{
    public sealed class WizardOptionModel
    {
        public string Name { get; set; }
        public string Template { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double ModelParams { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ModelMaxLength { get; set; }
    }
}
