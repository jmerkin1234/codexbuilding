using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Core.Rules;

public static class ShotSummaryBuilder
{
    public static ShotSummary Build(SimulationReplayTrace trace)
    {
        var orderedEvents = new List<ShotEvent>();
        var pocketedBalls = new List<int>();
        var railContactBalls = new List<int>();
        var pocketedLookup = new HashSet<int>();
        var railLookup = new HashSet<int>();
        var firstContactBallNumber = default(int?);
        var firstPocketedBallNumber = default(int?);
        var hasRailOrPocketAfterFirstContact = false;
        var firstContactSeen = false;
        var isScratch = false;

        foreach (var frame in trace.Frames)
        {
            foreach (var shotEvent in frame.Events)
            {
                orderedEvents.Add(shotEvent);

                if (shotEvent.EventType == ShotEventType.FirstContact && firstContactBallNumber is null)
                {
                    firstContactBallNumber = shotEvent.BallNumber;
                    firstContactSeen = true;
                    continue;
                }

                if (firstContactSeen && shotEvent.EventType is ShotEventType.CushionContact or ShotEventType.Pocketed)
                {
                    hasRailOrPocketAfterFirstContact = true;
                }

                if (shotEvent.EventType == ShotEventType.Pocketed && shotEvent.BallNumber.HasValue)
                {
                    var ballNumber = shotEvent.BallNumber.Value;
                    if (pocketedLookup.Add(ballNumber))
                    {
                        pocketedBalls.Add(ballNumber);
                    }

                    if (ballNumber != 0 && firstPocketedBallNumber is null)
                    {
                        firstPocketedBallNumber = ballNumber;
                    }
                }

                if (shotEvent.EventType == ShotEventType.CushionContact &&
                    shotEvent.BallNumber.HasValue &&
                    shotEvent.BallNumber.Value != 0 &&
                    railLookup.Add(shotEvent.BallNumber.Value))
                {
                    railContactBalls.Add(shotEvent.BallNumber.Value);
                }

                if (shotEvent.EventType == ShotEventType.Scratch)
                {
                    isScratch = true;
                }
            }
        }

        var finalFrame = trace.FinalFrame;
        return new ShotSummary(
            trace.ResolvedCueStrike,
            finalFrame.Balls,
            orderedEvents,
            trace.Completed,
            finalFrame.Phase,
            firstContactBallNumber,
            firstPocketedBallNumber,
            pocketedBalls,
            railContactBalls,
            hasRailOrPocketAfterFirstContact,
            isScratch);
    }
}
