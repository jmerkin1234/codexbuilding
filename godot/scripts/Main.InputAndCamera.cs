using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void CycleTuningField(int direction)
	{
		var tuningFields = Enum.GetValues<DebugTuningField>();
		var currentIndex = Array.IndexOf(tuningFields, _selectedTuningField);
		if (currentIndex < 0)
		{
			currentIndex = 0;
		}

		var nextIndex = (currentIndex + direction + tuningFields.Length) % tuningFields.Length;
		_selectedTuningField = tuningFields[nextIndex];
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Debug tuning: {GetTuningFieldLabel(_selectedTuningField)}");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void AdjustSelectedTuning(int direction, bool coarse)
	{
		if (direction == 0)
		{
			return;
		}

		if (_world.Phase == SimulationPhase.Running || _shotCaptureActive)
		{
			_recentRuleNotes.Clear();
			_recentRuleNotes.Add("Stop balls before adjusting debug tuning.");
			UpdateStatusLabel(Array.Empty<ShotEvent>());
			return;
		}

		var stepScale = coarse ? 5.0f : 1.0f;
		var updatedConfig = _selectedTuningField switch
		{
			DebugTuningField.SlidingFriction => CreateAdjustedConfig(
				slidingFrictionAccelerationMetersPerSecondSquared: AdjustFloat(
					_config.SlidingFrictionAccelerationMetersPerSecondSquared,
					0.08f * stepScale * direction,
					0.0f,
					5.0f)),
			DebugTuningField.RollingFriction => CreateAdjustedConfig(
				rollingFrictionAccelerationMetersPerSecondSquared: AdjustFloat(
					_config.RollingFrictionAccelerationMetersPerSecondSquared,
					0.02f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.SpinDecay => CreateAdjustedConfig(
				spinDecayRpsPerSecond: AdjustFloat(
					_config.SpinDecayRpsPerSecond,
					0.05f * stepScale * direction,
					0.0f,
					5.0f)),
			DebugTuningField.SideSpinCurve => CreateAdjustedConfig(
				sideSpinCurveAccelerationMetersPerSecondSquaredPerRps: AdjustFloat(
					_config.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps,
					0.002f * stepScale * direction,
					0.0f,
					0.2f)),
			DebugTuningField.MovingSideSpinDecay => CreateAdjustedConfig(
				movingSideSpinDecayRpsPerSecondPerMetersPerSecond: AdjustFloat(
					_config.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond,
					0.05f * stepScale * direction,
					0.0f,
					5.0f)),
			DebugTuningField.BallRestitution => CreateAdjustedConfig(
				ballCollisionRestitution: AdjustFloat(
					_config.BallCollisionRestitution,
					0.01f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.BallTangentialTransfer => CreateAdjustedConfig(
				ballCollisionTangentialTransferFactor: AdjustFloat(
					_config.BallCollisionTangentialTransferFactor,
					0.02f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.BallSpinTransfer => CreateAdjustedConfig(
				ballCollisionSpinTransferFactor: AdjustFloat(
					_config.BallCollisionSpinTransferFactor,
					0.02f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.BallForwardSpinCarry => CreateAdjustedConfig(
				ballCollisionForwardSpinCarryFactor: AdjustFloat(
					_config.BallCollisionForwardSpinCarryFactor,
					0.02f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.RailRestitution => CreateAdjustedConfig(
				boundaryRestitution: AdjustFloat(
					_config.BoundaryRestitution,
					0.01f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.RailGlancingRestitution => CreateAdjustedConfig(
				boundaryGlancingRestitution: AdjustFloat(
					_config.BoundaryGlancingRestitution,
					0.01f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.RailTangentialRetention => CreateAdjustedConfig(
				boundaryTangentialVelocityRetention: AdjustFloat(
					_config.BoundaryTangentialVelocityRetention,
					0.01f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.RailTangentialFriction => CreateAdjustedConfig(
				boundaryTangentialFrictionFactor: AdjustFloat(
					_config.BoundaryTangentialFrictionFactor,
					0.02f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.RailEnglishTransfer => CreateAdjustedConfig(
				boundaryEnglishTransferFactor: AdjustFloat(
					_config.BoundaryEnglishTransferFactor,
					0.02f * stepScale * direction,
					0.0f,
					2.0f)),
			DebugTuningField.RailSpinTransfer => CreateAdjustedConfig(
				boundarySpinTransferFactor: AdjustFloat(
					_config.BoundarySpinTransferFactor,
					0.02f * stepScale * direction,
					0.0f,
					1.0f)),
			DebugTuningField.SettleThreshold => CreateAdjustedConfig(
				settleSpeedThresholdMetersPerSecond: AdjustFloat(
					_config.SettleSpeedThresholdMetersPerSecond,
					0.001f * stepScale * direction,
					0.0f,
					0.2f)),
			DebugTuningField.CollisionIterations => CreateAdjustedConfig(
				maxCollisionIterationsPerStep: AdjustInt(
					_config.MaxCollisionIterationsPerStep,
					coarse ? direction * 2 : direction,
					1,
					16)),
			DebugTuningField.BoundaryIterations => CreateAdjustedConfig(
				maxBoundaryIterationsPerStep: AdjustInt(
					_config.MaxBoundaryIterationsPerStep,
					coarse ? direction * 2 : direction,
					1,
					16)),
			_ => _config
		};

		if (ConfigsEquivalent(updatedConfig, _config))
		{
			return;
		}

		_config = updatedConfig;
		RebuildWorldWithCurrentState();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Tuned {GetTuningFieldLabel(_selectedTuningField)} -> {GetSelectedTuningValueText()}");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void AdjustOverlayThickness(int direction, bool coarse)
	{
		if (direction == 0)
		{
			return;
		}

		var step = coarse ? 0.25f : 0.1f;
		var updatedThickness = AdjustFloat(
			_overlayLineThicknessPixels,
			step * direction,
			MinOverlayThicknessPixels,
			MaxOverlayThicknessPixels);

		if (Mathf.IsEqualApprox(updatedThickness, _overlayLineThicknessPixels))
		{
			return;
		}

		_overlayLineThicknessPixels = updatedThickness;
		BuildHardcodeOverlay();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Overlay thickness: {_overlayLineThicknessPixels:0.0} px");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void AdjustAimWithMouseWheel(int direction, bool fineStep)
	{
		if (direction == 0)
		{
			return;
		}

		var stepDegrees = fineStep ? MouseWheelAimStepDegrees * 0.5f : MouseWheelAimStepDegrees;
		_aimAngleRadians += Mathf.DegToRad(stepDegrees * direction);
		MarkAimPreviewDirty();
		UpdateCueGuide();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void CycleCameraPreset()
	{
		_cameraPresetIndex = (_cameraPresetIndex + 1) % _cameraPresets.Length;
		ApplyCameraPreset();
		BuildHardcodeOverlay();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Camera preset: {GetActiveCameraPreset().Name}");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void AdjustCameraZoom(float delta)
	{
		_cameraZoomScale = Mathf.Clamp(_cameraZoomScale + delta, 0.65f, 1.85f);
		ApplyCameraPreset();
		BuildHardcodeOverlay();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Camera zoom: {_cameraZoomScale:0.00}x");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ApplyCameraPreset()
	{
		if (_camera == null)
		{
			return;
		}

		var preset = GetActiveCameraPreset();
		var center = GetTableCenter3D();
		if (preset.UseOrthographic)
		{
			_camera.Projection = Camera3D.ProjectionType.Orthogonal;
			_camera.Size = preset.ViewSize * _cameraZoomScale;
			_camera.Position = center + preset.Offset;
		}
		else
		{
			_camera.Projection = Camera3D.ProjectionType.Perspective;
			_camera.Fov = preset.FieldOfView;
			_camera.Position = center + (preset.Offset * _cameraZoomScale);
		}

		_camera.LookAt(center, Vector3.Up);
	}

	private CameraPreset GetActiveCameraPreset()
	{
		return _cameraPresets[_cameraPresetIndex];
	}

	private void SelectTrainingBall(int direction)
	{
		if ((_ruleMode != RuleMode.Training && _ruleMode != RuleMode.Calibration) || direction == 0)
		{
			return;
		}

		var selectable = Enumerable.Range(0, 16).ToArray();
		var currentIndex = Array.IndexOf(selectable, _trainingSelectedBallNumber);
		if (currentIndex < 0)
		{
			currentIndex = 0;
		}

		var nextIndex = (currentIndex + direction + selectable.Length) % selectable.Length;
		_trainingSelectedBallNumber = selectable[nextIndex];
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
			? $"Tuning selection: {GetTrainingSelectionLabel()}"
			: $"FreePlay selection: {GetTrainingSelectionLabel()}");
		MarkAimPreviewDirty();
		UpdateCueGuide();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void UpdatePlacementControls(float deltaSeconds)
	{
		if (IsComputerTurnPending() || !CanAdjustPlacement())
		{
			return;
		}

		var moveInput = Vector2.Zero;

		if (Input.IsKeyPressed(Key.Left))
		{
			moveInput.X -= 1.0f;
		}

		if (Input.IsKeyPressed(Key.Right))
		{
			moveInput.X += 1.0f;
		}

		if (Input.IsKeyPressed(Key.Up))
		{
			moveInput.Y -= 1.0f;
		}

		if (Input.IsKeyPressed(Key.Down))
		{
			moveInput.Y += 1.0f;
		}

		if (moveInput == Vector2.Zero)
		{
			return;
		}

		var placementBallNumber = GetPlacementBallNumber();
		var selectedBall = _world.Balls.FirstOrDefault(ball => ball.BallNumber == placementBallNumber);
		var currentPosition = GetPreferredPlacementPosition(selectedBall);
		var desiredPosition = currentPosition +
							  new NumericsVector2(moveInput.Normalized().X, moveInput.Normalized().Y) *
							  (CueBallPlacementMetersPerSecond * deltaSeconds);

		MoveBallToPlacement(placementBallNumber, desiredPosition, keepPocketed: false);
	}

	private void UpdateShotControls(float deltaSeconds)
	{
		if (IsComputerTurnPending() || !CanEditShot())
		{
			return;
		}

		var changed = false;

		if (Input.IsKeyPressed(Key.A))
		{
			_aimAngleRadians -= AimTurnRadiansPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.D))
		{
			_aimAngleRadians += AimTurnRadiansPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.W))
		{
			_strikeSpeedMetersPerSecond += StrikeSpeedAdjustPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.S))
		{
			_strikeSpeedMetersPerSecond -= StrikeSpeedAdjustPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.J))
		{
			_tipOffsetNormalized.X -= TipAdjustPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.L))
		{
			_tipOffsetNormalized.X += TipAdjustPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.I))
		{
			_tipOffsetNormalized.Y += TipAdjustPerSecond * deltaSeconds;
			changed = true;
		}

		if (Input.IsKeyPressed(Key.K))
		{
			_tipOffsetNormalized.Y -= TipAdjustPerSecond * deltaSeconds;
			changed = true;
		}

		_strikeSpeedMetersPerSecond = Mathf.Clamp(
			_strikeSpeedMetersPerSecond,
			MinimumStrikeSpeedMetersPerSecond,
			GetCurrentMaximumStrikeSpeedMetersPerSecond());
		_tipOffsetNormalized = _tipOffsetNormalized.LimitLength(1.0f);

		if (changed)
		{
			MarkAimPreviewDirty();
		}
	}

	private void TryShoot()
	{
		var shot = new ShotInput(
			new NumericsVector2(Mathf.Cos(_aimAngleRadians), Mathf.Sin(_aimAngleRadians)),
			_strikeSpeedMetersPerSecond,
			new NumericsVector2(_tipOffsetNormalized.X, _tipOffsetNormalized.Y));

		ExecuteShot(
			shot,
			GetShotStartedNote(),
			GetShotStartedNote().TrimEnd('.'));
	}


}
