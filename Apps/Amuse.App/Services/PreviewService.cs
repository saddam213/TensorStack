using Amuse.App.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Image;
using TensorStack.Providers;

namespace Amuse.App.Services
{
    public sealed class PreviewService : IPreviewService
    {
        private readonly Settings _settings;
        private readonly ILogger<PreviewService> _logger;
        private readonly SemaphoreSlim _asyncLock;
        private readonly string _previewModelDirectory;
        private ModelSession<ModelConfig> _previewModelSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="logger">The logger.</param>
        public PreviewService(Settings settings, ILogger<PreviewService> logger)
        {
            _logger = logger;
            _settings = settings;
            _asyncLock = new SemaphoreSlim(1, 1);
            _previewModelDirectory = Path.Combine(App.DirectoryData, "Plugins", "Preview");
        }


        /// <summary>
        /// Load the preview model
        /// </summary>
        /// <param name="pipelineType">Type of the pipeline.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task LoadAsync(PipelineModel pipeline, CancellationToken cancellationToken = default)
        {
            if (!_settings.IsDiffusionImagePreviewEnabled)
                return;

            var timestamp = Stopwatch.GetTimestamp();
            var device = pipeline.Device;
            var mediaType = pipeline.DiffusionModel.MediaType;
            var pipelineType = pipeline.DiffusionModel.Pipeline;
            _logger.LogInformation("[Load] Loading {pipelineType} preview model...", pipelineType);

            try
            {
                var previewModelPath = GetModelPath(pipelineType, mediaType);
                if (!File.Exists(previewModelPath))
                {
                    _logger.LogInformation("[Load] No preview model found for {pipelineType}", pipelineType);
                    return;
                }

                _previewModelSession = new ModelSession<ModelConfig>(new ModelConfig
                {
                    Path = previewModelPath,
                    ExecutionProvider = device.GetProvider(Microsoft.ML.OnnxRuntime.GraphOptimizationLevel.ORT_DISABLE_ALL),
                });
                await _previewModelSession.LoadAsync(cancellationToken: cancellationToken);
                _logger.LogInformation("[Load] {pipelineType} preview model loaded, Elapsed: {Elapsed:c}", pipelineType, Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                _previewModelSession?.Dispose();
                _previewModelSession = null;
                _logger.LogError(ex, "[Load] An exception occured loading preview model, Pipeline: {pipelineType}", pipelineType);
            }
        }


        /// <summary>
        /// Unload the pipeline model
        /// </summary>
        public async Task UnloadAsync()
        {
            if (_previewModelSession == null)
                return;

            try
            {
                _logger.LogInformation("[Unload] Unloading preview model...");
                await _previewModelSession.UnloadAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Unload] An exception occured unloading preview model");
            }
            finally
            {
                _previewModelSession?.Dispose();
                _previewModelSession = null;
                _logger.LogInformation("[Unload] Preview model unloaded.");
            }
        }


        /// <summary>
        /// Generate the preview image.
        /// </summary>
        /// <param name="inputTensor">The input tensor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<ImageInput> GenerateAsync(Tensor<float> inputTensor, CancellationToken cancellationToken = default)
        {
            if (!_settings.IsDiffusionImagePreviewEnabled || _previewModelSession == null || !_previewModelSession.IsLoaded())
                return default;

            if (!await _asyncLock.WaitAsync(0, cancellationToken))
                return default; // Skip current preview if previous is still rendering

            var timestamp = Stopwatch.GetTimestamp();
            try
            {
                _logger.LogInformation("[Generate] Generating preview image...");
                using (var modelParameters = new ModelParameters(_previewModelSession.Metadata, cancellationToken))
                {
                    modelParameters.AddInput(inputTensor);
                    modelParameters.AddOutput();
                    using (var results = _previewModelSession.RunInference(modelParameters))
                    {
                        return await results[0]
                            .ToTensor()
                            .AsImageTensor()
                            .ToImageInputAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Generate] An exception occured generation preview tensor.");
                return null;
            }
            finally
            {
                _asyncLock.Release();
                _logger.LogInformation("[Generate] Generating preview image complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
        }


        /// <summary>
        /// Gets the model path.
        /// </summary>
        /// <param name="pipelineType">Type of the pipeline.</param>
        private string GetModelPath(PipelineType pipelineType, MediaType mediaType)
        {
            switch (pipelineType)
            {
                case PipelineType.GlmImagePipeline:
                    return null; // not supported

                case PipelineType.StableDiffusionPipeline:
                case PipelineType.LatentConsistencyPipeline:
                    return Path.Combine(_previewModelDirectory, "StableDiffusion.onnx");
                case PipelineType.StableDiffusionXLPipeline:
                    return Path.Combine(_previewModelDirectory, "StableDiffusionXL.onnx");
                case PipelineType.StableDiffusion3Pipeline:
                    return Path.Combine(_previewModelDirectory, "StableDiffusion3.onnx");
                case PipelineType.FluxPipeline:
                case PipelineType.ChromaPipeline:
                case PipelineType.ZImagePipeline:
                    return Path.Combine(_previewModelDirectory, "Flux1.onnx");
                case PipelineType.Flux2Pipeline:
                case PipelineType.Flux2KleinPipeline:
                case PipelineType.ErniePipeline:
                case PipelineType.IdeogramPipeline:
                    return Path.Combine(_previewModelDirectory, "Flux2.onnx");
                case PipelineType.AnimaPipeline:
                case PipelineType.QwenImagePipeline:
                    return Path.Combine(_previewModelDirectory, "Qwen.onnx");
                case PipelineType.Kandinsky5Pipeline:
                    return mediaType switch
                    {
                        MediaType.Image => Path.Combine(_previewModelDirectory, "Flux1.onnx"),
                        _ => null
                    };
            }
            return null;
        }


        public void Dispose()
        {
            _asyncLock?.Dispose();
            _previewModelSession?.Dispose();
            _previewModelSession = null;
        }
    }


    public interface IPreviewService : IDisposable
    {
        Task UnloadAsync();
        Task LoadAsync(PipelineModel pipeline, CancellationToken cancellationToken = default);
        Task<ImageInput> GenerateAsync(Tensor<float> inputTensor, CancellationToken cancellationToken = default);
    }
}
