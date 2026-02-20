using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finalmouse.Shared;

public class AppConfig
{
    [JsonPropertyName("idle_rate_hz")]
    public int IdleRateHz { get; set; } = 1000;

    [JsonPropertyName("gaming_rate_hz")]
    public int GamingRateHz { get; set; } = 4000;

    [JsonPropertyName("scan_interval_seconds")]
    public int ScanIntervalSeconds { get; set; } = 5;

    [JsonPropertyName("show_tray_icon")]
    public bool ShowTrayIcon { get; set; } = true;

    [JsonPropertyName("start_on_startup")]
    public bool StartOnStartup { get; set; } = false;

    [JsonPropertyName("game_processes")]
    public Dictionary<string, string> GameProcesses { get; set; } = new()
    {
        ["VALORANT-Win64-Shipping.exe"] = "Valorant",
        ["cs2.exe"] = "Counter-Strike 2",
        ["FortniteClient-Win64-Shipping.exe"] = "Fortnite",
        ["r5apex.exe"] = "Apex Legends",
        ["overwatch.exe"] = "Overwatch 2",
        ["cod.exe"] = "Call of Duty",
        ["PioneerGame.exe"] = "Arc Raiders",
        ["FLClient-Win64-Shipping.exe"] = "The Finals",
        ["FPSAimTrainer-Win64-Shipping.exe"] = "KovaaK's",
        ["AimLab_tb.exe"] = "Aim Lab",
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string GetConfigDir()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FinalmousePollingRateSwitcher"
        );
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetConfigPath() => Path.Combine(GetConfigDir(), "config.json");
    public static string GetLogPath() => Path.Combine(GetConfigDir(), "polling_switcher.log");

    public static AppConfig Load()
    {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        var config = new AppConfig();
        config.Save();
        return config;
    }

    public void Save()
    {
        var path = GetConfigPath();
        var json = JsonSerializer.Serialize(this, _jsonOptions);
        File.WriteAllText(path, json);
    }
}
