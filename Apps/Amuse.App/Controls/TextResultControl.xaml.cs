
using Amuse.App.Common;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
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
        private bool _isContextMenuEnabled = true;
        private bool _isToolbarEnabled = true;
        private TextInput _selectedResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextResultControl"/> class.
        /// </summary>
        public TextResultControl()
        {
            ClearCommand = new AsyncRelayCommand(ClearAsync, CanClear);
            SaveSourceCommand = new AsyncRelayCommand(SaveSourceAsync, CanSaveSource);
            CopySourceCommand = new AsyncRelayCommand(CopySourceAsync, CanCopySource);
            InitializeComponent();
        }

        public static readonly DependencyProperty ConfigurationProperty = DependencyProperty.Register(nameof(Configuration), typeof(IUIConfiguration), typeof(TextResultControl));
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(nameof(Result), typeof(TextResult), typeof(TextResultControl), new PropertyMetadata<TextResultControl>((c) => c.OnValueChanged()));
        public static readonly DependencyProperty PreviewProperty = DependencyProperty.Register(nameof(Preview), typeof(string), typeof(TextResultControl));
        public static readonly DependencyProperty IsSaveEnabledProperty = DependencyProperty.Register(nameof(IsSaveEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty IsRemoveEnabledProperty = DependencyProperty.Register(nameof(IsRemoveEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));
        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(ProgressInfo), typeof(TextResultControl), new PropertyMetadata(new ProgressInfo()));
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(BitmapSource), typeof(TextResultControl));
        public static readonly DependencyProperty IsInputEnabledProperty = DependencyProperty.Register(nameof(IsInputEnabled), typeof(bool), typeof(TextResultControl), new PropertyMetadata(true));

        public AsyncRelayCommand ClearCommand { get; }
        public AsyncRelayCommand SaveSourceCommand { get; }
        public AsyncRelayCommand CopySourceCommand { get; }
        public bool HasSourceText => Result != null;

        public IUIConfiguration Configuration
        {
            get { return (IUIConfiguration)GetValue(ConfigurationProperty); }
            set { SetValue(ConfigurationProperty, value); }
        }

        public TextResult Result
        {
            get { return (TextResult)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public string Preview
        {
            get { return (string)GetValue(PreviewProperty); }
            set { SetValue(PreviewProperty, value); }
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
            set { SetProperty(ref _selectedResult, value); }
        }


        /// <summary>
        /// Called when DependencyProperty changeded.
        /// </summary>
        private Task OnValueChanged()
        {
            Preview = null;
            SelectedResult = Result?.Result;
            return Task.CompletedTask;
        }


        /// <summary>
        /// Clears thes control
        /// </summary>
        public Task ClearAsync()
        {
            Result = null;
            Preview = null;
            SelectedResult = null;
            return Task.CompletedTask;
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
            Clipboard.SetText(SelectedResult.Text);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Determines whether this instance can copy source.
        /// </summary>
        /// <returns><c>true</c> if this instance can copy source; otherwise, <c>false</c>.</returns>
        private bool CanCopySource()
        {
            return HasSourceText;
        }

    }
}
