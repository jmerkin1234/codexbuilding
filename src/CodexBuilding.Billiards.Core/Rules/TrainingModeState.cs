namespace CodexBuilding.Billiards.Core.Rules;

public sealed class TrainingModeState
{
    public TrainingModeState(
        int shotCount,
        IReadOnlyList<int> pocketedObjectBallNumbers,
        bool cueBallInHand)
    {
        ShotCount = shotCount;
        PocketedObjectBallNumbers = pocketedObjectBallNumbers;
        CueBallInHand = cueBallInHand;
    }

    public int ShotCount { get; }

    public IReadOnlyList<int> PocketedObjectBallNumbers { get; }

    public bool CueBallInHand { get; }

    public static TrainingModeState CreateNew()
    {
        return new TrainingModeState(
            shotCount: 0,
            pocketedObjectBallNumbers: Array.Empty<int>(),
            cueBallInHand: true);
    }
}
