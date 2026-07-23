using Amuse.App.Common;
using Amuse.App.Services;
using Amuse.App.Views;
using Amuse.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TensorStack.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for TextModelControl.xaml
    /// </summary>
    public partial class TextModelControl : BaseControl
    {
        private ListCollectionView _deviceCollectionView;
        private ListCollectionView _modelCollectionView;

        private ProcessType _processType;
        private DeviceModel _selectedDevice;
        private DiffusionModel _selectedModel;
        private MemoryProfileModel _selectedMemoryMode;
        private QualityMode _selectedQualityMode;

        private DeviceModel _currentDevice;
        private DiffusionModel _currentModel;
        private MemoryMode _currentMemoryMode;
        private QualityMode _currentQualityMode;


        /// <summary>
        /// Initializes a new instance of the <see cref="TextModelControl"/> class.
        /// </summary>
        public TextModelControl()
        {
            MemoryModes =
            [
                new MemoryProfileModel{ MemoryMode = MemoryMode.Auto },
                new MemoryProfileModel{ MemoryMode = MemoryMode.Balanced },
                new MemoryProfileModel{ MemoryMode = MemoryMode.Low },
                new MemoryProfileModel{ MemoryMode = MemoryMode.Medium },
                new MemoryProfileModel{ MemoryMode = MemoryMode.High }
            ];
            LoadCommand = new AsyncRelayCommand(LoadAsync, CanLoad);
            UnloadCommand = new AsyncRelayCommand(UnloadAsync, CanUnload);
            InitializeComponent();
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(Settings), typeof(TextModelControl), new PropertyMetadata<TextModelControl>((c) => c.OnSettingsChanged()));
        public static readonly DependencyProperty IsPipelineLoadedProperty = DependencyProperty.Register(nameof(IsPipelineLoaded), typeof(bool), typeof(TextModelControl), new PropertyMetadata<TextModelControl>((c) => c.OnIsPipelineLoadedChanged()));
        public static readonly DependencyProperty IsSelectionValidProperty = DependencyProperty.Register(nameof(IsSelectionValid), typeof(bool), typeof(TextModelControl));
        public static readonly DependencyProperty DownloadServiceProperty = DependencyProperty.Register(nameof(DownloadService), typeof(IModelDownloadService), typeof(TextModelControl));
        public static readonly DependencyProperty NavigationServiceProperty = DependencyProperty.Register(nameof(NavigationService), typeof(NavigationService), typeof(TextModelControl));

        public event EventHandler<PipelineModel> SelectionChanged;
        public View ViewType { get; set; }
        public AsyncRelayCommand LoadCommand { get; }
        public AsyncRelayCommand UnloadCommand { get; }
        public MemoryProfileModel[] MemoryModes { get; }

        public Settings Settings
        {
            get { return (Settings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        public bool IsPipelineLoaded
        {
            get { return (bool)GetValue(IsPipelineLoadedProperty); }
            set { SetValue(IsPipelineLoadedProperty, value); }
        }

        public bool IsSelectionValid
        {
            get { return (bool)GetValue(IsSelectionValidProperty); }
            set { SetValue(IsSelectionValidProperty, value); }
        }

        public IModelDownloadService DownloadService
        {
            get { return (IModelDownloadService)GetValue(DownloadServiceProperty); }
            set { SetValue(DownloadServiceProperty, value); }
        }

        public NavigationService NavigationService
        {
            get { return (NavigationService)GetValue(NavigationServiceProperty); }
            set { SetValue(NavigationServiceProperty, value); }
        }

        public ProcessType ProcessType
        {
            get { return _processType; }
            set { SetProperty(ref _processType, value); }
        }

        public DeviceModel SelectedDevice
        {
            get { return _selectedDevice; }
            set { SetProperty(ref _selectedDevice, value); ValidateSelection(); }
        }

        public DiffusionModel SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); ValidateSelection(); }
        }

        public MemoryProfileModel SelectedMemoryMode
        {
            get { return _selectedMemoryMode; }
            set { SetProperty(ref _selectedMemoryMode, value); ValidateSelection(); }
        }

        public QualityMode SelectedQualityMode
        {
            get { return _selectedQualityMode; }
            set { SetProperty(ref _selectedQualityMode, value); ValidateSelection(); }
        }

        public ListCollectionView DeviceCollectionView
        {
            get { return _deviceCollectionView; }
            set { SetProperty(ref _deviceCollectionView, value); }
        }

        public ListCollectionView ModelCollectionView
        {
            get { return _modelCollectionView; }
            set { SetProperty(ref _modelCollectionView, value); }
        }

        private async Task LoadAsync()
        {
            if (await IsDownloadingAsync())
                return;

            _currentDevice = SelectedDevice;
            _currentModel = SelectedModel;
            _currentMemoryMode = SelectedMemoryMode.MemoryMode;
            _currentQualityMode = SelectedQualityMode;

            var pipeline = new PipelineModel
            {
                Device = _currentDevice,
                DiffusionModel = _currentModel,
                MemoryMode = _currentMemoryMode,
                QualityMode = _currentQualityMode,
                ProcessType = GetProcessType()
            };

            SelectionChanged?.Invoke(this, pipeline);
            ValidateSelection();
        }


        private bool CanLoad()
        {
            return _selectedDevice != null && !IsSelectionValid;
        }


        private Task UnloadAsync()
        {
            _currentModel = default;
            IsSelectionValid = false;

            var pipeline = new PipelineModel
            {
                Device = _selectedDevice,
                MemoryMode = _selectedMemoryMode.MemoryMode,
                QualityMode = _selectedQualityMode,
                ProcessType = _processType
            };

            SelectionChanged?.Invoke(this, pipeline);
            Model_SelectionChanged(default, default);

            ValidateSelection();
            return Task.CompletedTask;
        }


        private bool CanUnload()
        {
            return _currentModel is not null;
        }


        private Task OnIsPipelineLoadedChanged()
        {
            ValidateSelection();
            return Task.CompletedTask;
        }


        private void ValidateSelection()
        {
            var isModelValid = ModelCollectionView?.IsEmpty == false;
            var isCurrentValid = !HasCurrentChanged();
            IsSelectionValid = isCurrentValid && isModelValid && IsPipelineLoaded;
            LoadCommand.RaiseCanExecuteChanged();
        }


        private bool HasCurrentChanged()
        {
            return _currentDevice != SelectedDevice
                || _currentModel != SelectedModel
                || _currentMemoryMode != SelectedMemoryMode?.MemoryMode
                || _currentQualityMode != SelectedQualityMode;
        }


        private Task OnSettingsChanged()
        {
            // Devices
            DeviceCollectionView = new ListCollectionView(Settings.Devices);
            DeviceCollectionView.Filter = (obj) =>
            {
                if (obj is not DeviceModel device)
                    return false;

                if (!Settings.Vendors.Contains(device.Vendor))
                    return false;

                return true;
            };

            // Base Models
            ModelCollectionView = new ListCollectionView(Settings.DiffusionModels);
            ModelCollectionView.IsLiveSorting = true;
            ModelCollectionView.IsLiveFiltering = true;
            ModelCollectionView.SortDescriptions.Add(new SortDescription(nameof(DiffusionModel.Status), ListSortDirection.Descending));
            ModelCollectionView.SortDescriptions.Add(new SortDescription(nameof(DiffusionModel.Name), ListSortDirection.Ascending));
            ModelCollectionView.Filter = (obj) =>
            {
                if (obj is not DiffusionModel viewModel)
                    return false;

                if (_selectedDevice is null)
                    return false;

                if (!_selectedDevice.SupportedBackends.Contains(viewModel.Backend))
                    return false;

                if (!viewModel.ProcessTypes.Contains(_processType))
                    return false;

                if (!viewModel.Vendor.IsNullOrEmpty() && !viewModel.Vendor.Contains(_selectedDevice.Vendor))
                    return false;

                if (!viewModel.ViewFilter.IsNullOrEmpty() && !viewModel.ViewFilter.Contains(ViewType))
                    return false;

                return true;
            };

            SelectedDevice = Settings.GetDefaultDevice();
            Device_SelectionChanged(default, default);

            Settings.PropertyChanged += (s, p) =>
            {
                if (p.PropertyName == nameof(Settings.Vendors))
                {
                    DeviceCollectionView.Refresh();
                    SelectedDevice = Settings.GetDefaultDevice();
                }
            };

            return Task.CompletedTask;
        }


        private void Device_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SetDeviceDataTypes();
            if (ModelCollectionView is not null)
            {
                ModelCollectionView.Refresh();
                SelectedModel = ModelCollectionView.Cast<DiffusionModel>().FirstOrDefault(x => x == _currentModel)
                             ?? ModelCollectionView.Cast<DiffusionModel>().FirstOrDefault();
            }

            RefreshMemoryProfile();
        }


        private void Model_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshMemoryProfile();
            if (_selectedModel is null)
                return;

            SelectedQualityMode = _selectedModel.UserQualityMode is null
                ? _selectedDevice?.DefaultQualityMode ?? QualityMode.Standard
                : _selectedModel.UserQualityMode.Value;
            SelectedMemoryMode = _selectedModel.UserMemoryMode is null
                ? MemoryModes.FirstOrDefault(x => x.MemoryMode == MemoryMode.Auto)
                : MemoryModes.FirstOrDefault(x => x.MemoryMode == _selectedModel.UserMemoryMode.Value);
        }


        private void Memory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshMemoryProfile();
        }


        private void SetDeviceDataTypes()
        {
            if (_selectedDevice is null)
                return;

            SelectedQualityMode = _selectedDevice.QualityModes.Contains(_selectedQualityMode)
                ? _selectedQualityMode
                : _selectedDevice.DefaultQualityMode;
        }


        private void RefreshMemoryProfile()
        {
            if (_selectedDevice is null || _selectedModel is null || _selectedMemoryMode is null)
                return;

            var deviceMemory = _selectedDevice.MemoryGB;
            var profile = _selectedModel.MemoryProfile?.FirstOrDefault(x => x.QualityMode == _selectedQualityMode);
            if (profile is null)
                return;

            var modeIndex = profile.GetIndex(deviceMemory);
            MemoryModes[0].MemoryGB = profile.MemoryModes.ElementAtOrDefault(modeIndex);
            MemoryModes[0].DetectedMode = Enum.GetValues<MemoryMode>()[modeIndex + 2];
            MemoryModes[2].MemoryGB = profile.MemoryModes.ElementAtOrDefault(0);
            MemoryModes[3].MemoryGB = profile.MemoryModes.ElementAtOrDefault(1);
            MemoryModes[4].MemoryGB = profile.MemoryModes.ElementAtOrDefault(2);
        }


        public void SetPipeline(PipelineModel pipeline)
        {
            if (pipeline == null)
                return;

            if (!ModelCollectionView.Contains(pipeline.DiffusionModel))
                return;

            SelectedDevice = pipeline.Device;
            SelectedModel = pipeline.DiffusionModel;

            SelectedQualityMode = pipeline.QualityMode;
            SelectedMemoryMode = MemoryModes.FirstOrDefault(x => x.MemoryMode == pipeline.MemoryMode);

            ValidateSelection();
        }


        private async Task<bool> IsDownloadingAsync()
        {
            var status = GetPipelineStatus();
            if (status.Contains(ModelStatusType.Downloading) || status.Contains(ModelStatusType.DownloadQueue) || status.Contains(ModelStatusType.DownloadFailed))
            {
                await DialogService.ShowMessageAsync("Model Downloading", "This model is downloading or queued for download", TensorStack.WPF.Dialogs.MessageDialogType.Ok, TensorStack.WPF.Dialogs.MessageBoxIconType.Info, TensorStack.WPF.Dialogs.MessageBoxStyleType.Info);
                return true;
            }
            else if (status.Contains(ModelStatusType.Available) || status.Contains(ModelStatusType.Unknown))
            {
                var queueDownload = await DialogService.ShowMessageAsync("Queue Download", "Would you like to queue this model for download?", TensorStack.WPF.Dialogs.MessageDialogType.YesNo, TensorStack.WPF.Dialogs.MessageBoxIconType.Question, TensorStack.WPF.Dialogs.MessageBoxStyleType.Info);
                if (queueDownload)
                {
                    // Base Model
                    if (_selectedModel.Status != ModelStatusType.Installed)
                    {
                        if (!await DownloadService.QueueAsync(_selectedModel))
                            return true;
                    }
                    await NavigationService.NavigateAsync((int)View.Downloads);
                }
                return true;
            }
            return false;
        }


        private List<ModelStatusType> GetPipelineStatus()
        {
            return [_selectedModel.Status];
        }


        private ProcessType GetProcessType()
        {
            return _processType;
        }

    }
}
