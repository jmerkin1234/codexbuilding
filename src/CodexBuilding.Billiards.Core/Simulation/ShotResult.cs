namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class ShotResult
{
    public ShotResult(
        IReadOnlyList<BallState> balls,
        IReadOnlyList<ShotEvent> events,
        SimulationPhase phase,
        int fixedStepsExecuted,
        float simulationTimeSeconds,
        float accumulatorSeconds)
    {
        Balls = balls;
        Events = events;
        Phase = phase;
        FixedStepsExecuted = fixedStepsExecuted;
        SimulationTimeSeconds = simulationTimeSeconds;
        AccumulatorSeconds = accumulatorSeconds;
    }

    public IReadOnlyList<BallState> Balls { get; }

    public IReadOnlyList<ShotEvent> Events { get; }

    public SimulationPhase Phase { get; }

    public int FixedStepsExecuted { get; }

    public float SimulationTimeSeconds { get; }

    public float AccumulatorSeconds { get; }
}
