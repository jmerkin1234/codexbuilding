using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class TableBoundaryResolver
{
    public static int Resolve(
        List<BallState> balls,
        IReadOnlyList<CushionSegment> segments,
        float ballRadiusMeters,
        SimulationConfig config,
        Action<int, string>? onCollision)
    {
        var collisionCount = 0;

        for (var iteration = 0; iteration < config.MaxBoundaryIterationsPerStep; iteration++)
        {
            var resolvedAnyCollision = false;

            for (var ballIndex = 0; ballIndex < balls.Count; ballIndex++)
            {
                var ball = balls[ballIndex];
                if (ball.IsPocketed)
                {
                    continue;
                }

                foreach (var segment in segments)
                {
                    if (!TryResolveSegmentCollision(ref ball, segment, ballRadiusMeters, config))
                    {
                        continue;
                    }

                    balls[ballIndex] = ball;
                    onCollision?.Invoke(ball.BallNumber, segment.SourceName);
                    resolvedAnyCollision = true;
                    collisionCount++;
                }
            }

            if (!resolvedAnyCollision)
            {
                break;
            }
        }

        return collisionCount;
    }

    private static bool TryResolveSegmentCollision(
        ref BallState ball,
        CushionSegment segment,
        float ballRadiusMeters,
        SimulationConfig config)
    {
        var closestPoint = ClosestPointOnSegment(ball.Position, segment.Start, segment.End);
        var delta = ball.Position - closestPoint;
        var distance = delta.Length();
        if (distance >= ballRadiusMeters)
        {
            return false;
        }

        var contactNormal = distance > float.Epsilon
            ? Vector2.Normalize(delta)
            : segment.InwardNormal;
        if (Vector2.Dot(contactNormal, segment.InwardNormal) < 0.0f)
        {
            contactNormal = -contactNormal;
        }

        var penetration = ballRadiusMeters - distance;
        var correctedPosition = ball.Position + (contactNormal * penetration);
        var correctedVelocity = ball.Velocity;
        var inwardSpeed = Vector2.Dot(correctedVelocity, contactNormal);
        var sideSpin = ball.Spin.SideSpinRps;

        if (inwardSpeed < 0.0f)
        {
            var tangent = segment.Direction;
            var totalSpeed = MathF.Max(correctedVelocity.Length(), 0.0001f);
            var impactRatio = Math.Clamp(MathF.Abs(inwardSpeed) / totalSpeed, 0.0f, 1.0f);
            var effectiveRestitution = Lerp(
                config.BoundaryGlancingRestitution,
                config.BoundaryRestitution,
                impactRatio);

            var tangentSpeed = Vector2.Dot(correctedVelocity, tangent) *
                               config.BoundaryTangentialVelocityRetention;
            var slipSpeed = tangentSpeed + SpinToSurfaceSpeed(ballRadiusMeters, sideSpin);
            var tangentialVelocityDelta = -slipSpeed * config.BoundaryTangentialFrictionFactor;
            var correctedTangentSpeed = tangentSpeed + tangentialVelocityDelta;
            var correctedNormalSpeed = -inwardSpeed * effectiveRestitution;

            correctedVelocity = (tangent * correctedTangentSpeed) + (contactNormal * correctedNormalSpeed);
            sideSpin += SurfaceSpeedToSpinRps(ballRadiusMeters, tangentialVelocityDelta) *
                        config.BoundarySpinTransferFactor;
        }

        ball = ball with
        {
            Position = correctedPosition,
            Velocity = correctedVelocity,
            Spin = ball.Spin with { SideSpinRps = sideSpin }
        };

        return true;
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

    private static float SpinToSurfaceSpeed(float ballRadiusMeters, float spinRps)
    {
        return spinRps * 2.0f * MathF.PI * ballRadiusMeters;
    }

    private static float SurfaceSpeedToSpinRps(float ballRadiusMeters, float surfaceSpeedMetersPerSecond)
    {
        if (ballRadiusMeters <= 0.0f)
        {
            return 0.0f;
        }

        return surfaceSpeedMetersPerSecond / (2.0f * MathF.PI * ballRadiusMeters);
    }

    private static float Lerp(float start, float end, float amount)
    {
        return start + ((end - start) * amount);
    }
}
