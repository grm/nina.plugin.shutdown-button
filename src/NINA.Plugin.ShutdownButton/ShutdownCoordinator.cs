namespace NINA.Plugin.ShutdownButton;

/// <summary>
/// Runs outside N.I.N.A.'s cancelled sequence. Hardware side-effects must be implemented
/// behind an adapter, to make stop/park failure paths independently testable.
/// </summary>
public interface IShutdownHardware {
    Task StopSequenceAsync(CancellationToken ct);
    Task StopGuidingAsync(CancellationToken ct);
    Task ParkAndVerifyAsync(CancellationToken ct);
    Task WarmCameraAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task PowerOffAsync(CancellationToken ct);
}

public sealed class ShutdownCoordinator {
    private readonly IShutdownHardware hardware;
    private int running;
    public ShutdownCoordinator(IShutdownHardware hardware) => this.hardware = hardware;

    public async Task RunAsync(Action<string> status, CancellationToken ct = default) {
        if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            throw new InvalidOperationException("Shutdown already in progress.");
        try {
            status("Stopping sequencer...");
            await hardware.StopSequenceAsync(ct);
            status("Stopping guiding...");
            await hardware.StopGuidingAsync(ct);
            status("Parking telescope...");
            await hardware.ParkAndVerifyAsync(ct);
            status("Warming camera...");
            await hardware.WarmCameraAsync(ct);
            status("Disconnecting equipment...");
            await hardware.DisconnectAsync(ct);
            status("Power-off / dry run...");
            await hardware.PowerOffAsync(ct);
            status("Shutdown completed.");
        } catch (Exception ex) {
            status("Shutdown blocked: " + ex.Message);
            throw;
        } finally {
            Interlocked.Exchange(ref running, 0);
        }
    }
}
