namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationConfig
{
    public static SimulationConfig Default { get; } = new(
        fixedStepSeconds: 1.0f / 240.0f,
        settleSpeedThresholdMetersPerSecond: 0.01f,
        maxFixedStepsPerAdvance: 512,
        maxSideSpinRps: 12.0f,
        maxFollowSpinRps: 10.0f,
        maxDrawSpinRps: 11.0f,
        slidingFrictionAccelerationMetersPerSecondSquared: 1.8f,
        rollingFrictionAccelerationMetersPerSecondSquared: 0.22f,
        spinDecayRpsPerSecond: 1.2f,
        rollingMatchToleranceMetersPerSecond: 0.01f,
        spinSettleThresholdRps: 0.05f,
        ballCollisionRestitution: 0.96f,
        maxCollisionIterationsPerStep: 4);

    public SimulationConfig(
        float fixedStepSeconds,
        float settleSpeedThresholdMetersPerSecond,
        int maxFixedStepsPerAdvance,
        float maxSideSpinRps,
        float maxFollowSpinRps,
        float maxDrawSpinRps,
        float slidingFrictionAccelerationMetersPerSecondSquared = 1.8f,
        float rollingFrictionAccelerationMetersPerSecondSquared = 0.22f,
        float spinDecayRpsPerSecond = 1.2f,
        float rollingMatchToleranceMetersPerSecond = 0.01f,
        float spinSettleThresholdRps = 0.05f,
        float ballCollisionRestitution = 0.96f,
        int maxCollisionIterationsPerStep = 4)
    {
        if (fixedStepSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedStepSeconds), "Fixed step must be positive.");
        }

        if (settleSpeedThresholdMetersPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settleSpeedThresholdMetersPerSecond),
                "Settle threshold cannot be negative.");
        }

        if (maxFixedStepsPerAdvance <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFixedStepsPerAdvance),
                "Max fixed steps per advance must be positive.");
        }

        if (maxSideSpinRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSideSpinRps), "Max side spin cannot be negative.");
        }

        if (maxFollowSpinRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFollowSpinRps), "Max follow spin cannot be negative.");
        }

        if (maxDrawSpinRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDrawSpinRps), "Max draw spin cannot be negative.");
        }

        if (slidingFrictionAccelerationMetersPerSecondSquared < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slidingFrictionAccelerationMetersPerSecondSquared),
                "Sliding friction acceleration cannot be negative.");
        }

        if (rollingFrictionAccelerationMetersPerSecondSquared < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollingFrictionAccelerationMetersPerSecondSquared),
                "Rolling friction acceleration cannot be negative.");
        }

        if (spinDecayRpsPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(spinDecayRpsPerSecond), "Spin decay cannot be negative.");
        }

        if (rollingMatchToleranceMetersPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollingMatchToleranceMetersPerSecond),
                "Rolling-match tolerance cannot be negative.");
        }

        if (spinSettleThresholdRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spinSettleThresholdRps),
                "Spin-settle threshold cannot be negative.");
        }

        if (ballCollisionRestitution < 0.0f || ballCollisionRestitution > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ballCollisionRestitution),
                "Ball collision restitution must be between zero and one.");
        }

        if (maxCollisionIterationsPerStep <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCollisionIterationsPerStep),
                "Max collision iterations per step must be positive.");
        }

        FixedStepSeconds = fixedStepSeconds;
        SettleSpeedThresholdMetersPerSecond = settleSpeedThresholdMetersPerSecond;
        MaxFixedStepsPerAdvance = maxFixedStepsPerAdvance;
        MaxSideSpinRps = maxSideSpinRps;
        MaxFollowSpinRps = maxFollowSpinRps;
        MaxDrawSpinRps = maxDrawSpinRps;
        SlidingFrictionAccelerationMetersPerSecondSquared = slidingFrictionAccelerationMetersPerSecondSquared;
        RollingFrictionAccelerationMetersPerSecondSquared = rollingFrictionAccelerationMetersPerSecondSquared;
        SpinDecayRpsPerSecond = spinDecayRpsPerSecond;
        RollingMatchToleranceMetersPerSecond = rollingMatchToleranceMetersPerSecond;
        SpinSettleThresholdRps = spinSettleThresholdRps;
        BallCollisionRestitution = ballCollisionRestitution;
        MaxCollisionIterationsPerStep = maxCollisionIterationsPerStep;
    }

    public float FixedStepSeconds { get; }

    public float SettleSpeedThresholdMetersPerSecond { get; }

    public int MaxFixedStepsPerAdvance { get; }

    public float MaxSideSpinRps { get; }

    public float MaxFollowSpinRps { get; }

    public float MaxDrawSpinRps { get; }

    public float SlidingFrictionAccelerationMetersPerSecondSquared { get; }

    public float RollingFrictionAccelerationMetersPerSecondSquared { get; }

    public float SpinDecayRpsPerSecond { get; }

    public float RollingMatchToleranceMetersPerSecond { get; }

    public float SpinSettleThresholdRps { get; }

    public float BallCollisionRestitution { get; }

    public int MaxCollisionIterationsPerStep { get; }
}
