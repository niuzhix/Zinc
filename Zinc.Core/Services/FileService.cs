using System.Text.Json;
using System.Xml.Linq;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;
public class FileService : IFileService
{
    public string LoadFile(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    public void SaveFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
    }
}
