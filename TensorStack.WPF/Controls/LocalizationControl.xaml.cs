using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TensorStack.WPF.Services;

namespace TensorStack.WPF.Controls
{
    /// <summary>
    /// Interaction logic for LocalizationControl.xaml
    /// </summary>
    public partial class LocalizationControl : BaseControl
    {
        private LocalizationItem _selectedItem;

        public LocalizationControl()
        {
            Items =
            [
                new LocalizationItem("en", "English"),
                new LocalizationItem("ja", "日本語")
            ];
            InitializeComponent();
        }

        public static readonly DependencyProperty UILanguageProperty = DependencyProperty.Register(nameof(UILanguage), typeof(string), typeof(LocalizationControl), new PropertyMetadata<LocalizationControl>((c) => c.OnLanguageChanged()) { DefaultValue = "" });
        public LocalizationItem[] Items { get; }

        public string UILanguage
        {
            get { return (string)GetValue(UILanguageProperty); }
            set { SetValue(UILanguageProperty, value); }
        }

        public LocalizationItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    if (_selectedItem == null)
                        return;

                    LocalizationService.Instance.SetLanguage(_selectedItem.Key);
                }
            }
        }


        private Task OnLanguageChanged()
        {
            if (string.IsNullOrEmpty(UILanguage))
            {
                SelectedItem = Items.FirstOrDefault(x => x.Key.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)) ?? Items[0];
            }
            return Task.CompletedTask;
        }

    }

    public record LocalizationItem(string Key, string Name);
}
