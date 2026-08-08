using System.Collections.Generic;

namespace Zinc.Core.Abstractions;

public class FileFilter
{
    public string Name { get; set; } = string.Empty;
    public List<string> Patterns { get; set; } = new();
    public List<string>? MimeTypes { get; set; }

    public FileFilter() { }

    public FileFilter(string name, params string[] patterns)
    {
        Name = name;
        Patterns = new List<string>(patterns);
    }
}