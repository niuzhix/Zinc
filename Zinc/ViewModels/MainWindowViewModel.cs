using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
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
        private readonly IFileService _file;

        public static FilePickerFileType CodeAll { get; } = new("All Codes")
        {
            Patterns = new[] { "*.cpp", "*.c", "*.hpp", "*.h" },
            AppleUniformTypeIdentifiers = new[] { "public.code" },
            MimeTypes = new[] { "code/*" }
        };

        public MainWindowViewModel()
        {
            _file = new FileService();
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

        private Window GetOwner()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow!;
            }

            throw new InvalidOperationException("无法获取所有者窗口");
        }

        [RelayCommand]
        private async Task OpenFile()
        {
            var window = GetOwner();
            var storage = window.StorageProvider;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "选择要打开的文件...",
                FileTypeFilter = [CodeAll]
            });
            if (files.Count > 0)
            {
                var code = _file.LoadFile(files[0].Path.LocalPath);
                Console.WriteLine(code);
            }
        }
    }
}
