using Avalonia;
using Avalonia.Styling;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;

namespace Zinc.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ISettingsService _settings;

        public MainWindowViewModel()
        {
            _settings = new SettingsService();
            _settings.Preload();
            ApplyTheme();
        }

        public AppSettings Settings => _settings.appSettings;
        public void SaveSettings() => _settings.Save();

        private void ApplyTheme()
        {
            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeVariant = (Settings.AppStyle == "Dark")
                    ? ThemeVariant.Dark
                    : (Settings.AppStyle == "Light") 
                    ? ThemeVariant.Light
                    : ThemeVariant.Default;
            }
        }
    }
}
