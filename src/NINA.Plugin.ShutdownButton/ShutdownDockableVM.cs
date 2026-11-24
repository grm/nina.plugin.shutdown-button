using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel;

namespace NINA.Plugin.ShutdownButton;

[Export(typeof(NINA.Equipment.Interfaces.ViewModel.IDockableVM))]
public sealed class ShutdownDockableVM : DockableVM {
    private readonly ShutdownCoordinator coordinator;
    private readonly ShutdownSettings settings;
    private bool busy;
    private string status = "Ready (REST power-off disabled by default)";

    [ImportingConstructor]
    public ShutdownDockableVM(IProfileService profile,
        ISequenceMediator sequence, ITelescopeMediator telescope, IGuiderMediator guider,
        ICameraMediator camera, IFilterWheelMediator filterWheel,
        IFocuserMediator focuser, IRotatorMediator rotator) : base(profile) {
        Title = "Shutdown Telescope";
        settings = ShutdownSettings.Load();
        coordinator = new ShutdownCoordinator(new NinaShutdownHardware(
            sequence, telescope, guider, camera, filterWheel, focuser, rotator, settings));
        SaveSettingsCommand = new NINA.Core.Utility.RelayCommand(_ => SaveSettings(), _ => !Busy);
        ShutdownCommand = new NINA.Core.Utility.RelayCommand(
            _ => _ = ExecuteShutdownAsync(), _ => !Busy);
    }

    public ICommand ShutdownCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public EquipmentSelection Selection => settings.Disconnect;
    public bool RestPowerOffEnabled => settings.EnableRestPowerOff && !settings.DryRun;

    private void SaveSettings() {
        try {
            settings.Save();
            Status = "Equipment selection saved. Power control remains " + (RestPowerOffEnabled ? "ENABLED" : "disabled / dry-run") + ".";
        } catch {
            Status = "Failed to save shutdown settings.";
        }
    }

    public string Status {
        get => status;
        private set { status = value; RaisePropertyChanged(nameof(Status)); }
    }

    public bool Busy {
        get => busy;
        private set {
            busy = value;
            RaisePropertyChanged(nameof(Busy));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task ExecuteShutdownAsync() {
        if (Busy) return;
        var confirmation = MessageBox.Show(
            "Stop the N.I.N.A. sequence, park the mount, warm and disconnect equipment, then run configured power-off?\n\nThis cannot be undone automatically.",
            "Confirm telescope shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes) return;
        Busy = true;
        try {
            await coordinator.RunAsync(UpdateStatus);
        } catch (Exception) {
            // Coordinator already emits a sanitized step failure; leave the panel visible.
        } finally {
            Busy = false;
        }
    }

    private void UpdateStatus(string step) {
        if (Application.Current?.Dispatcher.CheckAccess() == false)
            Application.Current.Dispatcher.Invoke(() => Status = step);
        else Status = step;
    }
}
