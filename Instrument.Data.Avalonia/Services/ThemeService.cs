using Avalonia;
using Avalonia.Styling;
using Material.Styles.Themes;

namespace Instrument.Data.Avalonia.Services
{
    public class ThemeService : IThemeService
    {
        private bool _isDarkTheme;
        
        public bool IsDarkTheme => _isDarkTheme;
        
        public void SetLightTheme()
        {
            if (_isDarkTheme)
            {
                var materialTheme = Application.Current.Styles.OfType<MaterialTheme>().FirstOrDefault();
                if (materialTheme != null)
                {
                    materialTheme.BaseTheme = Material.Styles.Themes.Base.BaseThemeMode.Light;
                    _isDarkTheme = false;
                }
            }
        }
        
        public void SetDarkTheme()
        {
            if (!_isDarkTheme)
            {
                var materialTheme = Application.Current.Styles.OfType<Material.Styles.Themes.MaterialTheme>().FirstOrDefault();
                if (materialTheme != null)
                {
                    materialTheme.BaseTheme = Material.Styles.Themes.Base.BaseThemeMode.Dark;
                    _isDarkTheme = true;
                }
            }
        }
        
        public void ToggleTheme()
        {
            if (_isDarkTheme)
                SetLightTheme();
            else
                SetDarkTheme();
        }
    }
}
