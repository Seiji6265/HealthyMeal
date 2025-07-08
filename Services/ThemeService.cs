using System;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace HealthyMeal.Services
{
    public class ThemeService : IThemeService
    {
        public Theme CurrentTheme { get; private set; } = Theme.Light;

        public ThemeService()
        {
            // Ensure there is always a dictionary to work with
            if (Application.Current.Resources.MergedDictionaries.Count == 0)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary());
            }
        }

        public void SetTheme(Theme theme)
        {
            Debug.WriteLine($"[ThemeService] Attempting to set theme to: {theme}");
            CurrentTheme = theme;
            var themeName = theme.ToString();
            var uri = new Uri($"Resources/Themes/{themeName}Theme.xaml", UriKind.Relative);

            // This is now safe because we ensured the dictionary exists in the constructor.
            Application.Current.Resources.MergedDictionaries[0].Source = uri;
            Debug.WriteLine($"[ThemeService] Successfully set theme source to: {uri.OriginalString}");
        }
    }
}