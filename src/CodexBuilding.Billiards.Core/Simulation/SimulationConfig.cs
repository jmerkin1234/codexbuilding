namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationConfig
{
    public static SimulationConfig Default { get; } = new(
        fixedStepSeconds: 1.0f / 240.0f,
        settleSpeedThresholdMetersPerSecond: 0.01f);

    public SimulationConfig(float fixedStepSeconds, float settleSpeedThresholdMetersPerSecond)
    {
        FixedStepSeconds = fixedStepSeconds;
        SettleSpeedThresholdMetersPerSecond = settleSpeedThresholdMetersPerSecond;
    }

    public float FixedStepSeconds { get; }

    public float SettleSpeedThresholdMetersPerSecond { get; }
}
