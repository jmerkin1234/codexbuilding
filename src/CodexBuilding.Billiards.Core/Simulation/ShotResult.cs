namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class ShotResult
{
    public ShotResult(IReadOnlyList<BallState> balls, IReadOnlyList<ShotEvent> events)
    {
        Balls = balls;
        Events = events;
    }

    public IReadOnlyList<BallState> Balls { get; }

    public IReadOnlyList<ShotEvent> Events { get; }
}
