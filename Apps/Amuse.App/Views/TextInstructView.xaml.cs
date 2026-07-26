using Amuse.App.Common;
using Amuse.App.Controls;
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
    public partial class TextInstructView : ViewBaseLanguage
    {
        private string _automationPrompt;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextInstructView"/> class.
        /// </summary>
        public TextInstructView(Settings settings, NavigationService navigationService, IModelDownloadService downloadService, IGenerateService generateService, IExtractService extractService, IUpscaleService upscaleService, IHistoryService historyService, ILogger<TextInstructView> logger)
            : base(settings, navigationService, downloadService, generateService, extractService, upscaleService, historyService, logger)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        public override View View => View.TextInstruct;
       
        /// <summary>
        /// Gets or sets the automation prompt.
        /// </summary>
        public string AutomationPrompt
        {
            get { return _automationPrompt; }
            set { SetProperty(ref _automationPrompt, value); }
        }

        /// <summary>
        /// Gets the text result control.
        /// </summary>
        protected override TextResultControl TextResultControl => TextResultElement;


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
            Logger.LogInformation($"[TextInstruct] [Execute] Executing pipeline...");

            try
            {
                Progress.Clear();
                Statistics.Clear();
                ResultText = default;
                await TextResultControl.ClearAsync();
                Statistics.Start();

                // Context
                var textContext = InputControl.GetTextContext(Options.Prompt);
                var imageContext = InputControl.GetImageContext(Options.Prompt);

                // Conversation
                var conversation = new List<ConversationModel>();
                if (!string.IsNullOrEmpty(Options.Prompt2))
                {
                    // System Prompt
                    conversation.Add(new ConversationModel
                    {
                        Role = ConversationRole.System,
                        Content = Options.Prompt2
                    });
                }

                // User Prompt
                textContext.Append(Options.Prompt);
                conversation.Add(new ConversationModel
                {
                    Role = ConversationRole.User,
                    Content = textContext.ToString(),
                    ImageIndex = [.. Enumerable.Range(0, imageContext.Count)]
                });

                // Options
                var options = Options with
                {
                    Prompt = null,
                    Prompt2 = null,
                    InputImages = imageContext,
                    Conversation = conversation
                };

                // Execute
                var textResult = await ExecuteLanguageModelAsync(options);

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
                IsPipelineLoaded = GenerateService.IsLoaded;
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

                // Context
                var textContext = InputControl.GetTextContext(Options.Prompt);
                var imageContext = InputControl.GetImageContext(Options.Prompt);
                var imageIndex = Enumerable.Range(0, imageContext.Count).ToList();

                // Conversation
                var conversation = new List<ConversationModel>();
                if (!string.IsNullOrEmpty(Options.Prompt2))
                {
                    // System Prompt
                    conversation.Add(new ConversationModel 
                    { 
                        Role =  ConversationRole.System, 
                        Content = Options.Prompt2 
                    });
                }

                AutomationProgress.Indeterminate($"Automation Started");
                var cancellationToken = CancellationTokenSource.Token;
                await foreach (var automationJob in AutomationManager.CreateJobsAsync(AutomationOptions, Options, MediaType.Text, MediaType.Text))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Reset
                    ResultText = default;
                    await TextResultControl.ClearAsync();
                    conversation.RemoveAll(x => !x.Role.Equals("system"));
                    AutomationPrompt = $"{Options.Prompt}{automationJob.GenerateOptions.Prompt}";

                    // User Prompt
                    conversation.Add(new ConversationModel
                    {
                        Role =  ConversationRole.User,
                        ImageIndex = imageIndex,
                        Content = $"{textContext}{automationJob.GenerateOptions.Prompt}",
                    });

                    // Options
                    var options = automationJob.GenerateOptions with
                    {
                        Prompt = null,
                        Prompt2 = null,
                        InputImages = imageContext,
                        Conversation = conversation
                    };

                    // Diffusion
                    var textResult = await ExecuteLanguageModelAsync(options);

                    // Result
                    ResultText = textResult;

                    // History
                    if (AutomationOptions.IsHistoryEnabled)
                    {
                        await SaveHistoryAsync(options);
                    }

                    await automationJob.SaveAsync(Utils.GetResponseText(ResultText.Result.Text));
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
                IsPipelineLoaded = GenerateService.IsLoaded;
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
                AutomationPrompt = null;
            }
        }


        /// <summary>
        /// Save history
        /// </summary>
        /// <param name="options">The options.</param>
        private async Task<TextInput> SaveHistoryAsync(GenerateInputOptions options)
        {
            Logger.LogInformation($"[TextInstruct] [SaveHistory] Saving history...");
            var history = new TextInput(Utils.GetResponseText(ResultText.Result.Text));
            options.Conversation.Add(new ConversationModel { Role = ConversationRole.Assistant, Content = history.Text });
            var result = await HistoryService.AddAsync(history, new DiffusionHistory
            {
                Options = options,
                Model = CurrentPipeline.LanguageModel.Name,
                LoraModels = CurrentPipeline.LoraAdapterModel?.Select(x => x.Name).ToArray(),
                Source = View.TextInstruct,
            });
            Logger.LogInformation($"[TextInstruct] [SaveHistory] History saved.");
            return result;
        }
    }
}
