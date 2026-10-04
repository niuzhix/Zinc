using System.Collections.Generic;
using System.Threading.Tasks;
using Zinc.Models;

namespace Zinc.Abstractions;

public interface IDialogService
{
    Task<string?> OpenFilePathAsync(string title, IReadOnlyList<FileFilter>? filters = null);
    Task<string?> SaveFilePathAsync(string title, string defaultFileName = "", string defaultExtension = "", IReadOnlyList<FileFilter>? filters = null);
}
