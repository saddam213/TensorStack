using System;
using System.Collections.Generic;
using System.Threading;
using TensorStack.AudioCpp.Common;

namespace TensorStack.AudioCpp.Native
{
    internal sealed class AudioCppContext : IDisposable
    {
        private const string ModeOffline = "offline";
        private const string TaskSpeech = "tts";
        private const string TaskClone = "clon";
        private readonly Action<LogLevelType, string> _logCallback;
        private readonly AudioCppModelSafeHandle _modelHandle;
        private readonly AudioCppOptions _options;
        private AudioCppSessionSafeHandle _sessionHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCppContext"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="logCallback">The log callback.</param>
        public AudioCppContext(AudioCppOptions options, Action<LogLevelType, string> logCallback = null)
        {
            _options = options;
            _logCallback = logCallback;
            NativeApi.SetLogCallback(_logCallback);
            _modelHandle = new AudioCppModelSafeHandle(_options);
        }

        /// <summary>
        /// Gets the audio.cpp pipeline (model family) of the loaded model.
        /// </summary>
        public string Pipeline => _modelHandle.Pipeline;

        /// <summary>
        /// Gets the task of the current session, null if no request has run.
        /// </summary>
        public string Task => _sessionHandle?.Task;


        /// <summary>
        /// Determines whether the model supports the task.
        /// </summary>
        /// <param name="task">The task (gen, tts, clon, asr etc).</param>
        public bool IsTaskSupported(string task)
        {
            return _modelHandle.IsTaskSupported(task, ModeOffline);
        }


        /// <summary>
        /// Generates audio (music, sound effects, speech).
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public AudioCppResult GenerateAudio(GenerateAudioOptions options, CancellationToken cancellationToken = default)
        {
            // Speech models take the input audio as the speaker reference and the task as the voice
            var isSpeech = IsTaskSupported(TaskSpeech) || IsTaskSupported(TaskClone);
            return Run(options.ToRequest(Pipeline, isSpeech), cancellationToken);
        }


        /// <summary>
        /// Generates text (speech recognition).
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public AudioCppResult GenerateText(GenerateTextOptions options, CancellationToken cancellationToken = default)
        {
            return Run(options.ToRequest(), cancellationToken);
        }


        /// <summary>
        /// Runs the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private AudioCppResult Run(AudioCppRequest request, CancellationToken cancellationToken = default)
        {
            // Session
            var session = GetSession(GetSupportedTask(request.Tasks));

            // Request
            var requestOptions = FilterOptions(request.Options, _modelHandle.RequestOptions, "request");
            var nativeRequest = nint.Zero;
            var nativeResult = nint.Zero;
            try
            {
                nativeRequest = request.ToUnmanaged(requestOptions);
                cancellationToken.ThrowIfCancellationRequested();

                // Run, the C ABI has no cancellation so this blocks until the task completes
                NativeApi.ThrowIfFailed(NativeApi.audiocpp_session_run(session.GetSession(), nativeRequest, out nativeResult), $"'{session.Task}' run");
                cancellationToken.ThrowIfCancellationRequested();

                // Result
                return NativeApiExtensions.ToManaged(nativeResult);
            }
            finally
            {
                NativeApi.audiocpp_result_free(nativeResult);
                NativeApi.audiocpp_request_free(nativeRequest);
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _sessionHandle?.Dispose();
            _modelHandle?.Dispose();
            NativeApi.SetLogCallback(null);
        }


        /// <summary>
        /// Gets the first task the loaded model supports.
        /// </summary>
        /// <param name="tasks">The candidate tasks.</param>
        private string GetSupportedTask(string[] tasks)
        {
            foreach (var task in tasks)
            {
                if (IsTaskSupported(task))
                    return task;
            }
            throw new NotSupportedException($"audio.cpp pipeline '{Pipeline}' does not support task '{string.Join("' or '", tasks)}'.");
        }


        /// <summary>
        /// Gets the session for the task, a session is bound to a single task so it is recreated when the task changes.
        /// </summary>
        /// <param name="task">The task.</param>
        private AudioCppSessionSafeHandle GetSession(string task)
        {
            if (_sessionHandle != null && _sessionHandle.Task == task)
                return _sessionHandle;

            _sessionHandle?.Dispose();
            _sessionHandle = null;

            var sessionOptions = FilterOptions(_options.ToSessionOptions(), _modelHandle.SessionOptions, "session");
            _sessionHandle = new AudioCppSessionSafeHandle(_modelHandle, task, ModeOffline, _options, sessionOptions);
            return _sessionHandle;
        }


        /// <summary>
        /// Filters the options to those the model declares, pipelines validate their options strictly.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="declared">The declared option names.</param>
        /// <param name="scope">The scope name.</param>
        private Dictionary<string, string> FilterOptions(Dictionary<string, string> options, IReadOnlySet<string> declared, string scope)
        {
            var result = new Dictionary<string, string>();
            if (options == null)
                return result;

            foreach (var option in options)
            {
                if (option.Value == null)
                    continue;

                // Options may be declared with or without the pipeline prefix
                var prefixedKey = $"{Pipeline}.{option.Key}";
                if (declared.Count == 0 || declared.Contains(option.Key))
                    result[option.Key] = option.Value;
                else if (declared.Contains(prefixedKey))
                    result[prefixedKey] = option.Value;
                else
                    _logCallback?.Invoke(LogLevelType.Debug, $"Skipped {scope} option '{option.Key}', not declared by pipeline '{Pipeline}'.");
            }
            return result;
        }
    }
}
