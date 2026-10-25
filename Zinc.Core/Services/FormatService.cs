using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Zinc.Core.Abstractions;

namespace Zinc.Core.Services;

public sealed class FormatService : IFormatService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private const string DefaultStyle = "file";
    private const string AssumeFileName = "temp.cpp";

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

        if (_path is null)
            _initError = $"当前平台不支持调用 clang-format: {RuntimeInformation.RuntimeIdentifier}";
        else if (!File.Exists(_path))
            _initError = $"clang-format 不存在: {_path}";
    }

    public async Task<FormatResult> FormatAsync(string sourceCode, string style = DefaultStyle, int offset = -1)
    {
        if (_initError is not null)
            return FormatResult.Fail(_initError);

        var effectiveOffset = offset >= 0 && offset <= sourceCode.Length ? offset : 0;
        var byteOffset = Utf8NoBom.GetByteCount(sourceCode.AsSpan(0, effectiveOffset));

        var psi = new ProcessStartInfo
        {
            FileName = _path,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom
        };

        psi.ArgumentList.Add($"--style={style}");
        psi.ArgumentList.Add($"--assume-filename={AssumeFileName}");
        psi.ArgumentList.Add($"--cursor={byteOffset.ToString(CultureInfo.InvariantCulture)}");
        psi.ArgumentList.Add("-output-replacements-xml");

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            // 先挂上两路读取，再写输入，避免 stderr 填满管道造成互等。
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.StandardInput.WriteAsync(sourceCode);
            process.StandardInput.Close();

            await process.WaitForExitAsync();

            var xml = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
                return FormatResult.Fail($"clang-format 执行失败: {error}");

            return ApplyReplacements(sourceCode, xml, effectiveOffset);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[FormatService] 调用 clang-format 失败: {ex}");
            return FormatResult.Fail($"调用 clang-format 出错: {ex.Message}");
        }
    }

    private static FormatResult ApplyReplacements(string source, string xml, int fallbackCursor)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return FormatResult.Ok(source, fallbackCursor);

        XmlDocument document;
        try
        {
            document = new XmlDocument();
            document.LoadXml(xml);
        }
        catch (XmlException ex)
        {
            return FormatResult.Fail($"解析 clang-format 输出失败: {ex.Message}");
        }

        if (document.DocumentElement is not { } root)
            return FormatResult.Fail("clang-format 输出为空。");

        var replacements = new List<(int Offset, int Length, byte[] NewText)>();
        var cursorByte = 0;
        var hasCursor = false;

        foreach (XmlNode node in root.ChildNodes)
        {
            switch (node.Name)
            {
                case "replacement":
                    if (!TryReadByteRange(node, out var offset, out var length))
                        return FormatResult.Fail("clang-format 输出的替换区间无效。");

                    replacements.Add((offset, length, Utf8NoBom.GetBytes(node.InnerText)));
                    break;

                case "cursor":
                    hasCursor = int.TryParse(node.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out cursorByte);
                    break;
            }
        }

        var buffer = new List<byte>(Utf8NoBom.GetBytes(source));
        foreach (var (offset, length, newText) in replacements.OrderByDescending(r => r.Offset))
        {
            if (offset < 0 || offset + length > buffer.Count)
                return FormatResult.Fail("clang-format 输出的替换区间超出源文本范围。");

            buffer.RemoveRange(offset, length);
            buffer.InsertRange(offset, newText);
        }

        var resultBytes = buffer.ToArray();
        var result = Utf8NoBom.GetString(resultBytes);
        var cursor = hasCursor
            ? ByteOffsetToCharIndex(resultBytes, cursorByte)
            : fallbackCursor;

        return FormatResult.Ok(result, cursor);
    }

    private static bool TryReadByteRange(XmlNode node, out int offset, out int length)
    {
        offset = 0;
        length = 0;

        var offsetText = node.Attributes?["offset"]?.Value;
        var lengthText = node.Attributes?["length"]?.Value;

        return int.TryParse(offsetText, NumberStyles.Integer, CultureInfo.InvariantCulture, out offset)
               && int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out length)
               && offset >= 0
               && length >= 0;
    }

    /// <summary>把字节位置换算为字符位置，并避开 UTF-8 多字节序列的中间位置。</summary>
    private static int ByteOffsetToCharIndex(byte[] bytes, int byteOffset)
    {
        var limit = Math.Clamp(byteOffset, 0, bytes.Length);

        while (limit > 0 && limit < bytes.Length && (bytes[limit] & 0xC0) == 0x80)
        {
            limit--;
        }

        return Utf8NoBom.GetCharCount(bytes, 0, limit);
    }

    private static void EnsureExecutable(string path)
    {
        if (!OperatingSystem.IsLinux()) return;

        try
        {
            var mode = File.GetUnixFileMode(path);
            File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[FormatService] 设置可执行权限失败，改用 chmod: {ex.Message}");

            try
            {
                using var chmod = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    ArgumentList = { "+x", path },
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                chmod?.WaitForExit();
            }
            catch (Exception chmodEx)
            {
                Trace.TraceError($"[FormatService] chmod 执行失败: {chmodEx.Message}");
            }
        }
    }
}
