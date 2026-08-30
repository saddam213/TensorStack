using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;
using TensorStack.Media;
using TensorStack.Media.Image;
using TensorStack.StableDiffusionCpp;
using TensorStack.StableDiffusionCpp.Common;

namespace DebugApp
{
    internal class Program
    {
        private static ILogger Logger { get; set; }

        static async Task Main(string[] args)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            Logger = loggerFactory.CreateLogger<Program>();

            await StableDiffusionCppImage();
        }



        private static async Task StableDiffusionCppImage()
        {
            var backendDirectory = Path.Combine(AppContext.BaseDirectory, "sd-cpp-cuda");
            using (var pipeline = new StableDiffusionPipeline(backendDirectory))
            {
                await pipeline.LoadContextAsync(new ContextOptions
                {
                    Backend = "cuda0",
                    ParamsBackend = "*=cpu",
                    MaxVram = "-1",
                    FlashAttn = true,
                    DiffusionFlashAttn = true,

                    LlmPath = "Qwen3VL-4B-Instruct-Q8_0.gguf",
                    VaePath = "diffusion_pytorch_model.safetensors",
                    DiffusionModelPath = "Krea-2-Turbo-Q8_0.gguf",
                });

                var defaultOptions = pipeline.DefaultImageOptions;
                var options = defaultOptions with
                {
                    Prompt = "cute cat",
                    Width = 1024,
                    Height = 1024,
                    Seed = 420,
                    SampleParameters = defaultOptions.SampleParameters with
                    {
                        TxtCfg = 1,
                        SampleSteps = 8,
                        SampleMethod = SamplerType.Euler,
                    }
                };

                var result = await pipeline.GenerateImageAsync(options);
                var imageTensor = result.FirstOrDefault();
                await imageTensor.SaveAsync("OutputImage.png");
            }
        }

        private static void OnLogCallback(LogLevelType type, string message)
        {
            Console.WriteLine(message);
        }

        private static async Task StableDiffusionCppVideo()
        {
            var backendDirectory = Path.Combine(AppContext.BaseDirectory, "sd-cpp-cuda");
            using (var pipeline = new StableDiffusionPipeline(backendDirectory))
            {
                await pipeline.LoadContextAsync(new ContextOptions
                {
                    Backend = "cuda0",
                    ParamsBackend = "*=cpu",
                    MaxVram = "-1",
                    FlashAttn = true,
                    DiffusionFlashAttn = true,

                    LlmPath = "qwen3vl_32b_minimax_h3-Q4_K_M.gguf",
                    VaePath = "minimax_h3_video_vae_fp16.safetensors",
                    AudioVaePath = "minimax_h3_audio_vae_fp32.safetensors",
                    DiffusionModelPath = "minimax_h3_fl2va_pruned-Q8_0.gguf",
                });

                var defaultOptions = pipeline.DefaultVideoOptions;
                var options = defaultOptions with
                {
                    Prompt = "cute cat",
                    Width = 864,
                    Height = 480,
                    Fps = 24,
                    Seed = 420,
                    VideoFrames = 56,
                    SampleParameters = defaultOptions.SampleParameters with
                    {
                        TxtCfg = 1,
                        SampleSteps = 20,
                        SampleMethod = SamplerType.Euler,
                    }
                };

                var videoSequence = await pipeline.GenerateVideoAsync(options);
                await videoSequence.SaveAsync("OutputVideo.mp4");
            }
        }
    }

    public static class Extensions
    {

    }
}
