using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class SimulationReplayRunner
{
    public static SimulationReplayTrace RunShot(
        TableSpec tableSpec,
        SimulationConfig config,
        IEnumerable<BallState> initialBalls,
        ShotInput shotInput,
        int maxSteps = 4096)
    {
        if (maxSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSteps), "Max steps must be positive.");
        }

        var world = new SimulationWorld(tableSpec, config, initialBalls.ToArray());
        var resolvedCueStrike = world.ApplyCueStrike(shotInput);
        var frames = new List<SimulationReplayFrame>(capacity: Math.Min(maxSteps + 1, 8192));

        for (var stepIndex = 0; stepIndex < maxSteps; stepIndex++)
        {
            var result = world.Advance(config.FixedStepSeconds);
            frames.Add(new SimulationReplayFrame(
                stepIndex,
                result.Phase,
                result.SimulationTimeSeconds,
                result.Balls.ToArray(),
                result.Events.ToArray()));

            if (result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
            {
                return new SimulationReplayTrace(resolvedCueStrike, frames, completed: true, maxSteps);
            }
        }

        return new SimulationReplayTrace(resolvedCueStrike, frames, completed: false, maxSteps);
    }
}
