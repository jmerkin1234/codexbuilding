using CodexBuilding.Billiards.Core.Simulation;
using Godot;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{
	public override void _UnhandledInput(InputEvent @event)
	{
		if (_draggingAimPanel)
		{
			if (@event is InputEventMouseMotion)
			{
				var viewportSize = GetViewport().GetVisibleRect().Size;
				var desiredPosition = GetViewport().GetMousePosition() + _aimPanelDragOffset;
				_aimPanel.Position = new Vector2(
					Mathf.Clamp(desiredPosition.X, 0.0f, Mathf.Max(0.0f, viewportSize.X - _aimPanel.Size.X)),
					Mathf.Clamp(desiredPosition.Y, 0.0f, Mathf.Max(0.0f, viewportSize.Y - _aimPanel.Size.Y)));
				return;
			}

			if (@event is InputEventMouseButton dragButtonEvent &&
				dragButtonEvent.ButtonIndex == MouseButton.Left &&
				!dragButtonEvent.Pressed)
			{
				_draggingAimPanel = false;
				return;
			}
		}

		if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
		{
			HandleMouseButtonInput(mouseButtonEvent);
			return;
		}

		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.Keycode == Key.Escape)
		{
			ToggleMenuVisibility();
			return;
		}

		switch (keyEvent.Keycode)
		{
			case Key.F1:
				ToggleDebugMode();
				return;
			case Key.F6:
				ToggleHelpPanel();
				return;
			case Key.F7:
				ToggleHudVisibility();
				return;
			case Key.H:
				ToggleHardcodeOverlay();
				return;
			case Key.C:
				CycleCameraPreset();
				return;
			case Key.F2:
				if (_debugModeEnabled)
				{
					CycleTuningField(-1);
				}
				return;
			case Key.F3:
				if (_debugModeEnabled)
				{
					CycleTuningField(1);
				}
				return;
			case Key.F4:
				if (_debugModeEnabled)
				{
					AdjustSelectedTuning(-1, keyEvent.ShiftPressed);
				}
				return;
			case Key.F5:
				if (_debugModeEnabled)
				{
					AdjustSelectedTuning(1, keyEvent.ShiftPressed);
				}
				return;
			case Key.Q:
				AdjustCameraZoom(-0.1f);
				return;
			case Key.E:
				AdjustCameraZoom(0.1f);
				return;
			case Key.Key1:
				ToggleOverlayLayer("Cloth", ref _overlayClothVisible);
				return;
			case Key.Key2:
				ToggleOverlayLayer("Cushions", ref _overlayCushionVisible);
				return;
			case Key.Key3:
				ToggleOverlayLayer("Jaws", ref _overlayJawVisible);
				return;
			case Key.Key4:
				ToggleOverlayLayer("Pockets", ref _overlayPocketVisible);
				return;
			case Key.Key5:
				ToggleOverlayLayer("Spots", ref _overlaySpotVisible);
				return;
		}

		if (_menuVisible)
		{
			switch (keyEvent.Keycode)
			{
				case Key.Key1:
					StartMenuSelection(RuleMode.EightBall);
					break;
				case Key.Key2:
					StartMenuSelection(RuleMode.Training);
					break;
				case Key.Key3:
					StartMenuSelection(RuleMode.Calibration);
					break;
			}

			return;
		}

		if (_world.Phase == SimulationPhase.Running)
		{
			return;
		}

		if (_ruleMode == RuleMode.Calibration)
		{
			switch (keyEvent.Keycode)
			{
				case Key.P:
					SaveCalibrationProfile();
					return;
				case Key.O:
					ReloadCalibrationProfile();
					return;
				case Key.U:
					ResetCalibrationProfile();
					return;
			}
		}

		switch (keyEvent.Keycode)
		{
			case Key.Space:
				if (!IsComputerTurnPending())
				{
					TryShoot();
				}
				break;
			case Key.R:
				ResetSessionForCurrentMode();
				break;
			case Key.Backspace:
				if (!IsComputerTurnPending())
				{
					_tipOffsetNormalized = Vector2.Zero;
					UpdateCueGuide();
					UpdateStatusLabel(System.Array.Empty<ShotEvent>());
				}
				break;
			case Key.Tab:
				ToggleRuleMode();
				break;
			case Key.Z:
				SelectTrainingBall(-1);
				break;
			case Key.X:
				SelectTrainingBall(1);
				break;
		}
	}
}
