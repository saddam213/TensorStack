using System;
using System.Collections.Generic;
using System.Globalization;
using TensorStack.AudioCpp.Common;

namespace TensorStack.AudioCpp.Native
{
    internal static class OptionExtensions
    {
        private const string TaskGenerate = "gen";
        private const string TaskSpeech = "tts";
        private const string TaskClone = "clon";
        private const string TaskRecognition = "asr";
        private const string PipelineMiniMaxMusic = "minimax_music3";
        private const string PipelineStableAudio = "stable_audio";
        private const string PipelineYuE = "yue2";
        private const int YuESemanticFrameRate = 25;
        private const int YuESemanticMinTokens = 200;

        /// <summary>
        /// Creates the session options (--session-option).
        /// </summary>
        /// <param name="options">The options.</param>
        internal static Dictionary<string, string> ToSessionOptions(this AudioCppOptions options)
        {
            var sessionOptions = new Dictionary<string, string>
            {
                ["weight_type"] = options.DataType == DataType.Default ? default : options.DataType.ToString().ToLowerInvariant(),
                ["mem_saver"] = options.IsMemorySaverEnabled ? "true" : default
            };
            return sessionOptions.Merge(options.SessionOptions);
        }


        /// <summary>
        /// Creates the request for audio generation (music, sound effects, speech).
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="pipeline">The audio.cpp pipeline (model family) of the loaded model.</param>
        /// <param name="isSpeech">if set to <c>true</c> the model generates speech.</param>
        internal static AudioCppRequest ToRequest(this GenerateAudioOptions options, string pipeline, bool isSpeech)
        {
            var inputAudio = options.InputAudios == null || options.InputAudios.Count == 0 ? default : options.InputAudios[0];
            var language = options.Language.DefaultIfWhiteSpace();
            var requestOptions = new Dictionary<string, string>
            {
                ["seed"] = options.Seed >= 0 ? ToString(options.Seed) : default,
                ["num_inference_steps"] = options.Steps > 0 ? ToString(options.Steps) : default,
                ["guidance_scale"] = options.GuidanceScale > 0 ? ToString(options.GuidanceScale) : default,
                ["duration_sec"] = options.Duration > 0 ? ToString(options.Duration) : default,
                ["negative_prompt"] = options.NegativePrompt.DefaultIfWhiteSpace(),
                ["lyrics"] = options.Prompt2.DefaultIfWhiteSpace(),
                ["instruct"] = options.Instruction.DefaultIfWhiteSpace(),
                ["language"] = language,
                ["bpm"] = options.Bpm > 0 ? ToString(options.Bpm) : default,
                ["keyscale"] = options.Keyscale.DefaultIfWhiteSpace(),
                ["timesignature"] = options.TimeSignature.DefaultIfWhiteSpace(),
                ["track_name"] = options.TrackName.DefaultIfWhiteSpace(),
                ["speed"] = options.Speed > 0 ? ToString(options.Speed) : default
            };

            // Speech: Task is the voice, Music: Task is the route (text2music, cover, repaint etc)
            var text = options.Prompt ?? string.Empty;
            if (isSpeech)
            {
                requestOptions["voice_id"] = options.Task.DefaultIfWhiteSpace();
            }
            else if (pipeline == PipelineMiniMaxMusic)
            {
                // MiniMax Music: Prompt is the caption, Prompt2 is the lyrics
                requestOptions["ar_guidance_scale"] = options.GuidanceScale2 > 0 ? ToString(options.GuidanceScale2) : default;
            }
            else if (pipeline == PipelineStableAudio)
            {
                // Stable Audio: input audio is the init audio, Strength is the noise level
                if (inputAudio != null)
                {
                    requestOptions["audio_input_kind"] = "init_audio";
                    requestOptions["init_noise_level"] = ToString(Math.Clamp(options.Strength, 0f, 1f));
                }
            }
            else if (pipeline == PipelineYuE)
            {
                // YuE: text is the lyrics (empty for instrumental), Prompt is the style, Task is the planning route (full, melody, off)
                text = options.Prompt2 ?? string.Empty;
                requestOptions["style"] = options.Prompt.DefaultIfWhiteSpace();
                requestOptions["cot"] = options.Task.DefaultIfWhiteSpace();

                // YuE has no duration option, the length is bounded by the semantic token budget
                if (options.Duration > 0)
                {
                    var semanticTokens = Math.Max(1, (int)MathF.Round(options.Duration * YuESemanticFrameRate));
                    requestOptions["semantic_max_tokens"] = ToString(semanticTokens);
                    if (semanticTokens < YuESemanticMinTokens)
                        requestOptions["semantic_min_tokens"] = ToString(semanticTokens);
                }
            }
            else
            {
                requestOptions["route"] = options.Task.DefaultIfWhiteSpace();
            }

            return new AudioCppRequest
            {
                Tasks = GetGenerateTasks(isSpeech, inputAudio != null),
                Text = text,
                Language = language,
                Audio = isSpeech ? default : inputAudio,
                VoiceAudio = isSpeech ? inputAudio : default,
                VoiceId = isSpeech ? options.Task.DefaultIfWhiteSpace() : default,
                Options = requestOptions.Merge(options.Options)
            };
        }


