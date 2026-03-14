using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void ResetSessionForCurrentMode()
	{
		_world.Reset(StandardEightBallRack.Create(_tableSpec));
		_eightBallState = EightBallMatchState.CreateNew();
		_trainingState = TrainingModeState.CreateNew();
		_sessionStarted = true;
		_computerTurnThinkSeconds = 0.0f;
		_trainingSelectedBallNumber = 0;
		_capturedCueStrike = null;
		_capturedShotFrameIndex = 0;
		_shotCaptureActive = false;
		_capturedShotFrames.Clear();
		_recentFrameEvents.Clear();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(GetModeReadyText());
		_aimAngleRadians = GetDefaultAimAngle();
		_strikeSpeedMetersPerSecond = DefaultStrikeSpeedMetersPerSecond;
		_tipOffsetNormalized = Vector2.Zero;
		if (_ruleMode == RuleMode.Calibration)
		{
			_hardcodeOverlayVisible = true;
		}
		ResetShotSummary();
		MarkAimPreviewDirty();

		if (CanPlaceCueBall())
		{
			ResetWorldForNextTurn(cueBallInHand: true, requiresEightBallRespot: false);
		}

		ShowShotBanner(
			GetModeReadyText(),
			new ShotBannerStyle(
				new Color(0.08f, 0.18f, 0.22f, 0.9f),
				new Color(0.48f, 0.83f, 0.92f, 0.95f),
				new Color(0.95f, 0.99f, 1.0f)),
			1.8f);
		SyncBallVisuals(_world.Balls);
		UpdateCueGuide();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleRuleMode()
	{
		_ruleMode = _ruleMode switch
		{
			RuleMode.EightBall => RuleMode.Training,
			RuleMode.Training => RuleMode.Calibration,
			_ => RuleMode.EightBall
		};
		ResetSessionForCurrentMode();
	}

	private void StartMenuSelection(RuleMode mode)
	{
		_ruleMode = mode;
		ResetSessionForCurrentMode();
		CloseMenu();
	}

	private void ResetCurrentModeFromMenu()
	{
		ResetSessionForCurrentMode();
		CloseMenu();
	}

	private void ReturnToStartMenu()
	{
		_menuVisible = true;
		_sessionStarted = false;
		_shotBannerSecondsRemaining = 0.0f;
		_shotBannerPanel.Visible = false;
		_shotBannerLabel.Visible = false;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add("Start menu opened.");
		UpdateMenuState();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleMenuVisibility()
	{
		if (_menuVisible)
		{
			if (_sessionStarted)
			{
				CloseMenu();
			}

			return;
		}

		_menuVisible = true;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add("Menu opened.");
		UpdateMenuState();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void CloseMenu()
	{
		_menuVisible = false;
		UpdateMenuState();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void UpdateMenuState()
	{
		_menuOverlay.Visible = _menuVisible;
		_menuPanel.Visible = _menuVisible;
		UpdateHudVisibility();

		if (!_menuVisible)
		{
			return;
		}

		var activeMode = GetRuleModeLabel();
		_menuSubtitleLabel.Text = _sessionStarted
			? "Pause the table, swap modes, or reset without digging through hotkeys. The portable core stays live underneath this menu."
			: "Choose how you want to play. Eight-ball uses the simple computer opponent; FreePlay leaves the whole table open for practice and layout work; Tuning mode calibrates the hardcoded table geometry.";
		_menuModeLabel.Text = _sessionStarted
			? $"Current mode: {activeMode}  |  Esc closes menu"
			: $"Start mode: {activeMode}  |  Keyboard: 1 = EightBall, 2 = FreePlay, 3 = Tuning";

		_menuResumeButton.Visible = _sessionStarted;
		_menuResumeButton.Disabled = !_sessionStarted;
		_menuResetButton.Visible = _sessionStarted;
		_menuResetButton.Disabled = !_sessionStarted;
		_menuReturnToMenuButton.Visible = _sessionStarted;
		_menuReturnToMenuButton.Disabled = !_sessionStarted;
	}

	private void ToggleHardcodeOverlay()
	{
		_hardcodeOverlayVisible = !_hardcodeOverlayVisible;
		UpdateOverlayVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_hardcodeOverlayVisible
			? "Hardcoded-table overlay visible."
			: "Hardcoded-table overlay hidden.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleOverlayLayer(string label, ref bool enabled)
	{
		enabled = !enabled;
		_hardcodeOverlayVisible = true;
		UpdateOverlayVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Overlay {label}: {(enabled ? "visible" : "hidden")}");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleDebugMode()
	{
		SetDebugModeEnabled(!_debugModeEnabled);
	}

	private void ToggleHelpPanel()
	{
		_helpPanelVisible = !_helpPanelVisible;
		UpdateAuxiliaryPanelVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_helpPanelVisible ? "Controls panel visible." : "Controls panel hidden.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleHudVisibility()
	{
		_hudVisible = !_hudVisible;
		UpdateHudVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_hudVisible ? "Gameplay HUD visible." : "Gameplay HUD hidden.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleTuningInfoVisibility()
	{
		_tuningInfoVisible = !_tuningInfoVisible;
		SyncCalibrationControls();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_tuningInfoVisible ? "Tuning window info visible." : "Tuning window info hidden.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void OnAimHeaderGuiInput(InputEvent @event)
	{
		if (_ruleMode != RuleMode.Calibration)
		{
			return;
		}

		if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.ButtonIndex == MouseButton.Left)
		{
			_draggingAimPanel = mouseButtonEvent.Pressed;
			if (_draggingAimPanel)
			{
				_aimPanelDragOffset = _aimPanel.Position - GetViewport().GetMousePosition();
			}

			GetViewport().SetInputAsHandled();
		}
	}

	private void HandleMouseButtonInput(InputEventMouseButton mouseButtonEvent)
	{
		if (_menuVisible || _world.Phase == SimulationPhase.Running || IsComputerTurnPending())
		{
			return;
		}

		var direction = mouseButtonEvent.ButtonIndex switch
		{
			MouseButton.WheelUp => -1,
			MouseButton.WheelDown => 1,
			_ => 0
		};

		if (direction == 0)
		{
			return;
		}

		if (_debugModeEnabled && mouseButtonEvent.CtrlPressed)
		{
			AdjustOverlayThickness(direction, mouseButtonEvent.ShiftPressed);
			return;
		}

		AdjustAimWithMouseWheel(direction, mouseButtonEvent.ShiftPressed);
	}

	private void SetDebugModeEnabled(bool enabled)
	{
		if (_debugModeEnabled == enabled)
		{
			UpdateAuxiliaryPanelVisibility();
			return;
		}

		_debugModeEnabled = enabled;
		UpdateAuxiliaryPanelVisibility();
		UpdateOverlayVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_debugModeEnabled
			? "Debug window opened. Move it to the second monitor if you want."
			: "Debug window closed.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void OnDebugWindowCloseRequested()
	{
		SetDebugModeEnabled(false);
	}

	private void OnTuningWindowCloseRequested()
	{
		SetWindowVisible(_tuningWindow, false);
		_menuVisible = true;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add("Tuning window closed. Reopen from the menu or switch back to tuning mode.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
		UpdateHudVisibility();
	}

}
