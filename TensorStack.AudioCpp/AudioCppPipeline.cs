using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.AudioCpp.Common;
using TensorStack.AudioCpp.Native;
using TensorStack.Common.Tensor;

namespace TensorStack.AudioCpp
{
    public sealed class AudioCppPipeline : IDisposable
    {
        private readonly ILogger _logger;
        private AudioCppContext _audioCppContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppPipeline"/> class.
        /// </summary>
        /// <param name="backendPath">The backend path.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.Exception">Failed to load 'audio.cpp' native library: {backendPath}</exception>
        public AudioCppPipeline(string backendPath, ILogger logger = null)
        {
            _logger = logger;
            if (!NativeApi.LoadNativeLibrary(out var backendInfo, backendPath))
                throw new Exception($"Failed to load 'audio.cpp' native library: {backendPath}");
            if (!NativeApi.LibraryVersion.Equals(backendInfo.Commit, StringComparison.OrdinalIgnoreCase))
                logger?.LogWarning($"[audio.cpp] Native library version mismatch: loaded '{backendInfo.Commit}', expected '{NativeApi.LibraryVersion}'.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppPipeline"/> class.
        /// </summary>
        /// <param name="backend">The backend environment.</param>
        /// <param name="logger">The logger.</param>
        public AudioCppPipeline(AudioCppBackend backend, ILogger logger = null)
            : this(backend.GetDirectory(), logger) { }

        /// <summary>
        /// Gets the backend information.
        /// </summary>
        public BackendInfo BackendInfo { get; }

        /// <summary>
        /// Gets the audio.cpp pipeline (model family) of the loaded model.
        /// </summary>
        public string Pipeline => _audioCppContext?.Pipeline;

        /// <summary>
        /// Gets a value indicating whether a model is loaded.
        /// </summary>
        public bool IsLoaded => _audioCppContext != null;


        /// <summary>
        /// Loads the model with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public void Load(AudioCppOptions options)
        {
            Unload();

            var timestamp = Stopwatch.GetTimestamp();
            _audioCppContext = new AudioCppContext(options, OnNativeLogCallback);
            _logger?.LogInformation("[audio.cpp] Model loaded, Pipeline: {Pipeline}, Elapsed: {Elapsed:c}", _audioCppContext.Pipeline, Stopwatch.GetElapsedTime(timestamp));
        }


        /// <summary>
        /// Loads the model with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task LoadAsync(AudioCppOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                Load(options);
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken);
        }


        /// <summary>
        /// Determines whether the loaded model supports the task.
        /// </summary>
        /// <param name="task">The task (gen, tts, clon, asr etc).</param>
        public bool IsTaskSupported(string task)
        {
            return _audioCppContext?.IsTaskSupported(task) ?? false;
        }


        /// <summary>
        /// Generates audio (music, sound effects, speech) with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public AudioTensor GenerateAudio(GenerateAudioOptions options, CancellationToken cancellationToken = default)
        {
            if (_audioCppContext == null)
                throw new InvalidOperationException("audio.cpp model is not loaded.");

            var timestamp = Stopwatch.GetTimestamp();
            var result = _audioCppContext.GenerateAudio(options, cancellationToken);
            if (result.Audio == null)
                throw new Exception($"audio.cpp pipeline '{Pipeline}' did not return audio.");

            _logger?.LogInformation("[audio.cpp] Generate audio complete, Task: {Task}, Elapsed: {Elapsed:c}", _audioCppContext.Task, Stopwatch.GetElapsedTime(timestamp));
            return result.Audio;
        }


        /// <summary>
        /// Generates audio (music, sound effects, speech) with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<AudioTensor> GenerateAudioAsync(GenerateAudioOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GenerateAudio(options, cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Generates text (speech recognition) with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public string GenerateText(GenerateTextOptions options, CancellationToken cancellationToken = default)
        {
            if (_audioCppContext == null)
                throw new InvalidOperationException("audio.cpp model is not loaded.");

            var timestamp = Stopwatch.GetTimestamp();
            var result = _audioCppContext.GenerateText(options, cancellationToken);
            if (result.Text == null)
                throw new Exception($"audio.cpp pipeline '{Pipeline}' did not return text.");

            _logger?.LogInformation("[audio.cpp] Generate text complete, Task: {Task}, Elapsed: {Elapsed:c}", _audioCppContext.Task, Stopwatch.GetElapsedTime(timestamp));
            return result.Text;
        }


        /// <summary>
        /// Generates text (speech recognition) with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<string> GenerateTextAsync(GenerateTextOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GenerateText(options, cancellationToken), cancellationToken);
        }



        /// <summary>
        /// Unloads the context.
        /// </summary>
        public void Unload()
        {
            _audioCppContext?.Dispose();
            _audioCppContext = null;
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Unload();
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Called when logs are emitted
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        private void OnNativeLogCallback(LogLevelType level, string message)
        {
            var logLevel = level switch
            {
                LogLevelType.Info => LogLevel.Information,
                LogLevelType.Debug => LogLevel.Debug,
                LogLevelType.Warn => LogLevel.Warning,
                LogLevelType.Error => LogLevel.Error,
                _ => LogLevel.Trace
            };
            _logger?.Log(logLevel, "[audio.cpp] {message}", message);
        }
    }
}
