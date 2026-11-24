using System.IO;
using System.Text.Json;

namespace NINA.Plugin.ShutdownButton;

public sealed class EquipmentSelection {
    public bool Camera { get; set; } = true;
    public bool FilterWheel { get; set; } = true;
    public bool Focuser { get; set; } = true;
    public bool Rotator { get; set; } = true;
    public bool Guider { get; set; } = true;
    public bool Telescope { get; set; } = true;
}

public sealed class PowerOffAction {
    // A distinct power output may require its own endpoint and payload.
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string Url { get; set; } = "";
    public string Method { get; set; } = "POST";
    public string? AuthorizationHeader { get; set; }
    public string? Body { get; set; }
}

public sealed class ShutdownSettings {
    // REST power switching is disabled by default.
    public bool DryRun { get; set; } = true;
    public bool EnableRestPowerOff { get; set; }
    public EquipmentSelection Disconnect { get; set; } = new();
    public List<PowerOffAction> PowerOutputs { get; set; } = new();

    // Legacy single-endpoint configuration supported for migration only.
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

    public void Save() {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var tmp = SettingsPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, SettingsPath, overwrite: true);
    }
}
