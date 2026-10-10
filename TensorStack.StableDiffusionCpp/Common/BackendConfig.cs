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
               "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-vulkan-x64.zip"
            ]
        };


        public readonly static BackendConfig DefaultCUDA = new()
        {
            Name = "cuda",
            Directory = "Runtime\\cuda",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/cudart-sd-bin-win-cu12-x64.zip",
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-cuda12-x64.zip"
            ]
        };


        public readonly static BackendConfig DefaultROCM = new()
        {
            Name = "rocm",
            Directory = "Runtime\\rocm",
            Requirements =
            [
                "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-rocm-10.1.0-x64.zip"
            ]
        };
    }
}
