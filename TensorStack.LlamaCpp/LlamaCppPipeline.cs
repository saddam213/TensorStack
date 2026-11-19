using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.LlamaCpp.Common;
using TensorStack.LlamaCpp.Native;

namespace TensorStack.LlamaCpp
{
    public sealed class LlamaCppPipeline : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IProgress<PipelineProgress> _progressCallback;
        private LlamaCppContext _llamaCppContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaCppPipeline"/> class.
        /// </summary>
        /// <param name="backendPath">The backend path.</param>
        /// <param name="progressCallback">The progress callback, receives the generated tokens.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.Exception">Failed to load 'llama.cpp' native library: {backendPath}</exception>
        public LlamaCppPipeline(string backendPath, IProgress<PipelineProgress> progressCallback = null, ILogger logger = null)
        {
            _logger = logger;
            _progressCallback = progressCallback;
            if (!NativeApi.LoadNativeLibrary(out var backendInfo, backendPath))
                throw new Exception($"Failed to load 'llama.cpp' native library: {backendPath}");

            BackendInfo = backendInfo;
            if (!IsExpectedVersion(backendInfo.Version))
                logger?.LogWarning("[llama.cpp] Native library version mismatch: loaded '{Version}', expected '{Expected}'.", backendInfo.Version, NativeApi.LibraryApiVersion);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LlamaCppPipeline"/> class.
        /// </summary>
        /// <param name="backend">The backend environment.</param>
        /// <param name="progressCallback">The progress callback, receives the generated tokens.</param>
        /// <param name="logger">The logger.</param>
        public LlamaCppPipeline(LlamaCppBackend backend, IProgress<PipelineProgress> progressCallback = null, ILogger logger = null)
            : this(backend.GetDirectory(), progressCallback, logger) { }

        /// <summary>
        /// Gets the backend information.
        /// </summary>
        public BackendInfo BackendInfo { get; }

        /// <summary>
        /// Gets the devices.
        /// </summary>
        public IReadOnlyList<BackendDevice> Devices => BackendInfo?.Devices;

        /// <summary>
        /// Gets the description of the loaded model (architecture, size and quantization).
        /// </summary>
        public string Description => _llamaCppContext?.Description;

        /// <summary>
        /// Gets the context size of the loaded model in tokens.
        /// </summary>
        public int ContextSize => _llamaCppContext?.ContextSize ?? 0;

        /// <summary>
        /// Gets a value indicating whether the loaded model accepts image input.
        /// </summary>
        public bool IsVisionSupported => _llamaCppContext?.IsVisionSupported ?? false;

        /// <summary>
        /// Gets a value indicating whether the loaded model accepts audio input.
        /// </summary>
        public bool IsAudioSupported => _llamaCppContext?.IsAudioSupported ?? false;


        /// <summary>
        /// Loads the model with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public void Load(LlamaCppOptions options)
        {
            Unload();

            var timestamp = Stopwatch.GetTimestamp();
            _llamaCppContext = new LlamaCppContext(options, OnNativeLogCallback);
            _logger?.LogInformation("[llama.cpp] Model loaded, Model: {Description}, Context: {ContextSize}, Elapsed: {Elapsed:c}", _llamaCppContext.Description, _llamaCppContext.ContextSize, Stopwatch.GetElapsedTime(timestamp));
        }


        /// <summary>
        /// Loads the model with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task LoadAsync(LlamaCppOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                Load(options);
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken);
        }


        /// <summary>
        /// Generates text with the specified options, the generated tokens are reported to the progress callback as they are produced.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public TextInput[] GenerateText(GenerateTextOptions options, CancellationToken cancellationToken = default)
        {
            if (_llamaCppContext == null)
                throw new InvalidOperationException("llama.cpp model is not loaded.");

            var timestamp = Stopwatch.GetTimestamp();
            var result = _llamaCppContext.GenerateText(options, OnTokenCallback, cancellationToken);
            _logger?.LogInformation("[llama.cpp] Generate text complete, Tokens: {TokenCount}, Elapsed: {Elapsed:c}", result.TokenCount, Stopwatch.GetElapsedTime(timestamp));
            return [result];
        }


        /// <summary>
        /// Generates text with the specified options, the generated tokens are reported to the progress callback as they are produced.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task<TextInput[]> GenerateTextAsync(GenerateTextOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GenerateText(options, cancellationToken), cancellationToken);
        }


        /// <summary>
        /// Unloads the context.
        /// </summary>
        public void Unload()
        {
            _llamaCppContext?.Dispose();
            _llamaCppContext = null;
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
        /// Determines whether the loaded library is the version the bindings were written for.
        /// </summary>
        /// <param name="version">The loaded version.</param>
        private static bool IsExpectedVersion(string version)
        {
            return version != null && version.StartsWith(NativeApi.LibraryApiVersion, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Called when text is generated.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="tokenCount">The total token count.</param>
        /// <param name="elapsed">The elapsed milliseconds per token.</param>
        private void OnTokenCallback(string text, int tokenCount, float elapsed)
        {
            _progressCallback?.Report(new PipelineProgress
            {
                Key = "Generate",
                Subkey = "Token",
                ElapsedKey = "Token",
                Message = text,
                Value = tokenCount,
                Elapsed = elapsed,
                Timestamp = DateTime.UtcNow
            });
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
            _logger?.Log(logLevel, "[llama.cpp] {message}", message);
        }
    }
}
