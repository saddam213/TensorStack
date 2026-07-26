using Amuse.App.Views;
using Amuse.Common;
using System.Collections.Generic;
using TensorStack.Common;

namespace Amuse.App.Common
{
    public interface IGenerateModel : IDownloadModel
    {
        PipelineType Pipeline { get; set; }
        string ModelType { get; set; }
        string Template { get; set; }
        DataType BaseType { get; set; }
        MediaType MediaType { get; set; }
        View[] ViewFilter { get; set; }
        VendorType[] Vendor { get; set; }
        MemoryProfile[] MemoryProfile { get; set; }
        GenerateDefaultOptions DefaultOptions { get; set; }
        bool IsDefault { get; set; }

        public IEnumerable<CheckpointComponent> GetComponents();
    }
}
