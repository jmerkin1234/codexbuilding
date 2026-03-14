using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void UpdateAuxiliaryPanelVisibility()
	{
		var gameplayHudVisible = _hudVisible && !_menuVisible;
		SetWindowVisible(_debugWindow, _debugModeEnabled);
		SetWindowVisible(_tuningWindow, !_menuVisible && _ruleMode == RuleMode.Calibration);
		_debugPanel.Visible = _debugModeEnabled;
		_debugHeaderLabel.Visible = _debugModeEnabled;
		_debugLabel.Visible = _debugModeEnabled;
		_helpPanel.Visible = gameplayHudVisible && _helpPanelVisible && _ruleMode != RuleMode.Calibration;
		_helpHeaderLabel.Visible = _helpPanel.Visible;
		_helpLabel.Visible = _helpPanel.Visible;
	}

	private void UpdateDebugPanel()
	{
		UpdateAuxiliaryPanelVisibility();
		if (!_debugModeEnabled)
		{
			return;
		}

		_debugWindow.Title = $"CodexBuilding Debug | {GetRuleModeLabel()} | {_world.Phase}";
		_debugHeaderLabel.Text =
			"Portable Engine Debug\n" +
			"F1 closes this window. F2/F3 select a tuning field. F4/F5 adjust it. Hold Shift for coarse changes.";
		_debugLabel.Text = BuildDebugText();
	}

	private string BuildDebugText()
	{
		var cueBall = _world.Balls.FirstOrDefault(ball => ball.BallNumber == 0);
		var selectedBall = _world.Balls.FirstOrDefault(ball => ball.BallNumber == GetPlacementBallNumber());
		var movingBalls = _world.Balls
			.Where(ball => !ball.IsPocketed && ball.Velocity.LengthSquared() > 0.000001f)
			.OrderBy(ball => ball.BallNumber)
			.ToArray();
		var pocketedCount = _world.Balls.Count(ball => ball.IsPocketed);
		var preview = _cachedAimPreview;
		var previewPrimary = preview == null ? 0.0f : NumericsVector2.Distance(preview.CueStart, preview.PrimaryCueEnd);
		var previewSecondary = preview?.SecondaryCueEnd is NumericsVector2 secondaryCueEnd
			? NumericsVector2.Distance(preview.PrimaryCueEnd, secondaryCueEnd)
			: 0.0f;
		var previewTarget = preview?.TargetStart is NumericsVector2 targetStart && preview.TargetEnd is NumericsVector2 targetEnd
			? NumericsVector2.Distance(targetStart, targetEnd)
			: 0.0f;
		var movingSummary = movingBalls.Length == 0
			? "none"
			: string.Join(", ", movingBalls.Take(6).Select(ball => $"{ball.BallNumber}:{ball.Velocity.Length():0.000}"));
		var builder = new StringBuilder(2048);
		builder.AppendLine("OVERVIEW");
		builder.AppendLine($"  Mode: {GetRuleModeLabel()}");
		builder.AppendLine($"  Match state: {BuildModeStatusLine()}");
		builder.AppendLine($"  Phase: {_world.Phase} | Sim time: {_world.SimulationTimeSeconds:0.000}s | Fixed steps: {_world.TotalFixedStepsExecuted}");
		builder.AppendLine($"  Shot capture: {FormatYesNo(_shotCaptureActive)} | Captured frames: {_capturedShotFrames.Count}");
		builder.AppendLine($"  Recent state: {BuildDebugStateLine()}");
		builder.AppendLine();

		builder.AppendLine("TABLE GEOMETRY");
		builder.AppendLine($"  Table spec: {_tableSpec.Name}");
		builder.AppendLine($"  Source blend: {_tableSpec.SourceBlendPath}");
		builder.AppendLine($"  Cloth min/max: {FormatVector(_tableSpec.ClothMin)} -> {FormatVector(_tableSpec.ClothMax)}");
		builder.AppendLine($"  Ball diameter: {_tableSpec.BallDiameterMeters:0.00000} m");
		builder.AppendLine($"  Geometry counts: cushions={_tableSpec.Cushions.Count}, jaws={_tableSpec.JawSegments.Count}, pockets={_tableSpec.Pockets.Count}");
		builder.AppendLine($"  Overlay layers: {BuildOverlaySummary()}");
		builder.AppendLine($"  Overlay thickness: {_overlayLineThicknessPixels:0.0} px");
		builder.AppendLine();

		builder.AppendLine("ACTIVE TUNING");
		builder.AppendLine($"  Selected field: {GetTuningFieldLabel(_selectedTuningField)}");
		builder.AppendLine($"  Selected value: {GetSelectedTuningValueText()}");
		builder.AppendLine("  Controls: F2/F3 choose field | F4/F5 adjust | Shift = coarse step | Ctrl+wheel = overlay thickness");
		builder.AppendLine($"  Fixed step: {_config.FixedStepSeconds:0.000000} s");
		builder.AppendLine($"  Settle threshold: {_config.SettleSpeedThresholdMetersPerSecond:0.0000} m/s");
		builder.AppendLine($"  Cloth friction: slide={_config.SlidingFrictionAccelerationMetersPerSecondSquared:0.000}, roll={_config.RollingFrictionAccelerationMetersPerSecondSquared:0.000}");
		builder.AppendLine($"  Spin tuning: decay={_config.SpinDecayRpsPerSecond:0.000}, side curve={_config.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps:0.0000}, moving side decay={_config.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond:0.000}");
		builder.AppendLine($"  Ball contact: restitution={_config.BallCollisionRestitution:0.00}, tangent={_config.BallCollisionTangentialTransferFactor:0.00}, spin={_config.BallCollisionSpinTransferFactor:0.00}, follow/draw={_config.BallCollisionForwardSpinCarryFactor:0.00}");
		builder.AppendLine($"  Rail contact: restitution={_config.BoundaryRestitution:0.00}, glancing={_config.BoundaryGlancingRestitution:0.00}, tangential keep={_config.BoundaryTangentialVelocityRetention:0.00}, tangential friction={_config.BoundaryTangentialFrictionFactor:0.00}, english={_config.BoundaryEnglishTransferFactor:0.00}, spin={_config.BoundarySpinTransferFactor:0.00}");
		builder.AppendLine($"  Solver iterations: ball pairs={_config.MaxCollisionIterationsPerStep}, rails={_config.MaxBoundaryIterationsPerStep}");
		builder.AppendLine();

		builder.AppendLine("SHOT SETUP");
		builder.AppendLine($"  Strike speed: {_strikeSpeedMetersPerSecond:0.00} / {GetCurrentMaximumStrikeSpeedMetersPerSecond():0.00} m/s");
		builder.AppendLine($"  Aim angle: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg");
		builder.AppendLine($"  Tip offset: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})");
		builder.AppendLine($"  Camera: {GetActiveCameraPreset().Name} | Zoom: {_cameraZoomScale:0.00}x | Pos: ({_camera.Position.X:0.000}, {_camera.Position.Y:0.000}, {_camera.Position.Z:0.000})");
		builder.AppendLine($"  Manual overlay toggle: {FormatYesNo(_hardcodeOverlayVisible)} | Debug enabled: {FormatYesNo(_debugModeEnabled)}");
		builder.AppendLine();

		builder.AppendLine("CUE BALL");
		builder.AppendLine($"  Position: {FormatVector(cueBall.Position)}");
		builder.AppendLine($"  Velocity: {FormatVector(cueBall.Velocity)}");
		builder.AppendLine($"  Spin (side, forward, vertical): {FormatSpin(cueBall.Spin)}");
		builder.AppendLine($"  Pocketed: {FormatYesNo(cueBall.IsPocketed)}");
		builder.AppendLine();

		builder.AppendLine("SELECTED BALL");
		builder.AppendLine($"  Label: {GetTrainingSelectionLabel()}");
		builder.AppendLine($"  Position: {FormatVector(selectedBall.Position)}");
		builder.AppendLine($"  Velocity: {FormatVector(selectedBall.Velocity)}");
		builder.AppendLine($"  Spin (side, forward, vertical): {FormatSpin(selectedBall.Spin)}");
		builder.AppendLine($"  Pocketed: {FormatYesNo(selectedBall.IsPocketed)}");
		builder.AppendLine();

		builder.AppendLine("BALL SUMMARY");
		builder.AppendLine($"  Moving balls: {movingBalls.Length}");
		builder.AppendLine($"  Pocketed balls: {pocketedCount}");
		builder.AppendLine($"  Moving list: {movingSummary}");
		builder.AppendLine();

		builder.AppendLine("AIM PREVIEW");
		builder.AppendLine($"  Dirty: {FormatYesNo(_aimPreviewDirty)}");
		builder.AppendLine($"  Primary path: {previewPrimary:0.000} m");
		builder.AppendLine($"  Secondary path: {previewSecondary:0.000} m");
		builder.AppendLine($"  Target path: {previewTarget:0.000} m");

		return builder.ToString();
	}

	private string BuildDebugStateLine()
	{
		if (_ruleMode == RuleMode.Training)
		{
			return $"freeplay_shots={_trainingState.ShotCount} cue_ball_in_hand={_trainingState.CueBallInHand} selected={GetTrainingSelectionLabel()}";
		}

		return
			$"current={GetPlayerLabel(_eightBallState.CurrentPlayer)} open={_eightBallState.OpenTable} groups=P1:{_eightBallState.PlayerOneGroup}/P2:{_eightBallState.PlayerTwoGroup} " +
			$"break={_eightBallState.IsBreakShot} winner={_eightBallState.Winner?.ToString() ?? "none"} bih={_eightBallState.BallInHandPlayer?.ToString() ?? "none"}";
	}

	private static string FormatVector(NumericsVector2 value)
	{
		return $"({value.X:0.000},{value.Y:0.000})";
	}

	private static string FormatSpin(SpinState spin)
	{
		return $"({spin.SideSpinRps:0.000},{spin.ForwardSpinRps:0.000},{spin.VerticalSpinRps:0.000})";
	}

	private void UpdateShotBanner(float deltaSeconds)
	{
		if (_shotBannerSecondsRemaining <= 0.0f)
		{
			return;
		}

		_shotBannerSecondsRemaining = Mathf.Max(0.0f, _shotBannerSecondsRemaining - deltaSeconds);
		if (_shotBannerSecondsRemaining > 0.0f)
		{
			return;
		}

		_shotBannerPanel.Visible = false;
		_shotBannerLabel.Visible = false;
		_shotBannerLabel.Text = string.Empty;
	}

	private void ShowShotBanner(string text, ShotBannerStyle style, float durationSeconds)
	{
		_shotBannerSecondsRemaining = Mathf.Max(durationSeconds, 0.2f);
		_shotBannerLabel.Text = text;
		_shotBannerLabel.Modulate = style.TextColor;
		_shotBannerPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(style.BackgroundColor, style.BorderColor));
		UpdateHudVisibility();
	}

	private Vector3 GetTableCenter3D()
	{
		var center = (_tableSpec.ClothMin + _tableSpec.ClothMax) * 0.5f;
		return new Vector3(center.X, 0.0f, center.Y);
	}

	private string BuildOverlaySummary()
	{
		return
			$"{(_hardcodeOverlayRoot.Visible ? "on" : "off")} " +
			$"[cloth={_overlayClothVisible} cushions={_overlayCushionVisible} jaws={_overlayJawVisible} pockets={_overlayPocketVisible} spots={_overlaySpotVisible}]";
	}

	private string BuildEightBallTurnBanner(EightBallTurnResult turnResult)
	{
		if (turnResult.NextState.Winner.HasValue)
		{
			return $"Winner: {GetPlayerLabel(turnResult.NextState.Winner.Value)}";
		}

		if (turnResult.IsFoul)
		{
			return $"Foul: {string.Join(", ", turnResult.Fouls)}";
		}

		if (turnResult.RequiresEightBallRespot)
		{
			return "8-ball respot required";
		}

		if (turnResult.AssignedGroup.HasValue)
		{
			return $"{GetPlayerLabel(turnResult.ShootingPlayer)} claims {turnResult.AssignedGroup.Value}";
		}

		if (turnResult.PlayerContinues)
		{
			return $"{GetPlayerLabel(turnResult.ShootingPlayer)} continues";
		}

		if (turnResult.NextState.BallInHandPlayer.HasValue)
		{
			return $"Ball in hand: {GetPlayerLabel(turnResult.NextState.BallInHandPlayer.Value)}";
		}

		return $"Turn to {GetPlayerLabel(turnResult.NextState.CurrentPlayer)}";
	}

	private static ShotBannerStyle ResolveEightBallBannerStyle(EightBallTurnResult turnResult)
	{
		if (turnResult.NextState.Winner.HasValue)
		{
			return new ShotBannerStyle(
				new Color(0.22f, 0.17f, 0.03f, 0.94f),
				new Color(0.98f, 0.84f, 0.29f, 0.98f),
				new Color(1.0f, 0.98f, 0.9f));
		}

		if (turnResult.IsFoul || turnResult.RequiresEightBallRespot)
		{
			return new ShotBannerStyle(
				new Color(0.24f, 0.1f, 0.06f, 0.94f),
				new Color(0.98f, 0.54f, 0.28f, 0.98f),
				new Color(1.0f, 0.95f, 0.92f));
		}

		return new ShotBannerStyle(
			new Color(0.08f, 0.18f, 0.1f, 0.92f),
			new Color(0.48f, 0.86f, 0.54f, 0.96f),
			new Color(0.94f, 1.0f, 0.95f));
	}

	private static float ResolveEightBallBannerDuration(EightBallTurnResult turnResult)
	{
		if (turnResult.NextState.Winner.HasValue)
		{
			return 3.6f;
		}

		if (turnResult.IsFoul || turnResult.RequiresEightBallRespot)
		{
			return 2.8f;
		}

		return 2.2f;
	}

	private string BuildTrainingTurnBanner(TrainingTurnResult turnResult)
	{
		var modeLabel = GetNonMatchModeLabel();
		var pocketedObjectBalls = turnResult.Summary.PocketedBallNumbers
			.Where(ballNumber => ballNumber != 0)
			.ToArray();

		if (turnResult.RequiresEightBallRespot)
		{
			return $"{modeLabel}: 8-ball respot required";
		}

		if (pocketedObjectBalls.Length > 0)
		{
			return $"{modeLabel} pocketed {FormatBallNumberList(pocketedObjectBalls)}";
		}

		return $"{modeLabel} shot {_trainingState.ShotCount} settled";
	}

	private static ShotBannerStyle ResolveTrainingBannerStyle(TrainingTurnResult turnResult)
	{
		var pocketedObjectBalls = turnResult.Summary.PocketedBallNumbers
			.Where(ballNumber => ballNumber != 0)
			.ToArray();

		if (turnResult.RequiresEightBallRespot)
		{
			return new ShotBannerStyle(
				new Color(0.24f, 0.1f, 0.06f, 0.94f),
				new Color(0.98f, 0.54f, 0.28f, 0.98f),
				new Color(1.0f, 0.95f, 0.92f));
		}

		if (pocketedObjectBalls.Length > 0)
		{
			return new ShotBannerStyle(
				new Color(0.07f, 0.19f, 0.1f, 0.92f),
				new Color(0.45f, 0.88f, 0.53f, 0.98f),
				new Color(0.93f, 1.0f, 0.94f));
		}

		return new ShotBannerStyle(
			new Color(0.08f, 0.18f, 0.22f, 0.9f),
			new Color(0.48f, 0.83f, 0.92f, 0.95f),
			new Color(0.95f, 0.99f, 1.0f));
	}

	private static float ResolveTrainingBannerDuration(TrainingTurnResult turnResult)
	{
		return turnResult.RequiresEightBallRespot ? 2.6f : 2.0f;
	}

	private string FormatBallNumberList(IEnumerable<int> ballNumbers)
	{
		return string.Join(", ", ballNumbers.Select(FormatBallLabel));
	}

	private string FormatBallLabel(int ballNumber)
	{
		return ballNumber == 0 ? "CueBall" : $"Ball_{ballNumber:00}";
	}

}