        /// <summary>
        /// Creates the request for text generation (speech recognition).
        /// </summary>
        /// <param name="options">The options.</param>
        internal static AudioCppRequest ToRequest(this GenerateTextOptions options)
        {
            if (options.InputAudios == null || options.InputAudios.Count == 0)
                throw new ArgumentException("Speech recognition requires an input audio.");

            var language = options.Language.DefaultIfWhiteSpace();
            var requestOptions = new Dictionary<string, string>
            {
                ["seed"] = options.Seed >= 0 ? ToString(options.Seed) : default,
                ["language"] = language,
                ["max_tokens"] = options.MaxLength > 0 ? ToString(options.MaxLength) : default,
                ["num_beams"] = options.Beams > 0 ? ToString(options.Beams) : default,
                ["do_sample"] = options.IsSamplingEnabled ? "true" : default,
                ["temperature"] = options.IsSamplingEnabled && options.Temperature > 0 ? ToString(options.Temperature) : default,
                ["top_k"] = options.IsSamplingEnabled && options.TopK > 0 ? ToString(options.TopK) : default,
                ["top_p"] = options.IsSamplingEnabled && options.TopP > 0 ? ToString(options.TopP) : default,
                ["repetition_penalty"] = options.RepetitionPenalty > 0 ? ToString(options.RepetitionPenalty) : default,
                ["audio_chunk_duration_sec"] = options.ChunkSize > 0 ? ToString(options.ChunkSize) : default
            };

            return new AudioCppRequest
            {
                Tasks = [TaskRecognition],
                Text = options.Prompt.DefaultIfWhiteSpace(),
                Language = language,
                Audio = options.InputAudios[0],
                Options = requestOptions.Merge(options.Options)
            };
        }


        /// <summary>
        /// Gets the candidate tasks in order of preference.
        /// </summary>
        /// <param name="isSpeech">if set to <c>true</c> the model generates speech.</param>
        /// <param name="hasVoiceAudio">if set to <c>true</c> a speaker reference is supplied.</param>
        private static string[] GetGenerateTasks(bool isSpeech, bool hasVoiceAudio)
        {
            if (!isSpeech)
                return [TaskGenerate, TaskSpeech];

            return hasVoiceAudio
                ? [TaskClone, TaskSpeech, TaskGenerate]
                : [TaskSpeech, TaskGenerate];
        }


        /// <summary>
        /// Merges the additional options over the options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="additional">The additional options.</param>
        private static Dictionary<string, string> Merge(this Dictionary<string, string> options, Dictionary<string, string> additional)
        {
            if (additional == null)
                return options;

            foreach (var option in additional)
            {
                options[option.Key] = option.Value;
            }
            return options;
        }


        /// <summary>
        /// Formats the value as an audio.cpp option string.
        /// </summary>
        /// <param name="value">The value.</param>
        private static string ToString(IFormattable value)
        {
            return value.ToString(null, CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Return default string if its only white space.
        /// </summary>
        /// <param name="value">The value.</param>
        private static string DefaultIfWhiteSpace(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            return value;
        }
    }
}
