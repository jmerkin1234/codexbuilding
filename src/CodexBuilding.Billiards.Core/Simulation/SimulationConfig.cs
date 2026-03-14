namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationConfig
{
    public static SimulationConfig Default { get; } = new(
        fixedStepSeconds: 1.0f / 240.0f,
        settleSpeedThresholdMetersPerSecond: 0.01f,
        maxFixedStepsPerAdvance: 512);

    public SimulationConfig(
        float fixedStepSeconds,
        float settleSpeedThresholdMetersPerSecond,
        int maxFixedStepsPerAdvance)
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

        FixedStepSeconds = fixedStepSeconds;
        SettleSpeedThresholdMetersPerSecond = settleSpeedThresholdMetersPerSecond;
        MaxFixedStepsPerAdvance = maxFixedStepsPerAdvance;
    }

    public float FixedStepSeconds { get; }

    public float SettleSpeedThresholdMetersPerSecond { get; }

    public int MaxFixedStepsPerAdvance { get; }
}
