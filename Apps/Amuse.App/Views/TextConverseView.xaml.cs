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

        private int _contextLengthText;
        private int _contextLengthImage;
        private int _contextLengthAudio;

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
                var prompt = Options.Prompt;
                var textContext = InputControl.GetTextContext(prompt);
                var imageContext = InputControl.GetImageContext(prompt);
                var audioContext = InputControl.GetAudioContext(prompt);
                var imageIndex = default(Dictionary<int, string>);
                var audioIndex = default(Dictionary<int, string>);

                if (ConversationElement.Count == 0)
                {
                    _contextLengthText = textContext.Length;
                    _contextLengthImage = imageContext.Count;
                    _contextLengthAudio = audioContext.Count;
                    imageIndex = imageContext.GetIndexedInputs();
                    audioIndex = audioContext.GetIndexedInputs();
                    prompt = $"{textContext}\n{prompt}";
                }
                else
                {
                    if (textContext.Length > _contextLengthText)
                    {
                        // new content
                        prompt = $"{textContext.ToString(_contextLengthText, textContext.Length - _contextLengthText)}\n{prompt}";
                        _contextLengthText = textContext.Length;
                    }
                    if (imageContext.Count > _contextLengthImage)
                    {
                        // new imege
                        imageIndex = [];
                        for (int i = _contextLengthImage; i < imageContext.Count; i++)
                        {
                            imageIndex.Add(i, imageContext[i].SourceFile);
                        }
                        _contextLengthImage = imageContext.Count;
                    }
                    if (audioContext.Count > _contextLengthAudio)
                    {
                        // new audio
                        audioIndex = [];
                        for (int i = _contextLengthAudio; i < audioContext.Count; i++)
                        {
                            audioIndex.Add(i, audioContext[i].SourceFile);
                        }
                        _contextLengthAudio = audioContext.Count;
                    }
                }
                Options.Prompt = string.Empty;


                // System Prompt
                await ConversationElement.AddSystemPromptAsync(Options.Prompt2);

                // User Prompt
                await ConversationElement.AddUserPromptAsync(prompt, imageIndex, audioIndex);

                // Options
                var options = Options with
                {
                    Prompt = null,
                    Prompt2 = null,
                    InputAudios = audioContext,
                    InputImages = imageContext.AsImageTensors(),
                    Conversation = ConversationElement.Conversation
                };

                // Generate
                var textResult = await ExecuteLanguageModelAsync(options);

                // Result
                await ConversationElement.EndStreamResponseAsync();
                Statistics.Stop();

                // History
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
