using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Zinc.Core.Abstractions;

namespace Zinc.Core.Services;

public sealed class FormatService : IFormatService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly string? _path;
    private readonly string? _initError;

    public FormatService(string relativePath = "tools")
    {
        var baseDir = AppContext.BaseDirectory;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _path = Path.Combine(baseDir, relativePath, "clang-format.exe");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _path = Path.Combine(baseDir, relativePath, "clang-format");
            EnsureExecutable(_path);
        }

        if (!File.Exists(_path))
            _initError = $"clang-format 不存在: {_path}";
    }

    public async Task<FormatResult> FormatAsync(string sourceCode, string style = "file")
    {
        if (_initError != null)
            return FormatResult.Fail(_initError);

        var psi = new ProcessStartInfo
        {
            FileName = _path,
            Arguments = $"--style={style} --assume-filename=temp.cpp",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom
        };

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.StandardInput.WriteAsync(sourceCode);
            process.StandardInput.Close();

            var formatted = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return FormatResult.Fail($"clang-format 执行失败: {error}");

            return FormatResult.Ok(formatted);
        }
        catch (Exception ex)
        {
            return FormatResult.Fail($"调用 clang-format 出错: {ex.Message}");
        }
    }

    private static void EnsureExecutable(string path)
    {
        if (!OperatingSystem.IsLinux()) return;

        try
        {
            var mode = File.GetUnixFileMode(path);
            File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute);
        }
        catch
        {
            using var chmod = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            chmod?.WaitForExit();
        }
    }
}