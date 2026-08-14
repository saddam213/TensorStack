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


        public static ImageParams ToServerParams(this GenerateImageOptions options, Config.ModelConfig modelConfig, PipelineLoadOptions loadOptions, ImageParams defaultOptions)
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
                Lora = loadOptions.LoraAdapters.GetLoraOptions(modelConfig.LoraModelDirectory, options.LoraOptions),
                SampleParams = new SampleParams
                {
                    SampleSteps = options.Steps,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Scheduler = GetSigmaSchedule(options.SchedulerOptions),
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                }
            };
        }


        public static VideoParams ToServerParams(this GenerateVideoOptions options, Config.ModelConfig modelConfig, PipelineLoadOptions loadOptions, VideoParams defaultOptions)
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
                Lora = loadOptions.LoraAdapters.GetLoraOptions(modelConfig.LoraModelDirectory, options.LoraOptions),
                SampleParams = new SampleParams
                {
                    SampleSteps = options.Steps,
                    SampleMethod = GetSampler(options.SchedulerOptions),
                    Scheduler = GetSigmaSchedule(options.SchedulerOptions),
                    Guidance = new GuidanceParams
                    {
                        TxtCfg = Math.Max(1, options.GuidanceScale),
                        DistilledGuidance = Math.Max(1, options.GuidanceScale2)
                    }
                }
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
            var loraModelDirectory = options.LoraAdapters?.FirstOrDefault()?.Path;
            if (options.Pipeline == "FluxPipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    ClipL = options.CheckpointConfig.TextEncoder,
                    T5XXL = options.CheckpointConfig.TextEncoder2,
                    Diffusion = options.CheckpointConfig.Transformer,
                    LoraModelDirectory = loraModelDirectory
                };
            }
            if (options.Pipeline == "AnimaPipeline")
            {
                return new Config.ModelConfig
                {
                    Vae = options.CheckpointConfig.Vae,
                    LLM = options.CheckpointConfig.TextEncoder,
                    Diffusion = options.CheckpointConfig.Transformer,
                    LoraModelDirectory = loraModelDirectory
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
    }
}
