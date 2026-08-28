using System;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;
using TensorStack.StableDiffusionCpp.Common;
using TensorStack.StableDiffusionCpp.Native;

namespace TensorStack.StableDiffusionCpp
{
    public class StableDiffusionPipeline : IDisposable
    {
        private readonly Action<LogLevelType, string> _logCallback;
        private readonly IProgress<PipelineProgress> _progresssCallback;
        private Context _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="StableDiffusionPipeline"/> class.
        /// </summary>
        /// <param name="backendPath">The path to a StableDiffusion.cpp backend directory.</param>
        /// <param name="progresssCallback">The progresss callback.</param>
        /// <param name="logCallback">The log callback.</param>
        /// <exception cref="System.Exception">Failed to load 'StableDiffusion.cpp' native library: {backendPath}</exception>
        public StableDiffusionPipeline(string backendPath = null, IProgress<PipelineProgress> progresssCallback = null, Action<LogLevelType, string> logCallback = null)
        {
            _logCallback = logCallback;
            _progresssCallback = progresssCallback;
            if (!NativeApi.LoadNativeLibrary(out var backendInfo, backendPath))
                throw new Exception($"Failed to load 'StableDiffusion.cpp' native library: {backendPath}");

            Backend = backendInfo;
        }

        /// <summary>
        /// Gets the loaded backend information.
        /// </summary>
        public BackendInfo Backend { get; }

        /// <summary>
        /// Gets the default image options for the current context.
        /// </summary>
        public GenerateImageOptions DefaultImageOptions => _context?.DefaultImageOptions;

        /// <summary>
        /// Gets the default video options for the current context.
        /// </summary>
        public GenerateVideoOptions DefaultVideoOptions => _context?.DefaultVideoOptions;


        /// <summary>
        /// Loads the model context.
        /// </summary>
        /// <param name="contextParameters">The context parameters.</param>
        public void LoadContext(ContextOptions contextParameters)
        {
            UnloadContext();
            _context = new Context(contextParameters, OnProgressCallback, OnPreviewCallback, OnLogCallback);
        }


        /// <summary>
        /// Loads the modelcontext.
        /// </summary>
        /// <param name="contextParameters">The context parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task LoadContextAsync(ContextOptions contextParameters, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                LoadContext(contextParameters);
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Generates an image with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public ImageTensor[] GenerateImage(GenerateImageOptions options)
        {
            return _context.Generate(options);
        }


        /// <summary>
        /// Generates an image with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<ImageTensor[]> GenerateImageAsync(GenerateImageOptions options, CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => CancelGenerate());
            return await Task.Run(() =>
            {
                var result = GenerateImage(options);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Generates a video with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public VideoSequence GenerateVideo(GenerateVideoOptions options)
        {
            return _context.Generate(options);
        }


        /// <summary>
        /// Generates a video with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<VideoSequence> GenerateVideoAsync(GenerateVideoOptions options, CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => CancelGenerate());
            return await Task.Run(() =>
            {
                var result = GenerateVideo(options);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Unloads the model context.
        /// </summary>
        public void UnloadContext()
        {
            _context?.Dispose();
            _context = null;
        }


        /// <summary>
        /// Cancels the current generation.
        /// </summary>
        /// <param name="cancelType">Type of the cancel.</param>
        public void CancelGenerate(CancelType cancelType = CancelType.Immediate)
        {
            _context.Cancel(cancelType);
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            UnloadContext();
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Called when progress is emitted.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="timeSeconds">The time seconds.</param>
        private void OnProgressCallback(int value, int maximum, float timeSeconds)
        {
            var elapsedTime = GetProgresTime(timeSeconds);
            _progresssCallback?.Report(new PipelineProgress
            {
                Key = "Load",
                Subkey = "Component",
                Value = value,
                Maximum = maximum,
                Message = "Loading Pipeline Components...",
                Elapsed = (float)elapsedTime.TotalMilliseconds,
            });
            OnLogCallback(LogLevelType.Debug, $"{value}/{maximum}, {elapsedTime}");
        }


        /// <summary>
        /// Called when preview is emitted
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="steps">The steps.</param>
        /// <param name="timeSeconds">The time seconds.</param>
        /// <param name="frames">The frames.</param>
        private void OnPreviewCallback(int step, int steps, float timeSeconds, ImageTensor[] frames)
        {
            var elapsedTime = GetProgresTime(timeSeconds);
            _progresssCallback?.Report(new PipelineProgress
            {
                Key = "Generate",
                Subkey = "Step",
                Value = step,
                Maximum = steps,
                Tensors = frames,
                Elapsed = (float)elapsedTime.TotalMilliseconds,
            });
            OnLogCallback(LogLevelType.Debug, $"Step: {step}, Frames: {frames?.Length ?? 0}");
        }


        /// <summary>
        /// Called when logs are emitted
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        private void OnLogCallback(LogLevelType level, string message)
        {
            _logCallback?.Invoke(level, $"[StableDiffusion.cpp] {message}");
        }


        /// <summary>
        /// Gets the progres time.
        /// </summary>
        /// <param name="time">The time.</param>
        private static TimeSpan GetProgresTime(float time)
        {
            if ((int)time == 2000) // No idea?
                return TimeSpan.Zero;

            return TimeSpan.FromSeconds(time);
        }
    }
}
