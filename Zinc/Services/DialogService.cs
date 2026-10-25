using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zinc.Abstractions;
using Zinc.Models;

namespace Zinc.Services;

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

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = ConvertFilters(filters)
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> SaveFilePathAsync(
        string title,
        string defaultFileName = "",
        string defaultExtension = "",
        IReadOnlyList<FileFilter>? filters = null)
    {
        var window = GetOwner();
        if (window is null) return null;

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = defaultExtension,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = ConvertFilters(filters)
        });

        return file?.TryGetLocalPath();
    }

    private static List<FilePickerFileType>? ConvertFilters(IReadOnlyList<FileFilter>? filters)
    {
        if (filters is null || filters.Count == 0) return null;

        return filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Patterns,
            MimeTypes = f.MimeTypes
        }).ToList();
    }

    /// <summary>找不到显式 owner 时退回主窗口。</summary>
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
