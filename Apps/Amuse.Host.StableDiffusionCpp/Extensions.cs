using Amuse.Common;
using Amuse.Host.StableDiffusionCpp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using TensorStack.Common;


namespace Amuse.Host.StableDiffusionCpp
{
    public static class Extensions
    {

        public static Config.ServerConfig ToServerConfig(this PipelineLoadOptions options, PipelineCreateOptions createOptions)
        {
            var modelConfig = GetModelConfig(options);
            var config = new Config.ServerConfig
            {
                Address = createOptions.ServerAddress,
                Port = GetOpenPort(createOptions.ServerPort),
                BackendDirectory = Path.Combine(AppContext.BaseDirectory, "Backend"),
                DeviceId = options.DeviceId,
                Backend = Common.BackendType.CUDA,
                MemoryMode = options.MemoryMode,
                ModelConfig = modelConfig,
                IsFlashAttentionEnabled = true,
                QuantizationType = options.QuantType,
            };

            return config;
        }


        public static ImageParams ToServerParams(this GenerateImageOptions options, PipelineLoadOptions loadOptions, ImageParams defaultOptions)
        {
            // TODO: input tensors to base64

            return defaultOptions with
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                NegativePrompt = options.NegativePrompt ?? "",
                Width = options.Width,
                Height = options.Height,
                Strength = options.Strength,
                ControlStrength = options.ControlNetScale,
                SampleParams = new SampleParams
                {
                    SampleSteps = options.Steps,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                },
                Lora = loadOptions.LoraAdapters.GetLoraOptions(options.LoraOptions)
            };
        }


        public static VideoParams ToServerParams(this GenerateVideoOptions options, PipelineLoadOptions loadOptions, VideoParams defaultOptions)
        {
            // TODO: input tensors to base64
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
                SampleParams = new SampleParams
                {
                    SampleSteps = options.Steps,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                },
                Lora = loadOptions.LoraAdapters.GetLoraOptions(options.LoraOptions)
            };
        }


        private static List<LoraParams> GetLoraOptions(this List<LoraConfig> loraAdapters, List<LoraOptions> loraAdapterOptions)
        {
            if (loraAdapterOptions.IsNullOrEmpty())
                return [];

            var loraParams = new List<LoraParams>();
            foreach (var config in loraAdapters)
            {
                var options = loraAdapterOptions.FirstOrDefault(x => x.Name == config.Name);
                loraParams.Add(new LoraParams
                {
                    Multiplier = options.Strength,
                    Path = Path.Combine(config.Path, config.Weights),
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
                    Diffusion = options.CheckpointConfig.Transformer
                };
            }
            if (options.Pipeline == "AnimaPipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    LLM = options.CheckpointConfig.TextEncoder,
                    Diffusion = options.CheckpointConfig.Transformer
                };
            }

            throw new NotImplementedException(options.Pipeline);
        }


        private static string GetSampler(SchedulerOptions options)
        {
            switch (options.Scheduler)
            {
                case SchedulerType.LMS:
                    return "lms";
                case SchedulerType.Euler:
                    return "euler";
                case SchedulerType.EulerAncestral:
                    return "euler_a";
                case SchedulerType.DDIM:
                    return "ddim_trailing";
                case SchedulerType.LCM:
                    return "lcm";
                case SchedulerType.FlowMatchEuler:
                    return "euler";
                case SchedulerType.FlowMatchHeun:
                    return "heun";
                case SchedulerType.Heun:
                    return "heun";
                case SchedulerType.DPMSolverMultistep:
                    return "dpm++2m";
                case SchedulerType.DPMSolverSinglestep:
                    return "dpm++2s_a";
                case SchedulerType.DPMSolverSDE:
                    return "dpm++2m_sde";
                case SchedulerType.FlowMatchLCM:
                    return "lcm";
                case SchedulerType.IPNDM:
                    return "ipndm";
                case SchedulerType.TCD:
                    return "tcd";
                default:
                    break;
            }
            return "default";
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
    }
}
