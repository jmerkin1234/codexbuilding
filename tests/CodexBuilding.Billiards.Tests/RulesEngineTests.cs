using System.Numerics;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Tests;

public sealed class RulesEngineTests
{
    [Fact]
    public void EightBall_OpenTablePocketAssignsGroupAndKeepsTurn()
    {
        var state = new EightBallMatchState(
            currentPlayer: PlayerSlot.PlayerOne,
            breakingPlayer: PlayerSlot.PlayerOne,
            playerOneGroup: BallGroup.Unassigned,
            playerTwoGroup: BallGroup.Unassigned,
            isBreakShot: false,
            shotNumber: 1,
            isGameOver: false,
            winner: null,
            ballInHandPlayer: null,
            pocketedObjectBallNumbers: Array.Empty<int>());

        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 3, "cue->3"),
            new ShotEvent(ShotEventType.CushionContact, 3, "rail_head"),
            new ShotEvent(ShotEventType.Pocketed, 3, "Pocket_TM6"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace);

        Assert.False(result.IsFoul);
        Assert.True(result.PlayerContinues);
        Assert.Equal(BallGroup.Solids, result.AssignedGroup);
        Assert.Equal(BallGroup.Solids, result.NextState.PlayerOneGroup);
        Assert.Equal(BallGroup.Stripes, result.NextState.PlayerTwoGroup);
        Assert.Equal(PlayerSlot.PlayerOne, result.NextState.CurrentPlayer);
        Assert.Equal([3], result.NextState.PocketedObjectBallNumbers);
    }

    [Fact]
    public void EightBall_ScratchSwitchesTurnAndGrantsBallInHand()
    {
        var state = CreateAssignedState();
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 2, "cue->2"),
            new ShotEvent(ShotEventType.Pocketed, 0, "pocket_BL2"),
            new ShotEvent(ShotEventType.Scratch, 0, "pocket_BL2"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace);

        Assert.True(result.IsFoul);
        Assert.Contains(EightBallFoulType.Scratch, result.Fouls);
        Assert.Equal(PlayerSlot.PlayerTwo, result.NextState.CurrentPlayer);
        Assert.Equal(PlayerSlot.PlayerTwo, result.NextState.BallInHandPlayer);
    }

    [Fact]
    public void EightBall_WrongFirstContactIsFoul()
    {
        var state = CreateAssignedState();
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 10, "cue->10"),
            new ShotEvent(ShotEventType.CushionContact, 10, "rail_head"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace);

        Assert.True(result.IsFoul);
        Assert.Contains(EightBallFoulType.WrongFirstContact, result.Fouls);
        Assert.Equal(PlayerSlot.PlayerTwo, result.NextState.CurrentPlayer);
    }

    [Fact]
    public void EightBall_EarlyEightBallIsLoss()
    {
        var state = CreateAssignedState(pocketedObjectBallNumbers: new[] { 1, 2, 3 });
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 8, "cue->8"),
            new ShotEvent(ShotEventType.Pocketed, 8, "Pocket_TR5"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace);

        Assert.True(result.IsFoul);
        Assert.Contains(EightBallFoulType.EightBallPocketedEarly, result.Fouls);
        Assert.True(result.NextState.IsGameOver);
        Assert.Equal(PlayerSlot.PlayerTwo, result.NextState.Winner);
    }

    [Fact]
    public void EightBall_ClearedGroupCanLegallyWinOnEightBall()
    {
        var state = CreateAssignedState(pocketedObjectBallNumbers: new[] { 1, 2, 3, 4, 5, 6, 7 });
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 8, "cue->8"),
            new ShotEvent(ShotEventType.Pocketed, 8, "Pocket_TR5"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace);

        Assert.False(result.IsFoul);
        Assert.True(result.NextState.IsGameOver);
        Assert.Equal(PlayerSlot.PlayerOne, result.NextState.Winner);
    }

    [Fact]
    public void EightBall_IllegalBreakIsFoul()
    {
        var state = EightBallMatchState.CreateNew();
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 1, "cue->1"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace);

        Assert.True(result.IsFoul);
        Assert.False(result.BreakWasLegal);
        Assert.Contains(EightBallFoulType.IllegalBreak, result.Fouls);
        Assert.Equal(PlayerSlot.PlayerTwo, result.NextState.CurrentPlayer);
        Assert.Equal(PlayerSlot.PlayerTwo, result.NextState.BallInHandPlayer);
    }

    [Fact]
    public void EightBall_BreakPocketedEightCanRequireRespot()
    {
        var config = new EightBallRulesConfig(eightBallPocketedOnBreakWins: false);
        var state = EightBallMatchState.CreateNew();
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 1, "cue->1"),
            new ShotEvent(ShotEventType.CushionContact, 1, "rail_head"),
            new ShotEvent(ShotEventType.CushionContact, 2, "rail_foot"),
            new ShotEvent(ShotEventType.CushionContact, 3, "rail_upper_left"),
            new ShotEvent(ShotEventType.CushionContact, 4, "rail_upper_right"),
            new ShotEvent(ShotEventType.Pocketed, 8, "Pocket_TM6"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = EightBallRulesEngine.ResolveShot(state, trace, config);

        Assert.False(result.IsFoul);
        Assert.True(result.BreakWasLegal);
        Assert.True(result.RequiresEightBallRespot);
        Assert.False(result.NextState.IsGameOver);
        Assert.DoesNotContain(8, result.NextState.PocketedObjectBallNumbers);
    }

    [Fact]
    public void TrainingMode_TracksShotsWithoutWinnerAndAllowsResetStyleFlow()
    {
        var state = TrainingModeState.CreateNew();
        var trace = CreateTrace(
            new ShotEvent(ShotEventType.FirstContact, 5, "cue->5"),
            new ShotEvent(ShotEventType.Pocketed, 5, "pocket_BL2"),
            new ShotEvent(ShotEventType.Pocketed, 8, "Pocket_TR5"),
            new ShotEvent(ShotEventType.Scratch, 0, "pocket_BR4"),
            new ShotEvent(ShotEventType.Settled, null, "time=1.0"));

        var result = TrainingModeEngine.ResolveShot(state, trace);

        Assert.Equal(1, result.NextState.ShotCount);
        Assert.Equal([5], result.NextState.PocketedObjectBallNumbers);
        Assert.True(result.NextState.CueBallInHand);
        Assert.True(result.RequiresEightBallRespot);
        Assert.True(result.CanRepositionCueBallAnywhere);
    }

    private static EightBallMatchState CreateAssignedState(IReadOnlyList<int>? pocketedObjectBallNumbers = null)
    {
        return new EightBallMatchState(
            currentPlayer: PlayerSlot.PlayerOne,
            breakingPlayer: PlayerSlot.PlayerOne,
            playerOneGroup: BallGroup.Solids,
            playerTwoGroup: BallGroup.Stripes,
            isBreakShot: false,
            shotNumber: 1,
            isGameOver: false,
            winner: null,
            ballInHandPlayer: null,
            pocketedObjectBallNumbers: pocketedObjectBallNumbers ?? Array.Empty<int>());
    }

    private static SimulationReplayTrace CreateTrace(params ShotEvent[] events)
    {
        var frame = new SimulationReplayFrame(
            stepIndex: 0,
            phase: SimulationPhase.Settled,
            simulationTimeSeconds: 1.0f,
            balls: Array.Empty<BallState>(),
            events: events);

        return new SimulationReplayTrace(
            new ResolvedCueStrike(
                AimDirection: Vector2.UnitX,
                StrikeSpeedMetersPerSecond: 2.0f,
                TipOffsetNormalized: Vector2.Zero,
                InitialVelocity: new Vector2(2.0f, 0.0f),
                InitialSpin: new SpinState(0.0f, 0.0f, 0.0f)),
            new[] { frame },
            completed: true,
            maxSteps: 1);
    }
}
