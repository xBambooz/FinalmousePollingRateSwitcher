using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finalmouse.Shared;

/// <summary>
/// Written by the service to communicate current state to the tray icon.
/// Lives alongside config.json in the shared ProgramData folder.
/// </summary>
public class ServiceStatus
{
    [JsonPropertyName("is_gaming")]
    public bool IsGaming { get; set; }

    [JsonPropertyName("current_rate_hz")]
    public int CurrentRateHz { get; set; }

    [JsonPropertyName("game_name")]
    public string GameName { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static string GetStatusPath() => Path.Combine(AppConfig.GetConfigDir(), "status.json");

    public void Write()
    {
        try
        {
            UpdatedAt = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(this, _jsonOptions);
            var path = GetStatusPath();
            // Write to temp file then move for atomicity
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
        catch { /* Don't crash the service over status writing */ }
    }

    public static ServiceStatus? Read()
    {
        try
        {
            var path = GetStatusPath();
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ServiceStatus>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
