using System;
using System.IO;
using System.Text.Json;
using AoELauncher.Models;

namespace AoELauncher.Core;

/// <summary>
/// Reads/writes the single JSON config+save file that always lives right
/// next to the exe. There is intentionally no fallback location (such as
/// %LocalAppData%): the launcher ships as part of the Ashes of Erebus
/// folder, so its own directory is always where the config belongs. If that
/// location isn't writable, Save() throws and the caller is expected to
/// show the user a clear error rather than silently writing somewhere else.
/// </summary>
public static class ConfigManager
{
    private const string FileName = "aoe_launcher_config.json";

    public static string ConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, FileName);

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            // Corrupt or unreadable config: don't crash the app, just start fresh.
            return new AppConfig();
        }
    }

    /// <summary>Throws if the file can't be written (e.g. read-only location); callers should catch and inform the user.</summary>
    public static void Save(AppConfig config)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(ConfigPath, json);
    }
}
