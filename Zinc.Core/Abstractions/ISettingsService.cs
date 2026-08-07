using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;
public interface ISettingsService
{
	AppSettings appSettings { get; }
	void Preload();
	void Save();
}
