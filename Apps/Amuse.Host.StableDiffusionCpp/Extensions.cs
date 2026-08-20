using Amuse.Common;
using Amuse.Host.StableDiffusionCpp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Media.Image;

namespace Amuse.Host.StableDiffusionCpp
{
    public static class Extensions
    {
        public static Config.ServerConfig ToServerConfig(this PipelineLoadOptions loadOptions, PipelineCreateOptions createOptions)
        {
            if (!GetBackend(createOptions, out var backendType))
                throw new Exception($"{loadOptions.DeviceVendor} Backend Not Found.");

            var deviceId = backendType == Common.BackendType.Vulkan
                ? loadOptions.DeviceId
                : loadOptions.DeviceVendorIndex;
            var modelConfig = GetModelConfig(loadOptions);
            var config = new Config.ServerConfig
            {
                IsDebug = createOptions.IsDebug,
                Address = createOptions.ServerAddress,
                Port = GetOpenPort(createOptions.ServerPort),
                Directory = Path.Combine(createOptions.Directory, createOptions.Environment),
                DeviceId = deviceId,
                Backend = backendType,
                MemoryMode = loadOptions.MemoryMode,
                ModelConfig = modelConfig,
                QuantizationType = loadOptions.QuantType,
                IsFlashAttentionEnabled = loadOptions.IsFlashAttentionEnabled,
            };

            return config;
        }


        public static ImageParams ToServerParams(this GenerateImageOptions options, Config.ModelConfig modelConfig, PipelineLoadOptions loadOptions, ImageParams defaultOptions)
        {
            return defaultOptions with
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                NegativePrompt = options.NegativePrompt ?? "",
                Width = options.Width,
                Height = options.Height,
                Strength = options.Strength,
                ControlStrength = options.ControlNetScale,
                Lora = loadOptions.LoraAdapters.GetLoraOptions(modelConfig.LoraModelDirectory, options.LoraOptions),
                InitImage = GetInitImage(options, loadOptions.ProcessType),
                RefImages = GetReferenceImages(options, loadOptions.ProcessType),
                ControlImage = GetControlNetImage(options, loadOptions.ProcessType),
                MaskImage = GetMaskImage(options, loadOptions.ProcessType),
                SampleParams = new SampleParams
                {
                    SampleSteps = options.Steps,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Scheduler = GetSigmaSchedule(options.SchedulerOptions),
                    Eta = options.SchedulerOptions.Eta > 0 ? options.SchedulerOptions.Eta : null,
                    FlowShift = options.SchedulerOptions.FlowShift > 0 ? options.SchedulerOptions.FlowShift : null,
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                },
                VaeTilingParams = new VaeTilingParams
                {
                    Enabled = options.EnableVaeTiling
                },
                HiresParams = GetHiresParams(loadOptions, options)
            };
        }


