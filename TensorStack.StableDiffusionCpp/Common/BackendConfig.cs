namespace TensorStack.StableDiffusionCpp.Common
{
    public record BackendConfig
    {
        public string Name { get; init; }
        public string Directory { get; init; }
        public string[] Requirements { get; init; }

        public readonly static BackendConfig DefaultVulkan = new()
        {
            Name = "vulkan",
            Directory = "Runtime\\vulkan",
            Requirements =
            [
               "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-911-740c7ae/sd-master-740c7ae-bin-win-vulkan-x64.zip"
            ]
        };


        public readonly static BackendConfig DefaultCUDA = new()
        {
            Name = "cuda",
            Directory = "Runtime\\cuda",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-911-740c7ae/cudart-sd-bin-win-cu12-x64.zip",
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-911-740c7ae/sd-master-740c7ae-bin-win-cuda12-x64.zip"
            ]
        };


        public readonly static BackendConfig DefaultROCM = new()
        {
            Name = "rocm",
            Directory = "Runtime\\rocm",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-911-740c7ae/sd-master-740c7ae-bin-win-rocm-7.14.0-x64.zip",
                "https://repo.amd.com/rocm/tarball-multi-arch/therock-dist-windows-multiarch-7.14.0.tar.gz"
            ]
        };
    }
}
