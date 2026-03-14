using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Core.Rules;

public sealed class ShotSummary
{
    public ShotSummary(
        ResolvedCueStrike resolvedCueStrike,
        IReadOnlyList<BallState> finalBalls,
        IReadOnlyList<ShotEvent> events,
        bool completed,
        SimulationPhase finalPhase,
        int? firstContactBallNumber,
        int? firstPocketedBallNumber,
        IReadOnlyList<int> pocketedBallNumbers,
        IReadOnlyList<int> distinctObjectBallRailContacts,
        bool hasRailOrPocketAfterFirstContact,
        bool isScratch)
    {
        ResolvedCueStrike = resolvedCueStrike;
        FinalBalls = finalBalls;
        Events = events;
        Completed = completed;
        FinalPhase = finalPhase;
        FirstContactBallNumber = firstContactBallNumber;
        FirstPocketedBallNumber = firstPocketedBallNumber;
        PocketedBallNumbers = pocketedBallNumbers;
        DistinctObjectBallRailContacts = distinctObjectBallRailContacts;
        HasRailOrPocketAfterFirstContact = hasRailOrPocketAfterFirstContact;
        IsScratch = isScratch;
    }

    public ResolvedCueStrike ResolvedCueStrike { get; }

    public IReadOnlyList<BallState> FinalBalls { get; }

    public IReadOnlyList<ShotEvent> Events { get; }

    public bool Completed { get; }

    public SimulationPhase FinalPhase { get; }

    public int? FirstContactBallNumber { get; }

    public int? FirstPocketedBallNumber { get; }

    public IReadOnlyList<int> PocketedBallNumbers { get; }

    public IReadOnlyList<int> DistinctObjectBallRailContacts { get; }

    public bool HasRailOrPocketAfterFirstContact { get; }

    public bool IsScratch { get; }
}
