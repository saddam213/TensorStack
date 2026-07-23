using Amuse.App.Common;
using Amuse.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TensorStack.Common;

namespace Amuse.App
{
    public static class Extensions
    {
        private static readonly SearchValues<char> InvalidPathChars = SearchValues.Create(Path.GetInvalidPathChars());

        /// <summary>
        /// Gets the file extesnion.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        public static string GetExtension(this MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Text => "txt",
                MediaType.Audio => "wav",
                MediaType.Video => "mp4",
                MediaType.Image => "png",
                _ => throw new NotSupportedException()
            };
        }


        public static int GetIndex(this MemoryProfile profile, int deviceMemory)
        {
            int bestIndex = -1;
            int bestValue = int.MinValue;

            for (int i = 0; i < profile.MemoryModes.Length; i++)
            {
                int value = profile.MemoryModes[i];
                if (value <= deviceMemory && value >= bestValue)
                {
                    bestValue = value;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
                bestIndex = 0;

            return bestIndex;
        }


        public static bool HasChanged(this IReadOnlyList<LoraAdapterModel> existingAdapters, IReadOnlyList<LoraAdapterModel> newAdapters)
        {
            if (ReferenceEquals(existingAdapters, newAdapters))
                return false;

            if (existingAdapters == null || newAdapters == null)
                return true;

            if (existingAdapters.Count != newAdapters.Count)
                return true;

            for (int i = 0; i < existingAdapters.Count; i++)
            {
                if (!string.Equals(existingAdapters[i]?.Key, newAdapters[i]?.Key, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }


        public static CheckpointConfig ToConfig(this CheckpointModel checkpoint, Settings settings)
        {
            var modelDirectory = settings.DirectoryDiffusion;
            var checkpointConfig = new CheckpointConfig
            {
                Compute = checkpoint.Compute?.Resolve(settings, modelDirectory),
                TextEncoder = checkpoint.TextEncoder?.Resolve(settings, modelDirectory),
                TextEncoder2 = checkpoint.TextEncoder2?.Resolve(settings, modelDirectory),
                TextEncoder3 = checkpoint.TextEncoder3?.Resolve(settings, modelDirectory),
                Unet = checkpoint.Unet?.Resolve(settings, modelDirectory),
                Transformer = checkpoint.Transformer?.Resolve(settings, modelDirectory),
                Transformer2 = checkpoint.Transformer2?.Resolve(settings, modelDirectory),
                Vae = checkpoint.Vae?.Resolve(settings, modelDirectory),
                AudioVae = checkpoint.AudioVae?.Resolve(settings, modelDirectory),
                Vocoder = checkpoint.Vocoder?.Resolve(settings, modelDirectory),
                Connectors = checkpoint.Connectors?.Resolve(settings, modelDirectory),
                LatentUpsampler = checkpoint.LatentUpsampler?.Resolve(settings, modelDirectory),
                LatentUpsamplerTemporal = checkpoint.LatentUpsamplerTemporal?.Resolve(settings, modelDirectory),
                ConditionEncoder = checkpoint.ConditionEncoder?.Resolve(settings, modelDirectory),
                AudioTokenizer = checkpoint.AudioTokenizer?.Resolve(settings, modelDirectory),
                AudioDetokenizer = checkpoint.AudioDetokenizer?.Resolve(settings, modelDirectory),
            };
            return checkpointConfig;
        }


        public static SchedulerInputOptions[] Copy(this SchedulerInputOptions[] collection)
        {
            if (collection.IsNullOrEmpty())
                return null;

            return collection.Select(x => x with
            {
                ScaleFactors = x.ScaleFactors?.ToList(),
                StageRange = x.StageRange?.ToList(),
                DisableCorrector = x.DisableCorrector?.ToList(),
            }).ToArray();
        }


        public static MemoryProfile[] Copy(this MemoryProfile[] collection)
        {
            if (collection.IsNullOrEmpty())
                return [];

            return collection.Select(x => new MemoryProfile
            {
                QualityMode = x.QualityMode,
                MemoryModes = x.MemoryModes.ToArray(),
            }).ToArray();
        }


        public static SizeOption[] Copy(this SizeOption[] collection)
        {
            if (collection.IsNullOrEmpty())
                return [];

            return collection.Select(x => new SizeOption
            {
                Height = x.Height,
                Width = x.Width,
                IsDefault = x.IsDefault
            }).ToArray();
        }


        public static List<LoraConfig> GetLoraAdapters(this LoraAdapterModel[] loraAdapterModel, Settings settings)
        {
            if (loraAdapterModel.IsNullOrEmpty())
                return default;

            var loraConfigs = new List<LoraConfig>();
            var modelDirectory = settings.DirectoryLoraAdapter;
            foreach (var loraAdapter in loraAdapterModel)
            {
                var resolvedCheckpoint = loraAdapter.Checkpoint?.Resolve(settings, modelDirectory);
                var loraPath = Path.GetDirectoryName(resolvedCheckpoint);
                var loraWeights = Path.GetFileName(resolvedCheckpoint);
                loraConfigs.Add(new LoraConfig
                {
                    Path = loraPath,
                    Weights = loraWeights,
                    Name = loraAdapter.Key
                });
            }

            return loraConfigs;
        }


        public static List<LoraOptions> GetLoraOptions(this DiffusionInputOptions options)
        {
            if (options.LoraOptions.IsNullOrEmpty())
                return default;

            return [.. options.LoraOptions.Select(x => new LoraOptions
            {
                Name = x.Key,
                Strength = x.Strength
            })];
        }


        public static ControlNetConfig GetControlNet(this ControlNetModel model, Settings settings)
        {
            if (model is null)
                return null;

            var resolvedCheckpoint = model.Checkpoint.Resolve(settings, settings.DirectoryControlNet);
            return new ControlNetConfig
            {
                Name = model.Name,
                Path = resolvedCheckpoint,
                Invert = model.Invert,
                LayerCount = model.LayerCount,
                DisableProjections = model.DisableProjections
            };
        }


        public static PipelineLoadOptions ToClientOptions(this PipelineModel pipelineConfig, Settings settings)
        {
            var device = pipelineConfig.Device;
            var model = pipelineConfig.DiffusionModel;
            var controlNet = pipelineConfig.ControlNetModel;
            return new PipelineLoadOptions
            {
                Variant = model.Variant,
                ModelPath = Path.GetFullPath(settings.DirectoryDiffusion),
                Template = model.Template,
                Pipeline = model.Pipeline.ToString(),
                ModelType = model.ModelType,
                ProcessType = pipelineConfig.ProcessType,
                Device = device.DeviceCode,
                DeviceId = device.DeviceId,
                DeviceBusId = device.PCIBusId,
                DeviceVendor = device.Vendor,
                DataType = model.BaseType,
                IsOptimizeDeviceEnabled = settings.IsOptimizeDeviceEnabled,
                IsOptimizeChannelsEnabled = settings.IsOptimizeChannelsEnabled,
                IsDeviceQuantizationEnabled = settings.IsDeviceQuantizationEnabled,
                MemoryMode = pipelineConfig.GetMemoryMode(),
                QuantType = pipelineConfig.GetQuantizationType(),

                ControlNet = controlNet.GetControlNet(settings),
                LoraAdapters = pipelineConfig.LoraAdapterModel.GetLoraAdapters(settings),
                CheckpointConfig = model.Checkpoint.ToConfig(settings)
            };
        }


        private static MemoryModeType GetMemoryMode(this PipelineModel pipeline)
        {
            var memoryMode = pipeline.MemoryMode;
            if (memoryMode == MemoryMode.Auto)
            {
                var memoryProfile = pipeline.DiffusionModel.MemoryProfile.FirstOrDefault(x => x.QualityMode == pipeline.QualityMode);
                if (memoryProfile != null)
                {
                    var deviceMemory = pipeline.Device.MemoryGB;
                    var modeIndex = memoryProfile.GetIndex(deviceMemory);
                    memoryMode = Enum.GetValues<MemoryMode>()[modeIndex + 2];
                }
            }

            return memoryMode switch
            {
                MemoryMode.Balanced => MemoryModeType.Balanced,
                MemoryMode.Low => MemoryModeType.OffloadCPU,
                MemoryMode.Medium => MemoryModeType.OffloadModel,
                MemoryMode.High => MemoryModeType.Device,
                _ => MemoryModeType.OffloadCPU,
            };
        }


        private static QuantizationType GetQuantizationType(this PipelineModel pipeline)
        {
            return pipeline.QualityMode switch
            {
                QualityMode.Draft => QuantizationType.Q4Bit,
                QualityMode.Standard => QuantizationType.Q8Bit,
                QualityMode.Production => QuantizationType.Q16Bit,
                _ => QuantizationType.Q8Bit,
            };
        }


        public static PipelineCreateOptions ToClientOptions(this EnvironmentModel environment, Settings settings, EnvironmentMode environmentMode)
        {
            var environmentConfig = new PipelineCreateOptions
            {
                IsDebug = settings.IsServerDebugEnabled,
                Directory = App.DirectoryPython,
                Environment = environment.Environment,
                PythonVersion = environment.PythonVersion,
                Requirements = environment.Requirements.ToArray(),
                Variables = environment.Variables?.ToDictionary() ?? new Dictionary<string, string>(),
                Mode = environmentMode
            };

            environmentConfig.Variables.Add("HF_HUB_OFFLINE", "1");
            environmentConfig.Variables.Add("HF_HUB_CACHE", settings.DirectoryDiffusion);
            return environmentConfig;
        }


        public static GenerateImageOptions ToClientImageOptions(this DiffusionInputOptions options, DiffusionDefaultOptions defaultOptions, string tempFileName)
        {
            return new GenerateImageOptions
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                Prompt2 = options.Prompt2,
                NegativePrompt = options.NegativePrompt,
                GuidanceScale = options.GuidanceScale,
                GuidanceScale2 = options.GuidanceScale2,
                Steps = options.Steps,
                Steps2 = options.Steps2,
                Width = options.Width,
                Height = options.Height,
                Strength = options.Strength,
                ControlNetScale = options.ControlNetStrength,
                TempFileName = tempFileName,
                EnableVaeSlicing = options.IsVaeSlicingEnabled,
                EnableVaeTiling = options.IsVaeTilingEnabled,
                Language = options.Language,
                Instruction = options.Instruction,
                Task = options.Task,
                MaxLength = defaultOptions.MaxLength,
                MaxLength2 = defaultOptions.MaxLength2,
                SchedulerOptions = options.SchedulerOptions?.ToClientOptions(),
                LoraOptions = options.GetLoraOptions(),
                InputImages = options.InputImages,
                InputControlImages = options.InputControlImages
            };
        }

        public static GenerateVideoOptions ToClientVideoOptions(this DiffusionInputOptions options, DiffusionDefaultOptions defaultOptions, string tempFileName)
        {
            return new GenerateVideoOptions
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                Prompt2 = options.Prompt2,
                NegativePrompt = options.NegativePrompt,
                GuidanceScale = options.GuidanceScale,
                GuidanceScale2 = options.GuidanceScale2,
                Steps = options.Steps,
                Steps2 = options.Steps2,
                Width = options.Width,
                Height = options.Height,
                Frames = options.Frames,
                FrameRate = options.FrameRate,
                Strength = options.Strength,
                ControlNetScale = options.ControlNetStrength,
                TempFileName = tempFileName,
                FrameChunk = options.FrameChunk,
                FrameChunkOverlap = options.FrameChunkOverlap,
                NoiseCondition = options.NoiseCondition,
                EnableVaeSlicing = options.IsVaeSlicingEnabled,
                EnableVaeTiling = options.IsVaeTilingEnabled,
                Duration = options.Duration,
                Language = options.Language,
                Instruction = options.Instruction,
                Task = options.Task,
                MaxLength = defaultOptions.MaxLength,
                MaxLength2 = defaultOptions.MaxLength2,
                SampleRate = defaultOptions.SampleRate,
                SchedulerOptions = options.SchedulerOptions?.ToClientOptions(),
                LoraOptions = options.GetLoraOptions(),
                InputImages = options.InputImages,
                InputControlImages = options.InputControlImages
            };
        }


        public static GenerateAudioOptions ToClientAudioOptions(this DiffusionInputOptions options, DiffusionDefaultOptions defaultOptions, string tempFileName)
        {
            return new GenerateAudioOptions
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                Prompt2 = options.Prompt2,
                NegativePrompt = options.NegativePrompt,
                GuidanceScale = options.GuidanceScale,
                GuidanceScale2 = options.GuidanceScale2,
                Steps = options.Steps,
                Steps2 = options.Steps2,
                Strength = options.Strength,
                TempFileName = tempFileName,
                EnableVaeSlicing = options.IsVaeSlicingEnabled,
                EnableVaeTiling = options.IsVaeTilingEnabled,
                Duration = options.Duration,
                Language = options.Language,
                Instruction = options.Instruction,
                MaxLength = defaultOptions.MaxLength,
                MaxLength2 = defaultOptions.MaxLength2,
                Bpm = options.Bpm,
                Keyscale = options.Keyscale,
                Task = options.Task,
                TrackName = options.TrackName,
                TimeSignature = options.TimeSignature,
                Speed = options.Speed,
                SilenceDuration = options.SilenceDuration,
                SampleRate = defaultOptions.SampleRate,
                SchedulerOptions = options.SchedulerOptions?.ToClientOptions(),
                LoraOptions = options.GetLoraOptions()
            };
        }


        public static GenerateTextOptions ToClientTextOptions(this DiffusionInputOptions options, DiffusionDefaultOptions defaultOptions, string tempFileName)
        {
            return new GenerateTextOptions
            {
                Seed = options.Seed,
                Prompt = options.Prompt,
                Conversation = options.Conversation?.ToClientOptions(),
                TempFileName = tempFileName,
                Language = options.Language,
                MinLength = options.MinLength,
                MaxLength = options.MaxLength,
                Beams = options.Beams,
                NoRepeatNgramSize = options.NoRepeatNgramSize,
                LengthPenalty = options.LengthPenalty,
                Temperature = options.Temperature,
                TopK = options.TopK,
                TopP = options.TopP,
                TopH = options.TopH,
                TypicalP = options.TypicalP,
                RepetitionPenalty = options.RepetitionPenalty,
                IsSamplingEnabled = options.IsSamplingEnabled,
                ChunkSize = options.ChunkSize,
                EarlyStopping = options.EarlyStopping.ToString(),
                Instruction = options.Instruction,
                Task = options.Task,
                InputImages = options.InputImages,
            };
        }


        public static ConversationMessage[] ToClientOptions(this List<ConversationModel> conversation)
        {
            if (conversation.IsNullOrEmpty())
                return default;

            return [.. conversation.Select(x => new ConversationMessage(x.Role, x.Content, [.. x.ImageIndex ?? []]))];
        }


        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return path.AsSpan().IndexOfAny(InvalidPathChars) == -1;
        }


        public static bool AddIfNotNull<TSource>(this IList<TSource> source, TSource item)
        {
            if (item is null)
                return false;

            source.Add(item);
            return true;
        }


        public static int RemoveAll<T>(this IList<T> collection, Predicate<T> condition)
        {
            var removed = 0;
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (condition(collection[i]))
                {
                    collection.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }


        public static int NextId<T>(this Collection<T> collection) where T : IDownloadModel
        {
            return collection.NextId(x => x.Id);
        }


        public static int NextId<T>(this Collection<T> collection, Func<T, int> selector)
        {
            var nextId = Utils.FixedIdRange + 1;
            if (collection.IsNullOrEmpty())
                return nextId;

            return Math.Max(nextId, collection.Max(selector) + 1);
        }


        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }

    public static partial class Utils
    {
        public const int FixedIdRange = 1000;


        public static bool HasThinkingText(string content, string tagOpen = "<think>", string tagClose = "</think>")
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return content.StartsWith(tagOpen, StringComparison.OrdinalIgnoreCase)
                && content.Contains(tagClose, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Gets the thinking text.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="tagOpen">The tag open.</param>
        /// <param name="tagClose">The tag close.</param>
        public static string GetThinkingText(string content, string tagOpen = "<think>", string tagClose = "</think>")
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            if (content.StartsWith(tagOpen, StringComparison.OrdinalIgnoreCase))
            {
                var start = tagOpen.Length;
                var end = content.IndexOf(tagClose, StringComparison.OrdinalIgnoreCase);
                if (end > start)
                    return content[start..end].Trim();
            }
            return string.Empty;
        }


        /// <summary>
        /// Gets the response text.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="tagOpen">The tag open.</param>
        /// <param name="tagClose">The tag close.</param>
        /// <returns>System.String.</returns>
        public static string GetResponseText(string content, string tagOpen = "<think>", string tagClose = "</think>")
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            if (content.StartsWith(tagOpen, StringComparison.OrdinalIgnoreCase))
            {
                var start = content.IndexOf(tagClose, StringComparison.OrdinalIgnoreCase);
                if (start > 0)
                    return content[(start + tagClose.Length)..].Trim();
            }
            return content;
        }

    }



    public static class FontOptions
    {
        public static FontWeight[] FontWeightList { get; } = new[]
           {
            FontWeights.Thin,
            FontWeights.ExtraLight,
            FontWeights.Light,
            FontWeights.Normal,
            FontWeights.Medium,
            FontWeights.SemiBold,
            FontWeights.Bold,
            FontWeights.ExtraBold,
            FontWeights.Black
        };


        public static FontStyle[] FontStyleList { get; } = new[]
        {
            FontStyles.Normal,
            FontStyles.Italic,
            FontStyles.Oblique
        };


        public static ICollection<FontFamily> FontFamilies { get; } = System.Windows.Media.Fonts.SystemFontFamilies;
    }


    public static class BrushOptions
    {
        public static IEnumerable<Brush> AllBrushes { get; } =
            typeof(Brushes).GetProperties()
                .Where(p => p.PropertyType == typeof(Brush))
                .Select(p => (Brush)p.GetValue(null));
    }
}
