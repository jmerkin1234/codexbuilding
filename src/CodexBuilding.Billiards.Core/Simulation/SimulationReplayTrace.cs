namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationReplayTrace
{
    public SimulationReplayTrace(
        ResolvedCueStrike resolvedCueStrike,
        IReadOnlyList<SimulationReplayFrame> frames,
        bool completed,
        int maxSteps)
    {
        ResolvedCueStrike = resolvedCueStrike;
        Frames = frames;
        Completed = completed;
        MaxSteps = maxSteps;
    }

    public ResolvedCueStrike ResolvedCueStrike { get; }

    public IReadOnlyList<SimulationReplayFrame> Frames { get; }

    public bool Completed { get; }

    public int MaxSteps { get; }

    public SimulationReplayFrame FinalFrame => Frames[^1];
}
