namespace CodexBuilding.Billiards.Core.Rules;

public sealed class TrainingTurnResult
{
    public TrainingTurnResult(
        TrainingModeState nextState,
        ShotSummary summary,
        bool requiresEightBallRespot,
        bool canRepositionCueBallAnywhere)
    {
        NextState = nextState;
        Summary = summary;
        RequiresEightBallRespot = requiresEightBallRespot;
        CanRepositionCueBallAnywhere = canRepositionCueBallAnywhere;
    }

    public TrainingModeState NextState { get; }

    public ShotSummary Summary { get; }

    public bool RequiresEightBallRespot { get; }

    public bool CanRepositionCueBallAnywhere { get; }
}
