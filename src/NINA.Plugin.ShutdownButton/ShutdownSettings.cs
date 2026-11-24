using System.Text.Json;

namespace NINA.Plugin.ShutdownButton;

public sealed class ShutdownSettings {
    // Power switching is intentionally disabled by default.
    public bool DryRun { get; set; } = true;
    public bool EnableRestPowerOff { get; set; }
    public string RestUrl { get; set; } = "";
    public string RestMethod { get; set; } = "POST";
    public string? AuthorizationHeader { get; set; }
    public int StopTimeoutSeconds { get; set; } = 90;
    public int ParkTimeoutSeconds { get; set; } = 180;
    public int WarmCameraMinutes { get; set; } = 5;

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NINA", "ShutdownButton", "settings.json");

    public static ShutdownSettings Load() {
        try {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<ShutdownSettings>(File.ReadAllText(SettingsPath)) ?? new();
        } catch { /* Fail-safe defaults: no REST action */ }
        return new();
    }
}
