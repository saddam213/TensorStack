using System.ComponentModel.DataAnnotations;

namespace TensorStack.Common
{
    public enum BackendType
    {
        [Display(Name = "OnnxRuntime", ShortName = "Onnx", Description = "OnnxRuntime .NET model inference using TensorStack")]
        OnnxRuntime = 0,

        [Display(Name = "PyTorch", ShortName = "torch", Description = "PyTorch model inference using HuggingFace Diffusers & Transformers")]
        PyTorch = 10,

        [Display(Name = "StableDiffusionCpp", ShortName = "SD.cpp", Description = "GGML model inference using StableDiffusionCpp")]
        StableDiffusionCpp = 20
    }
}
