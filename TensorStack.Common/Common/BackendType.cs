using System.ComponentModel.DataAnnotations;

namespace TensorStack.Common
{
    public enum BackendType
    {
        [Display(Name = "OnnxRuntime", ShortName = "Onnx", Description = "OnnxRuntime .NET model inference using TensorStack")]
        OnnxRuntime = 0,

        [Display(Name = "HuggingFace", ShortName = "HuggingFace", Description = "Diffusers & Transformers model inference using HuggingFace")]
        HuggingFace = 10,

        [Display(Name = "StableDiffusionCpp", ShortName = "SD.cpp", Description = "GGML model inference using StableDiffusionCpp")]
        StableDiffusionCpp = 20,

        [Display(Name = "AudioCpp", ShortName = "Audio.cpp", Description = "GGML audio model inference using audio.cpp")]
        AudioCpp = 30,

        // Deprecated
        PyTorch = 1000
    }
}
