using System.Numerics;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class ClothMotionIntegrator
{
    public static BallState Advance(
        BallState ball,
        float ballRadiusMeters,
        SimulationConfig config,
        float deltaTimeSeconds)
    {
        if (ball.IsPocketed)
        {
            return ball;
        }

        var position = ball.Position;
        var velocity = ball.Velocity;
        var speed = velocity.Length();
        var direction = speed > config.SettleSpeedThresholdMetersPerSecond
            ? Vector2.Normalize(velocity)
            : Vector2.Zero;

        var sideSpin = MoveTowardsZero(ball.Spin.SideSpinRps, config.SpinDecayRpsPerSecond * deltaTimeSeconds);
        var verticalSpin = MoveTowardsZero(ball.Spin.VerticalSpinRps, config.SpinDecayRpsPerSecond * deltaTimeSeconds);
        var forwardSpin = ball.Spin.ForwardSpinRps;

        if (speed <= config.SettleSpeedThresholdMetersPerSecond)
        {
            forwardSpin = MoveTowardsZero(forwardSpin, config.SpinDecayRpsPerSecond * deltaTimeSeconds);
            return ball with
            {
                Position = position,
                Velocity = Vector2.Zero,
                Spin = new SpinState(sideSpin, forwardSpin, verticalSpin)
            };
        }

        var remainingTimeSeconds = deltaTimeSeconds;
        var surfaceSpeed = ForwardSpinToSurfaceSpeed(ballRadiusMeters, forwardSpin);
        var slipSpeed = speed - surfaceSpeed;

        if (MathF.Abs(slipSpeed) > config.RollingMatchToleranceMetersPerSecond &&
            config.SlidingFrictionAccelerationMetersPerSecondSquared > 0.0f)
        {
            var slideAcceleration = config.SlidingFrictionAccelerationMetersPerSecondSquared;
            var slipDecayRate = 3.5f * slideAcceleration;
            var timeToRolling = MathF.Abs(slipSpeed) / slipDecayRate;
            var slideTime = MathF.Min(remainingTimeSeconds, timeToRolling);
            var slideSign = MathF.Sign(slipSpeed);

            var newSpeed = MathF.Max(0.0f, speed - (slideSign * slideAcceleration * slideTime));
            var newSurfaceSpeed = surfaceSpeed + (slideSign * 2.5f * slideAcceleration * slideTime);

            position += direction * (((speed + newSpeed) * 0.5f) * slideTime);

            speed = newSpeed;
            surfaceSpeed = newSurfaceSpeed;
            remainingTimeSeconds -= slideTime;
        }
        else
        {
            surfaceSpeed = speed;
        }

        if (remainingTimeSeconds > 0.0f && speed > config.SettleSpeedThresholdMetersPerSecond)
        {
            if (config.RollingFrictionAccelerationMetersPerSecondSquared > 0.0f)
            {
                var rollingAcceleration = config.RollingFrictionAccelerationMetersPerSecondSquared;
                var timeToStop = speed / rollingAcceleration;
                var rollingTime = MathF.Min(remainingTimeSeconds, timeToStop);
                var newSpeed = MathF.Max(0.0f, speed - (rollingAcceleration * rollingTime));

                position += direction * (((speed + newSpeed) * 0.5f) * rollingTime);

                speed = newSpeed;
                surfaceSpeed = speed;
                remainingTimeSeconds -= rollingTime;
            }
            else
            {
                position += direction * (speed * remainingTimeSeconds);
                remainingTimeSeconds = 0.0f;
            }
        }

        if (remainingTimeSeconds > 0.0f)
        {
            forwardSpin = MoveTowardsZero(
                SurfaceSpeedToForwardSpin(ballRadiusMeters, surfaceSpeed),
                config.SpinDecayRpsPerSecond * remainingTimeSeconds);
        }
        else
        {
            forwardSpin = SurfaceSpeedToForwardSpin(ballRadiusMeters, surfaceSpeed);
        }

        if (speed <= config.SettleSpeedThresholdMetersPerSecond)
        {
            speed = 0.0f;
            direction = Vector2.Zero;
        }

        return ball with
        {
            Position = position,
            Velocity = direction * speed,
            Spin = new SpinState(sideSpin, forwardSpin, verticalSpin)
        };
    }

    private static float ForwardSpinToSurfaceSpeed(float ballRadiusMeters, float forwardSpinRps)
    {
        return forwardSpinRps * 2.0f * MathF.PI * ballRadiusMeters;
    }

    private static float SurfaceSpeedToForwardSpin(float ballRadiusMeters, float surfaceSpeedMetersPerSecond)
    {
        if (ballRadiusMeters <= 0.0f)
        {
            return 0.0f;
        }

        return surfaceSpeedMetersPerSecond / (2.0f * MathF.PI * ballRadiusMeters);
    }

    private static float MoveTowardsZero(float value, float maxDelta)
    {
        if (maxDelta <= 0.0f || value == 0.0f)
        {
            return value;
        }

        if (MathF.Abs(value) <= maxDelta)
        {
            return 0.0f;
        }

        return value - (MathF.Sign(value) * maxDelta);
    }
}
