using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class PocketCaptureResolver
{
    public static int Resolve(
        List<BallState> balls,
        IReadOnlyList<PocketSpec> pockets,
        Action<int, string>? onPocketed = null)
    {
        var captureCount = 0;

        for (var ballIndex = 0; ballIndex < balls.Count; ballIndex++)
        {
            var ball = balls[ballIndex];
            if (ball.IsPocketed)
            {
                continue;
            }

            var pocket = FindContainingPocket(ball.Position, pockets);
            if (pocket is null)
            {
                continue;
            }

            balls[ballIndex] = ball with
            {
                Position = pocket.Center,
                Velocity = Vector2.Zero,
                Spin = new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed = true
            };

            onPocketed?.Invoke(ball.BallNumber, pocket.SourceName);
            captureCount++;
        }

        return captureCount;
    }

    private static PocketSpec? FindContainingPocket(Vector2 position, IReadOnlyList<PocketSpec> pockets)
    {
        PocketSpec? bestPocket = null;
        var bestDistanceSquared = float.MaxValue;

        foreach (var pocket in pockets)
        {
            var distanceSquared = Vector2.DistanceSquared(position, pocket.Center);
            var captureRadiusSquared = pocket.CaptureRadiusMeters * pocket.CaptureRadiusMeters;
            if (distanceSquared > captureRadiusSquared || distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            bestPocket = pocket;
            bestDistanceSquared = distanceSquared;
        }

        return bestPocket;
    }
}
