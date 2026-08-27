using Avalonia;
using Avalonia.Controls.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;
using Zinc.Models;

namespace Zinc.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ISettingsService<AppSettings> _settings;
        private readonly IFileService _fileservice;
        private readonly IDialogService _dialogservice;

        [ObservableProperty]
        private MainViewModel _mainViewModel = ActivatorUtilities.CreateInstance<MainViewModel>(App.Services);
        [ObservableProperty]
        private string _editorContent = string.Empty;
        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        public MainWindowViewModel(IFileService fileService, IDialogService dialogService, ISettingsService<AppSettings> settingsService)
        {
            _fileservice = fileService;
            _dialogservice = dialogService;
            _settings = settingsService;
            ApplyTheme();
            ApplyColor();
            _settings.SettingsChanged += (s, e) =>
            {
                ApplyTheme();
                ApplyColor();
            };
        }

        public AppSettings Settings => _settings.Current;
        public void SaveSettings() => _settings.Save();

        #region Settings

        private void ApplyTheme()
        {
            if (Application.Current is not null)
            {
                Application.Current.RequestedThemeVariant = (Settings.Theme == 2)
                    ? ThemeVariant.Dark
                    : (Settings.Theme == 1)
                    ? ThemeVariant.Light
                    : ThemeVariant.Default;
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

        [RelayCommand]
        private async Task OpenFileAsync()
        {
            var filters = new[]
        {
            new FileFilter("所有文件", "*.*"),
            new FileFilter("文本文件", "*.txt"),
            new FileFilter("C# 文件", "*.cs"),
            new FileFilter("XML 文件", "*.xml"),
            new FileFilter("JSON 文件", "*.json")
        };

            var filePath = await _dialogservice.OpenFilePathAsync("打开代码文件...", filters);
            if (string.IsNullOrEmpty(filePath)) return;

            EditorContent = _fileservice.LoadFile(filePath);
            CurrentFilePath = filePath;
        }
    }
}
