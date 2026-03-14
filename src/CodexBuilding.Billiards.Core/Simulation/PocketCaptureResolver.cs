using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class PocketCaptureResolver
{
    public static int Resolve(
        List<BallState> balls,
        IReadOnlyList<PocketSpec> pockets,
        float ballRadiusMeters,
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

            var pocket = FindCapturePocket(ball, pockets, ballRadiusMeters);
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

    private static PocketSpec? FindCapturePocket(BallState ball, IReadOnlyList<PocketSpec> pockets, float ballRadiusMeters)
    {
        PocketSpec? bestPocket = null;
        var bestScore = float.MinValue;

        foreach (var pocket in pockets)
        {
            if (!TryScorePocketCapture(ball, pocket, ballRadiusMeters, out var score) || score <= bestScore)
            {
                continue;
            }

            bestPocket = pocket;
            bestScore = score;
        }

        return bestPocket;
    }

    private static bool TryScorePocketCapture(
        BallState ball,
        PocketSpec pocket,
        float ballRadiusMeters,
        out float captureScore)
    {
        var toCenter = pocket.Center - ball.Position;
        var distanceToCenter = toCenter.Length();
        if (distanceToCenter <= pocket.DropRadiusMeters)
        {
            captureScore = 500.0f - distanceToCenter;
            return true;
        }

        var entryDirection = pocket.EntryDirection;
        var lateralDirection = new Vector2(-entryDirection.Y, entryDirection.X);
        var fromMouth = ball.Position - pocket.MouthCenter;
        var depth = Vector2.Dot(fromMouth, entryDirection);
        if (depth < -(ballRadiusMeters * 0.2f))
        {
            captureScore = 0.0f;
            return false;
        }

        var funnelDepthMeters = MathF.Max(pocket.FunnelDepthMeters, 0.001f);
        if (depth > funnelDepthMeters + ballRadiusMeters)
        {
            captureScore = 0.0f;
            return false;
        }

        var lateralDistance = MathF.Abs(Vector2.Dot(fromMouth, lateralDirection));
        var lateralAllowance = pocket.MouthHalfWidthMeters +
                               (ballRadiusMeters * 0.35f) +
                               (MathF.Max(depth, 0.0f) * 0.12f);
        if (lateralDistance > lateralAllowance)
        {
            captureScore = 0.0f;
            return false;
        }

        var radialAllowance = pocket.CaptureRadiusMeters + (ballRadiusMeters * 0.55f);
        if (distanceToCenter > radialAllowance)
        {
            captureScore = 0.0f;
            return false;
        }

        var speed = ball.Velocity.Length();
        var inwardSpeed = Vector2.Dot(ball.Velocity, entryDirection);
        var depthNormalized = Math.Clamp(depth / funnelDepthMeters, 0.0f, 1.0f);
        var lateralNormalized = Math.Clamp(lateralDistance / MathF.Max(lateralAllowance, 0.0001f), 0.0f, 1.0f);
        var edgeSpeedPenalty = 1.05f - (0.45f * lateralNormalized);
        var depthSpeedBonus = 0.55f + (depthNormalized * 0.55f);
        var speedAllowance = pocket.MaxEntrySpeedMetersPerSecond * depthSpeedBonus * edgeSpeedPenalty;
        var lipDepthThreshold = pocket.Kind == PocketKind.Side ? 0.36f : 0.30f;
        var lipLateralThreshold = pocket.Kind == PocketKind.Side ? 0.74f : 0.80f;
        var hangingOnLip = speed <= 0.09f &&
                           depthNormalized <= lipDepthThreshold &&
                           lateralDistance >= pocket.MouthHalfWidthMeters * lipLateralThreshold &&
                           distanceToCenter > pocket.DropRadiusMeters * 1.05f;
        if (hangingOnLip)
        {
            captureScore = 0.0f;
            return false;
        }

        var deepInsideFunnel = depthNormalized >= 0.78f && speed <= pocket.MaxEntrySpeedMetersPerSecond * 1.15f;
        var creepingDepthThreshold = pocket.Kind == PocketKind.Side ? 0.38f : 0.28f;
        var creepingCenterThreshold = pocket.Kind == PocketKind.Side ? 0.50f : 0.58f;
        var creepingInMouth = speed <= 0.08f &&
                              depth >= 0.0f &&
                              (depthNormalized >= creepingDepthThreshold || lateralNormalized <= creepingCenterThreshold);
        var enteringPocket = inwardSpeed > 0.015f && speed <= speedAllowance;

        if (!deepInsideFunnel && !creepingInMouth && !enteringPocket)
        {
            captureScore = 0.0f;
            return false;
        }

        captureScore = (depthNormalized * 12.0f) +
                       ((1.0f - lateralNormalized) * 4.0f) +
                       MathF.Max(0.0f, speedAllowance - speed);
        return true;
    }
}
