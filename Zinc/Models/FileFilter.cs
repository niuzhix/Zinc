using System.Collections.Generic;

namespace Zinc.Models;

/// <summary>文件选择对话框中的过滤项。</summary>
public class FileFilter
{
    public string Name { get; set; } = string.Empty;

    public List<string> Patterns { get; set; } = new();

    /// <summary>可选的 MIME 类型，主要用于非 Windows 平台的文件选择器。</summary>
    public List<string>? MimeTypes { get; set; }
}
