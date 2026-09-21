using System.Globalization;
using System.Linq;
using TensorStack.WPF.Services;

namespace TensorStack.WPF.Controls
{
    /// <summary>
    /// Interaction logic for LocalizationControl.xaml
    /// </summary>
    public partial class LocalizationControl : BaseControl
    {
        private LocalizationItem selectedItem;

        public LocalizationControl()
        {
            Items =
            [
                new LocalizationItem("en-US", "English")
            ];
            SelectedItem = Items.FirstOrDefault(x => x.Key == CultureInfo.CurrentUICulture.IetfLanguageTag) ?? Items[^1];
            InitializeComponent();
        }

        public LocalizationItem[] Items { get; }

        public LocalizationItem SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (SetProperty(ref selectedItem, value))
                {
                    if (selectedItem != null)
                        LocalizationService.Instance.SetLanguage(selectedItem.Key);
                }
            }
        }

    }

    public record LocalizationItem(string Key, string Name);
}
