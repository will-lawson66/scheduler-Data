namespace Instrument.Data.Avalonia.Services
{
    public interface IThemeService
    {
        bool IsDarkTheme { get; }
        void SetLightTheme();
        void SetDarkTheme();
        void ToggleTheme();
    }
}
