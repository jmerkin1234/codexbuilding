namespace CodexBuilding.Billiards.Core.Rules;

public sealed class EightBallRulesConfig
{
    public static EightBallRulesConfig Default { get; } = new();

    public EightBallRulesConfig(
        int minimumObjectBallRailContactsOnBreak = 4,
        bool eightBallPocketedOnBreakWins = true,
        bool scratchOnEightBallIsLoss = true,
        bool earlyEightBallIsLoss = true,
        bool assignGroupsUsingFirstPocketedBallOnOpenTable = true)
    {
        if (minimumObjectBallRailContactsOnBreak <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumObjectBallRailContactsOnBreak),
                "Minimum object-ball rail contacts on break must be positive.");
        }

        MinimumObjectBallRailContactsOnBreak = minimumObjectBallRailContactsOnBreak;
        EightBallPocketedOnBreakWins = eightBallPocketedOnBreakWins;
        ScratchOnEightBallIsLoss = scratchOnEightBallIsLoss;
        EarlyEightBallIsLoss = earlyEightBallIsLoss;
        AssignGroupsUsingFirstPocketedBallOnOpenTable = assignGroupsUsingFirstPocketedBallOnOpenTable;
    }

    public int MinimumObjectBallRailContactsOnBreak { get; }

    public bool EightBallPocketedOnBreakWins { get; }

    public bool ScratchOnEightBallIsLoss { get; }

    public bool EarlyEightBallIsLoss { get; }

    public bool AssignGroupsUsingFirstPocketedBallOnOpenTable { get; }
}
