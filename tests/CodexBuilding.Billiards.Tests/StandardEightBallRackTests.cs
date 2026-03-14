using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Tests;

public sealed class StandardEightBallRackTests
{
    [Fact]
    public void Create_ReturnsCueBallAndFifteenObjectBalls()
    {
        var table = CustomTable9FtSpec.Create();

        var balls = StandardEightBallRack.Create(table);

        Assert.Equal(16, balls.Length);
        Assert.Contains(balls, ball => ball.BallNumber == 0 && ball.Kind == BallKind.Cue);
        Assert.Equal(15, balls.Count(ball => ball.BallNumber != 0));
    }

    [Fact]
    public void Create_PlacesCueBallAndEightBallAtExpectedReferenceSpots()
    {
        var table = CustomTable9FtSpec.Create();

        var balls = StandardEightBallRack.Create(table);
        var cueBall = balls.Single(ball => ball.BallNumber == 0);
        var eightBall = balls.Single(ball => ball.BallNumber == 8);
        var expectedEightBallX = table.RackApexSpot.X + ((table.BallDiameterMeters + 0.0005f) * 0.8660254f * 2.0f);

        Assert.InRange(Vector2.Distance(cueBall.Position, table.CueBallSpawn), 0.0f, 0.00001f);
        Assert.InRange(eightBall.Position.X, expectedEightBallX - 0.00001f, expectedEightBallX + 0.00001f);
        Assert.InRange(eightBall.Position.Y, -0.00001f, 0.00001f);
    }

    [Fact]
    public void Create_DoesNotOverlapAnyBalls()
    {
        var table = CustomTable9FtSpec.Create();

        var balls = StandardEightBallRack.Create(table);
        var minimumDistance = table.BallDiameterMeters;

        for (var firstIndex = 0; firstIndex < balls.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < balls.Length; secondIndex++)
            {
                var distance = Vector2.Distance(balls[firstIndex].Position, balls[secondIndex].Position);
                Assert.True(distance >= minimumDistance, $"Balls {balls[firstIndex].BallNumber} and {balls[secondIndex].BallNumber} overlap.");
            }
        }
    }
}
