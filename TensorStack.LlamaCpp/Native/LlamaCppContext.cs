using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.LlamaCpp.Common;
using TensorStack.LlamaCpp.Templates;
using TensorStack.Media.Audio;

namespace TensorStack.LlamaCpp.Native
{
    internal sealed unsafe class LlamaCppContext : IDisposable
    {
        private const int SequenceId = 0;
        private readonly Action<LogLevelType, string> _logCallback;
        private readonly LlamaCppOptions _options;
        private readonly LlamaModelSafeHandle _modelHandle;
        private readonly MtmdContextSafeHandle _projectorHandle;
        private readonly PromptFormatter _formatter;
        private readonly LlamaVocabulary _vocabulary;
        private readonly List<CachedChunk> _cache = [];
        private readonly string _mediaMarker;
        private readonly string _description;
        private readonly int _slidingWindow;
        private readonly int _trainedContextSize;
        private LlamaContextSafeHandle _contextHandle;
        private LlamaBatchSafeHandle _batchHandle;
        private int _requestedContextSize;
        private int _contextSize;
        private int _batchSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaCppContext"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="logCallback">The log callback.</param>
        public LlamaCppContext(LlamaCppOptions options, Action<LogLevelType, string> logCallback = null)
        {
            _options = options;
            _logCallback = logCallback;
            NativeApi.SetLogCallback(_logCallback);
            try
            {
                // Device
                var device = SelectDevice(options);

                // Model
                _modelHandle = LoadModel(options, device);
                _description = GetDescription(_modelHandle.DangerousGetHandle());
                _slidingWindow = NativeApi.llama_model_n_swa(_modelHandle.DangerousGetHandle());
                _trainedContextSize = NativeApi.llama_model_n_ctx_train(_modelHandle.DangerousGetHandle());

                // Context
                InitializeContext(options.ContextSize);

                // Projector
                if (!string.IsNullOrEmpty(options.ProjectorPath))
                    _projectorHandle = LoadProjector(options, _modelHandle.DangerousGetHandle(), device);

                // Vocabulary
                _mediaMarker = NativeApi.PtrToString(NativeApi.mtmd_default_marker());
                _formatter = PromptFormatter.Create(options.Template, _mediaMarker);
                _vocabulary = new LlamaVocabulary(NativeApi.llama_model_get_vocab(_modelHandle.DangerousGetHandle()), _formatter, _mediaMarker);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the description of the loaded model.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Gets the context size in tokens.
        /// </summary>
        public int ContextSize => _contextSize;

        /// <summary>
        /// Gets a value indicating whether the model accepts image input.
        /// </summary>
        public bool IsVisionSupported => _projectorHandle != null && NativeApi.mtmd_support_vision(_projectorHandle.DangerousGetHandle());

        /// <summary>
        /// Gets a value indicating whether the model accepts audio input.
        /// </summary>
        public bool IsAudioSupported => _projectorHandle != null && NativeApi.mtmd_support_audio(_projectorHandle.DangerousGetHandle());


        /// <summary>
        /// Generates text.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="tokenCallback">The callback that receives the generated text, the total token count and the elapsed milliseconds per token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public TextInput GenerateText(GenerateTextOptions options, Action<string, int, float> tokenCallback = null, CancellationToken cancellationToken = default)
        {
            // Context, recreated when a different size is requested, the model stays loaded
            ResizeContext(options.ContextSize);

            var context = _contextHandle.DangerousGetHandle();
            var chunks = nint.Zero;
            var bitmaps = new List<nint>();
            NativeApi.SetAbortRequested(false);
            using (cancellationToken.Register(() => NativeApi.SetAbortRequested(true)))
            using (var sampler = CreateSampler(options))
            {
                try
                {
                    // Prompt
                    var timestamp = Stopwatch.GetTimestamp();
                    var prompt = _formatter.Format(GetMessages(options), options.Tools, options.IsThinkingEnabled);
                    _logCallback?.Invoke(LogLevelType.Debug, $"Prompt formatted, Characters: {prompt.Prompt.Length}, Media: {prompt.Media.Count}");

                    // Tokenize
                    var promptChunks = Tokenize(prompt, options, bitmaps, out chunks);
                    var promptTokens = promptChunks.Sum(x => x.TokenCount);
                    var promptPositions = promptChunks.Sum(x => x.Positions);
                    if (promptPositions >= _contextSize)
                        throw new Exception($"The conversation is {promptPositions} tokens which exceeds the context size of {_contextSize} tokens.");

                    // Evaluate, only the part of the prompt that is not already cached is evaluated
                    var position = EvaluatePrompt(context, promptChunks, cancellationToken);
                    _logCallback?.Invoke(LogLevelType.Debug, $"Prompt evaluated, Tokens: {promptTokens}, Elapsed: {Stopwatch.GetElapsedTime(timestamp):c}");

                    // Generate
                    var result = new StringBuilder();
                    var decoder = _vocabulary.CreateDecoder();
                    var generated = GetGeneratedCache();
                    var maxTokens = options.MaxLength > 0 ? options.MaxLength : int.MaxValue;
                    var tokenCount = 0;
                    var reportedCount = 0;
                    while (tokenCount < maxTokens)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var token = NativeApi.llama_sampler_sample(sampler.DangerousGetHandle(), context, -1);
                        if (_vocabulary.IsStopToken(token))
                            break;

                        // Text
                        tokenCount++;
                        var text = decoder.Decode(token);
                        if (text.Length > 0)
                        {
                            result.Append(text);
                            var elapsed = (float)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
                            tokenCallback?.Invoke(text, promptTokens + tokenCount, reportedCount == 0 ? elapsed : elapsed / (tokenCount - reportedCount));
                            reportedCount = tokenCount;
                            timestamp = Stopwatch.GetTimestamp();
                        }

                        if (position >= _contextSize)
                        {
                            _logCallback?.Invoke(LogLevelType.Warn, $"Generation stopped, the context size of {_contextSize} tokens is full.");
                            break;
                        }

                        // Next
                        EvaluateTokens(context, [token], ref position, true);
                        generated.Add(token);
                    }

                    // Result
                    return new TextInput(result.ToString()) { TokenCount = promptTokens + tokenCount };
                }
                finally
                {
                    NativeApi.SetAbortRequested(false);
                    if (chunks != nint.Zero)
                        NativeApi.mtmd_input_chunks_free(chunks);

                    foreach (var bitmap in bitmaps)
                    {
                        NativeApi.mtmd_bitmap_free(bitmap);
                    }
                }
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _projectorHandle?.Dispose();
            _batchHandle?.Dispose();
            _contextHandle?.Dispose();
            _modelHandle?.Dispose();
            NativeApi.SetLogCallback(null);
        }


        /// <summary>
        /// Recreates the context when the requested size differs from the current one, if that fails the previous size is restored.
        /// </summary>
        /// <param name="contextSize">The requested context size, zero or less keeps the current context.</param>
        private void ResizeContext(int contextSize)
        {
            if (contextSize <= 0 || GetContextSize(contextSize) == _requestedContextSize)
                return;

            var previousSize = _requestedContextSize;
            try
            {
                _logCallback?.Invoke(LogLevelType.Info, $"Resizing context, Context Size: {GetContextSize(contextSize)}, Previous: {previousSize}");
                InitializeContext(contextSize);
            }
            catch (Exception)
            {
                InitializeContext(previousSize);
                throw;
            }
        }


        /// <summary>
        /// Creates the context and its batch, replacing the current ones and the prompt cache they held.
        /// </summary>
        /// <param name="contextSize">The requested context size, zero or less uses the trained context size.</param>
        private void InitializeContext(int contextSize)
        {
            // Release
            _cache.Clear();
            _batchHandle?.Dispose();
            _contextHandle?.Dispose();
            _batchHandle = null;
            _contextHandle = null;

            // Context
            var model = _modelHandle.DangerousGetHandle();
            _requestedContextSize = GetContextSize(contextSize);
            _contextHandle = CreateContext(_options, model, _requestedContextSize);
            _contextSize = (int)NativeApi.llama_n_ctx(_contextHandle.DangerousGetHandle());
            _batchSize = (int)NativeApi.llama_n_batch(_contextHandle.DangerousGetHandle());

            // Batch
            _batchHandle = new LlamaBatchSafeHandle(NativeApi.llama_batch_ext_init(_contextHandle.DangerousGetHandle()));
            if (_batchHandle.IsInvalid)
                throw new Exception("Failed to create llama.cpp batch.");
        }


        /// <summary>
        /// Gets the context size to create, limited to the size the model was trained with.
        /// </summary>
        /// <param name="contextSize">The requested context size, zero or less uses the trained context size.</param>
        private int GetContextSize(int contextSize)
        {
            return contextSize <= 0 ? _trainedContextSize : Math.Min(contextSize, _trainedContextSize);
        }


        /// <summary>
        /// Gets the conversation messages with special tokens removed from their content.
        /// </summary>
        /// <param name="options">The options.</param>
        private List<ConversationMessage> GetMessages(GenerateTextOptions options)
        {
            var messages = options.Conversation;
            if (messages == null || messages.Count == 0)
                messages = [new ConversationMessage(ConversationRole.User, options.Prompt ?? string.Empty)];

            return [.. messages.Select(x => x with { Content = _vocabulary.Sanitize(x.Content) })];
        }


        /// <summary>
        /// Tokenizes the prompt into text and media chunks.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="options">The options.</param>
        /// <param name="bitmaps">The native bitmaps created for the media, the caller must free them.</param>
        /// <param name="chunks">The native chunks that own the media chunks, the caller must free them.</param>
        private List<PromptChunk> Tokenize(PromptResult prompt, GenerateTextOptions options, List<nint> bitmaps, out nint chunks)
        {
            chunks = nint.Zero;
            var promptChunks = new List<PromptChunk>();
            if (prompt.Media.Count == 0)
            {
                promptChunks.Add(PromptChunk.CreateText(_vocabulary.Tokenize(prompt.Prompt, true, true)));
                return promptChunks;
            }

            if (_projectorHandle == null)
                throw new NotSupportedException("The model was loaded without a multimodal projector, image and audio input is not supported.");

            // Media
            foreach (var media in prompt.Media)
            {
                bitmaps.Add(media.IsAudio
                    ? CreateAudioBitmap(GetInput(options.InputAudios, media.Index, "audio"))
                    : CreateImageBitmap(GetInput(options.InputImages, media.Index, "image")));
            }

            // Tokenize
            chunks = NativeApi.mtmd_input_chunks_init();
            var promptBytes = Encoding.UTF8.GetBytes(prompt.Prompt);
            var bitmapArray = bitmaps.ToArray();
            fixed (byte* promptPtr = promptBytes)
            fixed (nint* bitmapsPtr = bitmapArray)
            {
                var inputText = new NativeApi.MtmdInputText { Text = promptPtr, TextLength = (nuint)promptBytes.Length, AddSpecial = 1, ParseSpecial = 1 };
                var status = NativeApi.mtmd_tokenize(_projectorHandle.DangerousGetHandle(), chunks, &inputText, bitmapsPtr, (nuint)bitmapArray.Length);
                if (status != 0)
                    throw new Exception(status == 1 ? "llama.cpp media count does not match the prompt." : "llama.cpp failed to process the input image or audio.");
            }

            // Chunks
            var chunkCount = (int)NativeApi.mtmd_input_chunks_size(chunks);
            for (int i = 0; i < chunkCount; i++)
            {
                var chunk = NativeApi.mtmd_input_chunks_get(chunks, (nuint)i);
                if (NativeApi.mtmd_input_chunk_get_type(chunk) == NativeApi.ChunkType.Text)
                {
                    var tokens = NativeApi.mtmd_input_chunk_get_tokens_text(chunk, out var tokenCount);
                    promptChunks.Add(PromptChunk.CreateText(new ReadOnlySpan<int>(tokens, (int)tokenCount).ToArray()));
                    continue;
                }

                var mediaId = NativeApi.PtrToString(NativeApi.mtmd_input_chunk_get_id(chunk));
                promptChunks.Add(PromptChunk.CreateMedia(chunk, mediaId, (int)NativeApi.mtmd_input_chunk_get_n_tokens(chunk), NativeApi.mtmd_input_chunk_get_n_pos(chunk)));
            }
            return promptChunks;
        }


        /// <summary>
        /// Evaluates the prompt, the leading part that matches the cached conversation is reused.
        /// </summary>
        /// <param name="context">The native context.</param>
        /// <param name="promptChunks">The prompt chunks.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The position after the prompt.</returns>
        private int EvaluatePrompt(nint context, List<PromptChunk> promptChunks, CancellationToken cancellationToken)
        {
            // Reuse
            var memory = NativeApi.llama_get_memory(context);
            var position = GetReusedPosition(memory, promptChunks, out var chunkIndex, out var tokenOffset);
            if (position == 0 || !NativeApi.llama_memory_seq_rm(memory, SequenceId, position, -1))
            {
                NativeApi.llama_memory_clear(memory, true);
                position = chunkIndex = tokenOffset = 0;
            }
            _logCallback?.Invoke(LogLevelType.Debug, $"Prompt cache reused {position} of {promptChunks.Sum(x => x.Positions)} positions.");

            // Evaluate, the cache is invalid until the prompt is evaluated
            _cache.Clear();
            for (int i = chunkIndex; i < promptChunks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunk = promptChunks[i];
                var isLast = i == promptChunks.Count - 1;
                if (chunk.IsMedia)
                {
                    var status = NativeApi.mtmd_helper_eval_chunk_single(_projectorHandle.DangerousGetHandle(), context, chunk.NativeChunk, position, SequenceId, _batchSize, isLast, out position);
                    if (status != 0)
                        throw new Exception($"llama.cpp failed to evaluate the input image or audio ({status}).");
                }
                else
                {
                    var offset = i == chunkIndex ? tokenOffset : 0;
                    EvaluateTokens(context, chunk.Tokens.AsSpan(offset), ref position, isLast, cancellationToken);
                }
            }

            // Cache
            _cache.AddRange(promptChunks.Select(x => new CachedChunk(x.IsMedia, x.MediaId, x.Positions, x.IsMedia ? [] : [.. x.Tokens])));
            return position;
        }


        /// <summary>
        /// Gets the position up to which the cached conversation matches the prompt.
        /// </summary>
        /// <param name="memory">The native memory.</param>
        /// <param name="promptChunks">The prompt chunks.</param>
        /// <param name="chunkIndex">The index of the first chunk to evaluate.</param>
        /// <param name="tokenOffset">The offset of the first token to evaluate in that chunk.</param>
        private int GetReusedPosition(nint memory, List<PromptChunk> promptChunks, out int chunkIndex, out int tokenOffset)
        {
            var position = 0;
            chunkIndex = 0;
            tokenOffset = 0;
            for (; chunkIndex < Math.Min(_cache.Count, promptChunks.Count); chunkIndex++)
            {
                var cached = _cache[chunkIndex];
                var chunk = promptChunks[chunkIndex];
                if (cached.IsMedia != chunk.IsMedia)
                    break;

                if (chunk.IsMedia)
                {
                    if (string.IsNullOrEmpty(chunk.MediaId) || cached.MediaId != chunk.MediaId || cached.Positions != chunk.Positions)
                        break;

                    position += chunk.Positions;
                    continue;
                }

                var matched = 0;
                var maximum = Math.Min(cached.Tokens.Count, chunk.Tokens.Length);
                while (matched < maximum && cached.Tokens[matched] == chunk.Tokens[matched])
                {
                    matched++;
                }

                position += matched;
                if (matched != cached.Tokens.Count || matched != chunk.Tokens.Length)
                {
                    tokenOffset = matched;
                    break;
                }
            }

            // The whole prompt is cached, step back to its last chunk
            if (chunkIndex >= promptChunks.Count)
            {
                chunkIndex = promptChunks.Count - 1;
                tokenOffset = promptChunks[chunkIndex].Tokens.Length;
            }

            // At least one token must be evaluated to produce the logits the first token is sampled from
            var lastChunk = promptChunks[^1];
            if (chunkIndex == promptChunks.Count - 1 && tokenOffset >= lastChunk.Tokens.Length)
            {
                if (lastChunk.IsMedia || tokenOffset == 0)
                    return chunkIndex = tokenOffset = 0;

                tokenOffset--;
                position--;
            }

            // The reused part must still be complete in the memory, otherwise everything is evaluated again
            if (position > 0)
            {
                var minimumPosition = NativeApi.llama_memory_seq_pos_min(memory, SequenceId);
                if (minimumPosition < 0 || minimumPosition > Math.Max(0, position - Math.Max(1, _slidingWindow)))
                    return chunkIndex = tokenOffset = 0;
            }
            return position;
        }


        /// <summary>
        /// Evaluates the tokens.
        /// </summary>
        /// <param name="context">The native context.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="position">The position, advanced by the number of tokens evaluated.</param>
        /// <param name="isOutputLast">if set to <c>true</c> the logits of the last token are computed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void EvaluateTokens(nint context, ReadOnlySpan<int> tokens, ref int position, bool isOutputLast, CancellationToken cancellationToken = default)
        {
            var batch = _batchHandle.DangerousGetHandle();
            var offset = 0;
            while (offset < tokens.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NativeApi.llama_batch_ext_clear(batch);
                var count = Math.Min(_batchSize, tokens.Length - offset);
                for (int i = 0; i < count; i++)
                {
                    var tokenPosition = position + i;
                    var index = NativeApi.llama_batch_ext_add_token(batch, SequenceId, tokens[offset + i]);
                    if (index < 0)
                        throw new Exception($"llama.cpp failed to add a token to the batch ({index}).");

                    NativeApi.llama_batch_ext_set_pos(batch, index, &tokenPosition);
                    if (isOutputLast && offset + i == tokens.Length - 1)
                        NativeApi.llama_batch_ext_set_output_logits(batch, index, true);
                }

                var status = NativeApi.llama_process(context, NativeApi.ProcessType.Decode, batch);
                if (status == 2)
                    throw new OperationCanceledException("llama.cpp evaluation was cancelled.");
                if (status == 1)
                    throw new Exception($"The conversation does not fit in the context size of {_contextSize} tokens.");
                if (status != 0)
                    throw new Exception($"llama.cpp failed to evaluate the tokens ({status}).");

                position += count;
                offset += count;
            }
        }


        /// <summary>
        /// Gets the cached token list the generated tokens are appended to, the last text chunk of the conversation.
        /// </summary>
        private List<int> GetGeneratedCache()
        {
            if (_cache.Count == 0 || _cache[^1].IsMedia)
                _cache.Add(new CachedChunk(false, null, 0, []));

            return _cache[^1].Tokens;
        }


        /// <summary>
        /// Creates the sampler chain.
        /// </summary>
        /// <param name="options">The options.</param>
        private LlamaSamplerSafeHandle CreateSampler(GenerateTextOptions options)
        {
            var chain = NativeApi.llama_sampler_chain_init(NativeApi.llama_sampler_chain_default_params());
            if (chain == nint.Zero)
                throw new Exception("Failed to create llama.cpp sampler.");

            if (options.RepetitionPenalty > 0 && options.RepetitionPenalty != 1f && options.RepetitionPenaltyRange != 0)
                NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_penalties(_vocabulary.TokenCount, options.RepetitionPenaltyRange, options.RepetitionPenalty, 0f, 0f));

            if (!options.IsSamplingEnabled || options.Temperature <= 0)
            {
                NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_greedy());
                return new LlamaSamplerSafeHandle(chain);
            }

            if (options.TopK > 0)
                NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_top_k(options.TopK));
            if (options.TypicalP > 0 && options.TypicalP < 1f)
                NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_typical(options.TypicalP, 1));
            if (options.TopP > 0 && options.TopP < 1f)
                NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_top_p(options.TopP, 1));
            if (options.MinP > 0)
                NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_min_p(options.MinP, 1));

            NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_temp(options.Temperature));
            NativeApi.llama_sampler_chain_add(chain, NativeApi.llama_sampler_init_dist(options.Seed > 0 ? (uint)options.Seed : NativeApi.DefaultSeed));
            return new LlamaSamplerSafeHandle(chain);
        }


        /// <summary>
        /// Creates a native bitmap from an image, the tensor is RGBA planar in the range -1 to 1.
        /// </summary>
        /// <param name="image">The image.</param>
        private nint CreateImageBitmap(ImageTensor image)
        {
            if (!IsVisionSupported)
                throw new NotSupportedException("The model does not support image input.");

            var width = image.Width;
            var height = image.Height;
            var channelSize = width * height;
            var source = image.Memory.Span;
            var pixels = new byte[channelSize * 3];
            for (int i = 0; i < channelSize; i++)
            {
                pixels[i * 3 + 0] = source[i].DenormalizeToByte();
                pixels[i * 3 + 1] = source[channelSize + i].DenormalizeToByte();
                pixels[i * 3 + 2] = source[channelSize * 2 + i].DenormalizeToByte();
            }

            fixed (byte* pixelsPtr = pixels)
            {
                var bitmap = NativeApi.mtmd_bitmap_init((uint)width, (uint)height, pixelsPtr);
                if (bitmap == nint.Zero)
                    throw new Exception("Failed to create llama.cpp image input.");

                // The id identifies the media in the prompt cache
                NativeApi.mtmd_bitmap_set_id(bitmap, Convert.ToHexString(SHA256.HashData(pixels)));
                return bitmap;
            }
        }


        /// <summary>
        /// Creates a native bitmap from an audio, the audio is converted to mono at the sample rate the model expects.
        /// </summary>
        /// <param name="audio">The audio.</param>
        private nint CreateAudioBitmap(AudioTensor audio)
        {
            if (!IsAudioSupported)
                throw new NotSupportedException("The model does not support audio input.");

            var sampleRate = NativeApi.mtmd_get_audio_sample_rate(_projectorHandle.DangerousGetHandle());
            var samples = audio.ToMono().Resample(sampleRate).Memory.Span;
            fixed (float* samplesPtr = samples)
            {
                var bitmap = NativeApi.mtmd_bitmap_init_from_audio((nuint)samples.Length, samplesPtr);
                if (bitmap == nint.Zero)
                    throw new Exception("Failed to create llama.cpp audio input.");

                // The id identifies the media in the prompt cache
                NativeApi.mtmd_bitmap_set_id(bitmap, Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(samples))));
                return bitmap;
            }
        }


        /// <summary>
        /// Gets the input a conversation message refers to.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="index">The index.</param>
        /// <param name="name">The input name.</param>
        private static T GetInput<T>(List<T> inputs, int index, string name)
        {
            if (inputs == null || index < 0 || index >= inputs.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"The conversation refers to input {name} {index} which was not provided.");

            return inputs[index];
        }


        /// <summary>
        /// Selects the GPU device, zero lets llama.cpp use all devices.
        /// </summary>
        /// <param name="options">The options.</param>
        private nint SelectDevice(LlamaCppOptions options)
        {
            var devices = new List<nint>();
            var deviceCount = (int)NativeApi.ggml_backend_dev_count();
            for (int i = 0; i < deviceCount; i++)
            {
                var device = NativeApi.ggml_backend_dev_get((nuint)i);
                var deviceType = NativeApi.ggml_backend_dev_type(device);
                NativeApi.ggml_backend_dev_memory(device, out var free, out var total);
                _logCallback?.Invoke(LogLevelType.Info, $"Device {i}: {GetDeviceName(device)} ({GetDeviceDescription(device)}), Type: {deviceType}, Memory: {(ulong)free / 1048576}MB free of {(ulong)total / 1048576}MB");
                if (deviceType == NativeApi.DeviceType.Gpu || deviceType == NativeApi.DeviceType.IntegratedGpu)
                    devices.Add(device);
            }

            if (devices.Count == 0 || options.Device < 0)
                return nint.Zero;

            // Name
            var selected = nint.Zero;
            if (!string.IsNullOrEmpty(options.DeviceName))
                selected = devices.FirstOrDefault(x => GetDeviceDescription(x).Contains(options.DeviceName, StringComparison.OrdinalIgnoreCase) || GetDeviceName(x).Equals(options.DeviceName, StringComparison.OrdinalIgnoreCase));

            // Index, Vulkan uses the raw device index while CUDA and ROCm use the index within the vendor devices
            if (selected == nint.Zero)
            {
                if (options.Device >= devices.Count)
                    throw new ArgumentOutOfRangeException(nameof(options), $"Device {options.Device} was not found, llama.cpp found {devices.Count} GPU devices: {string.Join(", ", devices.Select(x => $"{GetDeviceName(x)} ({GetDeviceDescription(x)})"))}");

                selected = devices[options.Device];
            }

            _logCallback?.Invoke(LogLevelType.Info, $"Selected device: {GetDeviceName(selected)} ({GetDeviceDescription(selected)})");
            return selected;
        }


        /// <summary>
        /// Gets the device name (e.g. Vulkan0, CUDA0).
        /// </summary>
        /// <param name="device">The device.</param>
        private static string GetDeviceName(nint device)
        {
            return NativeApi.PtrToString(NativeApi.ggml_backend_dev_name(device));
        }


        /// <summary>
        /// Gets the device description (the hardware name).
        /// </summary>
        /// <param name="device">The device.</param>
        private static string GetDeviceDescription(nint device)
        {
            return NativeApi.PtrToString(NativeApi.ggml_backend_dev_description(device));
        }


        /// <summary>
        /// Loads the model.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="device">The device, zero to use all devices.</param>
        private static LlamaModelSafeHandle LoadModel(LlamaCppOptions options, nint device)
        {
            // The device list is null terminated and only needs to live for the duration of the load
            var devices = stackalloc nint[2] { device, nint.Zero };
            var parameters = NativeApi.llama_model_default_params();
            parameters.GpuLayers = options.GpuLayers;
            if (device != nint.Zero)
                parameters.Devices = devices;

            var model = new LlamaModelSafeHandle(NativeApi.llama_model_load_from_file(options.ModelPath, parameters));
            if (model.IsInvalid)
                throw new Exception($"Failed to load llama.cpp model: {options.ModelPath}");

            return model;
        }


        /// <summary>
        /// Creates the context.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="model">The native model.</param>
        /// <param name="contextSize">The context size.</param>
        private static LlamaContextSafeHandle CreateContext(LlamaCppOptions options, nint model, int contextSize)
        {
            var parameters = NativeApi.llama_context_default_params();
            parameters.ContextSize = (uint)contextSize;
            parameters.BatchSize = (uint)Math.Clamp(options.BatchSize, 1, contextSize);
            parameters.MicroBatchSize = Math.Min(parameters.MicroBatchSize, parameters.BatchSize);
            parameters.FlashAttentionType = GetFlashAttention(options);
            parameters.TypeK = parameters.TypeV = GetCacheType(options.CacheType);
            parameters.OffloadKqv = (byte)(options.IsCacheOffloadEnabled ? 1 : 0);
            parameters.AbortCallback = NativeApi.GetAbortCallback();
            parameters.NoPerformance = 1;
            parameters.SwaFull = 1; // Sliding window models keep the full cache so earlier turns can be reused
            if (options.Threads > 0)
                parameters.Threads = parameters.ThreadsBatch = options.Threads;

            var context = new LlamaContextSafeHandle(NativeApi.llama_init_from_model(model, parameters));
            if (context.IsInvalid)
                throw new Exception($"Failed to create llama.cpp context, Context Size: {contextSize}. Try a smaller context size.");

            return context;
        }


        /// <summary>
        /// Loads the multimodal projector.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="model">The native model.</param>
        /// <param name="device">The device, zero to use the default device.</param>
        private static MtmdContextSafeHandle LoadProjector(LlamaCppOptions options, nint model, nint device)
        {
            var parameters = NativeApi.mtmd_context_params_default();
            parameters.UseGpu = (byte)(options.GpuLayers != 0 ? 1 : 0);
            parameters.Device = device;
            parameters.PrintTimings = 0;
            parameters.FlashAttentionType = GetFlashAttention(options);
            if (options.Threads > 0)
                parameters.Threads = options.Threads;
            if (options.ImageMinTokens > 0)
                parameters.ImageMinTokens = options.ImageMinTokens;
            if (options.ImageMaxTokens > 0)
                parameters.ImageMaxTokens = options.ImageMaxTokens;

            var projector = new MtmdContextSafeHandle(NativeApi.mtmd_init_from_file(options.ProjectorPath, model, parameters));
            if (projector.IsInvalid)
                throw new Exception($"Failed to load llama.cpp multimodal projector: {options.ProjectorPath}");

            return projector;
        }


        /// <summary>
        /// Gets the model description (architecture, size and quantization).
        /// </summary>
        /// <param name="model">The native model.</param>
        private static string GetDescription(nint model)
        {
            var buffer = stackalloc byte[256];
            var length = NativeApi.llama_model_desc(model, buffer, 256);
            return length > 0 ? Encoding.UTF8.GetString(buffer, Math.Min(length, 255)) : string.Empty;
        }


        /// <summary>
        /// Gets the native flash attention type.
        /// </summary>
        /// <param name="options">The options.</param>
        private static NativeApi.FlashAttentionType GetFlashAttention(LlamaCppOptions options)
        {
            if (!options.IsFlashAttentionEnabled.HasValue)
                return NativeApi.FlashAttentionType.Auto;

            return options.IsFlashAttentionEnabled.Value ? NativeApi.FlashAttentionType.Enabled : NativeApi.FlashAttentionType.Disabled;
        }


        /// <summary>
        /// Gets the native key/value cache type.
        /// </summary>
        /// <param name="cacheType">The cache type.</param>
        private static NativeApi.GgmlType GetCacheType(CacheDataType cacheType)
        {
            return cacheType switch
            {
                CacheDataType.Q8_0 => NativeApi.GgmlType.Q8_0,
                CacheDataType.Q4_0 => NativeApi.GgmlType.Q4_0,
                _ => NativeApi.GgmlType.F16
            };
        }


        /// <summary>
        /// A chunk of the tokenized prompt, text tokens or one media.
        /// </summary>
        private sealed class PromptChunk
        {
            /// <summary>
            /// Gets a value indicating whether the chunk is an image or audio.
            /// </summary>
            public bool IsMedia { get; private init; }

            /// <summary>
            /// Gets the text tokens, empty for media.
            /// </summary>
            public int[] Tokens { get; private init; }

            /// <summary>
            /// Gets the native chunk of a media, it is owned by the native chunk list.
            /// </summary>
            public nint NativeChunk { get; private init; }

            /// <summary>
            /// Gets the id that identifies the media content.
            /// </summary>
            public string MediaId { get; private init; }

            /// <summary>
            /// Gets the number of tokens.
            /// </summary>
            public int TokenCount { get; private init; }

            /// <summary>
            /// Gets the number of context positions the chunk occupies.
            /// </summary>
            public int Positions { get; private init; }


            /// <summary>
            /// Creates a text chunk.
            /// </summary>
            /// <param name="tokens">The tokens.</param>
            public static PromptChunk CreateText(int[] tokens)
            {
                return new PromptChunk { Tokens = tokens, TokenCount = tokens.Length, Positions = tokens.Length };
            }


            /// <summary>
            /// Creates a media chunk.
            /// </summary>
            /// <param name="nativeChunk">The native chunk.</param>
            /// <param name="mediaId">The media id.</param>
            /// <param name="tokenCount">The token count.</param>
            /// <param name="positions">The positions.</param>
            public static PromptChunk CreateMedia(nint nativeChunk, string mediaId, int tokenCount, int positions)
            {
                return new PromptChunk { IsMedia = true, Tokens = [], NativeChunk = nativeChunk, MediaId = mediaId, TokenCount = tokenCount, Positions = positions };
            }
        }


        /// <summary>
        /// A chunk of the conversation that is held in the context memory.
        /// </summary>
        /// <param name="IsMedia">if set to <c>true</c> the chunk is an image or audio.</param>
        /// <param name="MediaId">The id that identifies the media content.</param>
        /// <param name="Positions">The number of context positions a media occupies.</param>
        /// <param name="Tokens">The text tokens.</param>
        private sealed record CachedChunk(bool IsMedia, string MediaId, int Positions, List<int> Tokens);
    }
}
