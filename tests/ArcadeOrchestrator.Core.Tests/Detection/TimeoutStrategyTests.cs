using ArcadeOrchestrator.Core.Detection;
using FluentAssertions;
using NSubstitute;
using System.Diagnostics;
using Xunit;

namespace ArcadeOrchestrator.Core.Tests.Detection;

public sealed class TimeoutStrategyTests
{
    private static Core.Application.Interfaces.EmulatorProcess FakeProcess() =>
        new(1, Substitute.For<Process>());

    [Fact]
    public async Task WatchAsync_ShouldFireCallback_AfterTimeoutWithNoActivity()
    {
        var strategy = new TimeoutStrategy(TimeSpan.FromMilliseconds(200));
        var fired = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await strategy.WatchAsync(FakeProcess(), () => fired = true, cts.Token);

        fired.Should().BeTrue();
    }

    [Fact]
    public async Task ReportActivity_ShouldResetTimer()
    {
        var strategy = new TimeoutStrategy(TimeSpan.FromMilliseconds(300));
        var fired = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var watchTask = strategy.WatchAsync(FakeProcess(), () => fired = true, cts.Token);

        // Simula atividade antes do timeout
        await Task.Delay(100);
        strategy.ReportActivity();
        await Task.Delay(100);
        strategy.ReportActivity();

        // Aguarda o timeout real disparar
        await watchTask;

        fired.Should().BeTrue();
    }

    [Fact]
    public async Task WatchAsync_ShouldNotFire_WhenCancelledBeforeTimeout()
    {
        var strategy = new TimeoutStrategy(TimeSpan.FromSeconds(60));
        var fired = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        try
        {
            await strategy.WatchAsync(FakeProcess(), () => fired = true, cts.Token);
        }
        catch (OperationCanceledException) { }

        fired.Should().BeFalse();
    }
}
