using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Tests;

public sealed class SimulationWorldTests
{
    [Fact]
    public void Advance_AccumulatesPartialDeltaUntilFixedStepBoundary()
    {
        var world = CreateWorld(new Vector2(1.0f, 0.0f));

        var first = world.Advance(0.05f);
        var second = world.Advance(0.05f);

        Assert.Equal(0, first.FixedStepsExecuted);
        Assert.Equal(0.0f, first.SimulationTimeSeconds, precision: 5);
        Assert.Equal(0.05f, first.AccumulatorSeconds, precision: 5);
        Assert.Equal(Vector2.Zero, first.Balls[0].Position);
        Assert.Equal(SimulationPhase.Running, first.Phase);

        Assert.Equal(1, second.FixedStepsExecuted);
        Assert.Equal(0.1f, second.SimulationTimeSeconds, precision: 5);
        Assert.Equal(0.0f, second.AccumulatorSeconds, precision: 5);
        Assert.Equal(new Vector2(0.1f, 0.0f), second.Balls[0].Position);
    }

    [Fact]
    public void Advance_UsesOnlyWholeFixedSteps()
    {
        var world = CreateWorld(new Vector2(2.0f, 0.0f));

        var result = world.Advance(0.35f);

        Assert.Equal(3, result.FixedStepsExecuted);
        Assert.Equal(0.3f, result.SimulationTimeSeconds, precision: 5);
        Assert.Equal(0.05f, result.AccumulatorSeconds, precision: 5);
        Assert.Equal(new Vector2(0.6f, 0.0f), result.Balls[0].Position);
        Assert.Equal(3, world.TotalFixedStepsExecuted);
    }

    [Fact]
    public void Advance_DoesNotConsumeTimeWhileIdle()
    {
        var world = CreateWorld(Vector2.Zero);

        var result = world.Advance(0.5f);

        Assert.Equal(SimulationPhase.Idle, result.Phase);
        Assert.Equal(0, result.FixedStepsExecuted);
        Assert.Equal(0.0f, result.SimulationTimeSeconds, precision: 5);
        Assert.Equal(0.0f, result.AccumulatorSeconds, precision: 5);
    }

    [Fact]
    public void Advance_EmitsSettledEventOnlyOnceAfterShotStops()
    {
        var world = CreateWorld(Vector2.Zero);
        world.RecordCueStrike(new ShotInput(Vector2.UnitX, 2.5f, Vector2.Zero));

        var first = world.Advance(0.1f);
        var second = world.Advance(0.1f);

        Assert.Equal(SimulationPhase.Settled, first.Phase);
        Assert.Equal(2, first.Events.Count);
        Assert.Equal(ShotEventType.CueStrike, first.Events[0].EventType);
        Assert.Equal(ShotEventType.Settled, first.Events[1].EventType);

        Assert.Equal(SimulationPhase.Settled, second.Phase);
        Assert.Empty(second.Events);
        Assert.Equal(0.1f, second.SimulationTimeSeconds, precision: 5);
    }

    private static SimulationWorld CreateWorld(Vector2 cueBallVelocity)
    {
        var table = CustomTable9FtSpec.Create();
        var config = new SimulationConfig(
            fixedStepSeconds: 0.1f,
            settleSpeedThresholdMetersPerSecond: 0.01f,
            maxFixedStepsPerAdvance: 64);

        var cueBall = new BallState(
            BallNumber: 0,
            Kind: BallKind.Cue,
            Position: Vector2.Zero,
            Velocity: cueBallVelocity,
            Spin: new SpinState(0.0f, 0.0f, 0.0f),
            IsPocketed: false);

        return new SimulationWorld(table, config, new[] { cueBall });
    }
}
