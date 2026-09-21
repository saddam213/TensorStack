using System;
using System.Windows.Data;
using System.Windows.Markup;
using TensorStack.WPF.Services;

namespace TensorStack.WPF.Controls
{
    [MarkupExtensionReturnType(typeof(string))]
    public class LocalizeExtension : MarkupExtension
    {
        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public string Key { get; }
        public string Format { get; set; }
        public string Append { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (!string.IsNullOrEmpty(Append))
            {
                Format = string.IsNullOrEmpty(Format) 
                    ? $"{{0}}{Append}" 
                    : string.Concat(Format, Append);
            }

            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationService.Instance,
                Mode = BindingMode.OneWay,
                StringFormat = Format
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
