using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void AddOverlaySegment(
		Node3D parent,
		string name,
		NumericsVector2 start,
		NumericsVector2 end,
		Color color,
		float height)
	{
		var segmentNode = new MeshInstance3D
		{
			Name = name,
			Mesh = new BoxMesh(),
			MaterialOverride = CreateGuideMaterial(color),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};

		parent.AddChild(segmentNode);
		SetOverlaySegment(segmentNode, start, end, height);
	}

	private void AddOverlayCircle(
		Node3D parent,
		string namePrefix,
		NumericsVector2 center,
		float radius,
		Color color,
		float height)
	{
		for (var segmentIndex = 0; segmentIndex < OverlayPocketSegments; segmentIndex++)
		{
			var startAngle = (Mathf.Tau / OverlayPocketSegments) * segmentIndex;
			var endAngle = (Mathf.Tau / OverlayPocketSegments) * (segmentIndex + 1);
			var start = center + new NumericsVector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
			var end = center + new NumericsVector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;

			AddOverlaySegment(parent, $"{namePrefix}_{segmentIndex:00}", start, end, color, height);
		}
	}

	private void AddOverlayCross(
		Node3D parent,
		string namePrefix,
		NumericsVector2 center,
		float armLength,
		Color color,
		float height)
	{
		AddOverlaySegment(
			parent,
			$"{namePrefix}_Horizontal",
			center + new NumericsVector2(-armLength, 0.0f),
			center + new NumericsVector2(armLength, 0.0f),
			color,
			height);
		AddOverlaySegment(
			parent,
			$"{namePrefix}_Vertical",
			center + new NumericsVector2(0.0f, -armLength),
			center + new NumericsVector2(0.0f, armLength),
			color,
			height);
	}

	private void MarkAimPreviewDirty()
	{
		_aimPreviewDirty = true;
		_cachedAimPreview = null;
	}

	private NumericsVector2 GetPreferredPlacementPosition(BallState selectedBall)
	{
		if (!selectedBall.IsPocketed)
		{
			return selectedBall.Position;
		}

		return selectedBall.BallNumber switch
		{
			0 => _tableSpec.CueBallSpawn,
			8 => _tableSpec.RackApexSpot,
			_ => _tableSpec.RackApexSpot
		};
	}

	private string GetTrainingSelectionLabel()
	{
		return _trainingSelectedBallNumber == 0 ? "CueBall" : $"Ball_{_trainingSelectedBallNumber:00}";
	}

	private void UpdateAimPreviewGuides()
	{
		var preview = GetOrBuildAimPreview();
		if (preview == null)
		{
			HideAimPreviewGuides();
			return;
		}

		SetGuideSegment(
			_aimPrimaryGuide,
			preview.CueStart,
			preview.PrimaryCueEnd,
			height: (_tableSpec.BallDiameterMeters * 0.5f) + 0.006f);

		if (preview.SecondaryCueEnd.HasValue)
		{
			SetGuideSegment(
				_aimSecondaryGuide,
				preview.PrimaryCueEnd,
				preview.SecondaryCueEnd.Value,
				height: (_tableSpec.BallDiameterMeters * 0.5f) + 0.012f);
		}
		else
		{
			_aimSecondaryGuide.Visible = false;
		}

		if (preview.TargetStart.HasValue && preview.TargetEnd.HasValue)
		{
			SetGuideSegment(
				_aimTargetGuide,
				preview.TargetStart.Value,
				preview.TargetEnd.Value,
				height: (_tableSpec.BallDiameterMeters * 0.5f) + 0.018f);
		}
		else
		{
			_aimTargetGuide.Visible = false;
		}
	}

	private AimPreviewResult? GetOrBuildAimPreview()
	{
		if (!CanEditShot())
		{
			return null;
		}

		if (!_aimPreviewDirty)
		{
			return _cachedAimPreview;
		}

		_cachedAimPreview = BuildAimPreview();
		_aimPreviewDirty = false;
		return _cachedAimPreview;
	}

	private AimPreviewResult? BuildAimPreview()
	{
		try
		{
			var shotInput = new ShotInput(
				new NumericsVector2(Mathf.Cos(_aimAngleRadians), Mathf.Sin(_aimAngleRadians)),
				_strikeSpeedMetersPerSecond,
				new NumericsVector2(_tipOffsetNormalized.X, _tipOffsetNormalized.Y));
			var previewWorld = new SimulationWorld(_tableSpec, _config, _world.Balls.ToArray());
			previewWorld.ApplyCueStrike(shotInput);

			var startCueBall = previewWorld.Balls.First(ball => ball.BallNumber == 0);
			var cueStart = startCueBall.Position;
			var primaryCueEnd = cueStart;
			NumericsVector2? secondaryCueEnd = null;
			NumericsVector2? targetStart = null;
			NumericsVector2? targetEnd = null;
			int? targetBallNumber = null;
			var interactionSeen = false;
			var postInteractionFramesRemaining = 0;

			for (var step = 0; step < AimPreviewMaxSteps; step++)
			{
				var result = previewWorld.Advance(_config.FixedStepSeconds);
				var cueBall = result.Balls.First(ball => ball.BallNumber == 0);

				if (!interactionSeen && !cueBall.IsPocketed)
				{
					primaryCueEnd = cueBall.Position;
				}

				if (!interactionSeen)
				{
					var firstContact = result.Events.FirstOrDefault(evt => evt.EventType == ShotEventType.FirstContact);
					var cueBounce = result.Events.FirstOrDefault(evt =>
						evt.EventType == ShotEventType.CushionContact && evt.BallNumber == 0);

					if (firstContact != null || cueBounce != null)
					{
						interactionSeen = true;
						primaryCueEnd = cueBall.Position;
						postInteractionFramesRemaining = AimPreviewPostInteractionFrames;

						if (firstContact?.BallNumber is int contactedBallNumber)
						{
							targetBallNumber = contactedBallNumber;
							var contactedBall = result.Balls.First(ball => ball.BallNumber == contactedBallNumber);
							if (!contactedBall.IsPocketed)
							{
								targetStart = contactedBall.Position;
								targetEnd = contactedBall.Position;
							}
						}
					}
				}
				else
				{
					if (!cueBall.IsPocketed)
					{
						secondaryCueEnd = cueBall.Position;
					}

					if (targetBallNumber.HasValue)
					{
						var contactedBall = result.Balls.First(ball => ball.BallNumber == targetBallNumber.Value);
						if (!contactedBall.IsPocketed)
						{
							targetEnd = contactedBall.Position;
						}
					}

					postInteractionFramesRemaining--;
					if (postInteractionFramesRemaining <= 0 || result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
					{
						break;
					}
				}

				if (!interactionSeen && result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
				{
					break;
				}
			}

			return new AimPreviewResult(cueStart, primaryCueEnd, secondaryCueEnd, targetStart, targetEnd);
		}
		catch
		{
			return null;
		}
	}

	private void HideAimPreviewGuides()
	{
		_aimPrimaryGuide.Visible = false;
		_aimSecondaryGuide.Visible = false;
		_aimTargetGuide.Visible = false;
	}

	private void SetGuideSegment(
		MeshInstance3D guideNode,
		NumericsVector2 start,
		NumericsVector2 end,
		float height)
	{
		var startPoint = ToGodotPoint(start, height);
		var endPoint = ToGodotPoint(end, height);
		var segment = endPoint - startPoint;
		var segmentLength = segment.Length();

		if (segmentLength <= 0.0001f)
		{
			guideNode.Visible = false;
			return;
		}

		guideNode.Visible = true;
		guideNode.Position = (startPoint + endPoint) * 0.5f;
		((BoxMesh)guideNode.Mesh!).Size = new Vector3(GetGuideLineThicknessWorldMeters(AimGuideThicknessPixels), AimGuideHeightMeters, segmentLength);
		guideNode.LookAt(guideNode.Position + segment, Vector3.Up);
	}

	private void SetOverlaySegment(
		MeshInstance3D guideNode,
		NumericsVector2 start,
		NumericsVector2 end,
		float height)
	{
		var startPoint = ToGodotPoint(start, height);
		var endPoint = ToGodotPoint(end, height);
		var segment = endPoint - startPoint;
		var segmentLength = segment.Length();

		if (segmentLength <= 0.0001f)
		{
			guideNode.Visible = false;
			return;
		}

		guideNode.Visible = true;
		guideNode.Position = (startPoint + endPoint) * 0.5f;
		((BoxMesh)guideNode.Mesh!).Size = new Vector3(GetGuideLineThicknessWorldMeters(_overlayLineThicknessPixels), OverlayLineHeightMeters, segmentLength);
		guideNode.LookAt(guideNode.Position + segment, Vector3.Up);
	}

	private float GetGuideLineThicknessWorldMeters(float thicknessPixels)
	{
		if (_camera == null)
		{
			return 0.003f;
		}

		var viewportHeightPixels = Mathf.Max(GetViewport().GetVisibleRect().Size.Y, 1.0f);
		if (_camera.Projection == Camera3D.ProjectionType.Orthogonal)
		{
			var worldUnitsPerPixel = _camera.Size / viewportHeightPixels;
			return Mathf.Clamp(thicknessPixels * worldUnitsPerPixel, 0.0005f, 0.01f);
		}

		var tableCenter = GetTableCenter3D();
		var distanceToTable = Mathf.Max((_camera.GlobalPosition - tableCenter).Length(), 0.1f);
		var worldHeightAtTable = 2.0f * distanceToTable * Mathf.Tan(Mathf.DegToRad(_camera.Fov) * 0.5f);
		var perspectiveUnitsPerPixel = worldHeightAtTable / viewportHeightPixels;
		return Mathf.Clamp(thicknessPixels * perspectiveUnitsPerPixel, 0.0005f, 0.01f);
	}

	private Color ResolveOverlayColor(string overlayName, Color defaultColor)
	{
		if (_ruleMode != RuleMode.Calibration || _calibrationFields.Count == 0)
		{
			return defaultColor;
		}

		var selectedObjectKey = _selectedCalibrationObjectKey;
		if (string.IsNullOrWhiteSpace(selectedObjectKey))
		{
			return defaultColor;
		}

		var matchesSelectedObject = _calibrationFields.Any(field =>
			field.ObjectKey == selectedObjectKey &&
			field.OverlayTarget == overlayName);
		if (!matchesSelectedObject)
		{
			return defaultColor;
		}

		return defaultColor.Lerp(new Color(1.0f, 0.98f, 0.52f), 0.45f);
	}

}
