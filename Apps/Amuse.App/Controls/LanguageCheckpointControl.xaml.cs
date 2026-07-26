using Amuse.App.Common;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using TensorStack.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for LanguageCheckpointControl.xaml
    /// </summary>
    public partial class LanguageCheckpointControl : BaseControl
    {
        private int _selectedIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageCheckpointControl"/> class.
        /// </summary>
        public LanguageCheckpointControl()
        {
            Components = new ObservableCollection<CheckpointComponent>();
            CheckpointTypes = [CheckpointType.LocalFolder, CheckpointType.OnlineFolder];
            InitializeComponent();
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(Settings), typeof(LanguageCheckpointControl));
        public static readonly DependencyProperty CheckpointProperty = DependencyProperty.Register(nameof(Checkpoint), typeof(LanguageCheckpointModel), typeof(LanguageCheckpointControl), new PropertyMetadata<LanguageCheckpointControl, LanguageCheckpointModel>((c, o, n) => c.OnCheckpointChanged(o, n)));
        public static readonly DependencyProperty BackendProperty = DependencyProperty.Register(nameof(Backend), typeof(BackendType), typeof(LanguageCheckpointControl));
        public ObservableCollection<CheckpointComponent> Components { get; }
        public CheckpointType[] CheckpointTypes { get; }

        public Settings Settings
        {
            get { return (Settings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        public LanguageCheckpointModel Checkpoint
        {
            get { return (LanguageCheckpointModel)GetValue(CheckpointProperty); }
            set { SetValue(CheckpointProperty, value); }
        }

        public BackendType Backend
        {
            get { return (BackendType)GetValue(BackendProperty); }
            set { SetValue(BackendProperty, value); }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { SetProperty(ref _selectedIndex, value); }
        }


        private Task OnCheckpointChanged(LanguageCheckpointModel previous, LanguageCheckpointModel checkpoint)
        {
            Components.Clear();
            if (checkpoint != null)
            {
                foreach (var component in checkpoint.GetComponents())
                {
                    Components.Add(component);
                }

                SelectedIndex = 0;
            }
            return Task.CompletedTask;
        }

    }
}