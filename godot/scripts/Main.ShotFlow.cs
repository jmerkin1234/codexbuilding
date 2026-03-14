using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void ExecuteShot(ShotInput shot, string recentNote, string bannerText)
	{
		if (!CanEditShot())
		{
			return;
		}

		try
		{
			var resolvedCueStrike = _world.ApplyCueStrike(shot);
			BeginShotCapture(resolvedCueStrike);
			_recentFrameEvents.Clear();
			_recentRuleNotes.Clear();
			_recentRuleNotes.Add(recentNote);
			ShowShotBanner(
				bannerText,
				new ShotBannerStyle(
					new Color(0.09f, 0.17f, 0.26f, 0.9f),
					new Color(0.52f, 0.74f, 0.96f, 0.95f),
					new Color(0.94f, 0.98f, 1.0f)),
				1.4f);
		}
		catch (Exception exception)
		{
			_recentRuleNotes.Clear();
			_recentRuleNotes.Add(exception.Message);
		}

		MarkAimPreviewDirty();
		UpdateCueGuide();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}


	private bool CanEditShot()
	{
		return _world.Phase != SimulationPhase.Running &&
			   !_shotCaptureActive &&
			   !IsMatchOver() &&
			   _world.Balls.Any(ball => ball.Kind == BallKind.Cue && !ball.IsPocketed);
	}

	private bool CanAdjustPlacement()
	{
		if (_world.Phase == SimulationPhase.Running || _shotCaptureActive)
		{
			return false;
		}

		return _ruleMode switch
		{
			RuleMode.EightBall => _eightBallState.BallInHandPlayer == _eightBallState.CurrentPlayer,
			RuleMode.Training => true,
			RuleMode.Calibration => true,
			_ => false
		};
	}

	private bool CanPlaceCueBall()
	{
		return CanAdjustPlacement();
	}

	private int GetPlacementBallNumber()
	{
		return _ruleMode is RuleMode.Training or RuleMode.Calibration ? _trainingSelectedBallNumber : 0;
	}

	private bool IsMatchOver()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsGameOver;
	}

	private void BeginShotCapture(ResolvedCueStrike resolvedCueStrike)
	{
		_capturedCueStrike = resolvedCueStrike;
		_capturedShotFrameIndex = 0;
		_capturedShotFrames.Clear();
		_shotCaptureActive = true;
		MarkAimPreviewDirty();
	}

	private void CaptureShotFrame(ShotResult result)
	{
		if (!_shotCaptureActive || !_capturedCueStrike.HasValue)
		{
			return;
		}

		if (result.FixedStepsExecuted == 0 &&
			result.Events.Count == 0 &&
			result.Phase == SimulationPhase.Running)
		{
			return;
		}

		_capturedShotFrames.Add(new SimulationReplayFrame(
			stepIndex: _capturedShotFrameIndex++,
			phase: result.Phase,
			simulationTimeSeconds: result.SimulationTimeSeconds,
			balls: result.Balls.ToArray(),
			events: result.Events.ToArray()));

		if (result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
		{
			FinalizeCapturedShot();
		}
	}

	private void FinalizeCapturedShot()
	{
		if (!_capturedCueStrike.HasValue || _capturedShotFrames.Count == 0)
		{
			ClearShotCapture();
			return;
		}

		var trace = new SimulationReplayTrace(
			_capturedCueStrike.Value,
			_capturedShotFrames.ToArray(),
			completed: true,
			maxSteps: _capturedShotFrames.Count);

		if (_ruleMode == RuleMode.EightBall)
		{
			ApplyEightBallTurnResult(EightBallRulesEngine.ResolveShot(_eightBallState, trace));
		}
		else
		{
			ApplyTrainingTurnResult(TrainingModeEngine.ResolveShot(_trainingState, trace));
		}

		ClearShotCapture();
	}

	private void ApplyEightBallTurnResult(EightBallTurnResult turnResult)
	{
		_eightBallState = turnResult.NextState;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Turn: {GetPlayerLabel(turnResult.ShootingPlayer)} -> {GetPlayerLabel(turnResult.NextState.CurrentPlayer)}");

		if (turnResult.AssignedGroup.HasValue)
		{
			_recentRuleNotes.Add($"{GetPlayerLabel(turnResult.ShootingPlayer)} claimed {turnResult.AssignedGroup.Value}.");
		}

		if (turnResult.IsFoul)
		{
			_recentRuleNotes.Add($"Foul: {string.Join(", ", turnResult.Fouls)}");
		}

		if (turnResult.RequiresEightBallRespot)
		{
			_recentRuleNotes.Add("8-ball respot required.");
		}

		if (turnResult.NextState.Winner.HasValue)
		{
			_recentRuleNotes.Add($"Winner: {GetPlayerLabel(turnResult.NextState.Winner.Value)}");
		}
		else if (turnResult.NextState.BallInHandPlayer.HasValue)
		{
			_recentRuleNotes.Add($"Ball in hand: {GetPlayerLabel(turnResult.NextState.BallInHandPlayer.Value)}");
		}
		else if (turnResult.PlayerContinues)
		{
			_recentRuleNotes.Add($"{GetPlayerLabel(turnResult.ShootingPlayer)} continues.");
		}

		ShowShotBanner(
			BuildEightBallTurnBanner(turnResult),
			ResolveEightBallBannerStyle(turnResult),
			ResolveEightBallBannerDuration(turnResult));
		ApplyEightBallShotSummary(turnResult);

		if (!turnResult.NextState.IsGameOver)
		{
			ResetWorldForNextTurn(
				cueBallInHand: turnResult.NextState.BallInHandPlayer == turnResult.NextState.CurrentPlayer,
				requiresEightBallRespot: turnResult.RequiresEightBallRespot);
		}

		MarkAimPreviewDirty();
	}

	private void ApplyTrainingTurnResult(TrainingTurnResult turnResult)
	{
		_trainingState = turnResult.NextState;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
			? $"Tuning shots: {_trainingState.ShotCount}"
			: $"FreePlay shots: {_trainingState.ShotCount}");

		if (turnResult.Summary.PocketedBallNumbers.Count > 0)
		{
			_recentRuleNotes.Add($"Pocketed: {string.Join(", ", turnResult.Summary.PocketedBallNumbers)}");
		}

		if (turnResult.RequiresEightBallRespot)
		{
			_recentRuleNotes.Add("8-ball respot required.");
		}

		_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
			? "Tuning mode keeps cue ball free and table geometry live."
			: "Cue ball can be moved freely.");
		ShowShotBanner(
			BuildTrainingTurnBanner(turnResult),
			ResolveTrainingBannerStyle(turnResult),
			ResolveTrainingBannerDuration(turnResult));
		ApplyTrainingShotSummary(turnResult);

		ResetWorldForNextTurn(
			cueBallInHand: turnResult.CanRepositionCueBallAnywhere,
			requiresEightBallRespot: turnResult.RequiresEightBallRespot);
		MarkAimPreviewDirty();
	}

	private void ResetWorldForNextTurn(bool cueBallInHand, bool requiresEightBallRespot)
	{
		var updatedBalls = _world.Balls
			.Select(ball => ball with
			{
				Velocity = NumericsVector2.Zero,
				Spin = new SpinState(0.0f, 0.0f, 0.0f)
			})
			.ToArray();

		if (requiresEightBallRespot)
		{
			updatedBalls = ApplyBallPlacement(updatedBalls, 8, _tableSpec.RackApexSpot, keepPocketed: false);
		}

		if (cueBallInHand)
		{
			var cueBall = updatedBalls.First(ball => ball.BallNumber == 0);
			var preferredCueBallPosition = cueBall.IsPocketed ? _tableSpec.CueBallSpawn : cueBall.Position;
			updatedBalls = ApplyBallPlacement(updatedBalls, 0, preferredCueBallPosition, keepPocketed: false);
		}

		_world.Reset(updatedBalls);
		MarkAimPreviewDirty();
	}

	private void MoveBallToPlacement(int ballNumber, NumericsVector2 desiredPosition, bool keepPocketed)
	{
		var updatedBalls = _world.Balls
			.Select(ball => ball with
			{
				Velocity = NumericsVector2.Zero,
				Spin = new SpinState(0.0f, 0.0f, 0.0f)
			})
			.ToArray();

		updatedBalls = ApplyBallPlacement(updatedBalls, ballNumber, desiredPosition, keepPocketed);
		_world.Reset(updatedBalls);
		MarkAimPreviewDirty();

		if (_ruleMode is RuleMode.Training or RuleMode.Calibration)
		{
			_trainingState = new TrainingModeState(
				shotCount: _trainingState.ShotCount,
				pocketedObjectBallNumbers: _trainingState.PocketedObjectBallNumbers.Where(number => number != ballNumber).ToArray(),
				cueBallInHand: true);
			_recentRuleNotes.Clear();
			_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
				? $"Tuning layout: moved {GetTrainingSelectionLabel()}"
				: $"FreePlay layout: moved {GetTrainingSelectionLabel()}");
		}
	}

	private BallState[] ApplyBallPlacement(
		BallState[] balls,
		int ballNumber,
		NumericsVector2 preferredPosition,
		bool keepPocketed)
	{
		var candidatePosition = FindLegalPlacement(preferredPosition, ballNumber, balls);

		for (var index = 0; index < balls.Length; index++)
		{
			if (balls[index].BallNumber != ballNumber)
			{
				continue;
			}

			balls[index] = balls[index] with
			{
				Position = candidatePosition,
				Velocity = NumericsVector2.Zero,
				Spin = new SpinState(0.0f, 0.0f, 0.0f),
				IsPocketed = keepPocketed && balls[index].IsPocketed
			};
			break;
		}

		return balls;
	}

	private NumericsVector2 FindLegalPlacement(
		NumericsVector2 preferredPosition,
		int movingBallNumber,
		IReadOnlyList<BallState> balls)
	{
		var ballRadiusMeters = _tableSpec.BallDiameterMeters * 0.5f;
		var clampedPreferred = ClampToCloth(preferredPosition, ballRadiusMeters);

		if (IsPlacementLegal(clampedPreferred, movingBallNumber, balls))
		{
			return clampedPreferred;
		}

		const float searchStepMeters = 0.05f;
		for (var ring = 1; ring <= 28; ring++)
		{
			for (var offsetX = -ring; offsetX <= ring; offsetX++)
			{
				for (var offsetY = -ring; offsetY <= ring; offsetY++)
				{
					if (Math.Abs(offsetX) != ring && Math.Abs(offsetY) != ring)
					{
						continue;
					}

					var candidate = ClampToCloth(
						clampedPreferred + new NumericsVector2(offsetX * searchStepMeters, offsetY * searchStepMeters),
						ballRadiusMeters);

					if (IsPlacementLegal(candidate, movingBallNumber, balls))
					{
						return candidate;
					}
				}
			}
		}

		return clampedPreferred;
	}

	private bool IsPlacementLegal(
		NumericsVector2 candidatePosition,
		int movingBallNumber,
		IReadOnlyList<BallState> balls)
	{
		var minimumDistanceSquared = (_tableSpec.BallDiameterMeters - 0.0005f) * (_tableSpec.BallDiameterMeters - 0.0005f);

		foreach (var otherBall in balls)
		{
			if (otherBall.BallNumber == movingBallNumber || otherBall.IsPocketed)
			{
				continue;
			}

			if (NumericsVector2.DistanceSquared(candidatePosition, otherBall.Position) < minimumDistanceSquared)
			{
				return false;
			}
		}

		return true;
	}

	private NumericsVector2 ClampToCloth(NumericsVector2 position, float ballRadiusMeters)
	{
		return new NumericsVector2(
			Math.Clamp(position.X, _tableSpec.ClothMin.X + ballRadiusMeters, _tableSpec.ClothMax.X - ballRadiusMeters),
			Math.Clamp(position.Y, _tableSpec.ClothMin.Y + ballRadiusMeters, _tableSpec.ClothMax.Y - ballRadiusMeters));
	}

	private void ClearShotCapture()
	{
		_capturedCueStrike = null;
		_capturedShotFrameIndex = 0;
		_capturedShotFrames.Clear();
		_shotCaptureActive = false;
		MarkAimPreviewDirty();
	}

	private void CacheRecentEvents(IReadOnlyList<ShotEvent> events)
	{
		if (events.Count == 0)
		{
			return;
		}

		_recentFrameEvents.Clear();

		foreach (var shotEvent in events.TakeLast(4))
		{
			var ballText = shotEvent.BallNumber.HasValue ? $" ball={shotEvent.BallNumber.Value}" : string.Empty;
			_recentFrameEvents.Add($"{shotEvent.EventType}{ballText} {shotEvent.Detail}".Trim());
		}
	}

	private void ProcessShotFeedbackEvents(IReadOnlyList<ShotEvent> events)
	{
		if (events.Count == 0)
		{
			return;
		}

		if (events.Any(shotEvent => shotEvent.EventType == ShotEventType.Scratch))
		{
			ShowShotBanner(
				"Scratch",
				new ShotBannerStyle(
					new Color(0.25f, 0.08f, 0.08f, 0.92f),
					new Color(0.96f, 0.41f, 0.34f, 0.98f),
					new Color(1.0f, 0.93f, 0.92f)),
				2.4f);
			return;
		}

		var pocketedBalls = events
			.Where(shotEvent => shotEvent.EventType == ShotEventType.Pocketed && shotEvent.BallNumber is int ballNumber && ballNumber != 0)
			.Select(shotEvent => shotEvent.BallNumber!.Value)
			.Distinct()
			.OrderBy(ballNumber => ballNumber)
			.ToArray();

		if (pocketedBalls.Length > 0)
		{
			ShowShotBanner(
				$"Pocketed {FormatBallNumberList(pocketedBalls)}",
				new ShotBannerStyle(
					new Color(0.07f, 0.19f, 0.1f, 0.92f),
					new Color(0.45f, 0.88f, 0.53f, 0.98f),
					new Color(0.93f, 1.0f, 0.94f)),
				2.1f);
			return;
		}

		var firstContact = events.LastOrDefault(shotEvent => shotEvent.EventType == ShotEventType.FirstContact && shotEvent.BallNumber.HasValue);
		if (firstContact?.BallNumber.HasValue == true)
		{
			ShowShotBanner(
				$"First contact: {FormatBallLabel(firstContact.BallNumber.Value)}",
				new ShotBannerStyle(
					new Color(0.08f, 0.14f, 0.23f, 0.9f),
					new Color(0.41f, 0.72f, 0.96f, 0.95f),
					new Color(0.94f, 0.98f, 1.0f)),
				1.3f);
		}
	}

	private float GetCurrentMaximumStrikeSpeedMetersPerSecond()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsBreakShot
			? MaximumBreakStrikeSpeedMetersPerSecond
			: MaximumRegularStrikeSpeedMetersPerSecond;
	}

	private string GetCalibrationProfileAbsolutePath()
	{
		return ProjectSettings.GlobalizePath(CalibrationProfilePath);
	}

	private string GetModeReadyText()
	{
		return _ruleMode switch
		{
			RuleMode.EightBall => "Eight-ball vs computer ready.",
			RuleMode.Training => "FreePlay ready.",
			RuleMode.Calibration => "Tuning mode ready.",
			_ => "Mode ready."
		};
	}

	private string GetNonMatchModeLabel()
	{
		return _ruleMode == RuleMode.Calibration ? "Tuning" : "FreePlay";
	}

	private string GetShotStartedNote()
	{
		return _ruleMode switch
		{
			RuleMode.EightBall => "Eight-ball shot started.",
			RuleMode.Calibration => "Tuning shot started.",
			_ => "FreePlay shot started."
		};
	}
}
