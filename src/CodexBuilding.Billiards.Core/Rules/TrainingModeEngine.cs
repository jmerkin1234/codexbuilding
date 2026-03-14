using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Core.Rules;

public static class TrainingModeEngine
{
    public static TrainingTurnResult ResolveShot(TrainingModeState state, SimulationReplayTrace trace)
    {
        var summary = ShotSummaryBuilder.Build(trace);
        var pocketedObjectBalls = new HashSet<int>(state.PocketedObjectBallNumbers);
        var requiresEightBallRespot = false;

        foreach (var ballNumber in summary.PocketedBallNumbers)
        {
            if (ballNumber == 0)
            {
                continue;
            }

            if (ballNumber == 8)
            {
                requiresEightBallRespot = true;
                continue;
            }

            pocketedObjectBalls.Add(ballNumber);
        }

        var nextState = new TrainingModeState(
            shotCount: state.ShotCount + 1,
            pocketedObjectBallNumbers: pocketedObjectBalls.OrderBy(ballNumber => ballNumber).ToArray(),
            cueBallInHand: true);

        return new TrainingTurnResult(
            nextState,
            summary,
            requiresEightBallRespot,
            canRepositionCueBallAnywhere: true);
    }
}
