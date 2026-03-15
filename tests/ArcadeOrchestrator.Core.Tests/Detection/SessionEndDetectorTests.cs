using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Diagnostics;
using Xunit;

namespace ArcadeOrchestrator.Core.Tests.Detection;

public sealed class SessionEndDetectorTests
{
    private static EmulatorProcess FakeProcess()
    {
        var proc = Substitute.For<Process>();
        return new EmulatorProcess(9999, proc);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNoStrategiesProvided()
    {
        var act = () => new SessionEndDetector(
            Array.Empty<IDetectionStrategy>(),
            NullLogger<SessionEndDetector>.Instance);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task WaitForSessionEndAsync_ShouldComplete_WhenFirstStrategyFires()
    {
        var fastStrategy = Substitute.For<IDetectionStrategy>();
        fastStrategy.StrategyName.Returns("fast");
        fastStrategy
            .WatchAsync(Arg.Any<EmulatorProcess>(), Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                ci.ArgAt<Action>(1).Invoke(); // dispara imediatamente
                return Task.CompletedTask;
            });

        var slowStrategy = Substitute.For<IDetectionStrategy>();
        slowStrategy.StrategyName.Returns("slow");
        slowStrategy
            .WatchAsync(Arg.Any<EmulatorProcess>(), Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(async (ci) => await Task.Delay(Timeout.Infinite, ci.ArgAt<CancellationToken>(2)));

        var detector = new SessionEndDetector(
            new[] { fastStrategy, slowStrategy },
            NullLogger<SessionEndDetector>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await detector.WaitForSessionEndAsync(FakeProcess(), cts.Token);

        detector.LastTriggeredStrategy.Should().Be("fast");
    }

    [Fact]
    public async Task WaitForSessionEndAsync_LastTriggeredStrategy_ShouldBeFirstWinner()
    {
        var strategy = Substitute.For<IDetectionStrategy>();
        strategy.StrategyName.Returns("timeout");
        strategy
            .WatchAsync(Arg.Any<EmulatorProcess>(), Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                ci.ArgAt<Action>(1).Invoke();
                return Task.CompletedTask;
            });

        var detector = new SessionEndDetector(
            new[] { strategy },
            NullLogger<SessionEndDetector>.Instance);

        await detector.WaitForSessionEndAsync(FakeProcess(), CancellationToken.None);

        detector.LastTriggeredStrategy.Should().Be("timeout");
    }

    [Fact]
    public async Task TriggerManualEnd_ShouldCompleteDetection_WithManualSkipStrategy()
    {
        var strategy = Substitute.For<IDetectionStrategy>();
        strategy.StrategyName.Returns("process_watch");
        strategy
            .WatchAsync(Arg.Any<EmulatorProcess>(), Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(async (ci) => await Task.Delay(Timeout.Infinite, ci.ArgAt<CancellationToken>(2)));

        var detector = new SessionEndDetector(
            new[] { strategy },
            NullLogger<SessionEndDetector>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var watchTask = detector.WaitForSessionEndAsync(FakeProcess(), cts.Token);

        await Task.Delay(100);
        detector.TriggerManualEnd();

        await watchTask;

        detector.LastTriggeredStrategy.Should().Be("manual_skip");
    }
}
