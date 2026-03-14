using System.Linq;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using GodotEnvironment = Godot.Environment;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main : Node3D
{
    private const string ImportedTableScenePath = "res://art/ImportedTable.tscn";
    private const float FeltThicknessMeters = 0.04f;
    private const float FrameThicknessMeters = 0.1f;
    private const float FrameOverhangMeters = 0.22f;
    private const float BottomThicknessMeters = 0.08f;
    private const float RailVisualWidthMeters = 0.12f;
    private const float RailVisualHeightMeters = 0.1f;
    private const float PocketDepthMeters = 0.08f;
    private const float CueGuideThicknessMeters = 0.012f;
    private const float CueGuideHeightMeters = 0.012f;
    private const float AimTurnRadiansPerSecond = 1.8f;
    private const float StrikeSpeedAdjustPerSecond = 1.5f;
    private const float TipAdjustPerSecond = 1.2f;
    private const float MinimumStrikeSpeedMetersPerSecond = 0.3f;
    private const float MaximumStrikeSpeedMetersPerSecond = 4.0f;

    private readonly Color[] _ballPalette =
    [
        new Color(0.95f, 0.95f, 0.95f),
        new Color(0.92f, 0.8f, 0.16f),
        new Color(0.13f, 0.38f, 0.78f),
        new Color(0.76f, 0.14f, 0.16f),
        new Color(0.39f, 0.2f, 0.61f),
        new Color(0.86f, 0.41f, 0.12f),
        new Color(0.11f, 0.45f, 0.19f),
        new Color(0.45f, 0.09f, 0.08f),
        new Color(0.08f, 0.08f, 0.09f)
    ];

    private TableSpec _tableSpec = null!;
    private SimulationConfig _config = null!;
    private SimulationWorld _world = null!;
    private Node3D _godotRoot = null!;
    private Node3D _tableRoot = null!;
    private Node3D _ballsRoot = null!;
    private Node3D _cueRoot = null!;
    private Camera3D _camera = null!;
    private Label _statusLabel = null!;
    private MeshInstance3D _cueGuide = null!;
    private readonly Dictionary<int, MeshInstance3D> _ballVisuals = new();
    private readonly List<string> _recentFrameEvents = new(capacity: 4);
    private float _aimAngleRadians;
    private float _strikeSpeedMetersPerSecond = 2.0f;
    private Vector2 _tipOffsetNormalized = Vector2.Zero;

    public override void _Ready()
    {
        _tableSpec = CustomTable9FtSpec.Create();
        _config = SimulationConfig.Default;
        _world = new SimulationWorld(_tableSpec, _config, StandardEightBallRack.Create(_tableSpec));
        _aimAngleRadians = GetDefaultAimAngle();

        ConfigureSceneGraph();
        ConfigureCameraAndLighting();
        BuildTableVisual();
        BuildBallVisuals();
        SyncBallVisuals(_world.Balls);
        UpdateCueGuide();
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    public override void _Process(double delta)
    {
        var deltaSeconds = (float)delta;

        UpdateShotControls(deltaSeconds);

        var result = _world.Advance(deltaSeconds);
        CacheRecentEvents(result.Events);
        SyncBallVisuals(result.Balls);
        UpdateCueGuide();
        UpdateStatusLabel(result.Events);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Space:
                TryShoot();
                break;
            case Key.R:
                ResetTable();
                break;
            case Key.Backspace:
                _tipOffsetNormalized = Vector2.Zero;
                UpdateCueGuide();
                UpdateStatusLabel(Array.Empty<ShotEvent>());
                break;
        }
    }

    private void ConfigureSceneGraph()
    {
        _godotRoot = EnsureNode<Node3D>(this, "GodotRoot");
        _tableRoot = EnsureNode<Node3D>(_godotRoot, "TableRoot");
        _ballsRoot = EnsureNode<Node3D>(_godotRoot, "BallsRoot");
        _cueRoot = EnsureNode<Node3D>(_godotRoot, "CueRoot");

        _cueGuide = EnsureNode<MeshInstance3D>(_cueRoot, "CueStick");
        _cueGuide.Mesh = new BoxMesh();
        _cueGuide.MaterialOverride = CreateMaterial(new Color(0.92f, 0.84f, 0.58f), roughness: 0.65f);
        _cueGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        var hud = EnsureNode<CanvasLayer>(this, "Hud");
        _statusLabel = EnsureNode<Label>(hud, "StatusLabel");
        _statusLabel.Position = new Vector2(18.0f, 18.0f);
        _statusLabel.Size = new Vector2(740.0f, 180.0f);
        _statusLabel.Modulate = Colors.White;
    }

    private void ConfigureCameraAndLighting()
    {
        _camera = EnsureNode<Camera3D>(this, "ViewCamera");
        _camera.Position = new Vector3(-0.55f, 2.6f, 1.95f);
        _camera.LookAt(Vector3.Zero, Vector3.Up);
        _camera.Current = true;

        var sunLight = EnsureNode<DirectionalLight3D>(this, "SunLight");
        sunLight.RotationDegrees = new Vector3(-58.0f, -32.0f, 0.0f);
        sunLight.ShadowEnabled = true;
        sunLight.LightEnergy = 2.2f;

        var worldEnvironment = EnsureNode<WorldEnvironment>(this, "WorldEnvironment");
        worldEnvironment.Environment ??= new GodotEnvironment();
        worldEnvironment.Environment.BackgroundMode = GodotEnvironment.BGMode.Color;
        worldEnvironment.Environment.BackgroundColor = new Color(0.04f, 0.05f, 0.07f);
        worldEnvironment.Environment.AmbientLightSource = GodotEnvironment.AmbientSource.Color;
        worldEnvironment.Environment.AmbientLightColor = new Color(0.4f, 0.43f, 0.47f);
        worldEnvironment.Environment.AmbientLightEnergy = 0.8f;
    }

    private void BuildTableVisual()
    {
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

            rail.LookAt(rail.Position + segmentVector, Vector3.Up);
            _tableRoot.AddChild(rail);
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
        ClearChildren(_ballsRoot);
        _ballVisuals.Clear();

        var ballRadiusMeters = _tableSpec.BallDiameterMeters * 0.5f;

        foreach (var ball in _world.Balls.OrderBy(ball => ball.BallNumber))
        {
            var ballNode = new MeshInstance3D
            {
                Name = GetBallNodeName(ball),
                Mesh = new SphereMesh
                {
                    Radius = ballRadiusMeters,
                    Height = _tableSpec.BallDiameterMeters,
                    RadialSegments = 24,
                    Rings = 12
                },
                MaterialOverride = CreateMaterial(GetBallColor(ball), roughness: 0.25f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On
            };

            _ballsRoot.AddChild(ballNode);
            _ballVisuals.Add(ball.BallNumber, ballNode);
        }
    }

    private void ResetTable()
    {
        _world.Reset(StandardEightBallRack.Create(_tableSpec));
        _aimAngleRadians = GetDefaultAimAngle();
        _strikeSpeedMetersPerSecond = 2.0f;
        _tipOffsetNormalized = Vector2.Zero;
        _recentFrameEvents.Clear();
        SyncBallVisuals(_world.Balls);
        UpdateCueGuide();
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private void UpdateShotControls(float deltaSeconds)
    {
        if (!CanEditShot())
        {
            return;
        }

        if (Input.IsKeyPressed(Key.A))
        {
            _aimAngleRadians -= AimTurnRadiansPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.D))
        {
            _aimAngleRadians += AimTurnRadiansPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.W))
        {
            _strikeSpeedMetersPerSecond += StrikeSpeedAdjustPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.S))
        {
            _strikeSpeedMetersPerSecond -= StrikeSpeedAdjustPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.J))
        {
            _tipOffsetNormalized.X -= TipAdjustPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.L))
        {
            _tipOffsetNormalized.X += TipAdjustPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.I))
        {
            _tipOffsetNormalized.Y += TipAdjustPerSecond * deltaSeconds;
        }

        if (Input.IsKeyPressed(Key.K))
        {
            _tipOffsetNormalized.Y -= TipAdjustPerSecond * deltaSeconds;
        }

        _strikeSpeedMetersPerSecond = Mathf.Clamp(
            _strikeSpeedMetersPerSecond,
            MinimumStrikeSpeedMetersPerSecond,
            MaximumStrikeSpeedMetersPerSecond);
        _tipOffsetNormalized = _tipOffsetNormalized.LimitLength(1.0f);
    }

    private void TryShoot()
    {
        if (!CanEditShot())
        {
            return;
        }

        try
        {
            var shot = new ShotInput(
                new NumericsVector2(Mathf.Cos(_aimAngleRadians), Mathf.Sin(_aimAngleRadians)),
                _strikeSpeedMetersPerSecond,
                new NumericsVector2(_tipOffsetNormalized.X, _tipOffsetNormalized.Y));

            _world.ApplyCueStrike(shot);
            _recentFrameEvents.Clear();
            _recentFrameEvents.Add($"CueStrike speed={_strikeSpeedMetersPerSecond:0.00} tip=({_tipOffsetNormalized.X:0.00},{_tipOffsetNormalized.Y:0.00})");
        }
        catch (Exception exception)
        {
            _recentFrameEvents.Clear();
            _recentFrameEvents.Add(exception.Message);
        }

        UpdateCueGuide();
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private bool CanEditShot()
    {
        return _world.Phase != SimulationPhase.Running && _world.Balls.Any(ball => ball.Kind == BallKind.Cue && !ball.IsPocketed);
    }

    private void CacheRecentEvents(IReadOnlyList<ShotEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        _recentFrameEvents.Clear();

        foreach (var shotEvent in events.TakeLast(4))
        {
            var ballText = shotEvent.BallNumber.HasValue ? $" ball={shotEvent.BallNumber.Value}" : string.Empty;
            _recentFrameEvents.Add($"{shotEvent.EventType}{ballText} {shotEvent.Detail}".Trim());
        }
    }

    private void SyncBallVisuals(IReadOnlyList<BallState> balls)
    {
        var ballRadiusMeters = _tableSpec.BallDiameterMeters * 0.5f;

        foreach (var ball in balls)
        {
            if (!_ballVisuals.TryGetValue(ball.BallNumber, out var ballNode))
            {
                continue;
            }

            ballNode.Visible = !ball.IsPocketed;
            if (ball.IsPocketed)
            {
                continue;
            }

            ballNode.Position = ToGodotPoint(ball.Position, ballRadiusMeters);
        }
    }

    private void UpdateCueGuide()
    {
        if (!CanEditShot())
        {
            _cueGuide.Visible = false;
            return;
        }

        var cueBall = _world.Balls.First(ball => ball.Kind == BallKind.Cue && !ball.IsPocketed);
        var start = ToGodotPoint(cueBall.Position, (_tableSpec.BallDiameterMeters * 0.5f) + 0.012f);
        var aimDirection = new Vector3(Mathf.Cos(_aimAngleRadians), 0.0f, Mathf.Sin(_aimAngleRadians));
        var guideLength = 0.45f + ((_strikeSpeedMetersPerSecond - MinimumStrikeSpeedMetersPerSecond) * 0.18f);
        var end = start + (aimDirection * guideLength);
        var midpoint = (start + end) * 0.5f;

        _cueGuide.Visible = true;
        _cueGuide.Position = midpoint;
        ((BoxMesh)_cueGuide.Mesh!).Size = new Vector3(CueGuideThicknessMeters, CueGuideHeightMeters, guideLength);
        _cueGuide.LookAt(midpoint + aimDirection, Vector3.Up);
    }

    private void UpdateStatusLabel(IReadOnlyList<ShotEvent> events)
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

        _statusLabel.Text =
            $"Portable core: {_tableSpec.Name}\n" +
            $"Phase: {_world.Phase}  SimTime: {_world.SimulationTimeSeconds:0.000}s  FixedSteps: {_world.TotalFixedStepsExecuted}\n" +
            $"CueBall: {cueBallStatus}  Aim: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg  Speed: {_strikeSpeedMetersPerSecond:0.00} m/s  Tip: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})\n" +
            "Controls: A/D aim  W/S speed  J/L side spin  I/K follow-draw  Space shoot  Backspace center tip  R reset\n" +
            $"Recent events:\n{recentEventText}";
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
