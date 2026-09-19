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
               "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-868-f9ddc0f/sd-master-f9ddc0f-bin-win-vulkan-x64.zip"
            ]
        };


        public readonly static BackendConfig DefaultCUDA = new()
        {
            Name = "cuda",
            Directory = "Runtime\\cuda",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-868-f9ddc0f/cudart-sd-bin-win-cu12-x64.zip",
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-868-f9ddc0f/sd-master-f9ddc0f-bin-win-cuda12-x64.zip"
            ]
        };


        public readonly static BackendConfig DefaultROCM = new()
        {
            Name = "rocm",
            Directory = "Runtime\\rocm",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-868-f9ddc0f/sd-master-f9ddc0f-bin-win-rocm-7.14.0-x64.zip",
                "https://repo.amd.com/rocm/tarball-multi-arch/therock-dist-windows-multiarch-7.14.0.tar.gz"
            ]
        };
    }
}
