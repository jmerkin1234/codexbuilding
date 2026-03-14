using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Core.Rules;

public static class EightBallRulesEngine
{
    public static EightBallTurnResult ResolveShot(
        EightBallMatchState state,
        SimulationReplayTrace trace,
        EightBallRulesConfig? config = null)
    {
        if (state.IsGameOver)
        {
            throw new InvalidOperationException("Cannot resolve a shot after the match is already over.");
        }

        config ??= EightBallRulesConfig.Default;

        var summary = ShotSummaryBuilder.Build(trace);
        var shootingPlayer = state.CurrentPlayer;
        var opponent = GetOpponent(shootingPlayer);
        var fouls = new List<EightBallFoulType>();
        var preShotPocketed = new HashSet<int>(state.PocketedObjectBallNumbers);
        var postShotPocketed = new HashSet<int>(state.PocketedObjectBallNumbers);
        var shooterGroupBefore = state.GetGroupForPlayer(shootingPlayer);
        var opponentGroupBefore = state.GetGroupForPlayer(opponent);
        var shooterGroupClearedBefore = IsGroupCleared(shooterGroupBefore, preShotPocketed);
        var breakWasLegal = !state.IsBreakShot || IsLegalBreak(summary, config);
        var requiresEightBallRespot = false;

        foreach (var ballNumber in summary.PocketedBallNumbers)
        {
            if (ballNumber != 0)
            {
                postShotPocketed.Add(ballNumber);
            }
        }

        if (summary.FirstContactBallNumber is null)
        {
            fouls.Add(EightBallFoulType.NoFirstContact);
        }
        else if (!IsLegalFirstContact(
                     summary.FirstContactBallNumber.Value,
                     state.IsBreakShot,
                     state.OpenTable,
                     shooterGroupBefore,
                     shooterGroupClearedBefore))
        {
            fouls.Add(EightBallFoulType.WrongFirstContact);
        }

        if (!state.IsBreakShot &&
            summary.FirstContactBallNumber.HasValue &&
            !summary.HasRailOrPocketAfterFirstContact)
        {
            fouls.Add(EightBallFoulType.NoRailOrPocketAfterContact);
        }

        if (summary.IsScratch)
        {
            fouls.Add(EightBallFoulType.Scratch);
        }

        if (state.IsBreakShot && !breakWasLegal)
        {
            fouls.Add(EightBallFoulType.IllegalBreak);
        }

        var nextPlayerOneGroup = state.PlayerOneGroup;
        var nextPlayerTwoGroup = state.PlayerTwoGroup;
        BallGroup? assignedGroup = null;

        if (!state.IsBreakShot &&
            state.OpenTable &&
            fouls.Count == 0 &&
            config.AssignGroupsUsingFirstPocketedBallOnOpenTable &&
            summary.FirstPocketedBallNumber is >= 1 and <= 15 and not 8)
        {
            assignedGroup = GetBallGroup(summary.FirstPocketedBallNumber.Value);
            if (shootingPlayer == PlayerSlot.PlayerOne)
            {
                nextPlayerOneGroup = assignedGroup.Value;
                nextPlayerTwoGroup = GetOpposingGroup(assignedGroup.Value);
            }
            else
            {
                nextPlayerTwoGroup = assignedGroup.Value;
                nextPlayerOneGroup = GetOpposingGroup(assignedGroup.Value);
            }
        }

        PlayerSlot? winner = null;

        if (summary.PocketedBallNumbers.Contains(8))
        {
            if (state.IsBreakShot)
            {
                if (summary.IsScratch && config.ScratchOnEightBallIsLoss)
                {
                    fouls.Add(EightBallFoulType.EightBallScratch);
                    winner = opponent;
                }
                else if (config.EightBallPocketedOnBreakWins && breakWasLegal)
                {
                    winner = shootingPlayer;
                }
                else
                {
                    requiresEightBallRespot = true;
                    postShotPocketed.Remove(8);
                }
            }
            else if (summary.IsScratch && config.ScratchOnEightBallIsLoss)
            {
                fouls.Add(EightBallFoulType.EightBallScratch);
                winner = opponent;
            }
            else if (shooterGroupBefore == BallGroup.Unassigned || !shooterGroupClearedBefore)
            {
                fouls.Add(EightBallFoulType.EightBallPocketedEarly);
                if (config.EarlyEightBallIsLoss)
                {
                    winner = opponent;
                }
            }
            else
            {
                winner = shootingPlayer;
            }
        }

        var shooterGroupAfter = shootingPlayer == PlayerSlot.PlayerOne ? nextPlayerOneGroup : nextPlayerTwoGroup;
        var playerContinues = winner is null &&
                              fouls.Count == 0 &&
                              ShooterKeepsTurn(state, summary, shooterGroupAfter);
        var nextPlayer = winner is null
            ? playerContinues ? shootingPlayer : opponent
            : shootingPlayer;
        var nextState = new EightBallMatchState(
            currentPlayer: nextPlayer,
            breakingPlayer: state.BreakingPlayer,
            playerOneGroup: nextPlayerOneGroup,
            playerTwoGroup: nextPlayerTwoGroup,
            isBreakShot: false,
            shotNumber: state.ShotNumber + 1,
            isGameOver: winner.HasValue,
            winner: winner,
            ballInHandPlayer: winner is null && fouls.Count > 0 ? opponent : null,
            pocketedObjectBallNumbers: postShotPocketed.OrderBy(ballNumber => ballNumber).ToArray());

        return new EightBallTurnResult(
            shootingPlayer,
            nextState,
            summary,
            fouls,
            breakWasLegal,
            playerContinues,
            assignedGroup,
            requiresEightBallRespot);
    }