        public static VideoParams ToServerParams(this GenerateVideoOptions options, Config.ModelConfig modelConfig, PipelineLoadOptions loadOptions, VideoParams defaultOptions)
        {
            return defaultOptions with
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                NegativePrompt = options.NegativePrompt ?? "",
                Width = options.Width,
                Height = options.Height,
                Strength = options.Strength,
                Frames = options.Frames,
                FrameRate = (int)options.FrameRate,
                Lora = loadOptions.LoraAdapters.GetLoraOptions(modelConfig.LoraModelDirectory, options.LoraOptions),
                ImageFirst = GetFirstFrame(options, loadOptions.ProcessType),
                ImageLast = GetLastFrame(options, loadOptions.ProcessType),
                ControlFrames = GetControlFrames(options, loadOptions.ProcessType),
                VaceStrength = options.ControlNetScale,
                SampleParams = new SampleParams
                {
                    SampleSteps = options.Steps,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Scheduler = GetSigmaSchedule(options.SchedulerOptions),
                    Eta = options.SchedulerOptions.Eta > 0 ? options.SchedulerOptions.Eta : null,
                    FlowShift = options.SchedulerOptions.FlowShift > 0 ? options.SchedulerOptions.FlowShift : null,
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                },
                SampleParamsHighNoise = new SampleParams
                {
                    SampleSteps = options.Steps2,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Scheduler = GetSigmaSchedule(options.SchedulerOptions),
                    Eta = options.SchedulerOptions.Eta > 0 ? options.SchedulerOptions.Eta : null,
                    FlowShift = options.SchedulerOptions.FlowShift > 0 ? options.SchedulerOptions.FlowShift : null,
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                },
                VaeTilingParams = new VaeTilingParams
                {
                    Enabled = options.EnableVaeTiling,
                    TemporalTiling = options.EnableVaeSlicing
                },
                HiresParams = GetHiresParams(loadOptions, options)
            };
        }


        private static List<LoraParams> GetLoraOptions(this List<LoraConfig> loraAdapters, string loraModelDirectory, List<LoraOptions> loraAdapterOptions)
        {
            if (loraAdapterOptions.IsNullOrEmpty())
                return [];

            var loraParams = new List<LoraParams>();
            foreach (var config in loraAdapters.Where(x => x.Path == loraModelDirectory))
            {
                var options = loraAdapterOptions.FirstOrDefault(x => x.Name == config.Name);
                if (options == null)
                    continue;

                loraParams.Add(new LoraParams
                {
                    Multiplier = options.Strength,
                    Path = config.Weights,
                });
            }
            return loraParams;
        }


        private static Config.ModelConfig GetModelConfig(PipelineLoadOptions options)
        {
            if (options.Pipeline == "FluxPipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    ClipL = options.CheckpointConfig.TextEncoder,
                    T5XXL = options.CheckpointConfig.TextEncoder2,
                    Diffusion = options.CheckpointConfig.Transformer,
                    LoraModelDirectory = options.LoraAdapterPath
                };
            }
            if (options.Pipeline == "IdeogramPipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    LLM = options.CheckpointConfig.TextEncoder,
                    Diffusion = options.CheckpointConfig.Transformer,
                    DiffusionUncond = options.CheckpointConfig.Transformer2,
                    LoraModelDirectory = options.LoraAdapterPath
                };
            }
            if (options.Pipeline == "LTX20Pipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    VaeAudio = options.CheckpointConfig.AudioVae,
                    LLM = options.CheckpointConfig.TextEncoder,
                    Connectors = options.CheckpointConfig.Connectors,
                    Diffusion = options.CheckpointConfig.Transformer,
                    LoraModelDirectory = options.LoraAdapterPath,
                    UpscaleModelDirectory = GetHiresModelPath(options)
                };
            }
            if (options.Pipeline == "QwenImagePipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    LLM = options.CheckpointConfig.TextEncoder,
                    Diffusion = options.CheckpointConfig.Transformer,
                    LoraModelDirectory = options.LoraAdapterPath,
                    ExtraModelArgs = "qwen_image_zero_cond_t=true" // TODO: should be optional
                };
            }
            if (options.Pipeline == "AnimaPipeline"
             || options.Pipeline == "ErniePipeline"
             || options.Pipeline == "Flux2KleinPipeline"
             || options.Pipeline == "Krea2Pipeline"
             || options.Pipeline == "ZImagePipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    LLM = options.CheckpointConfig.TextEncoder,
                    Diffusion = options.CheckpointConfig.Transformer,
                    LoraModelDirectory = options.LoraAdapterPath
                };
            }
            throw new NotImplementedException(options.Pipeline);
        }


        private static string GetSampler(SchedulerOptions options)
        {
            return options.Scheduler switch
            {
                SchedulerType.Euler => "euler",
                SchedulerType.EulerAncestral => "euler_a",
                SchedulerType.Heun => "heun",
                SchedulerType.DPM2 => "dpm2",
                SchedulerType.DPMPlusPlus2SAncestral => "dpm++2s_a",
                SchedulerType.DPMPlusPlus2M => "dpm++2m",
                SchedulerType.DPMPlusPlus2Mv2 => "dpm++2mv2",
                SchedulerType.IPNDM => "ipndm",
                SchedulerType.LCM => "lcm",
                SchedulerType.DDIM => "ddim_trailing",
                SchedulerType.TCD => "tcd",
                SchedulerType.ResidualMultistep => "res_multistep",
                SchedulerType.Residual2S => "res_2s",
                SchedulerType.ERSDE => "er_sde",
                SchedulerType.DPMPlusPlus2MSDE => "dpm++2m_sde",
                SchedulerType.DPMPlusPlus2MSDEBT => "dpm++2m_sde_bt",
                SchedulerType.LMS => "lms",
                _ => "default"
            };
        }


        private static string GetSigmaSchedule(SchedulerOptions options)
        {
            return options.SigmaScheduleType switch
            {
                SigmaScheduleType.Discrete => "discrete",
                SigmaScheduleType.Normal => "normal",
                SigmaScheduleType.Karras => "karras",
                SigmaScheduleType.Exponential => "exponential",
                SigmaScheduleType.AYS => "ays",
                SigmaScheduleType.GITS => "gits",
                SigmaScheduleType.SGMUniform => "sgm_uniform",
                SigmaScheduleType.Simple => "simple",
                SigmaScheduleType.Smoothstep => "smoothstep",
                SigmaScheduleType.KLOptimal => "kl_optimal",
                SigmaScheduleType.LCM => "lcm",
                SigmaScheduleType.BongTangent => "bong_tangent",
                SigmaScheduleType.LTX2 => "ltx2",
                SigmaScheduleType.LogitNormal => "logit_normal",
                SigmaScheduleType.Flux => "flux",
                SigmaScheduleType.Flux2 => "flux2",
                SigmaScheduleType.Beta => "beta",
                _ => "default"
            };
        }


        /// <summary>
        /// Gets the open server port.
        /// </summary>
        /// <param name="defaultPort">The default port.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception">Unable to locate open port</exception>
        private static short GetOpenPort(int defaultPort)
        {
            int PortStart = 2000;
            int PortEnd = 8000;
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndpoints = properties.GetActiveTcpListeners();
            var usedPorts = tcpEndpoints
                .Select(endpoint => endpoint.Port)
                .Where(port => port >= PortStart && port <= PortEnd)
                .OrderBy(port => port)
                .Distinct();

            if (!usedPorts.Contains(defaultPort))
                return (short)defaultPort;

            int openPort = PortStart;
            foreach (int usedPort in usedPorts)
            {
                if (usedPort != openPort)
                    break;
                openPort++;
            }

            if (openPort <= PortEnd)
                return (short)openPort;

            throw new Exception("Unable to locate open port");
        }


        private static bool GetBackend(PipelineCreateOptions createOptions, out Common.BackendType backendType)
        {
            return Enum.TryParse<Common.BackendType>(createOptions.HostVersion, true, out backendType);
        }


        private static string GetInitImage(GenerateImageOptions options, ProcessType processType)
        {
            if (options.InputImages.IsNullOrEmpty())
                return default;

            if (processType == ProcessType.ImageToImage || processType == ProcessType.ImageToImageControlNet || processType == ProcessType.ImageInpaint)
                return GetBase64Image(options.InputImages[0]);

            return default;
        }


        private static string GetControlNetImage(GenerateImageOptions options, ProcessType processType)
        {
            if (options.InputControlImages.IsNullOrEmpty())
                return default;

            if (processType == ProcessType.ImageControlNet || processType == ProcessType.ImageToImageControlNet)
                return GetBase64Image(options.InputControlImages[0]);

            return default;
        }


        private static List<string> GetReferenceImages(GenerateImageOptions options, ProcessType processType)
        {
            if (options.InputImages.IsNullOrEmpty())
                return default;

            if (processType == ProcessType.ImageEdit)
                return GetBase64Images(options.InputImages);

            return default;
        }


        private static string GetMaskImage(GenerateImageOptions options, ProcessType processType)
        {
            if (options.InputImages.IsNullOrEmpty())
                return default;

            if (options.InputImages.Count < 2)
                return default;

            if (processType == ProcessType.ImageInpaint)
                return GetBase64Image(options.InputImages[1]);

            return default;
        }


        private static string GetFirstFrame(GenerateVideoOptions options, ProcessType processType)
        {
            if (options.InputImages.IsNullOrEmpty())
                return default;

            if (options.InputImages.Count > 2)
                return default;

            if (processType == ProcessType.ImageToVideo)
                return GetBase64Image(options.InputImages[0]);

            return default;
        }


        private static string GetLastFrame(GenerateVideoOptions options, ProcessType processType)
        {
            if (options.InputImages.IsNullOrEmpty())
                return default;

            if (options.InputImages.Count != 2)
                return default;

            if (processType == ProcessType.ImageToVideo)
                return GetBase64Image(options.InputImages.Last());

            return default;
        }


        private static List<string> GetControlFrames(GenerateVideoOptions options, ProcessType processType)
        {
            if (options.InputImages.IsNullOrEmpty())
                return default;

            if (options.InputImages.Count > 2 && processType == ProcessType.ImageToVideo)
                return GetBase64Images(options.InputImages);

            return default;
        }


        private static string GetBase64Image(this ImageTensor imageTensor)
        {
            if (imageTensor == null)
                return string.Empty;

            return imageTensor.ToImageBase64();
        }


        private static List<string> GetBase64Images(this List<ImageTensor> imageTensors)
        {
            if (imageTensors.IsNullOrEmpty())
                return default;

            var base64Images = new List<string>();
            foreach (var imageTensor in imageTensors)
            {
                var base64Image = GetBase64Image(imageTensor);
                if (string.IsNullOrEmpty(base64Image))
                    continue;

                base64Images.Add(base64Image);
            }
            return base64Images;
        }


        public static void SendMessage(this IProgress<PipelineProgress> progressCallback, string message)
        {
            progressCallback?.Report(new PipelineProgress
            {
                Message = message,
                Key = "Initialize"
            });
        }


        private static HiresParams GetHiresParams(PipelineLoadOptions loadOptions, GenerateImageOptions generateOptions)
        {
            if (generateOptions.LatentUpscale == LatentUpscale.Model || generateOptions.LatentUpscale == LatentUpscale.None)
                return default;

            var tileSize = generateOptions.LatentUpscaleTileSize <= 0 ? 64 : generateOptions.LatentUpscaleTileSize;
            var steps = generateOptions.LatentUpscaleSteps <= 0 ? generateOptions.Steps / 2 : generateOptions.LatentUpscaleSteps;
            return new HiresParams
            {
                Steps = steps,
                Enabled = true,
                UpscaleTileSize= tileSize,
                Upscaler = generateOptions.LatentUpscale.GetName(),
                DenoisingStrength = generateOptions.LatentUpscaleStrength,
            };
        }


        private static HiresParams GetHiresParams(PipelineLoadOptions loadOptions, GenerateVideoOptions generateOptions)
        {
            if (generateOptions.LatentUpscale == LatentUpscale.None)
                return default;

            var upscaleName = generateOptions.LatentUpscale.GetName();
            if (generateOptions.LatentUpscale == LatentUpscale.Model)
            {
                if (!File.Exists(loadOptions.CheckpointConfig.LatentUpsampler))
                    return default;

                upscaleName = Path.GetFileNameWithoutExtension(loadOptions.CheckpointConfig.LatentUpsampler);
            }

            var tileSize = generateOptions.LatentUpscaleTileSize <= 0 ? 64 : generateOptions.LatentUpscaleTileSize;
            var steps = generateOptions.LatentUpscaleSteps <= 0 ? generateOptions.Steps / 2 : generateOptions.LatentUpscaleSteps;
            return new HiresParams
            {
                Steps = steps,
                Enabled = true,
                Upscaler = upscaleName,
                UpscaleTileSize = tileSize,
                CustomSigmas = [0.85f, 0.725f, 0.421875f, 0.0f], // TODO: optional
                DenoisingStrength = generateOptions.LatentUpscaleStrength,
            };
        }


        private static string GetHiresModelPath(PipelineLoadOptions loadOptions)
        {
            if (!File.Exists(loadOptions.CheckpointConfig.LatentUpsampler))
                return default;

            return Path.GetDirectoryName(loadOptions.CheckpointConfig.LatentUpsampler);
        }
    }
}
