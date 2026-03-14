using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void UpdateStatusLabel(IReadOnlyList<ShotEvent> events)
	{
		if (events.Count > 0)
		{
			CacheRecentEvents(events);
		}

		var cueBall = _world.Balls.FirstOrDefault(ball => ball.Kind == BallKind.Cue);
		var cueBallStatus = cueBall.IsPocketed
			? "pocketed"
			: $"({cueBall.Position.X:0.000}, {cueBall.Position.Y:0.000})";
		var recentEventText = _recentFrameEvents.Count == 0
			? "none"
			: string.Join('\n', _recentFrameEvents);
		var recentRuleText = _recentRuleNotes.Count == 0
			? "none"
			: string.Join('\n', _recentRuleNotes);

		_statusHeaderLabel.Text = BuildStatusHeaderText();
		_statusHeaderLabel.Modulate = ResolveStatusAccentColor();
		_statusAccentBar.Color = ResolveStatusAccentColor();

		_statusLabel.Text = BuildStatusBody(cueBallStatus, recentEventText, recentRuleText);

		UpdateAimPanel(cueBall);
		UpdateHelpPanel();
		UpdateDebugPanel();
	}

	private string BuildStatusBody(string cueBallStatus, string recentEventText, string recentRuleText)
	{
		var builder = new StringBuilder(1024);
		builder.AppendLine("MATCH");
		builder.AppendLine($"  Mode: {GetRuleModeLabel()}");
		builder.AppendLine($"  State: {BuildModeStatusLine()}");
		builder.AppendLine();
		builder.AppendLine("WORLD");
		builder.AppendLine($"  Phase: {_world.Phase}");
		builder.AppendLine($"  Sim time: {_world.SimulationTimeSeconds:0.000}s");
		builder.AppendLine($"  Fixed steps: {_world.TotalFixedStepsExecuted}");
		builder.AppendLine($"  Camera: {GetActiveCameraPreset().Name} @ {_cameraZoomScale:0.00}x");
		builder.AppendLine();
		builder.AppendLine("TABLE");
		builder.AppendLine($"  Spec: {_tableSpec.Name}");
		builder.AppendLine($"  Cue ball: {cueBallStatus}");
		builder.AppendLine($"  Overlay: {BuildOverlaySummary()}");
		builder.AppendLine();
		builder.AppendLine("RECENT SHOT EVENTS");
		builder.AppendLine(IndentBlock(recentEventText, "  "));
		builder.AppendLine();
		builder.AppendLine("RULE NOTES");
		builder.Append(IndentBlock(recentRuleText, "  "));
		return builder.ToString();
	}

	private void UpdateAimPanel(BallState cueBall)
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			SyncCalibrationControls();
			_aimHeaderLabel.Text = "Tuning Mode";
			_aimMetricsLabel.Text =
				"WINDOW\n" +
				"  Use the separate Tuning window.\n" +
				$"  Jump target: {GetSelectedCalibrationObjectLabel()}\n" +
				$"  Total rows: {_calibrationFields.Count}\n\n" +
				"PROFILE\n" +
				$"  Path: {CalibrationProfilePath}\n" +
				$"  Overlay thickness: {_overlayLineThicknessPixels:0.0} px\n" +
				"  Adjust this in the Tuning window.\n\n" +
				"QUICK ACTIONS\n" +
				"  Drag the Tuning window to another monitor.\n" +
				"  Buttons: Save / Reload / Reset";
			_aimSpeedFill.Size = new Vector2(220.0f, _aimSpeedFill.Size.Y);
			_aimSpeedFill.Color = new Color(0.96f, 0.74f, 0.32f, 0.98f);
			_aimTipIndicator.Position = new Vector2(64.0f, 64.0f);
			return;
		}

		SyncCalibrationControls();
		var activeMaximumStrikeSpeed = GetCurrentMaximumStrikeSpeedMetersPerSecond();
		var speedNormalized = Mathf.InverseLerp(
			MinimumStrikeSpeedMetersPerSecond,
			activeMaximumStrikeSpeed,
			_strikeSpeedMetersPerSecond);
		var powerPercent = speedNormalized * 100.0f;
		_aimHeaderLabel.Text = IsComputerTurnPending() ? "Computer Shot Planning" : "Shot Setup";
		_aimMetricsLabel.Text =
			"SHOT\n" +
			$"  Aim: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg\n" +
			$"  Strike speed: {_strikeSpeedMetersPerSecond:0.00} / {activeMaximumStrikeSpeed:0.00} m/s\n" +
			$"  Power: {powerPercent:0}%\n\n" +
			"TIP / CUE\n" +
			$"  Tip offset: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})\n" +
			$"  Cue ball speed: {cueBall.Velocity.Length():0.000} m/s\n\n" +
			"DEBUG TUNE\n" +
			$"  Field: {GetTuningFieldLabel(_selectedTuningField)}\n" +
			$"  Value: {GetSelectedTuningValueText()}";

		_aimSpeedFill.Size = new Vector2(Mathf.Max(8.0f, 220.0f * speedNormalized), _aimSpeedFill.Size.Y);
		_aimSpeedFill.Color = ResolveAimSpeedColor(speedNormalized);

		var indicatorRadius = 48.0f;
		var center = new Vector2(70.0f, 70.0f);
		var indicatorCenter = center + new Vector2(_tipOffsetNormalized.X, -_tipOffsetNormalized.Y) * indicatorRadius;
		_aimTipIndicator.Position = indicatorCenter - new Vector2(6.0f, 6.0f);
	}

	private void UpdateHelpPanel()
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			_helpHeaderLabel.Text = "Controls | Tuning";
			_helpLabel.Text =
				"TUNING UI\n" +
				"  Use the separate Tuning window.\n" +
				"  All tunable rows stay visible in one flat list.\n" +
				"  The dropdown jumps to one object and highlights it.\n" +
				"  Endpoints, angles, pocket values, and spot positions each get their own row.\n" +
				"  Use Save / Reload / Reset in the window.\n\n" +
				"VIEW\n" +
				"  C camera preset   Q/E zoom\n" +
				"  H hardcode overlay   1-5 overlay layers\n\n" +
				"SHOT / MODE\n" +
				"  A/D aim   Mouse wheel fine aim\n" +
				"  W/S speed   J/L side spin   I/K follow-draw   Space shoot\n" +
				"  Esc menu   Tab quick-switch   R reset mode   F1 debug window   F6 help   F7 HUD\n" +
				"  Drag the Tuning window where you want it.\n\n" +
				"OPTIONAL SHORTCUTS\n" +
				"  P save   O reload   U reset profile";
			return;
		}

		_helpHeaderLabel.Text = _ruleMode == RuleMode.Training ? "Controls | FreePlay" : "Controls | EightBall";
		_helpLabel.Text =
			"SHOT\n" +
			"  Space shoot   A/D aim   Mouse wheel fine aim\n" +
			"  W/S speed   J/L side spin   I/K follow-draw   Backspace center tip\n\n" +
			"VIEW\n" +
			"  C camera preset   Q/E zoom\n" +
			"  H hardcode overlay   1-5 overlay layers\n\n" +
			"MODE / HUD\n" +
			"  Esc menu   Tab quick-switch   R reset rack/layout\n" +
			"  F1 debug window   F6 help   F7 HUD\n\n" +
			"PLACEMENT / DEBUG\n" +
			"  Arrow keys move selected ball when placement is active\n" +
			"  Z/X cycle freeplay ball\n" +
			"  F2/F3 choose tune   F4/F5 adjust   Shift+F4/F5 coarse   Ctrl+wheel overlay thickness";
	}

	private static string IndentBlock(string text, string prefix)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return prefix + "none";
		}

		var lines = text.Split('\n');
		var builder = new StringBuilder(text.Length + (lines.Length * prefix.Length));
		for (var index = 0; index < lines.Length; index++)
		{
			if (index > 0)
			{
				builder.Append('\n');
			}

			builder.Append(prefix);
			builder.Append(lines[index]);
		}

		return builder.ToString();
	}

	private void ResetShotSummary()
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			SetShotSummary(
				"Tuning Mode | Table Calibration",
				$"Jump target: {GetSelectedCalibrationObjectLabel()}\nUse the separate Tuning window for the full flat list of rows.\nEach row edits one endpoint, angle, pocket value, or spot position.",
				new Color(0.98f, 0.82f, 0.36f, 0.98f));
			return;
		}

		if (_ruleMode == RuleMode.Training)
		{
			SetShotSummary(
				"Last FreePlay Shot | Ready",
				"No completed freeplay shot yet.\nCue ball placement is free, and FreePlay keeps the table open for layout setup.",
				new Color(0.47f, 0.86f, 0.88f, 0.98f));
			return;
		}

		SetShotSummary(
			"Last EightBall Shot | Ready",
			$"No completed shot yet.\n{GetPlayerLabel(_eightBallState.CurrentPlayer)} is on the break and the table is open.",
			new Color(0.45f, 0.76f, 0.98f, 0.98f));
	}

	private void ApplyEightBallShotSummary(EightBallTurnResult turnResult)
	{
		var summary = turnResult.Summary;
		var header = turnResult.NextState.Winner.HasValue
			? $"Last EightBall Shot | Winner: {GetPlayerLabel(turnResult.NextState.Winner.Value)}"
			: turnResult.IsFoul
				? "Last EightBall Shot | Foul"
				: turnResult.PlayerContinues
					? "Last EightBall Shot | Table Run"
					: "Last EightBall Shot | Turn End";
		var summaryText =
			$"Shooter: {GetPlayerLabel(turnResult.ShootingPlayer)}  Shot: {turnResult.NextState.ShotNumber}\n" +
			$"Outcome: {BuildEightBallOutcomeSummary(turnResult)}\n" +
			$"First contact: {FormatOptionalBallLabel(summary.FirstContactBallNumber)}  Pocketed: {FormatBallNumberListOrNone(summary.PocketedBallNumbers)}\n" +
			$"Object rails: {FormatBallNumberListOrNone(summary.DistinctObjectBallRailContacts)}  Rail/pocket after contact: {FormatYesNo(summary.HasRailOrPocketAfterFirstContact)}  Scratch: {FormatYesNo(summary.IsScratch)}";

		SetShotSummary(
			header,
			summaryText,
			ResolveEightBallBannerStyle(turnResult).BorderColor);
	}

	private void ApplyTrainingShotSummary(TrainingTurnResult turnResult)
	{
		var summary = turnResult.Summary;
		var modeLabel = GetNonMatchModeLabel();
		var header = summary.IsScratch
			? $"Last {modeLabel} Shot | Scratch"
			: summary.PocketedBallNumbers.Count > 0
				? $"Last {modeLabel} Shot | Pocketed"
				: $"Last {modeLabel} Shot | Settled";
		var summaryText =
			$"{modeLabel} shot: {turnResult.NextState.ShotCount}\n" +
			$"Outcome: {BuildTrainingOutcomeSummary(turnResult)}\n" +
			$"First contact: {FormatOptionalBallLabel(summary.FirstContactBallNumber)}  Pocketed: {FormatBallNumberListOrNone(summary.PocketedBallNumbers)}\n" +
			$"Object rails: {FormatBallNumberListOrNone(summary.DistinctObjectBallRailContacts)}  Rail/pocket after contact: {FormatYesNo(summary.HasRailOrPocketAfterFirstContact)}  Scratch: {FormatYesNo(summary.IsScratch)}";

		SetShotSummary(
			header,
			summaryText,
			ResolveTrainingBannerStyle(turnResult).BorderColor);
	}

	private void SetShotSummary(string header, string text, Color accentColor)
	{
		_summaryHeaderLabel.Text = header;
		_summaryHeaderLabel.Modulate = accentColor;
		_summaryAccentBar.Color = accentColor;
		_summaryLabel.Text = text;
	}

	private string BuildEightBallOutcomeSummary(EightBallTurnResult turnResult)
	{
		var parts = new List<string>();
		if (turnResult.NextState.ShotNumber == 1)
		{
			parts.Add(turnResult.BreakWasLegal ? "legal break" : "illegal break");
		}

		if (turnResult.IsFoul)
		{
			parts.Add($"foul: {string.Join(", ", turnResult.Fouls)}");
		}

		if (turnResult.AssignedGroup.HasValue)
		{
			parts.Add($"claimed {turnResult.AssignedGroup.Value}");
		}

		if (turnResult.RequiresEightBallRespot)
		{
			parts.Add("8-ball respot");
		}

		if (turnResult.NextState.Winner.HasValue)
		{
			parts.Add($"winner {GetPlayerLabel(turnResult.NextState.Winner.Value)}");
		}
		else if (turnResult.PlayerContinues)
		{
			parts.Add($"{GetPlayerLabel(turnResult.ShootingPlayer)} continues");
		}
		else
		{
			parts.Add($"next: {GetPlayerLabel(turnResult.NextState.CurrentPlayer)}");
		}

		if (turnResult.NextState.BallInHandPlayer.HasValue)
		{
			parts.Add($"ball in hand {GetPlayerLabel(turnResult.NextState.BallInHandPlayer.Value)}");
		}

		return string.Join(" | ", parts);
	}

	private string BuildTrainingOutcomeSummary(TrainingTurnResult turnResult)
	{
		var parts = new List<string>();

		if (turnResult.Summary.PocketedBallNumbers.Count > 0)
		{
			parts.Add($"pocketed {FormatBallNumberList(turnResult.Summary.PocketedBallNumbers)}");
		}
		else
		{
			parts.Add("no balls pocketed");
		}

		if (turnResult.RequiresEightBallRespot)
		{
			parts.Add("8-ball respot");
		}

		if (turnResult.CanRepositionCueBallAnywhere)
		{
			parts.Add("cue ball free placement");
		}

		return string.Join(" | ", parts);
	}

	private string BuildModeStatusLine()
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			return
				$"Tuning target: {GetSelectedCalibrationObjectLabel()}  " +
				$"Visible rows: {_calibrationFields.Count}  Overlay thickness: {_overlayLineThicknessPixels:0.0} px";
		}

		if (_ruleMode == RuleMode.Training)
		{
			var pocketed = _trainingState.PocketedObjectBallNumbers.Count == 0
				? "none"
				: string.Join(", ", _trainingState.PocketedObjectBallNumbers);
			return $"FreePlay shots: {_trainingState.ShotCount}  Selected ball: {GetTrainingSelectionLabel()}  Free placement: true  Pocketed objects: {pocketed}";
		}

		var winnerText = _eightBallState.Winner.HasValue ? GetPlayerLabel(_eightBallState.Winner.Value) : "none";
		var ballInHandText = _eightBallState.BallInHandPlayer.HasValue ? GetPlayerLabel(_eightBallState.BallInHandPlayer.Value) : "none";
		return
			$"Current player: {GetPlayerLabel(_eightBallState.CurrentPlayer)}  Break shot: {_eightBallState.IsBreakShot}  Open table: {_eightBallState.OpenTable}  " +
			$"Groups: P1={_eightBallState.PlayerOneGroup} P2={_eightBallState.PlayerTwoGroup}  Ball in hand: {ballInHandText}  Winner: {winnerText}";
	}

	private string BuildStatusHeaderText()
	{
		if (_menuVisible)
		{
			return _sessionStarted
				? $"Menu Open | {GetRuleModeLabel()} paused"
				: "Start Menu | Choose a mode";
		}

		if (_ruleMode == RuleMode.Calibration)
		{
			return $"Tuning | {GetSelectedCalibrationObjectLabel()}";
		}

		if (_ruleMode == RuleMode.Training)
		{
			return $"FreePlay | Selected {GetTrainingSelectionLabel()} | Cue Ball In Hand";
		}

		if (_eightBallState.Winner.HasValue)
		{
			return $"EightBall | Winner: {GetPlayerLabel(_eightBallState.Winner.Value)}";
		}

		var groupText = _eightBallState.OpenTable
			? "Open Table"
			: $"P1 {_eightBallState.PlayerOneGroup} / P2 {_eightBallState.PlayerTwoGroup}";
		var ballInHandText = _eightBallState.BallInHandPlayer == _eightBallState.CurrentPlayer
			? " | Ball In Hand"
			: string.Empty;
		return $"EightBall | {GetPlayerLabel(_eightBallState.CurrentPlayer)} To Shoot | {groupText}{ballInHandText}";
	}

	private Color ResolveStatusAccentColor()
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			return new Color(0.98f, 0.82f, 0.36f, 0.98f);
		}

		if (_ruleMode == RuleMode.Training)
		{
			return new Color(0.47f, 0.86f, 0.88f, 0.98f);
		}

		if (_eightBallState.Winner.HasValue)
		{
			return new Color(0.99f, 0.85f, 0.31f, 0.98f);
		}

		if (HasRecentRulePrefix("Foul:"))
		{
			return new Color(0.97f, 0.42f, 0.38f, 0.98f);
		}

		if (_eightBallState.BallInHandPlayer == _eightBallState.CurrentPlayer)
		{
			return new Color(0.98f, 0.68f, 0.34f, 0.98f);
		}

		return _eightBallState.CurrentPlayer == PlayerSlot.PlayerOne
			? new Color(0.45f, 0.76f, 0.98f, 0.98f)
			: new Color(0.98f, 0.62f, 0.36f, 0.98f);
	}

	private static Color ResolveAimSpeedColor(float normalizedSpeed)
	{
		normalizedSpeed = Mathf.Clamp(normalizedSpeed, 0.0f, 1.0f);
		return normalizedSpeed switch
		{
			< 0.35f => new Color(0.4f, 0.78f, 0.96f, 0.98f),
			< 0.7f => new Color(0.52f, 0.86f, 0.48f, 0.98f),
			_ => new Color(0.97f, 0.69f, 0.3f, 0.98f)
		};
	}

	private static void SetWindowVisible(Window? window, bool visible)
	{
		if (window == null || window.Visible == visible)
		{
			return;
		}

		window.Visible = visible;
	}

	private bool HasRecentRulePrefix(string prefix)
	{
		return _recentRuleNotes.Any(note => note.StartsWith(prefix, StringComparison.Ordinal));
	}

	private string GetRuleModeLabel()
	{
		return _ruleMode switch
		{
			RuleMode.EightBall => "EightBall",
			RuleMode.Training => "FreePlay",
			RuleMode.Calibration => "Tuning",
			_ => "Unknown"
		};
	}

	private static string GetPlayerLabel(PlayerSlot player)
	{
		return player == PlayerSlot.PlayerOne ? "Player 1" : "Player 2";
	}

	private void UpdateTrainingSelectionHighlight(BallState? selectedBall, float ballRadiusMeters)
	{
		if (_trainingSelectionRoot == null)
		{
			return;
		}

		_trainingSelectionRoot.Visible = false;
	}

	private string FormatOptionalBallLabel(int? ballNumber)
	{
		return ballNumber.HasValue ? FormatBallLabel(ballNumber.Value) : "none";
	}

	private string FormatBallNumberListOrNone(IEnumerable<int> ballNumbers)
	{
		var values = ballNumbers.ToArray();
		return values.Length == 0 ? "none" : FormatBallNumberList(values);
	}

	private static string FormatYesNo(bool value)
	{
		return value ? "yes" : "no";
	}

	private void UpdateOverlayVisibility()
	{
		_hardcodeOverlayRoot.Visible = _hardcodeOverlayVisible || _debugModeEnabled || _ruleMode == RuleMode.Calibration;
		_overlayClothRoot.Visible = _overlayClothVisible;
		_overlayCushionRoot.Visible = _overlayCushionVisible;
		_overlayJawRoot.Visible = _overlayJawVisible;
		_overlayPocketRoot.Visible = _overlayPocketVisible;
		_overlaySpotRoot.Visible = _overlaySpotVisible;
	}

	private void UpdateHudVisibility()
	{
		var gameplayHudVisible = _hudVisible && !_menuVisible;
		var tuningModeVisible = _ruleMode == RuleMode.Calibration;
		_statusPanel.Visible = gameplayHudVisible && !tuningModeVisible;
		_summaryPanel.Visible = gameplayHudVisible && !tuningModeVisible;
		_aimPanel.Visible = gameplayHudVisible && !tuningModeVisible;
		_shotBannerPanel.Visible = gameplayHudVisible && !tuningModeVisible && _shotBannerSecondsRemaining > 0.0f;
		_shotBannerLabel.Visible = _shotBannerPanel.Visible;
		UpdateAuxiliaryPanelVisibility();
	}


}
