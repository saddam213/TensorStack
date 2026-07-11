using Amuse.App.Common;
using Amuse.App.Services;
using Amuse.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace Amuse.App.Views
{
    /// <summary>
    /// Interaction logic for TextInstructView.xaml
    /// </summary>
    public partial class TextInstructView : ViewBaseDiffusion
    {
        private TextInput _sourceText;
        private string _previewResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextInstructView"/> class.
        /// </summary>
        public TextInstructView(Settings settings, NavigationService navigationService, IModelDownloadService downloadService, IDiffusionService diffusionService, IExtractService extractService, IUpscaleService upscaleService, IHistoryService historyService, ILogger<TextInstructView> logger)
            : base(settings, navigationService, downloadService, diffusionService, extractService, upscaleService, historyService, logger)
        {
            _sourceText = new TextInput(string.Empty);
            InitializeComponent();
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        public override View View => View.TextInstruct;

        /// <summary>
        /// Gets or sets the source text.
        /// </summary>
        public TextInput SourceText
        {
            get { return _sourceText; }
            set { SetProperty(ref _sourceText, value); }
        }


        /// <summary>
        /// Gets or sets the preview result.
        /// </summary>
        public string PreviewResult
        {
            get { return _previewResult; }
            set { SetProperty(ref _previewResult, value); }
        }


        /// <summary>
        /// On View Open
        /// </summary>
        public override async Task OpenAsync(OpenViewArgs args = null)
        {
            await base.OpenAsync(args);
            if (!IsPipelineLoaded)
                ModelControl.SetPipeline(DiffusionService.Pipeline);
        }


        /// <summary>
        /// Execute thge pipeline.
        /// </summary>
        protected override async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Logger.LogInformation($"[AudioToText] [Execute] Executing pipeline...");

            try
            {
                Progress.Clear();
                Statistics.Clear();
                ResultText = default;
                PreviewResult = default;
                Statistics.Start();

                var conversation = new List<ConversationModel>
                {
                    new ConversationModel{ Role = "user", Content = _sourceText.Text }
                };

                // Options
                var options = Options with { Conversation = conversation };

                // Execute
                var textResult = await ExecuteTextDiffusionAsync(options);

                // Result
                Statistics.Stop();

                ResultText = textResult;

                // History
              //  await SaveHistoryAsync(options);
                Logger.LogInformation("[AudioToText] [Execute] Executing pipeline complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (OperationCanceledException)
            {
                Statistics.Clear();
                Logger.LogInformation("[AudioToText] [Execute] Executing pipeline cancelled, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                Statistics.Clear();
                IsPipelineLoaded = DiffusionService.IsLoaded;
                Logger.LogError(ex, "[AudioToText] [Execute] An exception occurred executing pipeline, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
                await DialogService.ShowErrorAsync("Execute Pipeline", ex.Message);
            }
            finally
            {
                Progress.Clear();
            }
        }


        /// <summary>
        /// Executes the pipeline automation.
        /// </summary>
        protected override async Task ExecuteAutomationAsync()
        {
            IsAutomating = true;
            var timestamp = Stopwatch.GetTimestamp();
            Logger.LogInformation($"[AudioToText] [ExecuteAutomation] Executing pipeline...");

            try
            {
                Progress.Clear();
                AutomationProgress.Clear();
                Statistics.Clear();
                ResultText = default;
                PreviewResult = default;
                Statistics.Start();
                CancellationTokenSource = new CancellationTokenSource();

                AutomationProgress.Indeterminate($"Automation Started");
                var cancellationToken = CancellationTokenSource.Token;
                await foreach (var automationJob in AutomationManager.CreateJobsAsync(AutomationOptions, Options, MediaType.Text, MediaType.Audio))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    PreviewResult = default;

                    // Diffusion
                    var textResult = await ExecuteTextDiffusionAsync(automationJob.DiffusionOptions);

                    // Result
                    ResultText = textResult;
    
                    // History
                    if (AutomationOptions.IsHistoryEnabled)
                    {
                        await SaveHistoryAsync(automationJob.DiffusionOptions);
                    }

                    await automationJob.SaveAsync(ResultAudio);
                    AutomationProgress.Update(automationJob.Id, automationJob.Count, $"Automation: {automationJob.Id}/{automationJob.Count}");
                }

                Statistics.Stop();
                Logger.LogInformation("[AudioToText] [ExecuteAutomation] Executing pipeline complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (OperationCanceledException)
            {
                Statistics.Clear();
                Logger.LogInformation("[AudioToText] [ExecuteAutomation] Executing pipeline cancelled, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                Statistics.Clear();
                IsPipelineLoaded = DiffusionService.IsLoaded;
                Logger.LogError(ex, "[AudioToText] [ExecuteAutomation] An exception occurred executing pipeline, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
                await DialogService.ShowErrorAsync("Execute Automation", ex.Message);
            }
            finally
            {
                Progress.Clear();
                AutomationProgress.Clear();
                IsAutomating = false;
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
            }
        }


        /// <summary>
        /// Save history
        /// </summary>
        /// <param name="options">The options.</param>
        private async Task<TextInput> SaveHistoryAsync(DiffusionInputOptions options)
        {
            Logger.LogInformation($"[TextToImage] [SaveHistory] Saving history...");
            var result = await HistoryService.AddAsync(ResultText.Result, new DiffusionHistory
            {
                Options = options,
                Model = CurrentPipeline.DiffusionModel.Name,
                LoraModels = CurrentPipeline.LoraAdapterModel?.Select(x => x.Name).ToArray(),
                Source = View.AudioToText,
            });
            Logger.LogInformation($"[TextToImage] [SaveHistory] History saved.");
            return result;
        }


        protected override void OnProgress(PipelineProgress progress)
        {
            base.OnProgress(progress);
            if (progress.Subkey == "Step")
            {
                PreviewResult += progress.Message;
            }
        }
    }
}
