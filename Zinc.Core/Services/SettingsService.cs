using System.Text.Json;
using System.Xml.Linq;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;
public class SettingsService : ISettingsService
{
	private readonly string _filePath;
	private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

	public AppSettings appSettings { get; private set; }

	public SettingsService()
	{
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var dir = Path.Combine(appData, "Zinc");
		Directory.CreateDirectory(dir);
		_filePath = Path.Combine(dir, "settings.json");
		appSettings = new AppSettings();
	}

	public void Preload()
	{
		if (File.Exists(_filePath))
		{
			try
			{
				var js = File.ReadAllText(_filePath);
				appSettings = JsonSerializer.Deserialize<AppSettings>(js) ?? new AppSettings();
			}
			catch
			{
				appSettings = new AppSettings();
			}
		}
		else
		{
			appSettings = new AppSettings();
			Save();
		}
	}

	public void Save()
	{
		var js = JsonSerializer.Serialize(appSettings, _options);
		File.WriteAllText(_filePath, js);
	}
}
