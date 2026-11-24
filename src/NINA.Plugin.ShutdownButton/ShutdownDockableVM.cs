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
    private readonly ISequenceMediator sequence;
    private readonly ITelescopeMediator telescope;
    private readonly IGuiderMediator guider;
    private readonly ICameraMediator camera;
    private readonly IFilterWheelMediator filterWheel;
    private readonly IFocuserMediator focuser;
    private readonly IRotatorMediator rotator;
    private bool busy;
    private string status = "Ready (configure shutdown in N.I.N.A. Options)";

    [ImportingConstructor]
    public ShutdownDockableVM(IProfileService profile,
        ISequenceMediator sequence, ITelescopeMediator telescope, IGuiderMediator guider,
        ICameraMediator camera, IFilterWheelMediator filterWheel,
        IFocuserMediator focuser, IRotatorMediator rotator) : base(profile) {
        Title = "Shutdown Telescope";
        this.sequence = sequence;
        this.telescope = telescope;
        this.guider = guider;
        this.camera = camera;
        this.filterWheel = filterWheel;
        this.focuser = focuser;
        this.rotator = rotator;
        ShutdownCommand = new NINA.Core.Utility.RelayCommand(
            _ => _ = ExecuteShutdownAsync(), _ => !Busy);
    }

    public ICommand ShutdownCommand { get; }
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
        if (MessageBox.Show(
            "Stop the N.I.N.A. sequence, park the mount, warm and disconnect selected equipment, then run enabled REST power-off actions?\n\nThis cannot be undone automatically.",
            "Confirm telescope shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Busy = true;
        try {
            // Options live in a distinct N.I.N.A. MEF composition container.
            // Always reload the saved snapshot before each shutdown.
            var settings = ShutdownSettings.Load();
            var coordinator = new ShutdownCoordinator(new NinaShutdownHardware(
                sequence, telescope, guider, camera, filterWheel, focuser, rotator, settings));
            await coordinator.RunAsync(UpdateStatus);
        } catch (Exception) {
            // Coordinator already emits a safe failure step; never continue on failure.
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