    private static bool ShooterKeepsTurn(
        EightBallMatchState state,
        ShotSummary summary,
        BallGroup shooterGroupAfter)
    {
        if (state.IsBreakShot)
        {
            return summary.PocketedBallNumbers.Any(ballNumber => ballNumber is >= 1 and <= 15 and not 8);
        }

        if (summary.PocketedBallNumbers.Contains(8))
        {
            return false;
        }

        if (shooterGroupAfter == BallGroup.Unassigned)
        {
            return summary.PocketedBallNumbers.Any(ballNumber => ballNumber is >= 1 and <= 15 and not 8);
        }

        return summary.PocketedBallNumbers.Any(ballNumber => IsBallInGroup(ballNumber, shooterGroupAfter));
    }

    private static bool IsLegalBreak(ShotSummary summary, EightBallRulesConfig config)
    {
        return summary.PocketedBallNumbers.Any(ballNumber => ballNumber is >= 1 and <= 15 and not 8) ||
               summary.DistinctObjectBallRailContacts.Count >= config.MinimumObjectBallRailContactsOnBreak;
    }

    private static bool IsLegalFirstContact(
        int ballNumber,
        bool isBreakShot,
        bool openTable,
        BallGroup shooterGroupBefore,
        bool shooterGroupClearedBefore)
    {
        if (isBreakShot)
        {
            return ballNumber is >= 1 and <= 15;
        }

        if (openTable)
        {
            return ballNumber is >= 1 and <= 15 and not 8;
        }

        if (shooterGroupClearedBefore)
        {
            return ballNumber == 8;
        }

        return IsBallInGroup(ballNumber, shooterGroupBefore);
    }

    private static bool IsGroupCleared(BallGroup group, IReadOnlyCollection<int> pocketedObjectBalls)
    {
        if (group == BallGroup.Unassigned)
        {
            return false;
        }

        for (var ballNumber = group == BallGroup.Solids ? 1 : 9;
             ballNumber <= (group == BallGroup.Solids ? 7 : 15);
             ballNumber++)
        {
            if (!pocketedObjectBalls.Contains(ballNumber))
            {
                return false;
            }
        }

        return true;
    }

    private static BallGroup GetBallGroup(int ballNumber)
    {
        return ballNumber < 8 ? BallGroup.Solids : BallGroup.Stripes;
    }

    private static bool IsBallInGroup(int ballNumber, BallGroup group)
    {
        return group switch
        {
            BallGroup.Solids => ballNumber is >= 1 and <= 7,
            BallGroup.Stripes => ballNumber is >= 9 and <= 15,
            _ => false
        };
    }

    private static BallGroup GetOpposingGroup(BallGroup group)
    {
        return group == BallGroup.Solids ? BallGroup.Stripes : BallGroup.Solids;
    }

    private static PlayerSlot GetOpponent(PlayerSlot player)
    {
        return player == PlayerSlot.PlayerOne ? PlayerSlot.PlayerTwo : PlayerSlot.PlayerOne;
    }
}
