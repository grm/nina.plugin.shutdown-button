using NINA.Plugin.ShutdownButton;
using Xunit;

namespace NINA.Plugin.ShutdownButton.Tests;

public sealed class ShutdownCoordinatorTests {
    private sealed class Fake : IShutdownHardware {
        public readonly List<string> Calls = new();
        public string? FailOn;
        private Task Call(string name) {
            Calls.Add(name);
            if (FailOn == name) throw new InvalidOperationException(name + " failed");
            return Task.CompletedTask;
        }
        public Task StopSequenceAsync(CancellationToken ct) => Call("stop");
        public Task StopGuidingAsync(CancellationToken ct) => Call("guide");
        public Task ParkAndVerifyAsync(CancellationToken ct) => Call("park");
        public Task WarmCameraAsync(CancellationToken ct) => Call("warm");
        public Task DisconnectAsync(CancellationToken ct) => Call("disconnect");
        public Task PowerOffAsync(CancellationToken ct) => Call("power");
    }

    [Fact]
    public async Task SuccessfulShutdownHasStrictOrder() {
        var fake = new Fake();
        await new ShutdownCoordinator(fake).RunAsync(_ => {});
        Assert.Equal(new[] {"stop","guide","park","warm","disconnect","power"}, fake.Calls);
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("guide")]
    [InlineData("park")]
    [InlineData("warm")]
    [InlineData("disconnect")]
    public async Task AnyFailedPrerequisiteBlocksPowerOff(string failed) {
        var fake = new Fake { FailOn = failed };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ShutdownCoordinator(fake).RunAsync(_ => {}));
        Assert.DoesNotContain("power", fake.Calls);
    }

    [Fact]
    public async Task PowerOffIsNotCalledTwiceDuringConcurrentRun() {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fake = new BlockingHardware(entered, resume);
        var coordinator = new ShutdownCoordinator(fake);
        var first = coordinator.RunAsync(_ => {});
        await entered.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RunAsync(_ => {}));
        resume.SetResult();
        await first;
        Assert.Equal(1, fake.PowerCalls);
    }

    private sealed class BlockingHardware(TaskCompletionSource entered, TaskCompletionSource resume) : IShutdownHardware {
        public int PowerCalls;
        public async Task StopSequenceAsync(CancellationToken ct) { entered.SetResult(); await resume.Task; }
        public Task StopGuidingAsync(CancellationToken ct) => Task.CompletedTask;
        public Task ParkAndVerifyAsync(CancellationToken ct) => Task.CompletedTask;
        public Task WarmCameraAsync(CancellationToken ct) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken ct) => Task.CompletedTask;
        public Task PowerOffAsync(CancellationToken ct) { PowerCalls++; return Task.CompletedTask; }
    }
}
