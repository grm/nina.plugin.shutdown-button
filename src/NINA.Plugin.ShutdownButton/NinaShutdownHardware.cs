using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Interfaces.Mediator;

namespace NINA.Plugin.ShutdownButton;

public sealed class NinaShutdownHardware : IShutdownHardware {
    private readonly ISequenceMediator sequence;
    private readonly ITelescopeMediator telescope;
    private readonly IGuiderMediator guider;
    private readonly ICameraMediator camera;
    private readonly IFilterWheelMediator filterWheel;
    private readonly IFocuserMediator focuser;
    private readonly IRotatorMediator rotator;
    private readonly ShutdownSettings settings;

    public NinaShutdownHardware(ISequenceMediator sequence, ITelescopeMediator telescope,
        IGuiderMediator guider, ICameraMediator camera, IFilterWheelMediator filterWheel,
        IFocuserMediator focuser, IRotatorMediator rotator, ShutdownSettings settings) {
        this.sequence = sequence;
        this.telescope = telescope;
        this.guider = guider;
        this.camera = camera;
        this.filterWheel = filterWheel;
        this.focuser = focuser;
        this.rotator = rotator;
        this.settings = settings;
    }

    public async Task StopSequenceAsync(CancellationToken ct) {
        if (!sequence.IsAdvancedSequenceRunning()) return;
        sequence.CancelAdvancedSequence();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(Math.Max(10, settings.StopTimeoutSeconds)));
        try {
            while (sequence.IsAdvancedSequenceRunning()) {
                await Task.Delay(250, deadline.Token);
            }
            // Require the host to leave the running state before hardware operations.
            await Task.Delay(500, deadline.Token);
            if (sequence.IsAdvancedSequenceRunning())
                throw new InvalidOperationException("Sequencer resumed after stop.");
        } catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
            throw new TimeoutException("N.I.N.A. sequencer did not stop; power-off blocked.");
        }
    }

    public async Task StopGuidingAsync(CancellationToken ct) {
        if (guider.GetInfo().Connected && !await guider.StopGuiding(ct))
            throw new InvalidOperationException("Failed to stop guiding.");
    }

    public async Task ParkAndVerifyAsync(CancellationToken ct) {
        var state = telescope.GetInfo();
        if (!state.Connected)
            throw new InvalidOperationException("Mount disconnected: parked state cannot be verified.");
        if (!state.AtPark) {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(Math.Max(10, settings.ParkTimeoutSeconds)));
            try {
                if (!await telescope.ParkTelescope(null!, deadline.Token))
                    throw new InvalidOperationException("N.I.N.A. reported mount parking failure.");
            } catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
                throw new TimeoutException("Mount parking timed out; power-off blocked.");
            }
        }
        if (!telescope.GetInfo().Connected || !telescope.GetInfo().AtPark)
            throw new InvalidOperationException("Mount parking is not confirmed.");
    }

    public async Task WarmCameraAsync(CancellationToken ct) {
        if (settings.Disconnect.Camera && camera.GetInfo().Connected && settings.WarmCameraMinutes > 0) {
            if (!await camera.WarmCamera(TimeSpan.FromMinutes(settings.WarmCameraMinutes), null!, ct))
                throw new InvalidOperationException("Camera warm-up failed.");
        }
    }

    public async Task DisconnectAsync(CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        // Preserve mount connection until parking was positively verified above.
        if (!telescope.GetInfo().Connected || !telescope.GetInfo().AtPark)
            throw new InvalidOperationException("Mount state changed after parking.");
        var selection = settings.Disconnect;
        if (selection.Camera && camera.GetInfo().Connected) await camera.Disconnect();
        if (selection.FilterWheel && filterWheel.GetInfo().Connected) await filterWheel.Disconnect();
        if (selection.Focuser && focuser.GetInfo().Connected) await focuser.Disconnect();
        if (selection.Rotator && rotator.GetInfo().Connected) await rotator.Disconnect();
        if (selection.Guider && guider.GetInfo().Connected) await guider.Disconnect();
        if (selection.Telescope) await telescope.Disconnect();
    }

    public async Task PowerOffAsync(CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        // A new sequencing run must not be allowed to race against power removal.
        // We cannot prevent N.I.N.A.'s own Start button; fail closed if it was used.
        if (sequence.IsAdvancedSequenceRunning())
            throw new InvalidOperationException("Sequencer was restarted; REST power-off blocked.");
        // Parking was positively checked before disconnect; never infer parking
        // from a disconnected mount.
        await new RestPowerController(settings).ExecuteAsync(ct);
    }
}
