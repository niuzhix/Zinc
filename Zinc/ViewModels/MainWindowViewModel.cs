using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;

namespace Zinc.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ISettingsService _settings;
        private readonly IFileService _fileservice;
        private readonly IDialogService _dialogservice;

        [ObservableProperty]
        private string _editorContent = string.Empty;
        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        public MainWindowViewModel()
        {
            _fileservice = new FileService();
            _dialogservice = new DialogService();
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
