using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void UpdateComputerTurn(float deltaSeconds)
	{
		if (!IsComputerTurnPending())
		{
			_computerTurnThinkSeconds = 0.0f;
			return;
		}

		_computerTurnThinkSeconds += deltaSeconds;
		if (_computerTurnThinkSeconds < ComputerTurnThinkDelaySeconds)
		{
			return;
		}

		_computerTurnThinkSeconds = 0.0f;
		ExecuteComputerTurn();
	}

	private bool IsComputerTurnPending()
	{
		return _ruleMode == RuleMode.EightBall &&
			   _eightBallState.CurrentPlayer == PlayerSlot.PlayerTwo &&
			   !_eightBallState.IsGameOver &&
			   !_shotCaptureActive &&
			   _world.Phase != SimulationPhase.Running;
	}

	private ComputerShotPlan? BuildComputerShotPlan()
	{
		var baseBalls = _world.Balls.ToArray();
		var placementCandidates = GetComputerPlacementCandidates(baseBalls);
		ComputerShotPlan? bestPlan = null;
		var bestScore = float.NegativeInfinity;

		foreach (var preferredPlacement in placementCandidates)
		{
			var placedBalls = preferredPlacement.HasValue
				? ApplyBallPlacement(baseBalls.ToArray(), 0, preferredPlacement.Value, keepPocketed: false)
				: baseBalls.ToArray();
			var cueBallCandidates = placedBalls
				.Where(ball => ball.BallNumber == 0 && !ball.IsPocketed)
				.Take(1)
				.ToArray();
			if (cueBallCandidates.Length == 0)
			{
				continue;
			}
			var cueBall = cueBallCandidates[0];

			var targetBalls = GetComputerTargetBalls(placedBalls, cueBall.Position)
				.Take(ComputerMaxTargetBallsToConsider)
				.ToArray();
			if (targetBalls.Length == 0)
			{
				continue;
			}

			foreach (var candidate in BuildComputerShotCandidates(cueBall.Position, targetBalls))
			{
				var trace = SimulateReplayTrace(candidate.Shot, placedBalls);
				var turnResult = EightBallRulesEngine.ResolveShot(_eightBallState, trace);
				var score = ScoreComputerShot(turnResult, candidate.TargetBallNumber);

				if (score <= bestScore)
				{
					continue;
				}

				bestScore = score;
				bestPlan = new ComputerShotPlan(
					preferredPlacement,
					candidate.Shot,
					score,
					$"at {FormatBallLabel(candidate.TargetBallNumber)}");
			}
		}

		if (bestPlan.HasValue)
		{
			return bestPlan;
		}

		var fallbackCueBallCandidates = baseBalls
			.Where(ball => ball.BallNumber == 0 && !ball.IsPocketed)
			.Take(1)
			.ToArray();
		if (fallbackCueBallCandidates.Length == 0)
		{
			return null;
		}
		var fallbackCueBall = fallbackCueBallCandidates[0];
		var fallbackTarget = GetComputerTargetBalls(baseBalls, fallbackCueBall.Position).FirstOrDefault();
		if (fallbackTarget.BallNumber == 0)
		{
			return null;
		}

		var fallbackAim = NumericsVector2.Normalize(fallbackTarget.Position - fallbackCueBall.Position);
		return new ComputerShotPlan(
			CueBallPlacement: null,
			Shot: new ShotInput(fallbackAim, GetComputerTargetStrikeSpeedMetersPerSecond(), NumericsVector2.Zero),
			Score: -1000.0f,
			Description: $"at {FormatBallLabel(fallbackTarget.BallNumber)}");
	}

	private IEnumerable<NumericsVector2?> GetComputerPlacementCandidates(IReadOnlyList<BallState> balls)
	{
		if (_eightBallState.BallInHandPlayer != PlayerSlot.PlayerTwo)
		{
			yield return null;
			yield break;
		}

		var cueSpawn = _tableSpec.CueBallSpawn;
		var center = (_tableSpec.ClothMin + _tableSpec.ClothMax) * 0.5f;
		var offsets = new[]
		{
			NumericsVector2.Zero,
			new NumericsVector2(-0.28f, 0.0f),
			new NumericsVector2(-0.18f, 0.18f),
			new NumericsVector2(-0.18f, -0.18f),
			new NumericsVector2(0.08f, 0.22f),
			new NumericsVector2(0.08f, -0.22f),
			new NumericsVector2(center.X - cueSpawn.X, 0.0f)
		};

		foreach (var offset in offsets)
		{
			yield return FindLegalPlacement(cueSpawn + offset, 0, balls);
		}
	}

	private IEnumerable<BallState> GetComputerTargetBalls(IReadOnlyList<BallState> balls, NumericsVector2 cueBallPosition)
	{
		var legalTargets = ResolveComputerLegalTargets(balls);
		return balls
			.Where(ball => legalTargets.Contains(ball.BallNumber))
			.OrderBy(ball => NumericsVector2.DistanceSquared(ball.Position, cueBallPosition));
	}

	private HashSet<int> ResolveComputerLegalTargets(IReadOnlyList<BallState> balls)
	{
		var available = balls
			.Where(ball => !ball.IsPocketed && ball.BallNumber != 0)
			.Select(ball => ball.BallNumber)
			.ToHashSet();

		if (_eightBallState.IsBreakShot || _eightBallState.OpenTable)
		{
			available.Remove(8);
			return available;
		}

		var computerGroup = _eightBallState.GetGroupForPlayer(PlayerSlot.PlayerTwo);
		if (computerGroup == BallGroup.Unassigned)
		{
			available.Remove(8);
			return available;
		}

		var groupTargets = available
			.Where(ballNumber => IsBallInGroup(ballNumber, computerGroup))
			.ToHashSet();
		if (groupTargets.Count > 0)
		{
			return groupTargets;
		}

		return available.Contains(8)
			? new HashSet<int> { 8 }
			: new HashSet<int>();
	}

	private IEnumerable<ComputerShotCandidate> BuildComputerShotCandidates(
		NumericsVector2 cueBallPosition,
		IReadOnlyList<BallState> targetBalls)
	{
		var strikeSpeeds = GetComputerStrikeSpeeds();

		foreach (var targetBall in targetBalls)
		{
			var directAim = targetBall.Position - cueBallPosition;
			if (directAim.LengthSquared() > 0.000001f)
			{
				foreach (var speed in strikeSpeeds)
				{
					yield return new ComputerShotCandidate(
						targetBall.BallNumber,
						new ShotInput(NumericsVector2.Normalize(directAim), speed, NumericsVector2.Zero));
				}
			}

			foreach (var pocket in _tableSpec.Pockets)
			{
				var pocketDirection = pocket.Center - targetBall.Position;
				if (pocketDirection.LengthSquared() <= 0.000001f)
				{
					continue;
				}

				var contactPoint = targetBall.Position -
								   (NumericsVector2.Normalize(pocketDirection) * _tableSpec.BallDiameterMeters);
				var cueDirection = contactPoint - cueBallPosition;
				if (cueDirection.LengthSquared() <= 0.000001f)
				{
					continue;
				}

				foreach (var speed in strikeSpeeds)
				{
					yield return new ComputerShotCandidate(
						targetBall.BallNumber,
						new ShotInput(NumericsVector2.Normalize(cueDirection), speed, NumericsVector2.Zero));
				}
			}
		}
	}

	private float[] GetComputerStrikeSpeeds()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsBreakShot
			? _computerBreakStrikeSpeeds
			: _computerRegularStrikeSpeeds;
	}

	private float GetComputerTargetStrikeSpeedMetersPerSecond()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsBreakShot
			? 6.8f
			: 2.3f;
	}

	private SimulationReplayTrace SimulateReplayTrace(ShotInput shot, IReadOnlyList<BallState> initialBalls)
	{
		var previewWorld = new SimulationWorld(_tableSpec, _config, initialBalls.ToArray());
		var resolvedCueStrike = previewWorld.ApplyCueStrike(shot);
		var frames = new List<SimulationReplayFrame>(ComputerMaxSimulationSteps);

		for (var step = 0; step < ComputerMaxSimulationSteps; step++)
		{
			var result = previewWorld.Advance(_config.FixedStepSeconds);
			frames.Add(new SimulationReplayFrame(
				step,
				result.Phase,
				result.SimulationTimeSeconds,
				result.Balls.ToArray(),
				result.Events.ToArray()));

			if (result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
			{
				break;
			}
		}

		return new SimulationReplayTrace(
			resolvedCueStrike,
			frames,
			completed: frames.Count > 0 && frames[^1].Phase is SimulationPhase.Settled or SimulationPhase.Idle,
			maxSteps: ComputerMaxSimulationSteps);
	}

	private float ScoreComputerShot(EightBallTurnResult turnResult, int intendedTargetBall)
	{
		var summary = turnResult.Summary;
		var score = 0.0f;

		if (_eightBallState.IsBreakShot)
		{
			score += turnResult.BreakWasLegal ? 120.0f : -240.0f;
		}

		if (turnResult.NextState.Winner == PlayerSlot.PlayerTwo)
		{
			score += 10000.0f;
		}
		else if (turnResult.NextState.Winner == PlayerSlot.PlayerOne)
		{
			score -= 10000.0f;
		}

		if (turnResult.IsFoul)
		{
			score -= 420.0f + (turnResult.Fouls.Count * 90.0f);
		}

		if (summary.IsScratch)
		{
			score -= 550.0f;
		}

		if (turnResult.NextState.BallInHandPlayer == PlayerSlot.PlayerOne)
		{
			score -= 180.0f;
		}

		if (summary.FirstContactBallNumber == intendedTargetBall)
		{
			score += 140.0f;
		}
		else if (summary.FirstContactBallNumber.HasValue)
		{
			score += 30.0f;
		}

		if (summary.HasRailOrPocketAfterFirstContact)
		{
			score += 35.0f;
		}

		var computerGroup = turnResult.NextState.GetGroupForPlayer(PlayerSlot.PlayerTwo);
		var pocketedScoreBalls = summary.PocketedBallNumbers.Count(ballNumber =>
			ballNumber != 0 &&
			(computerGroup == BallGroup.Unassigned || IsBallInGroup(ballNumber, computerGroup)));
		score += pocketedScoreBalls * 220.0f;

		if (turnResult.AssignedGroup.HasValue)
		{
			score += 150.0f;
		}

		if (turnResult.PlayerContinues)
		{
			score += 120.0f;
		}

		if (turnResult.RequiresEightBallRespot)
		{
			score -= 150.0f;
		}

		score -= MathF.Abs(
			summary.ResolvedCueStrike.StrikeSpeedMetersPerSecond - GetComputerTargetStrikeSpeedMetersPerSecond()) * 8.0f;
		return score;
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

	private void ExecuteComputerTurn()
	{
		try
		{
			var plan = BuildComputerShotPlan();
			if (plan == null)
			{
				HandleComputerTurnFailure("Computer could not find a playable shot.");
				return;
			}

			if (plan.Value.CueBallPlacement.HasValue)
			{
				MoveBallToPlacement(0, plan.Value.CueBallPlacement.Value, keepPocketed: false);
			}

			_aimAngleRadians = Mathf.Atan2(plan.Value.Shot.AimDirection.Y, plan.Value.Shot.AimDirection.X);
			_strikeSpeedMetersPerSecond = plan.Value.Shot.StrikeSpeedMetersPerSecond;
			_tipOffsetNormalized = new Vector2(
				plan.Value.Shot.TipOffsetNormalized.X,
				plan.Value.Shot.TipOffsetNormalized.Y);
			MarkAimPreviewDirty();
			ExecuteShot(
				plan.Value.Shot,
				$"Computer shot: {plan.Value.Description}",
				$"Computer shoots {plan.Value.Description}");
		}
		catch (Exception exception)
		{
			HandleComputerTurnFailure($"Computer shot failed: {exception.Message}");
		}
	}

	private void HandleComputerTurnFailure(string message)
	{
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(message);

		if (_ruleMode != RuleMode.EightBall || _eightBallState.IsGameOver)
		{
			UpdateStatusLabel(Array.Empty<ShotEvent>());
			return;
		}

		var opponent = _eightBallState.CurrentPlayer == PlayerSlot.PlayerOne
			? PlayerSlot.PlayerTwo
			: PlayerSlot.PlayerOne;
		_eightBallState = new EightBallMatchState(
			currentPlayer: opponent,
			breakingPlayer: _eightBallState.BreakingPlayer,
			playerOneGroup: _eightBallState.PlayerOneGroup,
			playerTwoGroup: _eightBallState.PlayerTwoGroup,
			isBreakShot: false,
			shotNumber: _eightBallState.ShotNumber + 1,
			isGameOver: false,
			winner: null,
			ballInHandPlayer: opponent,
			pocketedObjectBallNumbers: _eightBallState.PocketedObjectBallNumbers.ToArray());
		_recentRuleNotes.Add($"Computer forfeited turn. Ball in hand: {GetPlayerLabel(opponent)}");
		ShowShotBanner(
			$"Computer turn failed. {GetPlayerLabel(opponent)} gets ball in hand.",
			new ShotBannerStyle(
				new Color(0.24f, 0.1f, 0.06f, 0.94f),
				new Color(0.98f, 0.54f, 0.28f, 0.98f),
				new Color(1.0f, 0.95f, 0.92f)),
			2.8f);
		ResetWorldForNextTurn(cueBallInHand: true, requiresEightBallRespot: false);
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}


}
