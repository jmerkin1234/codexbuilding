namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationConfig
{
    public static SimulationConfig Default { get; } = new(
        fixedStepSeconds: 1.0f / 240.0f,
        settleSpeedThresholdMetersPerSecond: 0.01f,
        maxFixedStepsPerAdvance: 512,
        maxSideSpinRps: 12.0f,
        maxFollowSpinRps: 10.0f,
        maxDrawSpinRps: 11.0f);

    public SimulationConfig(
        float fixedStepSeconds,
        float settleSpeedThresholdMetersPerSecond,
        int maxFixedStepsPerAdvance,
        float maxSideSpinRps,
        float maxFollowSpinRps,
        float maxDrawSpinRps)
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

        FixedStepSeconds = fixedStepSeconds;
        SettleSpeedThresholdMetersPerSecond = settleSpeedThresholdMetersPerSecond;
        MaxFixedStepsPerAdvance = maxFixedStepsPerAdvance;
        MaxSideSpinRps = maxSideSpinRps;
        MaxFollowSpinRps = maxFollowSpinRps;
        MaxDrawSpinRps = maxDrawSpinRps;
    }

    public float FixedStepSeconds { get; }

    public float SettleSpeedThresholdMetersPerSecond { get; }

    public int MaxFixedStepsPerAdvance { get; }

    public float MaxSideSpinRps { get; }

    public float MaxFollowSpinRps { get; }

    public float MaxDrawSpinRps { get; }
}
