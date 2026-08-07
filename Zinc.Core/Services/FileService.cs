using System.Text.Json;
using System.Xml.Linq;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;

public class FileService : IFileService
{
	public string LoadFile(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			throw new ArgumentException("文件路径不能为空", nameof(filePath));
		}

		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"文件不存在: {filePath}");
		}

		try
		{
			return File.ReadAllText(filePath);
		}
		catch (Exception ex)
		{
			throw new IOException($"读取文件失败: {ex.Message}", ex);
		}
	}

	public void SaveFile(string filePath, string content)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			throw new ArgumentException("文件路径不能为空", nameof(filePath));
		}

		try
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			File.WriteAllText(filePath, content);
		}
		catch (Exception ex)
		{
			throw new IOException($"保存文件失败: {ex.Message}", ex);
		}
	}
}