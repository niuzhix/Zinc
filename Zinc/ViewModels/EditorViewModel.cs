using AvaloniaEdit.Document;
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
        private TextDocument content;

        public EditorViewModel(string? _content = null)
        {
            _settings = new SettingsService();
            _settings.Preload();
            Content = new TextDocument();
            if(_content != null)
            {
                Content.Insert(0, _content);
            }
        }

        public AppSettings Settings => _settings.appSettings;
        public void SaveSettings() => _settings.Save();
    }
}
