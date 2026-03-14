using System.Numerics;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class BallCollisionResolver
{
    public static int Resolve(List<BallState> balls, float ballDiameterMeters, SimulationConfig config)
    {
        return Resolve(balls, ballDiameterMeters, config, onCollision: null);
    }

    public static int Resolve(
        List<BallState> balls,
        float ballDiameterMeters,
        SimulationConfig config,
        Action<int, int>? onCollision)
    {
        var collisionCount = 0;
        var ballDiameterSquared = ballDiameterMeters * ballDiameterMeters;

        for (var iteration = 0; iteration < config.MaxCollisionIterationsPerStep; iteration++)
        {
            var resolvedAnyCollision = false;

            for (var firstIndex = 0; firstIndex < balls.Count - 1; firstIndex++)
            {
                var firstBall = balls[firstIndex];
                if (firstBall.IsPocketed)
                {
                    continue;
                }

                for (var secondIndex = firstIndex + 1; secondIndex < balls.Count; secondIndex++)
                {
                    var secondBall = balls[secondIndex];
                    if (secondBall.IsPocketed)
                    {
                        continue;
                    }

                    var separation = secondBall.Position - firstBall.Position;
                    var distanceSquared = separation.LengthSquared();
                    if (distanceSquared > ballDiameterSquared)
                    {
                        continue;
                    }

                    ResolvePair(ref firstBall, ref secondBall, ballDiameterMeters, config.BallCollisionRestitution);

                    balls[firstIndex] = firstBall;
                    balls[secondIndex] = secondBall;
                    onCollision?.Invoke(firstBall.BallNumber, secondBall.BallNumber);
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

    private static void ResolvePair(
        ref BallState firstBall,
        ref BallState secondBall,
        float ballDiameterMeters,
        float restitution)
    {
        var separation = secondBall.Position - firstBall.Position;
        var distance = separation.Length();
        var normal = ResolveCollisionNormal(separation, firstBall.Velocity, secondBall.Velocity);
        var penetration = ballDiameterMeters - distance;

        if (penetration > 0.0f)
        {
            var correction = normal * (penetration * 0.5f);
            firstBall = firstBall with { Position = firstBall.Position - correction };
            secondBall = secondBall with { Position = secondBall.Position + correction };
        }

        var relativeVelocity = secondBall.Velocity - firstBall.Velocity;
        var approachSpeed = Vector2.Dot(relativeVelocity, normal);
        if (approachSpeed >= 0.0f)
        {
            return;
        }

        var impulseMagnitude = -0.5f * (1.0f + restitution) * approachSpeed;
        var impulse = normal * impulseMagnitude;

        firstBall = firstBall with { Velocity = firstBall.Velocity - impulse };
        secondBall = secondBall with { Velocity = secondBall.Velocity + impulse };
    }

    private static Vector2 ResolveCollisionNormal(
        Vector2 separation,
        Vector2 firstVelocity,
        Vector2 secondVelocity)
    {
        if (separation.LengthSquared() > float.Epsilon)
        {
            return Vector2.Normalize(separation);
        }

        var relativeVelocity = secondVelocity - firstVelocity;
        if (relativeVelocity.LengthSquared() > float.Epsilon)
        {
            return Vector2.Normalize(relativeVelocity);
        }

        return Vector2.UnitX;
    }
}
