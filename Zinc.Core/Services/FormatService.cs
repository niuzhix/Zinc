using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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

    public async Task<FormatResult> FormatAsync(string sourceCode, string style = "file", int offset = -1)
    {
        if (_initError != null)
            return FormatResult.Fail(_initError);

        var effectiveOffset = offset >= 0 && offset <= sourceCode.Length ? offset : 0;
        var byteOffset = Encoding.UTF8.GetByteCount(sourceCode.AsSpan(0, effectiveOffset));
        var args = $"--style={style} --assume-filename=temp.cpp --cursor={byteOffset} -output-replacements-xml";

        var psi = new ProcessStartInfo
        {
            FileName = _path,
            Arguments = args,
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

            var xml = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return FormatResult.Fail($"clang-format 执行失败: {error}");

            return ApplyReplacements(sourceCode, xml);
        }
        catch (Exception ex)
        {
            return FormatResult.Fail($"调用 clang-format 出错: {ex.Message}");
        }
    }

    private static FormatResult ApplyReplacements(string source, string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return FormatResult.Ok(source, 0);

        var replacements = new List<(int Offset, int Length, byte[] NewText)>();
        var cursorByte = 0;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var root = doc.DocumentElement;
        if (root == null)
            return FormatResult.Fail("clang-format 输出为空。");

        foreach (XmlNode node in root.ChildNodes)
        {
            switch (node.Name)
            {
                case "replacement":
                    var offset = int.Parse(node.Attributes!["offset"]!.Value);
                    var length = int.Parse(node.Attributes!["length"]!.Value);
                    replacements.Add((offset, length, Encoding.UTF8.GetBytes(node.InnerText)));
                    break;
                case "cursor":
                    cursorByte = int.Parse(node.InnerText);
                    break;
            }
        }

        var buffer = new List<byte>(Encoding.UTF8.GetBytes(source));
        foreach (var (offset, length, newText) in replacements.OrderByDescending(r => r.Offset))
        {
            buffer.RemoveRange(offset, length);
            buffer.InsertRange(offset, newText);
        }

        var resultBytes = buffer.ToArray();
        var result = Encoding.UTF8.GetString(resultBytes);
        var cursorChar = Encoding.UTF8.GetCharCount(resultBytes, 0, Math.Min(cursorByte, resultBytes.Length));

        return FormatResult.Ok(result, cursorChar);
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