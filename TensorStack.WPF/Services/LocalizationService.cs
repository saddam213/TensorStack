using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace TensorStack.WPF.Services
{
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
        private readonly ResourceManager[] _resourceManagers;
        private CultureInfo _cultureInfo;

        private LocalizationService()
        {
            _resourceManagers = GetResourceManagers();
            _cultureInfo = CultureInfo.CurrentUICulture;
        }

        public static LocalizationService Instance => _instance.Value;

        public event PropertyChangedEventHandler PropertyChanged;
        public CultureInfo CurrentUICulture => _cultureInfo;


        public string this[string key]
        {
            get
            {
                foreach (var manager in _resourceManagers)
                {
                    var value = manager.GetString(key, _cultureInfo);

                    if (value is not null)
                        return value;
                }
                return $"[{key}]";
            }
        }


        public void SetLanguage(string cultureName)
        {
            _cultureInfo = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = _cultureInfo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }


        private static ResourceManager[] GetResourceManagers()
        {
            var results = new List<ResourceManager>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName();
                if (string.IsNullOrEmpty(assemblyName.Name))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    var property = type.GetProperty(nameof(ResourceManager), BindingFlags.Public | BindingFlags.Static);
                    if (property?.PropertyType != typeof(ResourceManager) || property.GetValue(null) is not ResourceManager manager)
                        continue;

                    results.Add(manager);
                }
            }
            return [.. results.Distinct()];
        }
    }
}
