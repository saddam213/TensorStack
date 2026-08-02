using Amuse.App.Common;
using Amuse.App.Services;
using Amuse.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace Amuse.App.Views
{
    /// <summary>
    /// Interaction logic for TextConverseView.xaml
    /// </summary>
    public partial class TextConverseView : ViewBaseLanguage
    {
        private string _automationPrompt;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextConverseView"/> class.
        /// </summary>
        public TextConverseView(Settings settings, NavigationService navigationService, IModelDownloadService downloadService, IGenerateService generateService, IExtractService extractService, IUpscaleService upscaleService, IHistoryService historyService, ILogger<TextConverseView> logger)
            : base(settings, navigationService, downloadService, generateService, extractService, upscaleService, historyService, logger)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        public override View View => View.TextConverse;

        /// <summary>
        /// Gets or sets the automation prompt.
        /// </summary>
        public string AutomationPrompt
        {
            get { return _automationPrompt; }
            set { SetProperty(ref _automationPrompt, value); }
        }


        /// <summary>
        /// On View Open
        /// </summary>
        public override async Task OpenAsync(OpenViewArgs args = null)
        {
            await base.OpenAsync(args);
            if (!IsPipelineLoaded)
                ModelControl.SetPipeline(GenerateService.Pipeline);
        }


        /// <summary>
        /// Execute thge pipeline.
        /// </summary>
        protected override async Task ExecuteAsync()
        {
            var timestamp = Stopwatch.GetTimestamp();
            Logger.LogInformation($"[TextConverse] [Execute] Executing pipeline...");

            try
            {
                Progress.Clear();
                Statistics.Clear();
                Statistics.Start();

                // Context
                // var textContext = InputControl.GetTextContext(Options.Prompt);
                // var imageContext = InputControl.GetImageContext(Options.Prompt);
                // var audioContext = InputControl.GetAudioContext(Options.Prompt);

                // System Prompt
                await ConversationElement.AddSystemPromptAsync(Options.Prompt2);


                // User Prompt
                await ConversationElement.AddUserPromptAsync(Options.Prompt);
                Options.Prompt = string.Empty;

                // Generate
                var generateResult = await ExecuteLanguageModelAsync(Options with
                {
                    Prompt = null,
                    Prompt2 = null,
                    InputImages = [],
                    InputAudios = [],
                    Conversation = ConversationElement.Conversation
                });

                // End Stream
                await ConversationElement.EndStreamResponseAsync();

                // Result
                Statistics.Stop();
                Logger.LogInformation("[TextConverse] [Execute] Executing pipeline complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (OperationCanceledException)
            {
                Statistics.Clear();
                Logger.LogInformation("[TextConverse] [Execute] Executing pipeline cancelled, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                Statistics.Clear();
                IsPipelineLoaded = GenerateService.IsLoaded;
                Logger.LogError(ex, "[TextConverse] [Execute] An exception occurred executing pipeline, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
                await DialogService.ShowErrorAsync("Execute Pipeline", ex.Message);
            }
            finally
            {
                Progress.Clear();
                await ConversationElement.EndStreamResponseAsync();
            }
        }




        /// <summary>
        /// Executes the pipeline automation.
        /// </summary>
        protected override async Task ExecuteAutomationAsync()
        {
            IsAutomating = true;
            var timestamp = Stopwatch.GetTimestamp();
            Logger.LogInformation($"[TextConverse] [ExecuteAutomation] Executing pipeline...");

            try
            {
                Progress.Clear();
                AutomationProgress.Clear();
                Statistics.Clear();
                Statistics.Start();
                CancellationTokenSource = new CancellationTokenSource();

                AutomationProgress.Indeterminate($"Automation Started");
                var cancellationToken = CancellationTokenSource.Token;
                await foreach (var automationJob in AutomationManager.CreateJobsAsync(AutomationOptions, Options, MediaType.Text, MediaType.Text))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // History
                    if (AutomationOptions.IsHistoryEnabled)
                    {
                        // await SaveHistoryAsync(options);
                    }

                    //await automationJob.SaveAsync(Utils.GetResponseText(ResultText.Result.Text));
                    // AutomationProgress.Update(automationJob.Id, automationJob.Count, $"Automation: {automationJob.Id}/{automationJob.Count}");
                }

                Statistics.Stop();
                Logger.LogInformation("[TextConverse] [ExecuteAutomation] Executing pipeline complete, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (OperationCanceledException)
            {
                Statistics.Clear();
                Logger.LogInformation("[TextConverse] [ExecuteAutomation] Executing pipeline cancelled, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
            }
            catch (Exception ex)
            {
                Statistics.Clear();
                IsPipelineLoaded = GenerateService.IsLoaded;
                Logger.LogError(ex, "[TextConverse] [ExecuteAutomation] An exception occurred executing pipeline, Elapsed: {Elapsed:c}", Stopwatch.GetElapsedTime(timestamp));
                await DialogService.ShowErrorAsync("Execute Automation", ex.Message);
            }
            finally
            {
                Progress.Clear();
                AutomationProgress.Clear();
                IsAutomating = false;
                CancellationTokenSource?.Dispose();
                CancellationTokenSource = null;
                AutomationPrompt = null;
            }
        }


        /// <summary>
        /// Save history
        /// </summary>
        /// <param name="options">The options.</param>
        //private async Task<TextInput> SaveHistoryAsync(GenerateInputOptions options)
        //{
        //    Logger.LogInformation($"[TextConverse] [SaveHistory] Saving history...");
        //    var history = new TextInput(Utils.GetResponseText(ResultText.Result.Text));
        //    options.Conversation.Add(new ConversationModel { Role = ConversationRole.Assistant, Content = history.Text });
        //    var result = await HistoryService.AddAsync(history, new DiffusionHistory
        //    {
        //        Options = options,
        //        Model = CurrentPipeline.LanguageModel.Name,
        //        LoraModels = CurrentPipeline.LoraAdapterModel?.Select(x => x.Name).ToArray(),
        //        Source = View.TextConverse,
        //    });
        //    Logger.LogInformation($"[TextConverse] [SaveHistory] History saved.");
        //    return result;
        //}


        protected override void OnProgress(PipelineProgress progress)
        {
            base.OnProgress(progress);
            if (progress.Key == "Generate" && progress.Subkey == "Token")
            {
                ConversationElement.UpdateStreamResponse(progress.Message, progress.Value);
            }
        }


        protected override bool CanExecute()
        {
            return base.CanExecute() && !string.IsNullOrEmpty(Options?.Prompt);
        }
    }
}
