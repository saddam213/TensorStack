using TensorStack.Common;

namespace TensorStack.StableDiffusionCpp
{
    public static class InstallConfigs
    {
        public static EnvironmentConfig VendorDefault(VendorType vendorType)
        {
            return vendorType switch
            {
                VendorType.AMD => DefaultROCM,
                VendorType.Nvidia => DefaultCUDA,
                _ => DefaultVulkan
            };
        }


        public readonly static EnvironmentConfig DefaultVulkan = new()
        {
            Name = "vulkan",
            Version = "956",
            Directory = "Runtime\\vulkan",
            Requirements =
            [
               "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-vulkan-x64.zip"
            ]
        };


        public readonly static EnvironmentConfig DefaultCUDA = new()
        {
            Name = "cuda",
            Version = "956",
            Directory = "Runtime\\cuda",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/cudart-sd-bin-win-cu12-x64.zip",
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-cuda12-x64.zip"
            ]
        };


        public readonly static EnvironmentConfig DefaultROCM = new()
        {
            Name = "rocm",
            Version = "956",
            Directory = "Runtime\\rocm",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-rocm-10.1.0-x64.zip",
                "https://stable.repo.amd.com/rocm/core/tarball/therock-dist-windows-multiarch-10.1.0.tar.gz"
            ]
        };
    }
}
