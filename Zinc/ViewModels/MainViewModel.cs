using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IFileService _fileService;
    private readonly IEditorViewModelFactory _editorFactory;

    private readonly IReadOnlyList<FileFilter> _filters =
    [
        new() { Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
    ];

    private int _untitledCount;

    public ObservableCollection<EditorViewModel> Tabs { get; } = new();

    [ObservableProperty]
    private EditorViewModel? _selectedItem;

    public MainViewModel(
        IDialogService dialogService,
        IFileService fileService,
        IEditorViewModelFactory editorFactory)
    {
        _dialogService = dialogService;
        _fileService = fileService;
        _editorFactory = editorFactory;

        AddNewTab();
    }

    // ====== 标签管理 ======

    [RelayCommand]
    public void AddNewTab()
    {
        var vm = _editorFactory.Create();
        vm.FileName = $"未标题 {++_untitledCount}";
        Tabs.Add(vm);
        SelectedItem = vm;
    }

    public void CloseTab(EditorViewModel vm)
    {
        Tabs.Remove(vm);

        if (Tabs.Count == 0)
        {
            AddNewTab();
        }

        SelectedItem = Tabs.FirstOrDefault();
    }

    // ====== 文件命令 ======

    [RelayCommand]
    private async Task OpenAsync()
    {
        var filePath = await _dialogService.OpenFilePathAsync("选择要打开的文件", _filters);
        if (string.IsNullOrEmpty(filePath))
            return;

        // 已打开则直接切换
        var existing = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedItem = existing;
            return;
        }

        var file = _fileService.LoadFile(filePath);
        var vm = _editorFactory.Create(file, filePath);
        vm.FileName = Path.GetFileName(filePath);

        Tabs.Add(vm);
        SelectedItem = vm;
    }

    [RelayCommand]
    private Task SaveCurrentTabAsync()
    {
        return SelectedItem?.SaveAsync() ?? Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveAsCurrentTabAsync()
    {
        return SelectedItem?.SaveAsAsync() ?? Task.CompletedTask;
    }
}