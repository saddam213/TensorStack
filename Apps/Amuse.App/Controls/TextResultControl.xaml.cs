
using Amuse.App.Common;
using Amuse.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TensorStack.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for TextResultControl.xaml
    /// </summary>
    public partial class TextResultControl : BaseControl
    {
        private readonly StreamingTextBuffer _previewBuffer = new();
        private readonly DispatcherTimer _previewTimer;
        private float _previewRefreshInterval = 100;
        private bool _isContextMenuEnabled = true;
        private bool _isToolbarEnabled = true;
        private TextInput _selectedResult;
        private int _previewTokenCount;

        ///// <summary>
        /// Initializes a new instance of the <see cref="TextResultControl"/> class.
        /// </summary>
        public TextResultControl()
        {
            _previewTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(_previewRefreshInterval), DispatcherPriority.Background, OnUpdatePreview, Dispatcher);
            ClearCommand = new AsyncRelayCommand(ClearAsync, CanClear);
            SaveSourceCommand = new AsyncRelayCommand(SaveSourceAsync, CanSaveSource);
            CopySourceCommand = new AsyncRelayCommand(CopySourceAsync, CanCopySource);
            CopyResponseCommand = new AsyncRelayCommand(CopyResponseAsync, CanCopyResponse);
            CopyThinkingCommand = new AsyncRelayCommand(CopyThinkingAsync, CanCopyThinking);
            Progress = new ProgressInfo();
            InitializeComponent();
            IsPreviewMarkdownEnabled = true;
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(Settings), typeof(TextResultControl));
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(nameof(Result), typeof(TextResult), typeof(TextResultControl), new PropertyMetadata<TextResultControl>((c) => c.OnValueChanged()));
        public static readonly DependencyProperty IsSaveEnabledProperty = DependencyProperty.Register(nameof(IsSaveEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty IsRemoveEnabledProperty = DependencyProperty.Register(nameof(IsRemoveEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(ProgressInfo), typeof(TextResultControl), new PropertyMetadata(new ProgressInfo()));
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(BitmapSource), typeof(TextResultControl));
        public static readonly DependencyProperty IsInputEnabledProperty = DependencyProperty.Register(nameof(IsInputEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty IsResultMarkdownEnabledProperty = DependencyProperty.Register(nameof(IsResultMarkdownEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(false));
        public static readonly DependencyProperty IsPreviewEnabledProperty = DependencyProperty.Register(nameof(IsPreviewEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty IsPreviewMarkdownEnabledProperty = DependencyProperty.Register(nameof(IsPreviewMarkdownEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata<TextResultControl>((c) => c.OnPreviewMarkdownChanged()));
        public static readonly DependencyProperty IsThinkingVisibleProperty = DependencyProperty.Register(nameof(IsThinkingVisible), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty MaxTokenLengthProperty = DependencyProperty.Register(nameof(MaxTokenLength), typeof(int), typeof(TextResultControl), new PropertyMetadata(0));
        public AsyncRelayCommand ClearCommand { get; }
        public AsyncRelayCommand SaveSourceCommand { get; }
        public AsyncRelayCommand CopySourceCommand { get; }
        public AsyncRelayCommand CopyResponseCommand { get; }
        public AsyncRelayCommand CopyThinkingCommand { get; }
        public bool HasSourceText => Result != null;

        public Settings Settings
        {
            get { return (Settings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        public TextResult Result
        {
            get { return (TextResult)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public bool IsSaveEnabled
        {
            get { return (bool)GetValue(IsSaveEnabledProperty); }
            set { SetValue(IsSaveEnabledProperty, value); }
        }

        public bool IsRemoveEnabled
        {
            get { return (bool)GetValue(IsRemoveEnabledProperty); }
            set { SetValue(IsRemoveEnabledProperty, value); }
        }

        public ProgressInfo Progress
        {
            get { return (ProgressInfo)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public BitmapSource Placeholder
        {
            get { return (BitmapSource)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        public bool IsInputEnabled
        {
            get { return (bool)GetValue(IsInputEnabledProperty); }
            set { SetValue(IsInputEnabledProperty, value); }
        }

        public bool IsResultMarkdownEnabled
        {
            get { return (bool)GetValue(IsResultMarkdownEnabledProperty); }
            set { SetValue(IsResultMarkdownEnabledProperty, value); }
        }

        public bool IsPreviewEnabled
        {
            get { return (bool)GetValue(IsPreviewEnabledProperty); }
            set { SetValue(IsPreviewEnabledProperty, value); }
        }

        public bool IsPreviewMarkdownEnabled
        {
            get { return (bool)GetValue(IsPreviewMarkdownEnabledProperty); }
            set { SetValue(IsPreviewMarkdownEnabledProperty, value); }
        }

        public bool IsThinkingVisible
        {
            get { return (bool)GetValue(IsThinkingVisibleProperty); }
            set { SetValue(IsThinkingVisibleProperty, value); }
        }

        public int MaxTokenLength
        {
            get { return (int)GetValue(MaxTokenLengthProperty); }
            set { SetValue(MaxTokenLengthProperty, value); }
        }

        public bool IsToolbarEnabled
        {
            get { return _isToolbarEnabled; }
            set { SetProperty(ref _isToolbarEnabled, value); }
        }

        public bool IsContextMenuEnabled
        {
            get { return _isContextMenuEnabled; }
            set { SetProperty(ref _isContextMenuEnabled, value); }
        }

        public TextInput SelectedResult
        {
            get { return _selectedResult; }
            set
            {
                SetProperty(ref _selectedResult, value);
                if (_selectedResult != null)
                {
                    Progress.Update(_selectedResult.TokenCount, MaxTokenLength);
                }
            }
        }

        public int PreviewTokenCount
        {
            get { return _previewTokenCount; }
            set { SetProperty(ref _previewTokenCount, value); }
        }

        public float PreviewRefreshInterval
        {
            get { return _previewRefreshInterval; }
            set
            {
                if (SetProperty(ref _previewRefreshInterval, value))
                {
                    _previewTimer.Interval = TimeSpan.FromMilliseconds(_previewRefreshInterval);
                }
            }
        }


        /// <summary>
        /// Called when DependencyProperty changeded.
        /// </summary>
        private async Task OnValueChanged()
        {
            await ClearPreviewAsync();
            SelectedResult = Result?.Result;
        }


        /// <summary>
        /// Clears thes control
        /// </summary>
        public async Task ClearAsync()
        {
            Result = null;
            SelectedResult = null;
            PreviewTokenCount = 0;
            Progress.Clear();
            await ClearMarkdownAsync();
        }


        /// <summary>
        /// Determines whether this instance can clear.
        /// </summary>
        /// <returns><c>true</c> if this instance can clear; otherwise, <c>false</c>.</returns>
        private bool CanClear()
        {
            return HasSourceText && IsRemoveEnabled;
        }


        /// <summary>
        /// Saves the source
        /// </summary>
        private async Task SaveSourceAsync()
        {
            var saveFilename = await DialogService.SaveFileAsync("Save Text", "TextResult", filter: "Text files (*.txt)|*.txt|Markdown files (*.md)|*.md|JSON files (*.json)|*.json|HTML files (*.html)|*.html|All files (*.*)|*.*", defualtExt: "txt");
            if (!string.IsNullOrEmpty(saveFilename))
            {
                await File.WriteAllTextAsync(saveFilename, SelectedResult.Text);
            }
        }


        /// <summary>
        /// Determines whether this instance can save source.
        /// </summary>
        /// <returns><c>true</c> if this instance can save source; otherwise, <c>false</c>.</returns>
        private bool CanSaveSource()
        {
            return HasSourceText;
        }


        /// <summary>
        /// Copies the source.
        /// </summary>
        private Task CopySourceAsync()
        {
            Clipboard.SetText(SelectedResult?.Text);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Determines whether this instance can copy source.
        /// </summary>
        private bool CanCopySource()
        {
            return HasSourceText;
        }


        /// <summary>
        /// Copies the response text.
        /// </summary>
        private Task CopyResponseAsync()
        {
            Clipboard.SetText(Utils.GetResponseText(SelectedResult?.Text));
            return Task.CompletedTask;
        }


        /// <summary>
        /// Determines whether this instance can copy response text.
        /// </summary>
        private bool CanCopyResponse()
        {
            return HasSourceText;
        }

        /// <summary>
        /// Copies the thinking asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        private Task CopyThinkingAsync()
        {
            Clipboard.SetText(Utils.GetThinkingText(SelectedResult?.Text));
            return Task.CompletedTask;
        }


        /// <summary>
        /// Determines whether this instance can copy thinking text.
        /// </summary>
        private bool CanCopyThinking()
        {
            return Utils.HasThinkingText(SelectedResult?.Text);
        }


        /// <summary>
        /// Updates the progress.
        /// </summary>
        public Task UpdateProgress(PipelineProgress progress)
        {
            _previewTokenCount = progress.Value;
            _previewBuffer.Append(progress.Message);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Handles the <see cref="E:OnUpdatePreview" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void OnUpdatePreview(object sender, EventArgs e)
        {
            if (!IsPreviewEnabled || Result != null)
                return;

            var text = _previewBuffer.Flush();
            if (!string.IsNullOrEmpty(text))
            {
                await PreviewControl.AppendTextAsync(text);
                NotifyPropertyChanged(nameof(PreviewTokenCount));
                Progress.Update(PreviewTokenCount, MaxTokenLength);
            }
        }


        /// <summary>
        /// Clear the preview preview
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task ClearPreviewAsync()
        {
            PreviewTokenCount = 0;
            _previewBuffer.Clear();
            await PreviewControl.ClearAsync();
        }


        /// <summary>
        /// Clear Markdown controls.
        /// </summary>
        private async Task ClearMarkdownAsync()
        {
            await ClearPreviewAsync();
            await ResultControl.ClearAsync();
            foreach (var markdownControl in ResultTabControl.FindVisualChildren<MarkdownElement>())
            {
                await markdownControl.ClearAsync();
            }
        }


        /// <summary>
        /// Called when IsPreviewMarkdownEnabled changed.
        /// </summary>
        /// <returns>Task.</returns>
        private Task OnPreviewMarkdownChanged()
        {
            if (IsPreviewMarkdownEnabled)
                IsResultMarkdownEnabled = true;

            return Task.CompletedTask;
        }


        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseEnter" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsKeyboardFocusWithin)
                Focus();
        }
    }
}
