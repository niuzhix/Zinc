using System.IO;

namespace Zinc.Core.Services;

/// <summary>编译产物与源文件之间的路径约定，供编译与运行共用。</summary>
public static class CompileArtifacts
{
    /// <summary>由源文件路径推导可执行文件路径。</summary>
    public static string GetExecutablePath(string sourcePath)
        => OperatingSystem.IsWindows()
            ? Path.ChangeExtension(sourcePath, ".exe")
            : Path.ChangeExtension(sourcePath, null);
}
