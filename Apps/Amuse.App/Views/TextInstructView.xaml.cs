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
using System.Windows.Input;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Image;
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
            Logger.LogInformation($"[TextInstruct] [Execute] Executing pipeline...");

            try
            {
                Progress.Clear();
                Statistics.Clear();
                ResultText = default;
                await TextResult.ClearAsync();
                Statistics.Start();

                // Images
                var inputImages = new List<ImageTensor>();
                var imageIndex = new List<int>();
                if (CurrentPipeline.DiffusionModel.ModelType == "Vision")
                {
                    imageIndex.Add(0);
                    inputImages.Add(await ImageInput.CreateAsync("C:\\Users\\Administrator\\Pictures\\2Untitled.png"));
                }

                // Conversation
                var conversation = new List<ConversationModel>();
                if (!string.IsNullOrEmpty(Options.Prompt2))
                    conversation.Add(new ConversationModel { Role = "system", Content = Options.Prompt2 });
                conversation.Add(new ConversationModel { Role = "user", ImageIndex = imageIndex, Content = Options.Prompt });

                // Options
                var options = Options with
                {
                    Prompt = null,
                    Prompt2 = null,
                    InputImages = inputImages,
                    Conversation = conversation
                };

                // Execute
                var textResult = await ExecuteTextDiffusionAsync(options);

                // Result
                Statistics.Stop();

                ResultText = textResult;

                // History
                await SaveHistoryAsync(options);
                Logger.LogInformation("[TextInstruct] [Execute] Executing pipeline complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (OperationCanceledException)
            {
                Statistics.Clear();
                Logger.LogInformation("[TextInstruct] [Execute] Executing pipeline cancelled, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                Statistics.Clear();
                IsPipelineLoaded = DiffusionService.IsLoaded;
                Logger.LogError(ex, "[TextInstruct] [Execute] An exception occurred executing pipeline, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
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
            Logger.LogInformation($"[TextInstruct] [ExecuteAutomation] Executing pipeline...");

            try
            {
                Progress.Clear();
                AutomationProgress.Clear();
                Statistics.Clear();
                ResultText = default;
                Statistics.Start();
                CancellationTokenSource = new CancellationTokenSource();

                AutomationProgress.Indeterminate($"Automation Started");
                var cancellationToken = CancellationTokenSource.Token;
                await foreach (var automationJob in AutomationManager.CreateJobsAsync(AutomationOptions, Options, MediaType.Text, MediaType.Audio))
                {
                    cancellationToken.ThrowIfCancellationRequested();

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
                Logger.LogInformation("[TextInstruct] [ExecuteAutomation] Executing pipeline complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (OperationCanceledException)
            {
                Statistics.Clear();
                Logger.LogInformation("[TextInstruct] [ExecuteAutomation] Executing pipeline cancelled, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                Statistics.Clear();
                IsPipelineLoaded = DiffusionService.IsLoaded;
                Logger.LogError(ex, "[TextInstruct] [ExecuteAutomation] An exception occurred executing pipeline, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
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
            Logger.LogInformation($"[TextInstruct] [SaveHistory] Saving history...");
            var result = await HistoryService.AddAsync(ResultText.Result, new DiffusionHistory
            {
                Options = options,
                Model = CurrentPipeline.DiffusionModel.Name,
                LoraModels = CurrentPipeline.LoraAdapterModel?.Select(x => x.Name).ToArray(),
                Source = View.TextInstruct,
            });
            Logger.LogInformation($"[TextInstruct] [SaveHistory] History saved.");
            return result;
        }


        protected override void OnProgress(PipelineProgress progress)
        {
            if (CurrentPipeline is null)
                return;

            if (progress.Key == "Generate")
            {
                if (progress.Subkey == "Initialize")
                {
                    CommandManager.InvalidateRequerySuggested();
                }
                else if (progress.Subkey == "Token")
                {
                    Statistics.Update(progress);
                    TextResult.UpdateProgress(progress);
                }
                else
                {
                    var message = progress.Subkey == "Transformer" && Options.Beams > 1
                         ? "Generating Beam Results..."
                         : Globalization.GetProgressMessage(progress);
                    Progress.Indeterminate(message);
                }
            }

            if (progress.Subkey != "Token")
            {
                Logger.LogDebug("[{View}] [OnProgress] {Subkey} - {Message}", ViewName, progress.Subkey, progress.Message);
            }
        }


        private void SourceText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                if (CanExecute())
                    ExecuteCommand.Execute(null);
            }
        }
    }
}
