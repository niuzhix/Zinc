using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IFileService
{
    string LoadFile(string path);
    void SaveFile(string path, string content);
}
