using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Media;
using TensorStack.Media.Image;
using TensorStack.Media.Video;
using TensorStack.Media.Windows.Video;
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

            // await StableDiffusionCppImage();
            await StableDiffusionCppVideo();
        }



        private static async Task StableDiffusionCppImage()
        {
            var configuration = BackendConfig.DefaultCUDA;
            await InstallManager.InitializeAsync(configuration, logCallback: OnLogCallback);

            using (var pipeline = new StableDiffusionPipeline(configuration, logCallback: OnLogCallback))
            {
                var device = pipeline.Devices.FirstOrDefault(x => x.Type == DeviceType.GPU);
                await pipeline.LoadContextAsync(new ContextOptions
                {
                    Backend = device.Backend,
                    ParamsBackend = "*=cpu",
                    MaxVram = "-1",
                    FlashAttn = true,
                    DiffusionFlashAttn = true,

                    LlmPath = "E:\\Qwen3VL-4B-Instruct-Q8_0.gguf",
                    VaePath = "E:\\QwenImageAutoEncoder.safetensors",
                    DiffusionModelPath = "E:\\Krea-2-Turbo-Q8_0.gguf",
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
            var configuration = BackendConfig.DefaultCUDA;
            await InstallManager.InitializeAsync(configuration, logCallback: OnLogCallback);

            using (var pipeline = new StableDiffusionPipeline(configuration, logCallback: OnLogCallback))
            {
                await pipeline.LoadContextAsync(new ContextOptions
                {
                    Backend = "cuda0",
                    ParamsBackend = "*=cpu",
                    MaxVram = "-1",
                    FlashAttn = true,
                    DiffusionFlashAttn = true,
                    LoraApplyMode = LoraApplyType.AtRuntime,

                    LlmPath = "E:\\qwen3vl_32b_minimax_h3-Q4_K_M.gguf",
                    VaePath = "E:\\minimax_h3_video_vae_fp16.safetensors",
                    AudioVaePath = "E:\\minimax_h3_audio_vae_fp32.safetensors",
                    DiffusionModelPath = "E:\\minimax_h3_fl2va_pruned-Q8_0.gguf",
                });

                string[] prompts =
                [
                    "integrated_multimodal_description: [Shot 1] Live-action, sitcom style, a medium-wide shot frames Jerry’s cluttered living room. The camera holds a static shot as Jerry stands by the fridge holding a coffee mug, George sits on the couch looking agitated, Elaine stands by the door with arms crossed, and Kramer leans against the wall. Jerry (S1) says: <d>[English] You guys are overthinking this. It’s just an algorithm.</d> George (S2) retorts, <d>[English] It’s not an algorithm, it’s a trap!</d> As the line ends, Kramer (S3) suddenly lunges forward, his face close to the camera, eyes wide.\r\n\r\noverall_soundscape: The hum of the refrigerator provides a low background drone. A chair creaks as George leans forward, and the soft thud of Kramer’s movement against the wall is audible.\r\n\r\nnon_diegetic_music: A light, pizzicato string arrangement at a moderate tempo, typical of a sitcom underscore, ending with a sharp stop.",
                    "For the target video, at 0.00 seconds into the target video, <Picture 1> (from [Shot 1]) is used as a visual anchor for character consistency, environment, and lighting.\r\n\r\nintegrated_multimodal_description: [Shot 1] Live-action, sitcom style, the scene begins with the composition and character positions established in <Picture 1>, where Kramer’s face is close to the camera and the others are reacting. The camera pulls out with small amplitude at normal speed to reveal the full living room again, maintaining the same lighting and character appearances. Kramer (S3) shouts, <d>[English] I know a guy who knows a guy who has the source code!</d> He spins around, knocking over a stack of magazines on the coffee table. Jerry (S1) sighs, rubbing his temples, while George (S2) stands up from the couch, looking exasperated. Elaine (S4) shakes her head slowly. Kramer gestures wildly toward the window, his body language erratic.\r\n\r\noverall_soundscape: The sharp rustle of magazines hitting the floor. The creak of the couch springs as George stands. A low, ambient room tone with the distant sound of city traffic outside.\r\n\r\nnon_diegetic_music: N/A",
                    "For the target video, at 0.00 seconds into the target video, <Picture 1> (from [Shot 1]) is used as a visual anchor for character consistency, environment, and lighting.\r\n\r\nintegrated_multimodal_description: [Shot 1] Live-action, sitcom style, the scene begins with the composition from <Picture 1>, where Kramer is gesturing toward the window and George is just rising from the couch. The camera tracks left with small amplitude at normal speed, following George as he moves away from the group, preserving his clothing and the room’s layout. George (S2) says, <d>[English] If this AI starts judging my life choices, I’m moving to the suburbs.</d> He points at Jerry accusingly. Jerry (S1) raises a hand to calm him down, saying, <d>[English] George, it’s just a chatbot.</d> Kramer (S3) re-enters the frame from the right, holding a strange, glowing device. Elaine (S4) stares at the device with suspicion.\r\n\r\noverall_soundscape: The scuff of George’s shoes on the hardwood floor. The faint electronic beep of the device Kramer is holding. The ambient hum of the apartment.\r\n\r\nnon_diegetic_music: N/A",
                    "For the target video, at 0.00 seconds into the target video, <Picture 1> (from [Shot 1]) is used as a visual anchor for character consistency, environment, and lighting.\r\n\r\nintegrated_multimodal_description: [Shot 1] Live-action, sitcom style, the scene begins with the framing from <Picture 1>, focusing on the glowing device in Kramer’s hands as he enters the frame. The camera pushes in with small amplitude at slow speed as Kramer holds the device up, maintaining the consistent appearance of all characters and the room. Kramer (S3) says, <d>[English] It’s called Amuse AI. It tells you what you’re thinking before you think it.</d> The device emits a soft, pulsing light. Jerry (S1) leans in, intrigued despite himself. George (S2) recoils, covering his ears. Elaine (S4) steps forward, her expression shifting from suspicion to curiosity. The light from the device reflects in their eyes.\r\n\r\noverall_soundscape: A soft, rhythmic electronic pulse emanating from the device. The subtle breathing of the characters as they lean in. The ambient room tone remains low.\r\n\r\nnon_diegetic_music: N/A",
                    "For the target video, at 0.00 seconds into the target video, <Picture 1> (from [Shot 1]) is used as a visual anchor for character consistency, environment, and lighting.\r\n\r\nintegrated_multimodal_description: [Shot 1] Live-action, sitcom style, the scene begins with the group gathered around the device, preserving the positions and lighting from <Picture 1>. The camera holds a static shot as the device suddenly flashes brightly. All four characters flinch simultaneously, maintaining their consistent appearances. Jerry (S1) says, <d>[English] Whoa!</d> George (S2) drops to his knees, clutching his head. Kramer (S3) looks proud, holding the device high. Elaine (S4) laughs nervously. The light fades, leaving the room dimly lit again. The characters remain in their flinched positions, looking at each other in stunned silence.\r\n\r\noverall_soundscape: A sharp, high-pitched electronic zap as the device flashes. The collective gasp of the four characters. The sudden silence of the room after the flash.\r\n\r\nnon_diegetic_music: N/A",
                ];

                var defaultOptions = pipeline.DefaultVideoOptions;
                var resultVideo = default(VideoSequence);
                var inputFrame = default(ImageTensor);
                foreach (var prompt in prompts)
                {
                    var options = defaultOptions with
                    {
                        Prompt = prompt,
                        Width = 672,
                        Height = 384,
                        Fps = 24,
                        Seed = 420,
                        VideoFrames = 124,
                        InitImage = inputFrame,
                        SampleParameters = defaultOptions.SampleParameters with
                        {
                            TxtCfg = 1,
                            SampleSteps = 20,
                            SampleMethod = SamplerType.Euler,
                        },
                        //Loras = [new LoraOptions
                        //{
                        //    Path = "E:\\minimax_h3_fl2v_turbo_8step_v1.0_comfyui_bf16.safetensors"
                        //}]
                    };

                    var videoSequence = await pipeline.GenerateVideoAsync(options);
                    resultVideo = resultVideo.Join(videoSequence);
                    inputFrame = videoSequence.Frames[^1];

                    await videoSequence.SaveAsync($"OutputSeq.mp4");
                    await resultVideo.SaveAsync($"OutputVideo.mp4");
                }
            }
        }
    }

    public static class Extensions
    {
        public static VideoSequence Join(this VideoSequence oldSequence, VideoSequence newSequence)
        {
            if (oldSequence == null)
                return newSequence;

            float[] audio = [.. oldSequence.Audio.Span, .. newSequence.Audio.Span];
            int[] dimensions = [oldSequence.Audio.Channels, audio.Length / oldSequence.Audio.Channels];
            var audioTensor = new Tensor<float>(audio, dimensions).AsAudioTensor(oldSequence.Audio.SampleRate);
            ImageTensor[] videoFrames = [.. oldSequence.Frames, .. newSequence.Frames];
            return new VideoSequence(videoFrames, oldSequence.FrameRate, audioTensor);
        }
    }
}
