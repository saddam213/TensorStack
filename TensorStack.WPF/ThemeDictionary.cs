using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using TensorStack.WPF.Resources;

namespace TensorStack.WPF
{
    /// <summary>
    /// ResourceDictionary that loads the colour palette for the selected theme.
    /// The theme is read when the dictionary is created, so it must be set before App resources are loaded.
    /// </summary>
    public sealed class ThemeDictionary : ResourceDictionary
    {
        private static ThemeType _theme = ThemeType.Dark;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeDictionary"/> class.
        /// </summary>
        public ThemeDictionary()
        {
            Source = GetSource(_theme);
        }

        /// <summary>
        /// Gets or sets the theme used by new ThemeDictionary instances.
        /// </summary>
        public static ThemeType Theme
        {
            get { return _theme; }
            set { _theme = value; }
        }


        /// <summary>
        /// Gets the colour dictionary source for the theme.
        /// </summary>
        /// <param name="theme">The theme.</param>
        private static Uri GetSource(ThemeType theme)
        {
            return theme switch
            {
                ThemeType.Light => new Uri("pack://application:,,,/TensorStack.WPF;component/ThemeColorsLight.xaml", UriKind.Absolute),
                _ => new Uri("pack://application:,,,/TensorStack.WPF;component/ThemeColorsDark.xaml", UriKind.Absolute)
            };
        }
    }


    public enum ThemeType
    {
        [Display(Name = "ThemeType0", ResourceType = typeof(Enums))]
        Dark = 0,

        [Display(Name = "ThemeType1", ResourceType = typeof(Enums))]
        Light = 1
    }
}
