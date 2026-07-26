using AvaloniaEdit.TextMate;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;

namespace Zinc.ViewModels
{
    public partial class EditorViewModel : ObservableObject
    {
        private readonly ISettingsService _settings;

        [ObservableProperty]
        private string header = string.Empty;
        [ObservableProperty]
        private string content = string.Empty;

        public EditorViewModel()
        {
            _settings = new SettingsService();
            _settings.Preload();
        }

        public AppSettings Settings => _settings.appSettings;
        public void SaveSettings() => _settings.Save();
    }
}
