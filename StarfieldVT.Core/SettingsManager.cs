using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

using Serilog;

using StarfieldVT.Core.Filesystem;

namespace StarfieldVT.Core;

public class AppSettings
{
    public string? StarfieldDataPath { get; set; }
}

public class SettingsManager
{
    private static readonly string SettingsFilePath =
        Path.Combine(AppDataFolder.GetAppDataFolder(), "settings.json");

    private static SettingsManager? _instance;
    public static SettingsManager Instance => _instance ??= new SettingsManager();

    private AppSettings _settings;

    private SettingsManager()
    {
        _settings = Load();
    }

    public string? StarfieldDataPath
    {
        get => _settings.StarfieldDataPath;
        set
        {
            _settings.StarfieldDataPath = value;
            Save();
        }
    }

    public bool HasValidGamePath()
    {
        return !string.IsNullOrEmpty(StarfieldDataPath) && Directory.Exists(StarfieldDataPath);
    }

    public string? TryAutoDetectGamePath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        try
        {
            return StarfieldManager.GetGamePathFromRegistry();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Auto-detection of Starfield data path failed");
            return null;
        }
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load settings from {Path}", SettingsFilePath);
        }

        return new AppSettings();
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save settings to {Path}", SettingsFilePath);
        }
    }
}
