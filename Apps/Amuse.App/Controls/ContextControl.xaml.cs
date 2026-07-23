using Amuse.App.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TensorStack.Audio;
using TensorStack.Common;
using TensorStack.Common.Tensor;
using TensorStack.Image;
using TensorStack.Video;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for ContextControl.xaml
    /// </summary>
    public partial class ContextControl : BaseControl
    {
        private bool _isTextEnabled;
        private bool _isImageEnabled;
        private bool _isVideoEnabled;
        private bool _isAudioEnabled;
        private ContextModel _selectedContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextControl"/> class.
        /// </summary>
        public ContextControl()
        {
            ContextCollection = [];
            InitializeComponent();
            AddTextCommand = new AsyncRelayCommand(AddTextAsync, () => _isTextEnabled);
            AddImageCommand = new AsyncRelayCommand(AddImageAsync, () => _isImageEnabled);
            AddVideoCommand = new AsyncRelayCommand(AddVideoAsync, () => _isVideoEnabled);
            AddAudioCommand = new AsyncRelayCommand(AddAudioAsync, () => _isAudioEnabled);
            ClearCommand = new AsyncRelayCommand(ClearAsync, () => ContextCollection.Count > 0);
            RemoveCommand = new AsyncRelayCommand<ContextModel>(RemoveAsync);
        }

        public AsyncRelayCommand AddTextCommand { get; }
        public AsyncRelayCommand AddImageCommand { get; }
        public AsyncRelayCommand AddVideoCommand { get; }
        public AsyncRelayCommand AddAudioCommand { get; }
        public AsyncRelayCommand ClearCommand { get; }
        public AsyncRelayCommand<ContextModel> RemoveCommand { get; }
        public ObservableCollection<ContextModel> ContextCollection { get; }

        public ContextModel SelectedContext
        {
            get { return _selectedContext; }
            set { SetProperty(ref _selectedContext, value); }
        }

        public bool IsTextEnabled
        {
            get { return _isTextEnabled; }
            set { SetProperty(ref _isTextEnabled, value); }
        }

        public bool IsImageEnabled
        {
            get { return _isImageEnabled; }
            set { SetProperty(ref _isImageEnabled, value); }
        }

        public bool IsVideoEnabled
        {
            get { return _isVideoEnabled; }
            set { SetProperty(ref _isVideoEnabled, value); }
        }

        public bool IsAudioEnabled
        {
            get { return _isAudioEnabled; }
            set { SetProperty(ref _isAudioEnabled, value); }
        }


        public StringBuilder GetTextContext(string query = default)
        {
            var contextBuilder = new StringBuilder();
            if (!_isTextEnabled || !ContextCollection.Any(x => x.MediaType == MediaType.Text))
                return contextBuilder;

            contextBuilder.AppendLine("The following are reference documents that may help answer the user's question.");
            contextBuilder.AppendLine("Use information from these documents when relevant.");
            contextBuilder.AppendLine("If the documents do not contain the answer, say you don't have enough information instead of making up details.");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("=== BEGIN REFERENCE DOCUMENTS ===");
            contextBuilder.AppendLine();
            foreach (var (i, contextModel) in ContextCollection.Index())
            {
                var documentId = i + 1;
                var filename = Path.GetFileName(contextModel.Filename);

                contextBuilder.AppendLine($"=== DOCUMENT: {filename} ===");
                contextBuilder.AppendLine();
                contextBuilder.AppendLine(contextModel.Text.Text.TrimEnd());
                contextBuilder.AppendLine();
                contextBuilder.AppendLine("=== END DOCUMENT ===");
                contextBuilder.AppendLine();
            }
            contextBuilder.AppendLine("=== END REFERENCE DOCUMENTS ===");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine();
            return contextBuilder;
        }


        public List<ImageTensor> GetImageContext(string query = default)
        {
            return ContextCollection
                .Where(x => _isImageEnabled && x.MediaType == MediaType.Image)
                .Select(x => x.Image)
                .ToList();
        }


        public List<AudioInputStream> GetAudioContext(string query = default)
        {
            return ContextCollection
                .Where(x => _isAudioEnabled && x.MediaType == MediaType.Audio)
                .Select(x => x.Audio)
                .ToList();
        }


        public List<VideoInputStream> GetVideoContext(string query = default)
        {
            return ContextCollection
                .Where(x => _isVideoEnabled && x.MediaType == MediaType.Video)
                .Select(x => x.Video)
                .ToList();
        }


        private async Task AddTextAsync()
        {
            var filename = await DialogService.OpenFileAsync("Import Document", filter: "Documents|*.txt;*.md;*.json;*.csv;*.pdf;*.docx;*.rtf|All Files|*.*");
            if (!string.IsNullOrEmpty(filename))
            {
                await AddTextAsync(filename);
            }
        }


        private async Task AddTextAsync(string filename)
        {
            if (ContextCollection.Any(x => x.MediaType == MediaType.Text && filename.Equals(x.Filename, StringComparison.OrdinalIgnoreCase)))
                return;

            var parsedDocument = await DocumentManager.ParseAsync(filename);
            if (string.IsNullOrWhiteSpace(parsedDocument))
                return;

            ContextCollection.Add(new ContextModel
            {
                MediaType = MediaType.Text,
                Filename = filename,
                Text = new TextInput(parsedDocument)
            });
        }


        private async Task AddImageAsync()
        {
            var filename = await DialogService.OpenFileAsync("Import Image", filter: "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif;*.tif;*.tiff|All Files|*.*");
            if (!string.IsNullOrEmpty(filename))
            {
                await AddImageAsync(filename);
            }
        }


        private async Task AddImageAsync(string filename)
        {
            if (ContextCollection.Any(x => x.MediaType == MediaType.Image && filename.Equals(x.Filename, StringComparison.OrdinalIgnoreCase)))
                return;

            ContextCollection.Add(new ContextModel
            {
                MediaType = MediaType.Image,
                Filename = filename,
                Image = await ImageInput.CreateAsync(filename)
            });
        }


        private async Task AddAudioAsync()
        {
            var filename = await DialogService.OpenFileAsync("Import Audio", "Audio", filter: "Audio/Video files (*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg;*.mp4;*.mov;*.mkv;*.webm)|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg;*.mp4;*.mov;*.mkv;*.webm", defualtExt: "wav");
            if (!string.IsNullOrEmpty(filename))
            {
                await AddAudioAsync(filename);
            }
        }


        private async Task AddAudioAsync(string filename)
        {
            if (ContextCollection.Any(x => x.MediaType == MediaType.Audio && filename.Equals(x.Filename, StringComparison.OrdinalIgnoreCase)))
                return;

            ContextCollection.Add(new ContextModel
            {
                MediaType = MediaType.Audio,
                Filename = filename,
                Audio = await AudioInputStream.CreateAsync(filename)
            });
        }


        private async Task AddVideoAsync()
        {
            var filename = await DialogService.OpenFileAsync("Import Video", filter: "Videos|*.mp4;*.gif;|All Files|*.*;");
            if (!string.IsNullOrEmpty(filename))
            {
                await AddVideoAsync(filename);
            }
        }


        private async Task AddVideoAsync(string filename)
        {
            if (ContextCollection.Any(x => x.MediaType == MediaType.Video && filename.Equals(x.Filename, StringComparison.OrdinalIgnoreCase)))
                return;

            ContextCollection.Add(new ContextModel
            {
                MediaType = MediaType.Video,
                Filename = filename,
                Video = await VideoInputStream.CreateAsync(filename)
            });
        }


        private Task RemoveAsync(ContextModel model)
        {
            ContextCollection.Remove(model);
            return Task.CompletedTask;
        }


        private Task ClearAsync()
        {
            ContextCollection.Clear();
            return Task.CompletedTask;
        }


        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var filename = ((string[])e.Data.GetData(DataFormats.FileDrop))?.FirstOrDefault();
                if (File.Exists(filename))
                {
                    var extension = Path.GetExtension(filename);
                    if (TensorStack.WPF.Common.ImageFileExtensions.Contains(extension))
                    {
                        await AddImageAsync(filename);
                    }
                    else if (TensorStack.WPF.Common.VideoFileExtensions.Contains(extension))
                    {
                        await AddVideoAsync(filename);
                    }
                    else if (TensorStack.WPF.Common.AudioFileExtensions.Contains(extension))
                    {
                        await AddAudioAsync(filename);
                    }
                    else
                    {
                        await AddTextAsync(filename);
                    }
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }


    }


    public sealed class ContextModel : BaseModel
    {
        public MediaType MediaType { get; set; }
        public string Filename { get; set; }
        public TextInput Text { get; set; }
        public ImageTensor Image { get; set; }
        public AudioInputStream Audio { get; set; }
        public VideoInputStream Video { get; set; }
    }
}
