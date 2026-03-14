using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationWorld
{
    private readonly List<BallState> _balls;
    private readonly List<ShotEvent> _events = new();

    public SimulationWorld(TableSpec tableSpec, SimulationConfig config, IEnumerable<BallState> initialBalls)
    {
        TableSpec = tableSpec;
        Config = config;
        _balls = new List<BallState>(initialBalls);
    }

    public TableSpec TableSpec { get; }

    public SimulationConfig Config { get; }

    public float SimulationTimeSeconds { get; private set; }

    public IReadOnlyList<BallState> Balls => _balls;

    public IReadOnlyList<ShotEvent> Events => _events;

    public void Reset(IEnumerable<BallState> balls)
    {
        _balls.Clear();
        _balls.AddRange(balls);
        _events.Clear();
        SimulationTimeSeconds = 0.0f;
    }

    public void RecordCueStrike(ShotInput shotInput)
    {
        _events.Add(new ShotEvent(
            ShotEventType.CueStrike,
            ballNumber: 0,
            detail: $"speed={shotInput.StrikeSpeedMetersPerSecond:0.###} tip=({shotInput.TipOffsetNormalized.X:0.###},{shotInput.TipOffsetNormalized.Y:0.###})"));
    }

    public ShotResult Step(float deltaTimeSeconds)
    {
        SimulationTimeSeconds += deltaTimeSeconds;

        return new ShotResult(
            balls: _balls.ToArray(),
            events: _events.ToArray());
    }
}
