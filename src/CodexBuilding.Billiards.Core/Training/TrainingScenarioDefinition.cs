using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Core.Training;

public sealed class TrainingScenarioDefinition
{
    public TrainingScenarioDefinition(
        string id,
        string displayName,
        string description,
        int selectedBallNumber,
        ShotInput suggestedShot,
        IReadOnlyList<BallState> balls)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        SelectedBallNumber = selectedBallNumber;
        SuggestedShot = suggestedShot;
        Balls = balls;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public int SelectedBallNumber { get; }

    public ShotInput SuggestedShot { get; }

    public IReadOnlyList<BallState> Balls { get; }
}
