using System;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{
	private readonly record struct CameraPreset(string Name, Vector3 Offset, bool UseOrthographic, float ViewSize, float FieldOfView);

	private readonly record struct ShotBannerStyle(Color BackgroundColor, Color BorderColor, Color TextColor);

	private enum DebugTuningField
	{
		SlidingFriction,
		RollingFriction,
		SpinDecay,
		SideSpinCurve,
		MovingSideSpinDecay,
		BallRestitution,
		BallTangentialTransfer,
		BallSpinTransfer,
		BallForwardSpinCarry,
		RailRestitution,
		RailGlancingRestitution,
		RailTangentialRetention,
		RailTangentialFriction,
		RailEnglishTransfer,
		RailSpinTransfer,
		SettleThreshold,
		CollisionIterations,
		BoundaryIterations
	}

	private enum RuleMode
	{
		EightBall,
		Training,
		Calibration
	}

	private sealed class AimPreviewResult
	{
		public AimPreviewResult(
			NumericsVector2 cueStart,
			NumericsVector2 primaryCueEnd,
			NumericsVector2? secondaryCueEnd,
			NumericsVector2? targetStart,
			NumericsVector2? targetEnd)
		{
			CueStart = cueStart;
			PrimaryCueEnd = primaryCueEnd;
			SecondaryCueEnd = secondaryCueEnd;
			TargetStart = targetStart;
			TargetEnd = targetEnd;
		}

		public NumericsVector2 CueStart { get; }

		public NumericsVector2 PrimaryCueEnd { get; }

		public NumericsVector2? SecondaryCueEnd { get; }

		public NumericsVector2? TargetStart { get; }

		public NumericsVector2? TargetEnd { get; }
	}

	private sealed class TuningFieldRow
	{
		public TuningFieldRow(
			int fieldIndex,
			PanelContainer panel,
			Label rowLabel,
			HSlider slider,
			Label valueLabel)
		{
			FieldIndex = fieldIndex;
			Panel = panel;
			RowLabel = rowLabel;
			Slider = slider;
			ValueLabel = valueLabel;
		}

		public int FieldIndex { get; }

		public PanelContainer Panel { get; }

		public Label RowLabel { get; }

		public HSlider Slider { get; }

		public Label ValueLabel { get; }
	}

	private sealed class TuningMiniPanel
	{
		public TuningMiniPanel(
			int fieldIndex,
			PanelContainer panel,
			Label label,
			HSlider slider,
			Label valueLabel)
		{
			FieldIndex = fieldIndex;
			Panel = panel;
			Label = label;
			Slider = slider;
			ValueLabel = valueLabel;
		}

		public int FieldIndex { get; }

		public PanelContainer Panel { get; }

		public Label Label { get; }

		public HSlider Slider { get; }

		public Label ValueLabel { get; }
	}

	private sealed class CalibrationField
	{
		public CalibrationField(
			string section,
			string objectKey,
			string objectLabel,
			string label,
			string overlayTarget,
			float minimum,
			float maximum,
			float fineStep,
			float coarseStep,
			Func<float> getter,
			Action<float> setter)
		{
			Section = section;
			ObjectKey = objectKey;
			ObjectLabel = objectLabel;
			Label = label;
			OverlayTarget = overlayTarget;
			Minimum = minimum;
			Maximum = maximum;
			FineStep = fineStep;
			CoarseStep = coarseStep;
			Getter = getter;
			Setter = setter;
		}

		public string Section { get; }

		public string ObjectKey { get; }

		public string ObjectLabel { get; }

		public string Label { get; }

		public string OverlayTarget { get; }

		public float Minimum { get; }

		public float Maximum { get; }

		public float FineStep { get; }

		public float CoarseStep { get; }

		private Func<float> Getter { get; }

		private Action<float> Setter { get; }

		public float GetValue()
		{
			return Getter();
		}

		public void SetValue(float value)
		{
			Setter(value);
		}

		public string GetFormattedValue(TableCalibrationProfile profile)
		{
			return $"{GetValue():0.0000}";
		}
	}

	private readonly record struct CalibrationObjectEntry(string Key, string Label);

	private readonly record struct ComputerShotCandidate(
		int TargetBallNumber,
		ShotInput Shot);

	private readonly record struct ComputerShotPlan(
		NumericsVector2? CueBallPlacement,
		ShotInput Shot,
		float Score,
		string Description);
}
