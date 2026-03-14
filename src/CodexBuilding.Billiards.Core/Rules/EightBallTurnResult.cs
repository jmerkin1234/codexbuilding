namespace CodexBuilding.Billiards.Core.Rules;

public sealed class EightBallTurnResult
{
    public EightBallTurnResult(
        PlayerSlot shootingPlayer,
        EightBallMatchState nextState,
        ShotSummary summary,
        IReadOnlyList<EightBallFoulType> fouls,
        bool breakWasLegal,
        bool playerContinues,
        BallGroup? assignedGroup,
        bool requiresEightBallRespot)
    {
        ShootingPlayer = shootingPlayer;
        NextState = nextState;
        Summary = summary;
        Fouls = fouls;
        BreakWasLegal = breakWasLegal;
        PlayerContinues = playerContinues;
        AssignedGroup = assignedGroup;
        RequiresEightBallRespot = requiresEightBallRespot;
    }

    public PlayerSlot ShootingPlayer { get; }

    public EightBallMatchState NextState { get; }

    public ShotSummary Summary { get; }

    public IReadOnlyList<EightBallFoulType> Fouls { get; }

    public bool BreakWasLegal { get; }

    public bool PlayerContinues { get; }

    public BallGroup? AssignedGroup { get; }

    public bool RequiresEightBallRespot { get; }

    public bool IsFoul => Fouls.Count > 0;
}
