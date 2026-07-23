using Amuse.App.Common;
using Amuse.App.Views;
using Amuse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TensorStack.Audio;
using TensorStack.Common.Tensor;
using TensorStack.Video;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for TextInputControl.xaml
    /// </summary>
    public partial class TextInputControl : BaseControl
    {
        private DiffusionInputOption _selectedOption;

        public TextInputControl()
        {
            SeedCommand = new RelayCommand<bool>(GenerateSeed);
            InitializeComponent();
        }

        public static readonly DependencyProperty PipelineProperty = DependencyProperty.Register(nameof(Pipeline), typeof(PipelineModel), typeof(TextInputControl), new PropertyMetadata<TextInputControl, PipelineModel>((c, o, n) => c.OnPipelineChanged(o, n)));
        public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(DiffusionInputOptions), typeof(TextInputControl));
        public static readonly DependencyProperty AutomationOptionsProperty = DependencyProperty.Register(nameof(AutomationOptions), typeof(AutomationOptions), typeof(TextInputControl));
        public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(nameof(IsExecuting), typeof(bool), typeof(TextInputControl));
        public static readonly DependencyProperty IsAutomatingProperty = DependencyProperty.Register(nameof(IsAutomating), typeof(bool), typeof(TextInputControl));
        public static readonly DependencyProperty AutomationProgressProperty = DependencyProperty.Register(nameof(AutomationProgress), typeof(ProgressInfo), typeof(TextInputControl));

        public View ViewType { get; set; }
        public ProcessType ProcessType { get; set; }
        public RelayCommand<bool> SeedCommand { get; }

        public PipelineModel Pipeline
        {
            get { return (PipelineModel)GetValue(PipelineProperty); }
            set { SetValue(PipelineProperty, value); }
        }

        public DiffusionInputOptions Options
        {
            get { return (DiffusionInputOptions)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        public AutomationOptions AutomationOptions
        {
            get { return (AutomationOptions)GetValue(AutomationOptionsProperty); }
            set { SetValue(AutomationOptionsProperty, value); }
        }

        public ProgressInfo AutomationProgress
        {
            get { return (ProgressInfo)GetValue(AutomationProgressProperty); }
            set { SetValue(AutomationProgressProperty, value); }
        }

        public bool IsExecuting
        {
            get { return (bool)GetValue(IsExecutingProperty); }
            set { SetValue(IsExecutingProperty, value); }
        }

        public bool IsAutomating
        {
            get { return (bool)GetValue(IsAutomatingProperty); }
            set { SetValue(IsAutomatingProperty, value); }
        }

        public DiffusionInputOption SelectedOption
        {
            get { return _selectedOption; }
            set { SetProperty(ref _selectedOption, value); }
        }

        private bool _isPromptEnabled;
        private bool _isInputImagesEnabled;

        public bool IsPromptEnabled
        {
            get { return _isPromptEnabled; }
            set { SetProperty(ref _isPromptEnabled, value); }
        }

        public bool IsInputImagesEnabled
        {
            get { return _isInputImagesEnabled; }
            set { SetProperty(ref _isInputImagesEnabled, value); }
        }


        public string DefaultPrompt { get; set; }

        private Task OnPipelineChanged(PipelineModel oldPipeline, PipelineModel newPipeline)
        {
            if (newPipeline is null || newPipeline.DiffusionModel is null)
            {
                return Task.CompletedTask;
            }

            var oldModel = oldPipeline?.DiffusionModel;
            var oldOptions = oldModel?.DefaultOptions;
            var newModel = newPipeline?.DiffusionModel;
            var newOptions = newModel?.DefaultOptions;

            var previousOptions = Options;
            Options = new DiffusionInputOptions
            {
                // Keep
                Seed = previousOptions?.Seed ?? 0,
                Prompt = previousOptions?.Prompt ?? DefaultPrompt,

                MinLength = newOptions.MinLength,
                MaxLength = newOptions.MaxLength,
                IsSamplingEnabled = newOptions.IsSamplingEnabled,
                Beams = newOptions.Beams,
                Temperature = newOptions.Temperature,
                TopK = newOptions.TopK,
                TopP = newOptions.TopP,
                TopH = newOptions.TopH,
                TypicalP = newOptions.TypicalP,
                RepetitionPenalty = newOptions.RepetitionPenalty,
                LengthPenalty = newOptions.LengthPenalty,
                NoRepeatNgramSize = newOptions.NoRepeatNgramSize,
                EarlyStopping = newOptions.EarlyStopping,
                ChunkSize = newOptions.ChunkSize
            };

            // Automation
            var automationType = AutomationOptions.GetSupportedTypes(ViewType);
            AutomationOptions = new AutomationOptions
            {
                ViewType = ViewType,
                Type = automationType.FirstOrDefault(),
                OutputPrefix = string.Empty,
                OutputPostFixSeed = false
            };

            // Context
            ContextControlElement.IsTextEnabled = true;
            ContextControlElement.IsImageEnabled = newModel.ModelType == "Vision";
            return Task.CompletedTask;
        }


        private void GenerateSeed(bool random)
        {
            Options.Seed = random ? 0 : Random.Shared.Next();
        }


        public StringBuilder GetTextContext(string query = default)
        {
            return ContextControlElement.GetTextContext(query);
        }


        public List<ImageTensor> GetImageContext(string query = default)
        {
            return ContextControlElement.GetImageContext(query);
        }


        public List<AudioInputStream> GetAudioContext(string query = default)
        {
            return ContextControlElement.GetAudioContext(query);
        }

        public List<VideoInputStream> GetVideoContext(string query = default)
        {
            return ContextControlElement.GetVideoContext(query);
        }
    }
}
