using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using static System.Net.WebRequestMethods;

namespace Zinc.Core.Services;
public class DialogService : IDialogService
{
    private readonly Window? _owner;

    public DialogService(Window? owner = null)
    {
        _owner = owner;
    }

    public async Task<string?> OpenFilePathAsync(string title, IReadOnlyList<FileFilter>? filters)
    {
        var window = GetOwner();
        if (window is null) return null;
        var storage = window.StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "选择要打开的文件...",
            FileTypeFilter = ConvertFilters(filters)
        });
        if(files.Count > 0)
        {
            return files[0].Path.LocalPath;
        }
        else
        {
            return null;
        }
    }

    public async Task<string?> SaveFilePathAsync(string title, string defaultFileName = "", string defaultExtension = "", IReadOnlyList<FileFilter>? filters = null)
    {
        var window = GetOwner();
        if (window is null) return null;
        var storage = window.StorageProvider;
        var files = await storage.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "选择要打开的文件...",
            DefaultExtension = defaultExtension,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = ConvertFilters(filters)
        });
        return files?.Path.LocalPath;
    }

    private List<FilePickerFileType>? ConvertFilters(IReadOnlyList<FileFilter>? filters)
    {
        if (filters is null || filters.Count == 0) return null;

        return filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Patterns,
            MimeTypes = f.MimeTypes
        }).ToList();
    }

    private Window? GetOwner()
    {
        if (_owner is not null) return _owner;

        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}
