namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationReplayFrame
{
    public SimulationReplayFrame(
        int stepIndex,
        SimulationPhase phase,
        float simulationTimeSeconds,
        IReadOnlyList<BallState> balls,
        IReadOnlyList<ShotEvent> events)
    {
        StepIndex = stepIndex;
        Phase = phase;
        SimulationTimeSeconds = simulationTimeSeconds;
        Balls = balls;
        Events = events;
    }

    public int StepIndex { get; }

    public SimulationPhase Phase { get; }

    public float SimulationTimeSeconds { get; }

    public IReadOnlyList<BallState> Balls { get; }

    public IReadOnlyList<ShotEvent> Events { get; }
}
