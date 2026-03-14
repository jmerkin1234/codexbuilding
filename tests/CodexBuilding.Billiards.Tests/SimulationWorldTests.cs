using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Tests;

public sealed class SimulationWorldTests
{
    [Fact]
    public void Advance_AccumulatesPartialDeltaUntilFixedStepBoundary()
    {
        var world = CreateShellWorld(new Vector2(1.0f, 0.0f));

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
        var world = CreateShellWorld(new Vector2(2.0f, 0.0f));

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
        var world = CreateShellWorld(Vector2.Zero);

        var result = world.Advance(0.5f);

        Assert.Equal(SimulationPhase.Idle, result.Phase);
        Assert.Equal(0, result.FixedStepsExecuted);
        Assert.Equal(0.0f, result.SimulationTimeSeconds, precision: 5);
        Assert.Equal(0.0f, result.AccumulatorSeconds, precision: 5);
    }

    [Fact]
    public void Advance_EmitsSettledEventOnlyOnceAfterShotStops()
    {
        var world = CreateShellWorld(Vector2.Zero);
        world.ApplyCueStrike(new ShotInput(Vector2.UnitX, 0.005f, Vector2.Zero));

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

    [Fact]
    public void ApplyCueStrike_NormalizesAimAndSetsCueBallVelocity()
    {
        var world = CreateShellWorld(Vector2.Zero);

        var resolved = world.ApplyCueStrike(new ShotInput(new Vector2(3.0f, 4.0f), 2.5f, Vector2.Zero));

        Assert.Equal(new Vector2(0.6f, 0.8f), resolved.AimDirection, new Vector2Comparer(0.0001f));
        Assert.Equal(new Vector2(1.5f, 2.0f), resolved.InitialVelocity, new Vector2Comparer(0.0001f));
        Assert.Equal(new Vector2(1.5f, 2.0f), world.Balls[0].Velocity, new Vector2Comparer(0.0001f));
        Assert.Equal(SimulationPhase.Running, world.Phase);
    }

    [Fact]
    public void ApplyCueStrike_ClampsTipOffsetAndMapsSpin()
    {
        var world = CreateShellWorld(Vector2.Zero);

        var resolved = world.ApplyCueStrike(new ShotInput(Vector2.UnitX, 3.0f, new Vector2(2.0f, 0.0f)));

        Assert.Equal(Vector2.UnitX, resolved.TipOffsetNormalized, new Vector2Comparer(0.0001f));
        Assert.Equal(12.0f, resolved.InitialSpin.SideSpinRps, precision: 5);
        Assert.Equal(0.0f, resolved.InitialSpin.ForwardSpinRps, precision: 5);
        Assert.Equal(resolved.InitialSpin, world.Balls[0].Spin);
    }

    [Fact]
    public void ApplyCueStrike_ThrowsForZeroAimDirection()
    {
        var world = CreateShellWorld(Vector2.Zero);

        Assert.Throws<ArgumentException>(() => world.ApplyCueStrike(new ShotInput(Vector2.Zero, 1.0f, Vector2.Zero)));
    }

    [Fact]
    public void ApplyCueStrike_ThrowsWithoutCueBall()
    {
        var table = CustomTable9FtSpec.Create();
        var config = new SimulationConfig(0.1f, 0.01f, 64, 12.0f, 10.0f, 11.0f);
        var world = new SimulationWorld(table, config, Array.Empty<BallState>());

        Assert.Throws<InvalidOperationException>(() => world.ApplyCueStrike(new ShotInput(Vector2.UnitX, 1.0f, Vector2.Zero)));
    }

    [Fact]
    public void ApplyCueStrike_ThrowsWhileBallsAreStillMoving()
    {
        var world = CreateShellWorld(new Vector2(1.0f, 0.0f));

        Assert.Throws<InvalidOperationException>(() => world.ApplyCueStrike(new ShotInput(Vector2.UnitX, 1.0f, Vector2.Zero)));
    }

    [Fact]
    public void Advance_SlidingBallDeceleratesAndBuildsForwardSpin()
    {
        var world = CreateClothWorld(
            velocity: new Vector2(1.0f, 0.0f),
            spin: new SpinState(0.0f, 0.0f, 0.0f));

        var result = world.Advance(0.1f);

        Assert.True(result.Balls[0].Velocity.Length() < 1.0f);
        Assert.True(result.Balls[0].Spin.ForwardSpinRps > 0.0f);
        Assert.True(result.Balls[0].Position.X > 0.0f);
    }

    [Fact]
    public void Advance_RollingBallStaysMatchedWhileSlowing()
    {
        const float speed = 1.0f;
        var rollingSpin = ToForwardSpinRps(speed);
        var world = CreateClothWorld(
            velocity: new Vector2(speed, 0.0f),
            spin: new SpinState(0.0f, rollingSpin, 0.0f));

        var result = world.Advance(0.1f);
        var newSpeed = result.Balls[0].Velocity.Length();
        var newSurfaceSpeed = ToSurfaceSpeed(result.Balls[0].Spin.ForwardSpinRps);

        Assert.True(newSpeed < speed);
        Assert.InRange(MathF.Abs(newSpeed - newSurfaceSpeed), 0.0f, 0.0001f);
    }

    [Fact]
    public void Advance_OverspinBallAcceleratesForwardBeforeItMatchesRoll()
    {
        const float initialSpeed = 0.4f;
        var overspin = ToForwardSpinRps(0.9f);
        var world = CreateClothWorld(
            velocity: new Vector2(initialSpeed, 0.0f),
            spin: new SpinState(0.0f, overspin, 0.0f));

        var result = world.Advance(0.1f);

        Assert.True(result.Balls[0].Velocity.Length() > initialSpeed);
        Assert.True(result.Balls[0].Spin.ForwardSpinRps < overspin);
    }

    [Fact]
    public void Advance_SideSpinDecaysWhileBallTravels()
    {
        var world = CreateClothWorld(
            velocity: new Vector2(1.0f, 0.0f),
            spin: new SpinState(4.0f, 0.0f, 0.0f));

        var result = world.Advance(0.2f);

        Assert.True(result.Balls[0].Spin.SideSpinRps < 4.0f);
    }

    [Fact]
    public void Advance_SideSpinAddsLateralDriftWhileBallTravels()
    {
        var world = CreateClothWorld(
            velocity: new Vector2(1.0f, 0.0f),
            spin: new SpinState(6.0f, 0.0f, 0.0f));

        var result = world.Advance(0.2f);

        Assert.True(result.Balls[0].Position.Y < -0.001f);
        Assert.True(result.Balls[0].Velocity.Y < -0.01f);
    }

    [Fact]
    public void Advance_HeadOnCollisionTransfersVelocityToSecondBall()
    {
        var world = CreateCollisionWorld(
            new BallState(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: new Vector2(-0.03f, 0.0f),
                Velocity: new Vector2(1.0f, 0.0f),
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 1,
                Kind: BallKind.Solid,
                Position: new Vector2(0.03f, 0.0f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false));

        var result = world.Advance(0.01f);

        Assert.Equal(Vector2.Zero, result.Balls[0].Velocity, new Vector2Comparer(0.0001f));
        Assert.Equal(new Vector2(1.0f, 0.0f), result.Balls[1].Velocity, new Vector2Comparer(0.0001f));
        Assert.True(Vector2.Distance(result.Balls[0].Position, result.Balls[1].Position) >= 0.05714f);
        Assert.Contains(result.Events, evt => evt.EventType == ShotEventType.FirstContact && evt.BallNumber == 1);
    }

    [Fact]
    public void Advance_GlancingCollisionTransfersNormalComponent()
    {
        var world = CreateCollisionWorld(
            new BallState(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: new Vector2(-0.03f, 0.0f),
                Velocity: new Vector2(1.0f, 0.0f),
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 1,
                Kind: BallKind.Solid,
                Position: new Vector2(0.03f, 0.025f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false));

        var result = world.Advance(0.01f);

        Assert.True(result.Balls[1].Velocity.X > 0.0f);
        Assert.True(result.Balls[1].Velocity.Y > 0.0f);
        Assert.True(result.Balls[0].Velocity.X < 1.0f);
        Assert.True(result.Balls[0].Velocity.Y < 0.0f);
    }

    [Fact]
    public void Advance_SpinningCueBallCollisionTransfersTangentialVelocityAndSpin()
    {
        var world = CreateCollisionWorld(
            new BallState(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: new Vector2(-0.03f, 0.0f),
                Velocity: new Vector2(1.0f, 0.0f),
                Spin: new SpinState(6.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 1,
                Kind: BallKind.Solid,
                Position: new Vector2(0.03f, 0.0f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false));

        var result = world.Advance(0.01f);

        Assert.True(MathF.Abs(result.Balls[1].Velocity.Y) > 0.01f);
        Assert.True(MathF.Abs(result.Balls[1].Spin.SideSpinRps) > 0.01f);
        Assert.True(MathF.Abs(result.Balls[0].Spin.SideSpinRps) < 6.0f);
    }

    [Fact]
    public void Advance_SeparatesBallsThatStartOverlapped()
    {
        var world = CreateCollisionWorld(
            new BallState(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: new Vector2(-0.02f, 0.0f),
                Velocity: new Vector2(0.2f, 0.0f),
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 1,
                Kind: BallKind.Solid,
                Position: new Vector2(0.02f, 0.0f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false));

        var result = world.Advance(0.01f);

        Assert.True(Vector2.Distance(result.Balls[0].Position, result.Balls[1].Position) >= 0.05714f);
    }

    [Fact]
    public void Advance_CushionCollisionReflectsBallBackIntoTable()
    {
        var table = CustomTable9FtSpec.Create();
        var segment = table.Cushions[0];
        var ball = CreateBallAgainstSegment(segment, incomingSpeed: 1.0f);
        var world = CreateBoundaryWorld(ball);

        var result = world.Advance(0.01f);
        var correctedBall = result.Balls[0];
        var signedDistance = Vector2.Dot(correctedBall.Position - segment.Start, segment.InwardNormal);

        Assert.True(Vector2.Dot(correctedBall.Velocity, segment.InwardNormal) > 0.0f);
        Assert.True(signedDistance >= 0.028575f - 0.0001f);
        Assert.Contains(result.Events, evt => evt.EventType == ShotEventType.CushionContact && evt.Detail == segment.SourceName);
    }

    [Fact]
    public void Advance_JawCollisionReflectsBallBackIntoTable()
    {
        var table = CustomTable9FtSpec.Create();
        var segment = table.JawSegments[8];
        var ball = CreateBallAgainstSegment(segment, incomingSpeed: 1.0f);
        var world = CreateBoundaryWorld(ball);

        var result = world.Advance(0.01f);
        var correctedBall = result.Balls[0];
        var closestPoint = ClosestPointOnSegment(correctedBall.Position, segment.Start, segment.End);
        var signedDistance = Vector2.Dot(correctedBall.Position - closestPoint, segment.InwardNormal);

        Assert.True(Vector2.Dot(correctedBall.Velocity, segment.InwardNormal) > 0.0f);
        Assert.True(signedDistance >= 0.028575f - 0.0001f);
        Assert.Contains(result.Events, evt => evt.EventType == ShotEventType.CushionContact && evt.Detail == segment.SourceName);
    }

    [Fact]
    public void Advance_CushionCollisionUsesSideSpinToAlterTangentVelocity()
    {
        var table = CustomTable9FtSpec.Create();
        var segment = table.Cushions[0];
        var midpoint = (segment.Start + segment.End) * 0.5f;
        var radius = 0.028575f;
        var ball = new BallState(
            BallNumber: 0,
            Kind: BallKind.Cue,
            Position: midpoint + (segment.InwardNormal * (radius - 0.002f)),
            Velocity: -segment.InwardNormal,
            Spin: new SpinState(5.0f, 0.0f, 0.0f),
            IsPocketed: false);
        var world = CreateBoundaryWorld(ball);

        var result = world.Advance(0.01f);
        var tangentVelocity = Vector2.Dot(result.Balls[0].Velocity, segment.Direction);

        Assert.True(MathF.Abs(tangentVelocity) > 0.01f);
        Assert.True(MathF.Abs(result.Balls[0].Spin.SideSpinRps) < 5.0f);
    }

    [Fact]
    public void Advance_TableSpecProvidesJawSegmentsWithInwardNormals()
    {
        var table = CustomTable9FtSpec.Create();

        Assert.All(table.JawSegments, segment =>
        {
            Assert.InRange(segment.InwardNormal.Length(), 0.999f, 1.001f);
        });
    }

    [Fact]
    public void Advance_PocketsObjectBallAndEmitsPocketedEvent()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BR4");
        var world = CreatePocketWorld(
            new BallState(
                BallNumber: 3,
                Kind: BallKind.Solid,
                Position: pocket.Center + new Vector2(-0.01f, -0.01f),
                Velocity: new Vector2(-0.1f, -0.1f),
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false));

        var result = world.Advance(0.01f);

        Assert.True(result.Balls[0].IsPocketed);
        Assert.Equal(Vector2.Zero, result.Balls[0].Velocity, new Vector2Comparer(0.0001f));
        Assert.Contains(result.Events, evt => evt.EventType == ShotEventType.Pocketed && evt.BallNumber == 3 && evt.Detail == pocket.SourceName);
        Assert.DoesNotContain(result.Events, evt => evt.EventType == ShotEventType.Scratch);
    }

    [Fact]
    public void Advance_PocketsCueBallAndEmitsScratchEvent()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "Pocket_TM6");
        var world = CreatePocketWorld(
            new BallState(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: pocket.Center + new Vector2(0.0f, 0.01f),
                Velocity: new Vector2(0.0f, 0.1f),
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false));

        var result = world.Advance(0.01f);

        Assert.True(result.Balls[0].IsPocketed);
        Assert.Contains(result.Events, evt => evt.EventType == ShotEventType.Pocketed && evt.BallNumber == 0 && evt.Detail == pocket.SourceName);
        Assert.Contains(result.Events, evt => evt.EventType == ShotEventType.Scratch && evt.BallNumber == 0 && evt.Detail == pocket.SourceName);
    }

    [Fact]
    public void ReplayRunner_RepeatsSameShotWithSameFingerprint()
    {
        var table = CustomTable9FtSpec.Create();
        var config = CreateReplayConfig();
        var balls = CreateReplayRack();
        var shot = new ShotInput(new Vector2(1.0f, 0.02f), 2.4f, new Vector2(0.1f, -0.2f));

        var firstTrace = SimulationReplayRunner.RunShot(table, config, balls, shot, maxSteps: 4096);
        var secondTrace = SimulationReplayRunner.RunShot(table, config, balls, shot, maxSteps: 4096);
        var firstFingerprint = SimulationFingerprintBuilder.BuildSha256(firstTrace);
        var secondFingerprint = SimulationFingerprintBuilder.BuildSha256(secondTrace);

        Assert.True(firstTrace.Completed);
        Assert.True(secondTrace.Completed);
        Assert.Equal(firstFingerprint, secondFingerprint);
    }

    [Fact]
    public void ReplayRunner_StraightShotFingerprintMatchesRegressionSignature()
    {
        var table = CustomTable9FtSpec.Create();
        var config = CreateReplayConfig();
        var balls = CreateReplayRack();
        var shot = new ShotInput(Vector2.UnitX, 2.0f, Vector2.Zero);

        var trace = SimulationReplayRunner.RunShot(table, config, balls, shot, maxSteps: 4096);
        var fingerprintHash = SimulationFingerprintBuilder.BuildSha256(trace);

        Assert.True(trace.Completed);
        Assert.Equal("d317d81f59530acc884987a9869805047203f4be969aa7034287c78a8e3eff6f", fingerprintHash);
    }

    private static SimulationWorld CreateShellWorld(Vector2 cueBallVelocity)
    {
        var table = CustomTable9FtSpec.Create();
        var config = new SimulationConfig(
            fixedStepSeconds: 0.1f,
            settleSpeedThresholdMetersPerSecond: 0.01f,
            maxFixedStepsPerAdvance: 64,
            maxSideSpinRps: 12.0f,
            maxFollowSpinRps: 10.0f,
            maxDrawSpinRps: 11.0f,
            slidingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            rollingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            spinDecayRpsPerSecond: 0.0f,
            rollingMatchToleranceMetersPerSecond: 0.01f,
            spinSettleThresholdRps: 0.05f);

        return CreateWorld(cueBallVelocity, new SpinState(0.0f, 0.0f, 0.0f), config);
    }

    private static SimulationWorld CreateClothWorld(Vector2 velocity, SpinState spin)
    {
        var config = new SimulationConfig(
            fixedStepSeconds: 0.1f,
            settleSpeedThresholdMetersPerSecond: 0.01f,
            maxFixedStepsPerAdvance: 64,
            maxSideSpinRps: 12.0f,
            maxFollowSpinRps: 10.0f,
            maxDrawSpinRps: 11.0f,
            slidingFrictionAccelerationMetersPerSecondSquared: 1.8f,
            rollingFrictionAccelerationMetersPerSecondSquared: 0.22f,
            spinDecayRpsPerSecond: 1.2f,
            rollingMatchToleranceMetersPerSecond: 0.01f,
            spinSettleThresholdRps: 0.05f);

        return CreateWorld(velocity, spin, config);
    }

    private static SimulationWorld CreateCollisionWorld(BallState firstBall, BallState secondBall)
    {
        var config = new SimulationConfig(
            fixedStepSeconds: 0.01f,
            settleSpeedThresholdMetersPerSecond: 0.0001f,
            maxFixedStepsPerAdvance: 64,
            maxSideSpinRps: 12.0f,
            maxFollowSpinRps: 10.0f,
            maxDrawSpinRps: 11.0f,
            slidingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            rollingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            spinDecayRpsPerSecond: 0.0f,
            rollingMatchToleranceMetersPerSecond: 0.01f,
            spinSettleThresholdRps: 0.05f,
            ballCollisionRestitution: 1.0f,
            maxCollisionIterationsPerStep: 4,
            boundaryRestitution: 1.0f,
            maxBoundaryIterationsPerStep: 4);

        var table = CustomTable9FtSpec.Create();
        return new SimulationWorld(table, config, new[] { firstBall, secondBall });
    }

    private static SimulationWorld CreateBoundaryWorld(BallState ball)
    {
        var config = new SimulationConfig(
            fixedStepSeconds: 0.01f,
            settleSpeedThresholdMetersPerSecond: 0.0001f,
            maxFixedStepsPerAdvance: 64,
            maxSideSpinRps: 12.0f,
            maxFollowSpinRps: 10.0f,
            maxDrawSpinRps: 11.0f,
            slidingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            rollingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            spinDecayRpsPerSecond: 0.0f,
            rollingMatchToleranceMetersPerSecond: 0.01f,
            spinSettleThresholdRps: 0.05f,
            ballCollisionRestitution: 1.0f,
            maxCollisionIterationsPerStep: 4,
            boundaryRestitution: 1.0f,
            maxBoundaryIterationsPerStep: 4);

        var table = CustomTable9FtSpec.Create();
        return new SimulationWorld(table, config, new[] { ball });
    }

    private static SimulationWorld CreatePocketWorld(BallState ball)
    {
        var config = new SimulationConfig(
            fixedStepSeconds: 0.01f,
            settleSpeedThresholdMetersPerSecond: 0.0001f,
            maxFixedStepsPerAdvance: 64,
            maxSideSpinRps: 12.0f,
            maxFollowSpinRps: 10.0f,
            maxDrawSpinRps: 11.0f,
            slidingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            rollingFrictionAccelerationMetersPerSecondSquared: 0.0f,
            spinDecayRpsPerSecond: 0.0f,
            rollingMatchToleranceMetersPerSecond: 0.01f,
            spinSettleThresholdRps: 0.05f,
            ballCollisionRestitution: 1.0f,
            maxCollisionIterationsPerStep: 4,
            boundaryRestitution: 1.0f,
            maxBoundaryIterationsPerStep: 4);

        var table = CustomTable9FtSpec.Create();
        return new SimulationWorld(table, config, new[] { ball });
    }

    private static SimulationConfig CreateReplayConfig()
    {
        return new SimulationConfig(
            fixedStepSeconds: 0.01f,
            settleSpeedThresholdMetersPerSecond: 0.0005f,
            maxFixedStepsPerAdvance: 64,
            maxSideSpinRps: 12.0f,
            maxFollowSpinRps: 10.0f,
            maxDrawSpinRps: 11.0f,
            slidingFrictionAccelerationMetersPerSecondSquared: 1.8f,
            rollingFrictionAccelerationMetersPerSecondSquared: 0.22f,
            spinDecayRpsPerSecond: 1.2f,
            rollingMatchToleranceMetersPerSecond: 0.01f,
            spinSettleThresholdRps: 0.05f,
            ballCollisionRestitution: 0.96f,
            maxCollisionIterationsPerStep: 4,
            boundaryRestitution: 0.9f,
            maxBoundaryIterationsPerStep: 4);
    }

    private static BallState[] CreateReplayRack()
    {
        return
        [
            new BallState(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: new Vector2(-0.733902f, 0.002383f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 1,
                Kind: BallKind.Solid,
                Position: new Vector2(0.616941f, 0.0f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 2,
                Kind: BallKind.Solid,
                Position: new Vector2(0.665027f, 0.026363f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false),
            new BallState(
                BallNumber: 3,
                Kind: BallKind.Solid,
                Position: new Vector2(0.665027f, -0.026363f),
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false)
        ];
    }

    private static SimulationWorld CreateWorld(Vector2 cueBallVelocity, SpinState spin, SimulationConfig config)
    {
        var table = CustomTable9FtSpec.Create();

        var cueBall = new BallState(
            BallNumber: 0,
            Kind: BallKind.Cue,
            Position: Vector2.Zero,
            Velocity: cueBallVelocity,
            Spin: spin,
            IsPocketed: false);

        return new SimulationWorld(table, config, new[] { cueBall });
    }

    private static float ToForwardSpinRps(float surfaceSpeed)
    {
        return surfaceSpeed / (2.0f * MathF.PI * 0.028575f);
    }

    private static float ToSurfaceSpeed(float forwardSpinRps)
    {
        return forwardSpinRps * 2.0f * MathF.PI * 0.028575f;
    }

    private static BallState CreateBallAgainstSegment(CushionSegment segment, float incomingSpeed)
    {
        var midpoint = (segment.Start + segment.End) * 0.5f;
        var radius = 0.028575f;
        var position = midpoint + (segment.InwardNormal * (radius - 0.002f));
        var velocity = -segment.InwardNormal * incomingSpeed;

        return new BallState(
            BallNumber: 0,
            Kind: BallKind.Cue,
            Position: position,
            Velocity: velocity,
            Spin: new SpinState(0.0f, 0.0f, 0.0f),
            IsPocketed: false);
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared <= float.Epsilon)
        {
            return start;
        }

        var t = Vector2.Dot(point - start, segment) / lengthSquared;
        t = Math.Clamp(t, 0.0f, 1.0f);
        return start + (segment * t);
    }

    private sealed class Vector2Comparer : IEqualityComparer<Vector2>
    {
        private readonly float _tolerance;

        public Vector2Comparer(float tolerance)
        {
            _tolerance = tolerance;
        }

        public bool Equals(Vector2 left, Vector2 right)
        {
            return MathF.Abs(left.X - right.X) <= _tolerance &&
                   MathF.Abs(left.Y - right.Y) <= _tolerance;
        }

        public int GetHashCode(Vector2 obj)
        {
            return obj.GetHashCode();
        }
    }
}
