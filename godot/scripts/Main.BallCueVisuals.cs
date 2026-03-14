using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void SyncBallVisuals(IReadOnlyList<BallState> balls, float deltaSeconds = 0.0f)
	{
		var ballRadiusMeters = _tableSpec.BallDiameterMeters * 0.5f;
		BallState? selectedTrainingBall = null;

		foreach (var ball in balls)
		{
			if (!_ballVisuals.TryGetValue(ball.BallNumber, out var ballNode))
			{
				continue;
			}

			var wasVisible = ballNode.Visible;
			ballNode.Visible = !ball.IsPocketed;
			if (ball.IsPocketed)
			{
				continue;
			}

			var targetPosition = ToGodotPoint(ball.Position, ballRadiusMeters);
			if (!_ballVisualLastPositions.TryGetValue(ball.BallNumber, out var previousPosition))
			{
				previousPosition = targetPosition;
			}

			var motion = targetPosition - previousPosition;
			var motionDistance = motion.Length();
			if (!wasVisible || motionDistance >= BallVisualTeleportResetMeters)
			{
				if (_ballVisualBaseRotations.TryGetValue(ball.BallNumber, out var baseRotation))
				{
					ballNode.Quaternion = baseRotation;
				}
			}
			else
			{
				ApplyBallVisualRotation(ballNode, ball, motion, motionDistance, ballRadiusMeters, deltaSeconds);
			}

			ballNode.Position = targetPosition;
			ballNode.Scale = Vector3.One;
			_ballVisualLastPositions[ball.BallNumber] = targetPosition;

			if (_ruleMode == RuleMode.Training && ball.BallNumber == _trainingSelectedBallNumber)
			{
				selectedTrainingBall = ball;
			}
		}

		UpdateTrainingSelectionHighlight(selectedTrainingBall, ballRadiusMeters);
	}

	private static void ApplyBallVisualRotation(
		Node3D ballNode,
		BallState ball,
		Vector3 motion,
		float motionDistance,
		float ballRadiusMeters,
		float deltaSeconds)
	{
		if (motionDistance > 0.00001f && ballRadiusMeters > 0.00001f)
		{
			var motionDirection = motion / motionDistance;
			var rollAxis = Vector3.Up.Cross(motionDirection);
			if (rollAxis.LengthSquared() > 0.000001f)
			{
				var rollRotation = new Quaternion(rollAxis.Normalized(), motionDistance / ballRadiusMeters);
				ballNode.Quaternion = rollRotation * ballNode.Quaternion;
			}
		}

		if (deltaSeconds <= 0.0f || Mathf.Abs(ball.Spin.SideSpinRps) <= 0.0001f)
		{
			return;
		}

		var sideSpinRotation = new Quaternion(Vector3.Up, ball.Spin.SideSpinRps * Mathf.Tau * deltaSeconds);
		ballNode.Quaternion = sideSpinRotation * ballNode.Quaternion;
	}

	private void UpdateCueGuide()
	{
		if (!CanEditShot())
		{
			_cueGuide.Visible = false;
			if (_importedCueStick != null)
			{
				_importedCueStick.Visible = false;
			}
			HideAimPreviewGuides();
			return;
		}

		var cueBall = _world.Balls.First(ball => ball.Kind == BallKind.Cue && !ball.IsPocketed);
		var start = ToGodotPoint(cueBall.Position, (_tableSpec.BallDiameterMeters * 0.5f) + 0.012f);
		var aimDirection = new Vector3(Mathf.Cos(_aimAngleRadians), 0.0f, Mathf.Sin(_aimAngleRadians));
		var speedNormalized = Mathf.InverseLerp(
			MinimumStrikeSpeedMetersPerSecond,
			GetCurrentMaximumStrikeSpeedMetersPerSecond(),
			_strikeSpeedMetersPerSecond);
		var defaultSpeedNormalized = Mathf.InverseLerp(
			MinimumStrikeSpeedMetersPerSecond,
			GetCurrentMaximumStrikeSpeedMetersPerSecond(),
			DefaultStrikeSpeedMetersPerSecond);
		var visualPullbackNormalized = speedNormalized <= defaultSpeedNormalized || defaultSpeedNormalized >= 0.999f
			? 0.0f
			: (speedNormalized - defaultSpeedNormalized) / (1.0f - defaultSpeedNormalized);
		var guideLength = 0.45f + (speedNormalized * 0.82f);
		var end = start + (aimDirection * guideLength);
		var midpoint = (start + end) * 0.5f;

		if (_importedCueStick != null)
		{
			var cueBallLookPoint = new Vector3(start.X, _cueStickHeightMeters, start.Z);
			var cueStickPosition = cueBallLookPoint - (aimDirection * (_cueStickBaseOffsetMeters + (visualPullbackNormalized * CueStickPowerPullbackMeters)));
			_importedCueStick.Visible = true;
			_importedCueStick.Position = cueStickPosition;
			_importedCueStick.LookAt(cueBallLookPoint, Vector3.Up);
			_importedCueStick.Quaternion *= _cueStickLookCorrection;
			_cueGuide.Visible = false;
		}
		else
		{
			_cueGuide.Visible = true;
			_cueGuide.Position = midpoint;
			((BoxMesh)_cueGuide.Mesh!).Size = new Vector3(CueGuideThicknessMeters, CueGuideHeightMeters, guideLength);
			_cueGuide.LookAt(midpoint + aimDirection, Vector3.Up);
		}

		UpdateAimPreviewGuides();
	}

	private static bool TryResolveCueStickTipOffsetMeters(
		Node3D cueStickRoot,
		Quaternion originalCueRotation,
		Vector3 cueToBallDirection,
		out float tipOffsetMeters)
	{
		tipOffsetMeters = 0.0f;
		if (cueToBallDirection.LengthSquared() <= 0.000001f)
		{
			return false;
		}

		var normalizedDirection = cueToBallDirection.Normalized();
		var candidateAxes = new[]
		{
			Vector3.Right,
			Vector3.Left,
			Vector3.Up,
			Vector3.Down,
			Vector3.Forward,
			Vector3.Back
		};

		var bestAxis = Vector3.Forward;
		var bestDot = float.NegativeInfinity;
		foreach (var axis in candidateAxes)
		{
			var rotatedAxis = originalCueRotation * axis;
			var alignment = rotatedAxis.Normalized().Dot(normalizedDirection);
			if (alignment > bestDot)
			{
				bestDot = alignment;
				bestAxis = axis;
			}
		}

		var foundGeometry = false;
		var maxProjection = float.NegativeInfinity;
		CollectCueTipProjection(cueStickRoot, Transform3D.Identity, bestAxis, ref foundGeometry, ref maxProjection);
		if (!foundGeometry || maxProjection <= 0.0f)
		{
			return false;
		}

		tipOffsetMeters = maxProjection;
		return true;
	}

	private static void CollectCueTipProjection(
		Node node,
		Transform3D toCueLocal,
		Vector3 tipAxisLocal,
		ref bool foundGeometry,
		ref float maxProjection)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			var aabb = meshInstance.GetAabb();
			foreach (var corner in GetAabbCorners(aabb))
			{
				var cornerInCueSpace = toCueLocal * corner;
				maxProjection = Mathf.Max(maxProjection, tipAxisLocal.Dot(cornerInCueSpace));
				foundGeometry = true;
			}
		}

		foreach (var child in node.GetChildren())
		{
			if (child is not Node3D childNode)
			{
				continue;
			}

			CollectCueTipProjection(childNode, toCueLocal * childNode.Transform, tipAxisLocal, ref foundGeometry, ref maxProjection);
		}
	}

	private static IEnumerable<Vector3> GetAabbCorners(Aabb aabb)
	{
		var min = aabb.Position;
		var max = aabb.Position + aabb.Size;

		yield return new Vector3(min.X, min.Y, min.Z);
		yield return new Vector3(max.X, min.Y, min.Z);
		yield return new Vector3(min.X, max.Y, min.Z);
		yield return new Vector3(max.X, max.Y, min.Z);
		yield return new Vector3(min.X, min.Y, max.Z);
		yield return new Vector3(max.X, min.Y, max.Z);
		yield return new Vector3(min.X, max.Y, max.Z);
		yield return new Vector3(max.X, max.Y, max.Z);
	}


}
