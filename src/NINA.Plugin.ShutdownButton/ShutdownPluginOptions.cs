using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Input;
using NINA.Core.Utility;

namespace NINA.Plugin.ShutdownButton;

/// <summary>Options page editor. Explicit save avoids accidental power-controller edits.</summary>
[Export(typeof(ShutdownPluginOptions))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class ShutdownPluginOptions : BaseINPC {
    private ShutdownSettings stored = ShutdownSettings.Load();
    private PowerOutputEditor? selectedOutput;
    private string status = "Power-off is disabled by default.";

    public ShutdownPluginOptions() {
        SaveCommand = new RelayCommand(_ => Save());
        AddOutputCommand = new RelayCommand(_ => {
            var row = new PowerOutputEditor { Name = "New outlet", Method = "POST" };
            PowerOutputs.Add(row);
            SelectedOutput = row;
        });
        RemoveOutputCommand = new RelayCommand(_ => {
            if (SelectedOutput is not null) PowerOutputs.Remove(SelectedOutput);
            SelectedOutput = null;
        });
        Reload();
    }

    public ICommand SaveCommand { get; }
    public ICommand AddOutputCommand { get; }
    public ICommand RemoveOutputCommand { get; }
    public ObservableCollection<PowerOutputEditor> PowerOutputs { get; } = new();

    public PowerOutputEditor? SelectedOutput {
        get => selectedOutput;
        set { selectedOutput = value; RaisePropertyChanged(); }
    }
    public string Status {
        get => status;
        private set { status = value; RaisePropertyChanged(); }
    }

    public bool Camera { get => stored.Disconnect.Camera; set { stored.Disconnect.Camera = value; RaisePropertyChanged(); } }
    public bool FilterWheel { get => stored.Disconnect.FilterWheel; set { stored.Disconnect.FilterWheel = value; RaisePropertyChanged(); } }
    public bool Focuser { get => stored.Disconnect.Focuser; set { stored.Disconnect.Focuser = value; RaisePropertyChanged(); } }
    public bool Rotator { get => stored.Disconnect.Rotator; set { stored.Disconnect.Rotator = value; RaisePropertyChanged(); } }
    public bool Guider { get => stored.Disconnect.Guider; set { stored.Disconnect.Guider = value; RaisePropertyChanged(); } }
    public bool Telescope { get => stored.Disconnect.Telescope; set { stored.Disconnect.Telescope = value; RaisePropertyChanged(); } }
    public bool DryRun { get => stored.DryRun; set { stored.DryRun = value; RaisePropertyChanged(); } }
    public bool EnableRestPowerOff { get => stored.EnableRestPowerOff; set { stored.EnableRestPowerOff = value; RaisePropertyChanged(); } }
    public int WarmCameraMinutes { get => stored.WarmCameraMinutes; set { stored.WarmCameraMinutes = value; RaisePropertyChanged(); } }
    public int StopTimeoutSeconds { get => stored.StopTimeoutSeconds; set { stored.StopTimeoutSeconds = value; RaisePropertyChanged(); } }
    public int ParkTimeoutSeconds { get => stored.ParkTimeoutSeconds; set { stored.ParkTimeoutSeconds = value; RaisePropertyChanged(); } }

    public void Reload() {
        stored = ShutdownSettings.Load();
        PowerOutputs.Clear();
        foreach (var item in stored.PowerOutputs) PowerOutputs.Add(PowerOutputEditor.From(item));
        SelectedOutput = null;
        foreach (var property in new[] { nameof(Camera), nameof(FilterWheel), nameof(Focuser),
            nameof(Rotator), nameof(Guider), nameof(Telescope), nameof(DryRun),
            nameof(EnableRestPowerOff), nameof(WarmCameraMinutes), nameof(StopTimeoutSeconds),
            nameof(ParkTimeoutSeconds) }) RaisePropertyChanged(property);
    }

    private void Save() {
        try {
            if (WarmCameraMinutes < 0 || WarmCameraMinutes > 120 || StopTimeoutSeconds < 10 ||
                ParkTimeoutSeconds < 10) throw new InvalidOperationException("Invalid duration or timeout.");
            var rows = PowerOutputs.Select(x => x.ToModel()).ToList();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(x => x.Enabled)) {
                if (string.IsNullOrWhiteSpace(row.Name) || !names.Add(row.Name.Trim()))
                    throw new InvalidOperationException("Enabled outputs need unique names.");
                if (!Uri.TryCreate(row.Url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                    throw new InvalidOperationException("Enabled outputs require a valid HTTP URL.");
                if (!new[] { "GET", "POST", "PUT", "PATCH" }.Contains(row.Method.ToUpperInvariant()))
                    throw new InvalidOperationException("Use GET, POST, PUT or PATCH for power-off.");
            }
            if (rows.Any(x => x.Enabled && string.Equals(x.Method, "GET", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Body)))
                throw new InvalidOperationException("GET actions cannot have a JSON body; put parameters in the URL.");
            stored.PowerOutputs = rows;
            stored.Save();
            Status = "Configuration saved. " + (stored.DryRun ? "REST DRY RUN" :
                stored.EnableRestPowerOff ? "REST POWER-OFF ENABLED" : "REST disabled");
        } catch (Exception ex) {
            Status = "Not saved: " + ex.Message;
        }
    }
}

public sealed class PowerOutputEditor : BaseINPC {
    private bool enabled;
    private string name = "";
    private string url = "";
    private string method = "POST";
    private string authorizationHeader = "";
    private string body = "";

    public bool Enabled { get => enabled; set { enabled = value; RaisePropertyChanged(); } }
    public string Name { get => name; set { name = value; RaisePropertyChanged(); } }
    public string Url { get => url; set { url = value; RaisePropertyChanged(); } }
    public string Method { get => method; set { method = value; RaisePropertyChanged(); } }
    public string AuthorizationHeader { get => authorizationHeader; set { authorizationHeader = value; RaisePropertyChanged(); } }
    public string Body { get => body; set { body = value; RaisePropertyChanged(); } }
    public PowerOffAction ToModel() => new() {
        Enabled = Enabled, Name = Name, Url = Url, Method = Method,
        AuthorizationHeader = AuthorizationHeader, Body = Body
    };
    public static PowerOutputEditor From(PowerOffAction a) => new() {
        Enabled = a.Enabled, Name = a.Name, Url = a.Url, Method = a.Method,
        AuthorizationHeader = a.AuthorizationHeader ?? "", Body = a.Body ?? ""
    };
}
