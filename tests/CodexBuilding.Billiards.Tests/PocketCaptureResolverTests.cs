using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Tests;

public sealed class PocketCaptureResolverTests
{
    [Fact]
    public void Resolve_CapturesBallDeepInsidePocketDropRegion()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BR4");
        var balls = new List<BallState>
        {
            CreateBall(
                ballNumber: 4,
                position: pocket.Center + (pocket.EntryDirection * 0.004f),
                velocity: Vector2.Zero)
        };

        var captureCount = PocketCaptureResolver.Resolve(balls, table.Pockets, table.BallDiameterMeters * 0.5f);

        Assert.Equal(1, captureCount);
        Assert.True(balls[0].IsPocketed);
        Assert.Equal(pocket.Center, balls[0].Position);
    }

    [Fact]
    public void Resolve_CapturesControlledBallTravelingInsidePocketMouthLane()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BM3");
        var lateral = new Vector2(-pocket.EntryDirection.Y, pocket.EntryDirection.X);
        var balls = new List<BallState>
        {
            CreateBall(
                ballNumber: 6,
                position: pocket.MouthCenter +
                          (pocket.EntryDirection * (pocket.FunnelDepthMeters * 0.45f)) +
                          (lateral * (pocket.MouthHalfWidthMeters * 0.12f)),
                velocity: pocket.EntryDirection * (pocket.MaxEntrySpeedMetersPerSecond * 0.62f))
        };

        var captureCount = PocketCaptureResolver.Resolve(balls, table.Pockets, table.BallDiameterMeters * 0.5f);

        Assert.Equal(1, captureCount);
        Assert.True(balls[0].IsPocketed);
    }

    [Fact]
    public void Resolve_DoesNotCaptureHighSpeedBallSkimmingPocketEdge()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BM3");
        var lateral = new Vector2(-pocket.EntryDirection.Y, pocket.EntryDirection.X);
        var balls = new List<BallState>
        {
            CreateBall(
                ballNumber: 7,
                position: pocket.MouthCenter +
                          (pocket.EntryDirection * (pocket.FunnelDepthMeters * 0.28f)) +
                          (lateral * (pocket.MouthHalfWidthMeters * 0.94f)),
                velocity: pocket.EntryDirection * (pocket.MaxEntrySpeedMetersPerSecond * 1.85f))
        };

        var captureCount = PocketCaptureResolver.Resolve(balls, table.Pockets, table.BallDiameterMeters * 0.5f);

        Assert.Equal(0, captureCount);
        Assert.False(balls[0].IsPocketed);
    }

    [Fact]
    public void Resolve_DoesNotCaptureBallOutsidePocketMouthLane()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BM3");
        var lateral = new Vector2(-pocket.EntryDirection.Y, pocket.EntryDirection.X);
        var balls = new List<BallState>
        {
            CreateBall(
                ballNumber: 2,
                position: pocket.MouthCenter +
                          (pocket.EntryDirection * 0.045f) +
                          (lateral * 0.0665f),
                velocity: pocket.EntryDirection * 0.18f)
        };

        var captureCount = PocketCaptureResolver.Resolve(balls, table.Pockets, table.BallDiameterMeters * 0.5f);

        Assert.Equal(0, captureCount);
        Assert.False(balls[0].IsPocketed);
    }

    [Fact]
    public void Resolve_DoesNotCaptureSlowBallHangingOnPocketLip()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BM3");
        var lateral = new Vector2(-pocket.EntryDirection.Y, pocket.EntryDirection.X);
        var balls = new List<BallState>
        {
            CreateBall(
                ballNumber: 11,
                position: pocket.MouthCenter +
                          (pocket.EntryDirection * (pocket.FunnelDepthMeters * 0.12f)) +
                          (lateral * (pocket.MouthHalfWidthMeters * 0.82f)),
                velocity: pocket.EntryDirection * 0.04f)
        };

        var captureCount = PocketCaptureResolver.Resolve(balls, table.Pockets, table.BallDiameterMeters * 0.5f);

        Assert.Equal(0, captureCount);
        Assert.False(balls[0].IsPocketed);
    }

    [Fact]
    public void Resolve_CapturesSlowBallRollingDownPocketCenter()
    {
        var table = CustomTable9FtSpec.Create();
        var pocket = table.Pockets.First(p => p.SourceName == "pocket_BM3");
        var balls = new List<BallState>
        {
            CreateBall(
                ballNumber: 12,
                position: pocket.MouthCenter + (pocket.EntryDirection * (pocket.FunnelDepthMeters * 0.18f)),
                velocity: pocket.EntryDirection * 0.04f)
        };

        var captureCount = PocketCaptureResolver.Resolve(balls, table.Pockets, table.BallDiameterMeters * 0.5f);

        Assert.Equal(1, captureCount);
        Assert.True(balls[0].IsPocketed);
    }

    private static BallState CreateBall(int ballNumber, Vector2 position, Vector2 velocity)
    {
        return new BallState(
            BallNumber: ballNumber,
            Kind: BallKind.Solid,
            Position: position,
            Velocity: velocity,
            Spin: new SpinState(0.0f, 0.0f, 0.0f),
            IsPocketed: false);
    }
}
