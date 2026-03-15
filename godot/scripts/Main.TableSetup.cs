using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using GodotEnvironment = Godot.Environment;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{	private void ConfigureCameraAndLighting()
	{
		_camera = EnsureNode<Camera3D>(this, "ViewCamera");
		_camera.Current = true;
		ApplyCameraPreset();

		var sunLight = EnsureNode<DirectionalLight3D>(this, "SunLight");
		sunLight.RotationDegrees = new Vector3(-58.0f, -32.0f, 0.0f);
		sunLight.ShadowEnabled = true;
		sunLight.LightEnergy = 1.4f;

		var tableFillLight = EnsureNode<OmniLight3D>(this, "TableFillLight");
		tableFillLight.Position = new Vector3(0.0f, 1.35f, 0.0f);
		tableFillLight.LightColor = new Color(1.0f, 0.98f, 0.95f);
		tableFillLight.LightEnergy = 1.8f;
		tableFillLight.OmniRange = 5.0f;
		tableFillLight.ShadowEnabled = false;

		var tableRimLight = EnsureNode<OmniLight3D>(this, "TableRimLight");
		tableRimLight.Position = new Vector3(-1.0f, 1.05f, -0.82f);
		tableRimLight.LightColor = new Color(0.86f, 0.92f, 1.0f);
		tableRimLight.LightEnergy = 1.1f;
		tableRimLight.OmniRange = 4.6f;
		tableRimLight.ShadowEnabled = false;

		var worldEnvironment = EnsureNode<WorldEnvironment>(this, "WorldEnvironment");
		worldEnvironment.Environment ??= new GodotEnvironment();
		var proceduralSky = new ProceduralSkyMaterial
		{
			SkyTopColor = new Color(0.3f, 0.42f, 0.58f),
			SkyHorizonColor = new Color(0.82f, 0.86f, 0.91f),
			GroundBottomColor = new Color(0.05f, 0.05f, 0.06f),
			GroundHorizonColor = new Color(0.16f, 0.15f, 0.14f)
		};
		var sky = new Sky
		{
			SkyMaterial = proceduralSky
		};
		worldEnvironment.Environment.Sky = sky;
		worldEnvironment.Environment.BackgroundMode = GodotEnvironment.BGMode.Sky;
		worldEnvironment.Environment.AmbientLightSource = GodotEnvironment.AmbientSource.Sky;
		worldEnvironment.Environment.AmbientLightEnergy = 0.75f;
	}

	private void BuildTableVisual()
	{
		if (_tableRoot.GetChildCount() > 0)
		{
			return;
		}

		if (_tableRoot.GetNodeOrNull<Node>("ImportedTable") != null)
		{
			return;
		}

		ClearChildren(_tableRoot);

		if (TryInstantiateImportedTable())
		{
			return;
		}

		var feltCenter = (_tableSpec.ClothMin + _tableSpec.ClothMax) * 0.5f;
		var clothSize = _tableSpec.ClothMax - _tableSpec.ClothMin;
		var frameSize = clothSize + new NumericsVector2(FrameOverhangMeters, FrameOverhangMeters);

		var frameBottom = CreateBoxVisual(
			"framebottom",
			frameSize.X + 0.08f,
			BottomThicknessMeters,
			frameSize.Y + 0.08f,
			new Color(0.17f, 0.11f, 0.08f),
			new Vector3(feltCenter.X, -(BottomThicknessMeters * 0.5f) - FrameThicknessMeters, feltCenter.Y));
		_tableRoot.AddChild(frameBottom);

		var frame = CreateBoxVisual(
			"Tableframe",
			frameSize.X,
			FrameThicknessMeters,
			frameSize.Y,
			new Color(0.31f, 0.18f, 0.1f),
			new Vector3(feltCenter.X, -(FrameThicknessMeters * 0.5f), feltCenter.Y));
		_tableRoot.AddChild(frame);

		var felt = CreateBoxVisual(
			"Tableslate",
			clothSize.X,
			FeltThicknessMeters,
			clothSize.Y,
			new Color(0.08f, 0.44f, 0.24f),
			new Vector3(feltCenter.X, FeltThicknessMeters * -0.5f + 0.001f, feltCenter.Y));
		_tableRoot.AddChild(felt);

		foreach (var cushion in _tableSpec.Cushions)
		{
			var segmentStart = ToGodotPoint(cushion.Start, RailVisualHeightMeters * 0.5f);
			var segmentEnd = ToGodotPoint(cushion.End, RailVisualHeightMeters * 0.5f);
			var midpoint = (segmentStart + segmentEnd) * 0.5f;
			var outwardOffset = ToGodotVector(-cushion.InwardNormal) * (RailVisualWidthMeters * 0.5f);
			var segmentVector = segmentEnd - segmentStart;
			var segmentLength = segmentVector.Length();
			var rail = CreateBoxVisual(
				cushion.SourceName,
				RailVisualWidthMeters,
				RailVisualHeightMeters,
				segmentLength + (_tableSpec.BallDiameterMeters * 0.4f),
				new Color(0.19f, 0.12f, 0.09f),
				midpoint + outwardOffset);

			_tableRoot.AddChild(rail);
			rail.LookAt(rail.Position + segmentVector, Vector3.Up);
		}

		foreach (var pocket in _tableSpec.Pockets)
		{
			var pocketMesh = new CylinderMesh
			{
				Height = PocketDepthMeters,
				TopRadius = pocket.CaptureRadiusMeters,
				BottomRadius = pocket.CaptureRadiusMeters
			};

			var pocketNode = new MeshInstance3D
			{
				Name = pocket.SourceName,
				Mesh = pocketMesh,
				MaterialOverride = CreateMaterial(new Color(0.03f, 0.03f, 0.04f), roughness: 1.0f),
				Position = new Vector3(pocket.Center.X, -(PocketDepthMeters * 0.5f), pocket.Center.Y)
			};

			_tableRoot.AddChild(pocketNode);
		}
	}

	private void BuildBallVisuals()
	{
		_ballVisuals.Clear();
		_ballVisualBaseRotations.Clear();
		_ballVisualLastPositions.Clear();
		var missingBallNodes = new List<string>();

		foreach (var ball in _world.Balls.OrderBy(ball => ball.BallNumber))
		{
			if (FindNodeRecursive<MeshInstance3D>(_ballsRoot, GetBallNodeName(ball)) is { } existingBallNode)
			{
				_ballVisuals.Add(ball.BallNumber, existingBallNode);
				_ballVisualBaseRotations[ball.BallNumber] = existingBallNode.Quaternion;
				_ballVisualLastPositions[ball.BallNumber] = existingBallNode.Position;
				continue;
			}

			missingBallNodes.Add(GetBallNodeName(ball));
		}

		if (missingBallNodes.Count == 0)
		{
			return;
		}

		throw new InvalidOperationException(
			$"Imported Blender ball visuals are required. Missing nodes under BallsRoot: {string.Join(", ", missingBallNodes)}");
	}

	private void BuildHardcodeOverlay()
	{
		ClearChildren(_hardcodeOverlayRoot);

		_overlayClothRoot = new Node3D
		{
			Name = "OverlayClothRoot"
		};
		_hardcodeOverlayRoot.AddChild(_overlayClothRoot);

		_overlayCushionRoot = new Node3D
		{
			Name = "OverlayCushionRoot"
		};
		_hardcodeOverlayRoot.AddChild(_overlayCushionRoot);

		_overlayJawRoot = new Node3D
		{
			Name = "OverlayJawRoot"
		};
		_hardcodeOverlayRoot.AddChild(_overlayJawRoot);

		_overlayPocketRoot = new Node3D
		{
			Name = "OverlayPocketRoot"
		};
		_hardcodeOverlayRoot.AddChild(_overlayPocketRoot);

		_overlaySpotRoot = new Node3D
		{
			Name = "OverlaySpotRoot"
		};
		_hardcodeOverlayRoot.AddChild(_overlaySpotRoot);

		var clothMin = _tableSpec.ClothMin;
		var clothMax = _tableSpec.ClothMax;
		var topLeft = new NumericsVector2(clothMin.X, clothMin.Y);
		var topRight = new NumericsVector2(clothMax.X, clothMin.Y);
		var bottomLeft = new NumericsVector2(clothMin.X, clothMax.Y);
		var bottomRight = new NumericsVector2(clothMax.X, clothMax.Y);

		AddOverlaySegment(_overlayClothRoot, "OverlayClothTop", topLeft, topRight, ResolveOverlayColor("OverlayClothTop", new Color(0.38f, 0.88f, 0.46f)), 0.020f);
		AddOverlaySegment(_overlayClothRoot, "OverlayClothBottom", bottomLeft, bottomRight, ResolveOverlayColor("OverlayClothBottom", new Color(0.38f, 0.88f, 0.46f)), 0.020f);
		AddOverlaySegment(_overlayClothRoot, "OverlayClothLeft", topLeft, bottomLeft, ResolveOverlayColor("OverlayClothLeft", new Color(0.38f, 0.88f, 0.46f)), 0.020f);
		AddOverlaySegment(_overlayClothRoot, "OverlayClothRight", topRight, bottomRight, ResolveOverlayColor("OverlayClothRight", new Color(0.38f, 0.88f, 0.46f)), 0.020f);

		foreach (var cushion in _tableSpec.Cushions)
		{
			AddOverlaySegment(
				_overlayCushionRoot,
				$"Overlay_{cushion.SourceName}",
				cushion.Start,
				cushion.End,
				ResolveOverlayColor($"Overlay_{cushion.SourceName}", new Color(0.98f, 0.59f, 0.2f)),
				0.024f);
		}

		foreach (var jaw in _tableSpec.JawSegments)
		{
			AddOverlaySegment(
				_overlayJawRoot,
				$"Overlay_{jaw.SourceName}",
				jaw.Start,
				jaw.End,
				ResolveOverlayColor($"Overlay_{jaw.SourceName}", new Color(0.95f, 0.31f, 0.35f)),
				0.028f);
		}

		foreach (var pocket in _tableSpec.Pockets)
		{
			AddOverlayCircle(
				_overlayPocketRoot,
				$"Overlay_{pocket.SourceName}",
				pocket.Center,
				pocket.CaptureRadiusMeters,
				ResolveOverlayColor($"Overlay_{pocket.SourceName}", new Color(0.18f, 0.52f, 0.98f)),
				0.032f);
		}

		AddOverlayCross(_overlaySpotRoot, "OverlayCueBallSpawn", _tableSpec.CueBallSpawn, 0.032f, ResolveOverlayColor("OverlayCueBallSpawn", new Color(0.95f, 0.95f, 0.95f)), 0.036f);
		AddOverlayCross(_overlaySpotRoot, "OverlayRackApexSpot", _tableSpec.RackApexSpot, 0.032f, ResolveOverlayColor("OverlayRackApexSpot", new Color(0.95f, 0.82f, 0.22f)), 0.036f);

		UpdateOverlayVisibility();
	}

	private bool TryInstantiateImportedTable()
	{
		if (!ResourceLoader.Exists(ImportedTableScenePath))
		{
			return false;
		}

		var packedScene = GD.Load<PackedScene>(ImportedTableScenePath);
		if (packedScene == null)
		{
			return false;
		}

		var importedTable = packedScene.Instantiate<Node>();
		importedTable.Name = "ImportedTable";
		_tableRoot.AddChild(importedTable);
		return true;
	}

	private MeshInstance3D CreateBoxVisual(
		string name,
		float sizeX,
		float sizeY,
		float sizeZ,
		Color color,
		Vector3 position)
	{
		return new MeshInstance3D
		{
			Name = name,
			Mesh = new BoxMesh
			{
				Size = new Vector3(sizeX, sizeY, sizeZ)
			},
			MaterialOverride = CreateMaterial(color, roughness: 0.55f),
			Position = position
		};
	}

	private static StandardMaterial3D CreateMaterial(Color color, float roughness)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = roughness
		};
	}

	private static StandardMaterial3D CreateGuideMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = 1.25f,
			Roughness = 0.15f
		};
	}

	private static Button CreateMenuButton(Control parent, string name, string text, float top)
	{
		var button = new Button
		{
			Name = name,
			Text = text,
			Position = new Vector2(40.0f, top),
			Size = new Vector2(420.0f, 42.0f),
			FocusMode = Control.FocusModeEnum.All
		};
		button.AddThemeFontSizeOverride("font_size", 17);
		parent.AddChild(button);
		return button;
	}

	private static StyleBoxFlat CreateHudPanelStyle(Color backgroundColor, Color borderColor)
	{
		return new StyleBoxFlat
		{
			BgColor = backgroundColor,
			BorderColor = borderColor,
			BorderWidthBottom = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.22f),
			ShadowSize = 10,
			ContentMarginBottom = 10.0f,
			ContentMarginLeft = 10.0f,
			ContentMarginRight = 10.0f,
			ContentMarginTop = 10.0f
		};
	}

	private static StyleBoxFlat CreateScrollBarStyle(Color backgroundColor, Color borderColor)
	{
		return new StyleBoxFlat
		{
			BgColor = backgroundColor,
			BorderColor = borderColor,
			BorderWidthBottom = 1,
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			BorderWidthTop = 1,
			CornerRadiusBottomLeft = 7,
			CornerRadiusBottomRight = 7,
			CornerRadiusTopLeft = 7,
			CornerRadiusTopRight = 7,
			ContentMarginBottom = 4.0f,
			ContentMarginLeft = 4.0f,
			ContentMarginRight = 4.0f,
			ContentMarginTop = 4.0f
		};
	}

	private static T EnsureNode<T>(Node parent, string name) where T : Node, new()
	{
		var existing = parent.GetNodeOrNull<T>(name);
		if (existing != null)
		{
			return existing;
		}

		var created = new T
		{
			Name = name
		};
		parent.AddChild(created);
		return created;
	}

	private static T? FindNodeRecursive<T>(Node parent, string name) where T : Node
	{
		if (parent is T typedParent && parent.Name == name)
		{
			return typedParent;
		}

		foreach (Node child in parent.GetChildren())
		{
			if (child is T typedChild && child.Name == name)
			{
				return typedChild;
			}

			if (FindNodeRecursive<T>(child, name) is { } nested)
			{
				return nested;
			}
		}

		return null;
	}

	private static void ClearChildren(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			child.QueueFree();
		}
	}

	private float GetDefaultAimAngle()
	{
		var aimVector = _tableSpec.RackApexSpot - _tableSpec.CueBallSpawn;
		return Mathf.Atan2(aimVector.Y, aimVector.X);
	}

	private Color GetBallColor(BallState ball)
	{
		if (ball.BallNumber == 0)
		{
			return _ballPalette[0];
		}

		var paletteIndex = ball.BallNumber <= 8 ? ball.BallNumber : ball.BallNumber - 8;
		var baseColor = _ballPalette[paletteIndex];

		return ball.Kind == BallKind.Stripe
			? baseColor.Lerp(Colors.White, 0.35f)
			: baseColor;
	}

	private static string GetBallNodeName(BallState ball)
	{
		return ball.BallNumber == 0 ? "CueBall" : $"Ball_{ball.BallNumber:00}";
	}

	private static Vector3 ToGodotPoint(NumericsVector2 point, float height)
	{
		return new Vector3(point.X, height, point.Y);
	}

	private static Vector3 ToGodotVector(NumericsVector2 vector)
	{
		return new Vector3(vector.X, 0.0f, vector.Y);
	}

}
