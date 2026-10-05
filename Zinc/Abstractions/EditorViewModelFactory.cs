using Microsoft.Extensions.DependencyInjection;
using System;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Models;
using Zinc.ViewModels;

public interface IEditorViewModelFactory
{
    EditorViewModel Create(string? content = null, string? path = null);
}

public sealed class EditorViewModelFactory : IEditorViewModelFactory
{
    private readonly IServiceProvider _sp;
    public EditorViewModelFactory(IServiceProvider sp) => _sp = sp;

    public EditorViewModel Create(string? content = null, string? path = null)
        => new EditorViewModel(
            _sp.GetRequiredService<ISettingsService<AppSettings>>(),
            _sp.GetRequiredService<IDialogService>(),
            _sp.GetRequiredService<IFileService>(),
            _sp.GetRequiredService<IProgramService>(),
            _sp.GetRequiredService<IJudgeService>(),
            _sp.GetRequiredService<IFormatService>(),
            content,
            path);
}