namespace CodexBuilding.Billiards.Core.Rules;

public sealed class EightBallMatchState
{
    public EightBallMatchState(
        PlayerSlot currentPlayer,
        PlayerSlot breakingPlayer,
        BallGroup playerOneGroup,
        BallGroup playerTwoGroup,
        bool isBreakShot,
        int shotNumber,
        bool isGameOver,
        PlayerSlot? winner,
        PlayerSlot? ballInHandPlayer,
        IReadOnlyList<int> pocketedObjectBallNumbers)
    {
        if ((playerOneGroup == BallGroup.Unassigned) != (playerTwoGroup == BallGroup.Unassigned))
        {
            throw new ArgumentException("Player groups must both be assigned or both be unassigned.");
        }

        if (playerOneGroup != BallGroup.Unassigned && playerOneGroup == playerTwoGroup)
        {
            throw new ArgumentException("Assigned groups must be opposite.");
        }

        CurrentPlayer = currentPlayer;
        BreakingPlayer = breakingPlayer;
        PlayerOneGroup = playerOneGroup;
        PlayerTwoGroup = playerTwoGroup;
        IsBreakShot = isBreakShot;
        ShotNumber = shotNumber;
        IsGameOver = isGameOver;
        Winner = winner;
        BallInHandPlayer = ballInHandPlayer;
        PocketedObjectBallNumbers = pocketedObjectBallNumbers;
    }

    public PlayerSlot CurrentPlayer { get; }

    public PlayerSlot BreakingPlayer { get; }

    public BallGroup PlayerOneGroup { get; }

    public BallGroup PlayerTwoGroup { get; }

    public bool IsBreakShot { get; }

    public int ShotNumber { get; }

    public bool IsGameOver { get; }

    public PlayerSlot? Winner { get; }

    public PlayerSlot? BallInHandPlayer { get; }

    public IReadOnlyList<int> PocketedObjectBallNumbers { get; }

    public bool OpenTable => PlayerOneGroup == BallGroup.Unassigned;

    public BallGroup GetGroupForPlayer(PlayerSlot player)
    {
        return player == PlayerSlot.PlayerOne ? PlayerOneGroup : PlayerTwoGroup;
    }

    public static EightBallMatchState CreateNew(PlayerSlot breakingPlayer = PlayerSlot.PlayerOne)
    {
        return new EightBallMatchState(
            currentPlayer: breakingPlayer,
            breakingPlayer: breakingPlayer,
            playerOneGroup: BallGroup.Unassigned,
            playerTwoGroup: BallGroup.Unassigned,
            isBreakShot: true,
            shotNumber: 0,
            isGameOver: false,
            winner: null,
            ballInHandPlayer: null,
            pocketedObjectBallNumbers: Array.Empty<int>());
    }
}
