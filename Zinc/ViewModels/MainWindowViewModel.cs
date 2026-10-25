using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Zinc.Abstractions;
using Zinc.Models;

namespace Zinc.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settings;

    public MainWindowViewModel(
        ISettingsService<AppSettings> settingsService,
        MainViewModel mainViewModel)
    {
        _settings = settingsService;
        MainViewModel = mainViewModel;
        ApplyTheme();
        ApplyColor();
        _settings.SettingsChanged += (s, e) =>
        {
            ApplyTheme();
            ApplyColor();
        };
    }

    public MainViewModel MainViewModel { get; }

    public AppSettings Settings => _settings.Current;

    #region Settings

    private void ApplyTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = Settings.Theme switch
            {
                AppSettings.ThemeDark => ThemeVariant.Dark,
                AppSettings.ThemeLight => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
        }
    }

    private void ApplyColor()
    {
        var theme = App.Theme;
        if (theme == null) return;

        theme.PreferUserAccentColor = Settings.IsCustomThemeColorEnabled;

        if (Settings.IsCustomThemeColorEnabled)
        {
            theme.CustomAccentColor = Settings.ThemeColor;
        }
        else
        {
            theme.CustomAccentColor = null;
        }
    }

    #endregion Settings
}
