using System.ComponentModel.DataAnnotations;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public enum BackendType
    {
        [Display(Name = "", ShortName = "cpu")]
        CPU = 0,

        [Display(Name = "", ShortName = "cuda")]
        CUDA = 1,

        [Display(Name = "", ShortName = "vulkan")]
        Vulkan = 2,

        [Display(Name = "", ShortName = "metal")]
        Metal = 3,

        [Display(Name = "", ShortName = "rocm")]
        ROCM = 4
    }

}
