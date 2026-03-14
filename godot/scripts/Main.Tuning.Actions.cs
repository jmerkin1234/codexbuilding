using System;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{
	private void CycleCalibrationField(int direction, bool sectionOnly)
	{
		if (_calibrationFields.Count == 0 || direction == 0)
		{
			return;
		}

		var currentIndex = _selectedCalibrationFieldIndex;
		var currentSection = _calibrationFields[currentIndex].Section;

		for (var steps = 0; steps < _calibrationFields.Count; steps++)
		{
			currentIndex = (currentIndex + direction + _calibrationFields.Count) % _calibrationFields.Count;
			if (!sectionOnly || _calibrationFields[currentIndex].Section != currentSection)
			{
				_selectedCalibrationFieldIndex = currentIndex;
				_recentRuleNotes.Clear();
				_recentRuleNotes.Add($"Tuning field: {GetSelectedCalibrationField().Label}");
				BuildHardcodeOverlay();
				UpdateStatusLabel(Array.Empty<ShotEvent>());
				return;
			}
		}
	}

	private void AdjustSelectedCalibrationField(int direction, bool coarse)
	{
		if (_calibrationFields.Count == 0 || direction == 0)
		{
			return;
		}

		if (_world.Phase == SimulationPhase.Running || _shotCaptureActive)
		{
			_recentRuleNotes.Clear();
			_recentRuleNotes.Add("Stop balls before adjusting tuning mode values.");
			UpdateStatusLabel(Array.Empty<ShotEvent>());
			return;
		}

		var field = GetSelectedCalibrationField();
		var currentValue = field.GetValue();
		var step = coarse ? field.CoarseStep : field.FineStep;
		var updatedValue = Mathf.Clamp(currentValue + (step * direction), field.Minimum, field.Maximum);
		if (Mathf.IsEqualApprox(currentValue, updatedValue))
		{
			return;
		}

		field.SetValue(updatedValue);
		ApplyCalibrationProfile($"Tuned {field.Label} -> {field.GetFormattedValue(_tableCalibrationProfile)}");
	}

	private void ApplyCalibrationProfile(string note)
	{
		_tableSpec = TableCalibrationBuilder.Apply(_baseTableSpec, _tableCalibrationProfile);
		BuildHardcodeOverlay();
		RebuildWorldWithCurrentState();
		ResetShotSummary();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(note);
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void SaveCalibrationProfile()
	{
		_tableCalibrationProfile.Save(GetCalibrationProfileAbsolutePath());
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Saved tuning profile to {CalibrationProfilePath}");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ReloadCalibrationProfile()
	{
		_tableCalibrationProfile = TableCalibrationProfile.LoadOrDefault(GetCalibrationProfileAbsolutePath(), _baseTableSpec);
		BuildCalibrationFields();
		PopulateCalibrationFieldSelector();
		ApplyCalibrationProfile("Reloaded tuning profile.");
	}

	private void ResetCalibrationProfile()
	{
		_tableCalibrationProfile = TableCalibrationProfile.CreateDefault(_baseTableSpec);
		BuildCalibrationFields();
		PopulateCalibrationFieldSelector();
		ApplyCalibrationProfile("Reset tuning profile to source values.");
	}

	private void RebuildWorldWithCurrentState()
	{
		var preservedBalls = _world.Balls.ToArray();
		_world = new SimulationWorld(_tableSpec, _config, preservedBalls);
		MarkAimPreviewDirty();
		SyncBallVisuals(_world.Balls);
		UpdateCueGuide();
		SyncCalibrationControls();
	}

	private SimulationConfig CreateAdjustedConfig(
		float? settleSpeedThresholdMetersPerSecond = null,
		float? slidingFrictionAccelerationMetersPerSecondSquared = null,
		float? rollingFrictionAccelerationMetersPerSecondSquared = null,
		float? spinDecayRpsPerSecond = null,
		float? sideSpinCurveAccelerationMetersPerSecondSquaredPerRps = null,
		float? movingSideSpinDecayRpsPerSecondPerMetersPerSecond = null,
		float? ballCollisionRestitution = null,
		float? ballCollisionTangentialTransferFactor = null,
		float? ballCollisionSpinTransferFactor = null,
		float? ballCollisionForwardSpinCarryFactor = null,
		int? maxCollisionIterationsPerStep = null,
		float? boundaryRestitution = null,
		float? boundaryGlancingRestitution = null,
		float? boundaryTangentialVelocityRetention = null,
		float? boundaryTangentialFrictionFactor = null,
		float? boundaryEnglishTransferFactor = null,
		float? boundarySpinTransferFactor = null,
		int? maxBoundaryIterationsPerStep = null)
	{
		return new SimulationConfig(
			fixedStepSeconds: _config.FixedStepSeconds,
			settleSpeedThresholdMetersPerSecond: settleSpeedThresholdMetersPerSecond ?? _config.SettleSpeedThresholdMetersPerSecond,
			maxFixedStepsPerAdvance: _config.MaxFixedStepsPerAdvance,
			maxSideSpinRps: _config.MaxSideSpinRps,
			maxFollowSpinRps: _config.MaxFollowSpinRps,
			maxDrawSpinRps: _config.MaxDrawSpinRps,
			slidingFrictionAccelerationMetersPerSecondSquared: slidingFrictionAccelerationMetersPerSecondSquared ?? _config.SlidingFrictionAccelerationMetersPerSecondSquared,
			rollingFrictionAccelerationMetersPerSecondSquared: rollingFrictionAccelerationMetersPerSecondSquared ?? _config.RollingFrictionAccelerationMetersPerSecondSquared,
			spinDecayRpsPerSecond: spinDecayRpsPerSecond ?? _config.SpinDecayRpsPerSecond,
			sideSpinCurveAccelerationMetersPerSecondSquaredPerRps: sideSpinCurveAccelerationMetersPerSecondSquaredPerRps ?? _config.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps,
			movingSideSpinDecayRpsPerSecondPerMetersPerSecond: movingSideSpinDecayRpsPerSecondPerMetersPerSecond ?? _config.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond,
			rollingMatchToleranceMetersPerSecond: _config.RollingMatchToleranceMetersPerSecond,
			spinSettleThresholdRps: _config.SpinSettleThresholdRps,
			ballCollisionRestitution: ballCollisionRestitution ?? _config.BallCollisionRestitution,
			ballCollisionTangentialTransferFactor: ballCollisionTangentialTransferFactor ?? _config.BallCollisionTangentialTransferFactor,
			ballCollisionSpinTransferFactor: ballCollisionSpinTransferFactor ?? _config.BallCollisionSpinTransferFactor,
			ballCollisionForwardSpinCarryFactor: ballCollisionForwardSpinCarryFactor ?? _config.BallCollisionForwardSpinCarryFactor,
			maxCollisionIterationsPerStep: maxCollisionIterationsPerStep ?? _config.MaxCollisionIterationsPerStep,
			boundaryRestitution: boundaryRestitution ?? _config.BoundaryRestitution,
			boundaryGlancingRestitution: boundaryGlancingRestitution ?? _config.BoundaryGlancingRestitution,
			boundaryTangentialVelocityRetention: boundaryTangentialVelocityRetention ?? _config.BoundaryTangentialVelocityRetention,
			boundaryTangentialFrictionFactor: boundaryTangentialFrictionFactor ?? _config.BoundaryTangentialFrictionFactor,
			boundaryEnglishTransferFactor: boundaryEnglishTransferFactor ?? _config.BoundaryEnglishTransferFactor,
			boundarySpinTransferFactor: boundarySpinTransferFactor ?? _config.BoundarySpinTransferFactor,
			maxBoundaryIterationsPerStep: maxBoundaryIterationsPerStep ?? _config.MaxBoundaryIterationsPerStep);
	}

	private string GetSelectedTuningValueText()
	{
		return _selectedTuningField switch
		{
			DebugTuningField.SlidingFriction => $"{_config.SlidingFrictionAccelerationMetersPerSecondSquared:0.000}",
			DebugTuningField.RollingFriction => $"{_config.RollingFrictionAccelerationMetersPerSecondSquared:0.000}",
			DebugTuningField.SpinDecay => $"{_config.SpinDecayRpsPerSecond:0.000}",
			DebugTuningField.SideSpinCurve => $"{_config.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps:0.0000}",
			DebugTuningField.MovingSideSpinDecay => $"{_config.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond:0.000}",
			DebugTuningField.BallRestitution => $"{_config.BallCollisionRestitution:0.000}",
			DebugTuningField.BallTangentialTransfer => $"{_config.BallCollisionTangentialTransferFactor:0.000}",
			DebugTuningField.BallSpinTransfer => $"{_config.BallCollisionSpinTransferFactor:0.000}",
			DebugTuningField.BallForwardSpinCarry => $"{_config.BallCollisionForwardSpinCarryFactor:0.000}",
			DebugTuningField.RailRestitution => $"{_config.BoundaryRestitution:0.000}",
			DebugTuningField.RailGlancingRestitution => $"{_config.BoundaryGlancingRestitution:0.000}",
			DebugTuningField.RailTangentialRetention => $"{_config.BoundaryTangentialVelocityRetention:0.000}",
			DebugTuningField.RailTangentialFriction => $"{_config.BoundaryTangentialFrictionFactor:0.000}",
			DebugTuningField.RailEnglishTransfer => $"{_config.BoundaryEnglishTransferFactor:0.000}",
			DebugTuningField.RailSpinTransfer => $"{_config.BoundarySpinTransferFactor:0.000}",
			DebugTuningField.SettleThreshold => $"{_config.SettleSpeedThresholdMetersPerSecond:0.0000}",
			DebugTuningField.CollisionIterations => _config.MaxCollisionIterationsPerStep.ToString(),
			DebugTuningField.BoundaryIterations => _config.MaxBoundaryIterationsPerStep.ToString(),
			_ => "n/a"
		};
	}

	private static string GetTuningFieldLabel(DebugTuningField field)
	{
		return field switch
		{
			DebugTuningField.SlidingFriction => "Slide Friction",
			DebugTuningField.RollingFriction => "Roll Friction",
			DebugTuningField.SpinDecay => "Spin Decay",
			DebugTuningField.SideSpinCurve => "Side-Spin Curve",
			DebugTuningField.MovingSideSpinDecay => "Moving Side-Spin Decay",
			DebugTuningField.BallRestitution => "Ball Restitution",
			DebugTuningField.BallTangentialTransfer => "Ball Tangential Transfer",
			DebugTuningField.BallSpinTransfer => "Ball Spin Transfer",
			DebugTuningField.BallForwardSpinCarry => "Ball Follow/Draw Carry",
			DebugTuningField.RailRestitution => "Rail Restitution",
			DebugTuningField.RailGlancingRestitution => "Rail Glancing Restitution",
			DebugTuningField.RailTangentialRetention => "Rail Tangential Retention",
			DebugTuningField.RailTangentialFriction => "Rail Tangential Friction",
			DebugTuningField.RailEnglishTransfer => "Rail English Transfer",
			DebugTuningField.RailSpinTransfer => "Rail Spin Transfer",
			DebugTuningField.SettleThreshold => "Settle Threshold",
			DebugTuningField.CollisionIterations => "Pair Iterations",
			DebugTuningField.BoundaryIterations => "Rail Iterations",
			_ => field.ToString()
		};
	}

	private static bool ConfigsEquivalent(SimulationConfig left, SimulationConfig right)
	{
		return left.FixedStepSeconds == right.FixedStepSeconds &&
			   left.SettleSpeedThresholdMetersPerSecond == right.SettleSpeedThresholdMetersPerSecond &&
			   left.MaxFixedStepsPerAdvance == right.MaxFixedStepsPerAdvance &&
			   left.MaxSideSpinRps == right.MaxSideSpinRps &&
			   left.MaxFollowSpinRps == right.MaxFollowSpinRps &&
			   left.MaxDrawSpinRps == right.MaxDrawSpinRps &&
			   left.SlidingFrictionAccelerationMetersPerSecondSquared == right.SlidingFrictionAccelerationMetersPerSecondSquared &&
			   left.RollingFrictionAccelerationMetersPerSecondSquared == right.RollingFrictionAccelerationMetersPerSecondSquared &&
			   left.SpinDecayRpsPerSecond == right.SpinDecayRpsPerSecond &&
			   left.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps == right.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps &&
			   left.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond == right.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond &&
			   left.RollingMatchToleranceMetersPerSecond == right.RollingMatchToleranceMetersPerSecond &&
			   left.SpinSettleThresholdRps == right.SpinSettleThresholdRps &&
			   left.BallCollisionRestitution == right.BallCollisionRestitution &&
			   left.BallCollisionTangentialTransferFactor == right.BallCollisionTangentialTransferFactor &&
			   left.BallCollisionSpinTransferFactor == right.BallCollisionSpinTransferFactor &&
			   left.BallCollisionForwardSpinCarryFactor == right.BallCollisionForwardSpinCarryFactor &&
			   left.MaxCollisionIterationsPerStep == right.MaxCollisionIterationsPerStep &&
			   left.BoundaryRestitution == right.BoundaryRestitution &&
			   left.BoundaryGlancingRestitution == right.BoundaryGlancingRestitution &&
			   left.BoundaryTangentialVelocityRetention == right.BoundaryTangentialVelocityRetention &&
			   left.BoundaryTangentialFrictionFactor == right.BoundaryTangentialFrictionFactor &&
			   left.BoundaryEnglishTransferFactor == right.BoundaryEnglishTransferFactor &&
			   left.BoundarySpinTransferFactor == right.BoundarySpinTransferFactor &&
			   left.MaxBoundaryIterationsPerStep == right.MaxBoundaryIterationsPerStep;
	}

	private static float AdjustFloat(float value, float delta, float min, float max)
	{
		return Mathf.Clamp(value + delta, min, max);
	}

	private static int AdjustInt(int value, int delta, int min, int max)
	{
		return Math.Clamp(value + delta, min, max);
	}
}
