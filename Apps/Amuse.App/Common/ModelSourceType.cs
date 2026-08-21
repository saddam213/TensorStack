using System.ComponentModel.DataAnnotations;

namespace Amuse.App.Common
{
    public enum ModelSourceType
    {
        [Display(Name = "Local File", Description = "Diffusion Unet or Transformer file (Default Components)")]
        LocalFile = 0,

        [Display(Name = "Local Folder", Description = "Full checkpoint directory containing all components (Diffusers Format)")]
        LocalFolder = 1,

        [Display(Name = "Local Checkpoint", Description = "Single checkpoint file containing all components (safetensors or gguf)")]
        LocalCheckpoint = 2,

        [Display(Name = "Custom Checkpoint", Description = "Configure the model components manually")]
        Checkpoint = 3,
    }
}
