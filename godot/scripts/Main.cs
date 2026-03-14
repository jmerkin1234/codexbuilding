using System.Linq;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using GodotEnvironment = Godot.Environment;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main : Node3D
{
    private enum RuleMode
    {
        EightBall,
        Training
    }

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
    private const float AimGuideThicknessMeters = 0.008f;
    private const float AimGuideHeightMeters = 0.01f;
    private const float OverlayLineThicknessMeters = 0.01f;
    private const float OverlayLineHeightMeters = 0.008f;
    private const int OverlayPocketSegments = 20;
    private const float AimTurnRadiansPerSecond = 1.8f;
    private const float StrikeSpeedAdjustPerSecond = 1.5f;
    private const float TipAdjustPerSecond = 1.2f;
    private const float CueBallPlacementMetersPerSecond = 0.9f;
    private const float MinimumStrikeSpeedMetersPerSecond = 0.3f;
    private const float MaximumStrikeSpeedMetersPerSecond = 4.0f;
    private const int AimPreviewPostInteractionFrames = 18;
    private const int AimPreviewMaxSteps = 240;

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

    private readonly List<string> _recentFrameEvents = new(capacity: 4);
    private readonly List<string> _recentRuleNotes = new(capacity: 4);
    private readonly List<SimulationReplayFrame> _capturedShotFrames = new(capacity: 1024);
    private readonly Dictionary<int, MeshInstance3D> _ballVisuals = new();

    private TableSpec _tableSpec = null!;
    private SimulationConfig _config = null!;
    private SimulationWorld _world = null!;
    private Node3D _godotRoot = null!;
    private Node3D _tableRoot = null!;
    private Node3D _ballsRoot = null!;
    private Node3D _cueRoot = null!;
    private Node3D _guideRoot = null!;
    private Node3D _hardcodeOverlayRoot = null!;
    private Camera3D _camera = null!;
    private Label _statusLabel = null!;
    private MeshInstance3D _cueGuide = null!;
    private MeshInstance3D _aimPrimaryGuide = null!;
    private MeshInstance3D _aimSecondaryGuide = null!;
    private MeshInstance3D _aimTargetGuide = null!;
    private RuleMode _ruleMode = RuleMode.EightBall;
    private EightBallMatchState _eightBallState = EightBallMatchState.CreateNew();
    private TrainingModeState _trainingState = TrainingModeState.CreateNew();
    private ResolvedCueStrike? _capturedCueStrike;
    private bool _shotCaptureActive;
    private int _capturedShotFrameIndex;
    private bool _aimPreviewDirty = true;
    private AimPreviewResult? _cachedAimPreview;
    private bool _hardcodeOverlayVisible = true;
    private int _trainingSelectedBallNumber;
    private float _aimAngleRadians;
    private float _strikeSpeedMetersPerSecond = 2.0f;
    private Vector2 _tipOffsetNormalized = Vector2.Zero;

    public override void _Ready()
    {
        _tableSpec = CustomTable9FtSpec.Create();
        _config = SimulationConfig.Default;
        _world = new SimulationWorld(_tableSpec, _config, StandardEightBallRack.Create(_tableSpec));

        ConfigureSceneGraph();
        ConfigureCameraAndLighting();
        BuildTableVisual();
        BuildHardcodeOverlay();
        BuildBallVisuals();
        ResetSessionForCurrentMode();
    }

    public override void _Process(double delta)
    {
        var deltaSeconds = (float)delta;

        UpdatePlacementControls(deltaSeconds);
        UpdateShotControls(deltaSeconds);

        var result = _world.Advance(deltaSeconds);
        CacheRecentEvents(result.Events);
        CaptureShotFrame(result);
        SyncBallVisuals(_world.Balls);
        UpdateCueGuide();
        UpdateStatusLabel(result.Events);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (_world.Phase == SimulationPhase.Running)
        {
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Space:
                TryShoot();
                break;
            case Key.R:
                ResetSessionForCurrentMode();
                break;
            case Key.Backspace:
                _tipOffsetNormalized = Vector2.Zero;
                UpdateCueGuide();
                UpdateStatusLabel(Array.Empty<ShotEvent>());
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
            case Key.H:
                ToggleHardcodeOverlay();
                break;
        }
    }

    private void ConfigureSceneGraph()
    {
        _godotRoot = EnsureNode<Node3D>(this, "GodotRoot");
        _tableRoot = EnsureNode<Node3D>(_godotRoot, "TableRoot");
        _ballsRoot = EnsureNode<Node3D>(_godotRoot, "BallsRoot");
        _cueRoot = EnsureNode<Node3D>(_godotRoot, "CueRoot");
        _guideRoot = EnsureNode<Node3D>(_godotRoot, "GuideRoot");
        _hardcodeOverlayRoot = EnsureNode<Node3D>(_godotRoot, "HardcodeOverlayRoot");

        _cueGuide = EnsureNode<MeshInstance3D>(_cueRoot, "CueStick");
        _cueGuide.Mesh = new BoxMesh();
        _cueGuide.MaterialOverride = CreateMaterial(new Color(0.92f, 0.84f, 0.58f), roughness: 0.65f);
        _cueGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        _aimPrimaryGuide = EnsureNode<MeshInstance3D>(_guideRoot, "AimPrimaryGuide");
        _aimPrimaryGuide.Mesh = new BoxMesh();
        _aimPrimaryGuide.MaterialOverride = CreateGuideMaterial(new Color(0.94f, 0.97f, 0.98f));
        _aimPrimaryGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        _aimSecondaryGuide = EnsureNode<MeshInstance3D>(_guideRoot, "AimSecondaryGuide");
        _aimSecondaryGuide.Mesh = new BoxMesh();
        _aimSecondaryGuide.MaterialOverride = CreateGuideMaterial(new Color(0.39f, 0.84f, 0.94f));
        _aimSecondaryGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        _aimTargetGuide = EnsureNode<MeshInstance3D>(_guideRoot, "AimTargetGuide");
        _aimTargetGuide.Mesh = new BoxMesh();
        _aimTargetGuide.MaterialOverride = CreateGuideMaterial(new Color(0.98f, 0.72f, 0.24f));
        _aimTargetGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        var hud = EnsureNode<CanvasLayer>(this, "Hud");
        _statusLabel = EnsureNode<Label>(hud, "StatusLabel");
        _statusLabel.Position = new Vector2(18.0f, 18.0f);
        _statusLabel.Size = new Vector2(760.0f, 260.0f);
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

    private void BuildHardcodeOverlay()
    {
        ClearChildren(_hardcodeOverlayRoot);

        var clothMin = _tableSpec.ClothMin;
        var clothMax = _tableSpec.ClothMax;
        var topLeft = new NumericsVector2(clothMin.X, clothMin.Y);
        var topRight = new NumericsVector2(clothMax.X, clothMin.Y);
        var bottomLeft = new NumericsVector2(clothMin.X, clothMax.Y);
        var bottomRight = new NumericsVector2(clothMax.X, clothMax.Y);

        AddOverlaySegment("OverlayClothTop", topLeft, topRight, new Color(0.76f, 0.92f, 0.98f), 0.020f);
        AddOverlaySegment("OverlayClothBottom", bottomLeft, bottomRight, new Color(0.76f, 0.92f, 0.98f), 0.020f);
        AddOverlaySegment("OverlayClothLeft", topLeft, bottomLeft, new Color(0.76f, 0.92f, 0.98f), 0.020f);
        AddOverlaySegment("OverlayClothRight", topRight, bottomRight, new Color(0.76f, 0.92f, 0.98f), 0.020f);

        foreach (var cushion in _tableSpec.Cushions)
        {
            AddOverlaySegment(
                $"Overlay_{cushion.SourceName}",
                cushion.Start,
                cushion.End,
                new Color(0.98f, 0.59f, 0.2f),
                0.024f);
        }

        foreach (var jaw in _tableSpec.JawSegments)
        {
            AddOverlaySegment(
                $"Overlay_{jaw.SourceName}",
                jaw.Start,
                jaw.End,
                new Color(0.95f, 0.31f, 0.35f),
                0.028f);
        }

        foreach (var pocket in _tableSpec.Pockets)
        {
            AddOverlayCircle(
                $"Overlay_{pocket.SourceName}",
                pocket.Center,
                pocket.CaptureRadiusMeters,
                new Color(0.44f, 0.86f, 0.97f),
                0.032f);
        }

        AddOverlayCross("OverlayCueBallSpawn", _tableSpec.CueBallSpawn, 0.032f, new Color(0.95f, 0.95f, 0.95f), 0.036f);
        AddOverlayCross("OverlayRackApexSpot", _tableSpec.RackApexSpot, 0.032f, new Color(0.95f, 0.82f, 0.22f), 0.036f);

        _hardcodeOverlayRoot.Visible = _hardcodeOverlayVisible;
    }

    private void ResetSessionForCurrentMode()
    {
        _world.Reset(StandardEightBallRack.Create(_tableSpec));
        _eightBallState = EightBallMatchState.CreateNew();
        _trainingState = TrainingModeState.CreateNew();
        _trainingSelectedBallNumber = 0;
        _capturedCueStrike = null;
        _capturedShotFrameIndex = 0;
        _shotCaptureActive = false;
        _capturedShotFrames.Clear();
        _recentFrameEvents.Clear();
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add(_ruleMode == RuleMode.Training ? "Training freeplay ready." : "Eight-ball mode ready.");
        _aimAngleRadians = GetDefaultAimAngle();
        _strikeSpeedMetersPerSecond = 2.0f;
        _tipOffsetNormalized = Vector2.Zero;
        MarkAimPreviewDirty();

        if (CanPlaceCueBall())
        {
            ResetWorldForNextTurn(cueBallInHand: true, requiresEightBallRespot: false);
        }

        SyncBallVisuals(_world.Balls);
        UpdateCueGuide();
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private void ToggleRuleMode()
    {
        _ruleMode = _ruleMode == RuleMode.EightBall ? RuleMode.Training : RuleMode.EightBall;
        ResetSessionForCurrentMode();
    }

    private void ToggleHardcodeOverlay()
    {
        _hardcodeOverlayVisible = !_hardcodeOverlayVisible;
        _hardcodeOverlayRoot.Visible = _hardcodeOverlayVisible;
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add(_hardcodeOverlayVisible
            ? "Hardcoded-table overlay visible."
            : "Hardcoded-table overlay hidden.");
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private void SelectTrainingBall(int direction)
    {
        if (_ruleMode != RuleMode.Training || direction == 0)
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
        _recentRuleNotes.Add($"Practice selection: {GetTrainingSelectionLabel()}");
        MarkAimPreviewDirty();
        UpdateCueGuide();
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private void UpdatePlacementControls(float deltaSeconds)
    {
        if (!CanAdjustPlacement())
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
        if (!CanEditShot())
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
            MaximumStrikeSpeedMetersPerSecond);
        _tipOffsetNormalized = _tipOffsetNormalized.LimitLength(1.0f);

        if (changed)
        {
            MarkAimPreviewDirty();
        }
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

            var resolvedCueStrike = _world.ApplyCueStrike(shot);
            BeginShotCapture(resolvedCueStrike);
            _recentFrameEvents.Clear();
            _recentRuleNotes.Clear();
            _recentRuleNotes.Add(_ruleMode == RuleMode.Training ? "Practice shot started." : "Eight-ball shot started.");
        }
        catch (Exception exception)
        {
            _recentRuleNotes.Clear();
            _recentRuleNotes.Add(exception.Message);
        }

        MarkAimPreviewDirty();
        UpdateCueGuide();
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private bool CanEditShot()
    {
        return _world.Phase != SimulationPhase.Running &&
               !_shotCaptureActive &&
               !IsMatchOver() &&
               _world.Balls.Any(ball => ball.Kind == BallKind.Cue && !ball.IsPocketed);
    }

    private bool CanAdjustPlacement()
    {
        if (_world.Phase == SimulationPhase.Running || _shotCaptureActive)
        {
            return false;
        }

        return _ruleMode switch
        {
            RuleMode.EightBall => _eightBallState.BallInHandPlayer == _eightBallState.CurrentPlayer,
            RuleMode.Training => true,
            _ => false
        };
    }

    private bool CanPlaceCueBall()
    {
        return CanAdjustPlacement();
    }

    private int GetPlacementBallNumber()
    {
        return _ruleMode == RuleMode.Training ? _trainingSelectedBallNumber : 0;
    }

    private bool IsMatchOver()
    {
        return _ruleMode == RuleMode.EightBall && _eightBallState.IsGameOver;
    }

    private void BeginShotCapture(ResolvedCueStrike resolvedCueStrike)
    {
        _capturedCueStrike = resolvedCueStrike;
        _capturedShotFrameIndex = 0;
        _capturedShotFrames.Clear();
        _shotCaptureActive = true;
        MarkAimPreviewDirty();
    }

    private void CaptureShotFrame(ShotResult result)
    {
        if (!_shotCaptureActive || !_capturedCueStrike.HasValue)
        {
            return;
        }

        if (result.FixedStepsExecuted == 0 &&
            result.Events.Count == 0 &&
            result.Phase == SimulationPhase.Running)
        {
            return;
        }

        _capturedShotFrames.Add(new SimulationReplayFrame(
            stepIndex: _capturedShotFrameIndex++,
            phase: result.Phase,
            simulationTimeSeconds: result.SimulationTimeSeconds,
            balls: result.Balls.ToArray(),
            events: result.Events.ToArray()));

        if (result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
        {
            FinalizeCapturedShot();
        }
    }

    private void FinalizeCapturedShot()
    {
        if (!_capturedCueStrike.HasValue || _capturedShotFrames.Count == 0)
        {
            ClearShotCapture();
            return;
        }

        var trace = new SimulationReplayTrace(
            _capturedCueStrike.Value,
            _capturedShotFrames.ToArray(),
            completed: true,
            maxSteps: _capturedShotFrames.Count);

        if (_ruleMode == RuleMode.EightBall)
        {
            ApplyEightBallTurnResult(EightBallRulesEngine.ResolveShot(_eightBallState, trace));
        }
        else
        {
            ApplyTrainingTurnResult(TrainingModeEngine.ResolveShot(_trainingState, trace));
        }

        ClearShotCapture();
    }

    private void ApplyEightBallTurnResult(EightBallTurnResult turnResult)
    {
        _eightBallState = turnResult.NextState;
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add($"Turn: {GetPlayerLabel(turnResult.ShootingPlayer)} -> {GetPlayerLabel(turnResult.NextState.CurrentPlayer)}");

        if (turnResult.AssignedGroup.HasValue)
        {
            _recentRuleNotes.Add($"{GetPlayerLabel(turnResult.ShootingPlayer)} claimed {turnResult.AssignedGroup.Value}.");
        }

        if (turnResult.IsFoul)
        {
            _recentRuleNotes.Add($"Foul: {string.Join(", ", turnResult.Fouls)}");
        }

        if (turnResult.RequiresEightBallRespot)
        {
            _recentRuleNotes.Add("8-ball respot required.");
        }

        if (turnResult.NextState.Winner.HasValue)
        {
            _recentRuleNotes.Add($"Winner: {GetPlayerLabel(turnResult.NextState.Winner.Value)}");
        }
        else if (turnResult.NextState.BallInHandPlayer.HasValue)
        {
            _recentRuleNotes.Add($"Ball in hand: {GetPlayerLabel(turnResult.NextState.BallInHandPlayer.Value)}");
        }
        else if (turnResult.PlayerContinues)
        {
            _recentRuleNotes.Add($"{GetPlayerLabel(turnResult.ShootingPlayer)} continues.");
        }

        if (!turnResult.NextState.IsGameOver)
        {
            ResetWorldForNextTurn(
                cueBallInHand: turnResult.NextState.BallInHandPlayer == turnResult.NextState.CurrentPlayer,
                requiresEightBallRespot: turnResult.RequiresEightBallRespot);
        }

        MarkAimPreviewDirty();
    }

    private void ApplyTrainingTurnResult(TrainingTurnResult turnResult)
    {
        _trainingState = turnResult.NextState;
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add($"Practice shots: {_trainingState.ShotCount}");

        if (turnResult.Summary.PocketedBallNumbers.Count > 0)
        {
            _recentRuleNotes.Add($"Pocketed: {string.Join(", ", turnResult.Summary.PocketedBallNumbers)}");
        }

        if (turnResult.RequiresEightBallRespot)
        {
            _recentRuleNotes.Add("8-ball respot required.");
        }

        _recentRuleNotes.Add("Cue ball can be moved freely.");

        ResetWorldForNextTurn(
            cueBallInHand: turnResult.CanRepositionCueBallAnywhere,
            requiresEightBallRespot: turnResult.RequiresEightBallRespot);
        MarkAimPreviewDirty();
    }

    private void ResetWorldForNextTurn(bool cueBallInHand, bool requiresEightBallRespot)
    {
        var updatedBalls = _world.Balls
            .Select(ball => ball with
            {
                Velocity = NumericsVector2.Zero,
                Spin = new SpinState(0.0f, 0.0f, 0.0f)
            })
            .ToArray();

        if (requiresEightBallRespot)
        {
            updatedBalls = ApplyBallPlacement(updatedBalls, 8, _tableSpec.RackApexSpot, keepPocketed: false);
        }

        if (cueBallInHand)
        {
            var cueBall = updatedBalls.First(ball => ball.BallNumber == 0);
            var preferredCueBallPosition = cueBall.IsPocketed ? _tableSpec.CueBallSpawn : cueBall.Position;
            updatedBalls = ApplyBallPlacement(updatedBalls, 0, preferredCueBallPosition, keepPocketed: false);
        }

        _world.Reset(updatedBalls);
        MarkAimPreviewDirty();
    }

    private void MoveBallToPlacement(int ballNumber, NumericsVector2 desiredPosition, bool keepPocketed)
    {
        var updatedBalls = _world.Balls
            .Select(ball => ball with
            {
                Velocity = NumericsVector2.Zero,
                Spin = new SpinState(0.0f, 0.0f, 0.0f)
            })
            .ToArray();

        updatedBalls = ApplyBallPlacement(updatedBalls, ballNumber, desiredPosition, keepPocketed);
        _world.Reset(updatedBalls);
        MarkAimPreviewDirty();

        if (_ruleMode == RuleMode.Training && ballNumber != 0)
        {
            _trainingState = new TrainingModeState(
                shotCount: _trainingState.ShotCount,
                pocketedObjectBallNumbers: _trainingState.PocketedObjectBallNumbers.Where(number => number != ballNumber).ToArray(),
                cueBallInHand: true);
            _recentRuleNotes.Clear();
            _recentRuleNotes.Add($"Practice layout: moved {GetTrainingSelectionLabel()}");
        }
    }

    private BallState[] ApplyBallPlacement(
        BallState[] balls,
        int ballNumber,
        NumericsVector2 preferredPosition,
        bool keepPocketed)
    {
        var candidatePosition = FindLegalPlacement(preferredPosition, ballNumber, balls);

        for (var index = 0; index < balls.Length; index++)
        {
            if (balls[index].BallNumber != ballNumber)
            {
                continue;
            }

            balls[index] = balls[index] with
            {
                Position = candidatePosition,
                Velocity = NumericsVector2.Zero,
                Spin = new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed = keepPocketed && balls[index].IsPocketed
            };
            break;
        }

        return balls;
    }

    private NumericsVector2 FindLegalPlacement(
        NumericsVector2 preferredPosition,
        int movingBallNumber,
        IReadOnlyList<BallState> balls)
    {
        var ballRadiusMeters = _tableSpec.BallDiameterMeters * 0.5f;
        var clampedPreferred = ClampToCloth(preferredPosition, ballRadiusMeters);

        if (IsPlacementLegal(clampedPreferred, movingBallNumber, balls))
        {
            return clampedPreferred;
        }

        const float searchStepMeters = 0.05f;
        for (var ring = 1; ring <= 28; ring++)
        {
            for (var offsetX = -ring; offsetX <= ring; offsetX++)
            {
                for (var offsetY = -ring; offsetY <= ring; offsetY++)
                {
                    if (Math.Abs(offsetX) != ring && Math.Abs(offsetY) != ring)
                    {
                        continue;
                    }

                    var candidate = ClampToCloth(
                        clampedPreferred + new NumericsVector2(offsetX * searchStepMeters, offsetY * searchStepMeters),
                        ballRadiusMeters);

                    if (IsPlacementLegal(candidate, movingBallNumber, balls))
                    {
                        return candidate;
                    }
                }
            }
        }

        return clampedPreferred;
    }

    private bool IsPlacementLegal(
        NumericsVector2 candidatePosition,
        int movingBallNumber,
        IReadOnlyList<BallState> balls)
    {
        var minimumDistanceSquared = (_tableSpec.BallDiameterMeters - 0.0005f) * (_tableSpec.BallDiameterMeters - 0.0005f);

        foreach (var otherBall in balls)
        {
            if (otherBall.BallNumber == movingBallNumber || otherBall.IsPocketed)
            {
                continue;
            }

            if (NumericsVector2.DistanceSquared(candidatePosition, otherBall.Position) < minimumDistanceSquared)
            {
                return false;
            }
        }

        return true;
    }

    private NumericsVector2 ClampToCloth(NumericsVector2 position, float ballRadiusMeters)
    {
        return new NumericsVector2(
            Math.Clamp(position.X, _tableSpec.ClothMin.X + ballRadiusMeters, _tableSpec.ClothMax.X - ballRadiusMeters),
            Math.Clamp(position.Y, _tableSpec.ClothMin.Y + ballRadiusMeters, _tableSpec.ClothMax.Y - ballRadiusMeters));
    }

    private void ClearShotCapture()
    {
        _capturedCueStrike = null;
        _capturedShotFrameIndex = 0;
        _capturedShotFrames.Clear();
        _shotCaptureActive = false;
        MarkAimPreviewDirty();
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
            var highlightScale = _ruleMode == RuleMode.Training && ball.BallNumber == _trainingSelectedBallNumber ? 1.08f : 1.0f;
            ballNode.Scale = new Vector3(highlightScale, highlightScale, highlightScale);
        }
    }

    private void UpdateCueGuide()
    {
        if (!CanEditShot())
        {
            _cueGuide.Visible = false;
            HideAimPreviewGuides();
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

        UpdateAimPreviewGuides();
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
        var recentRuleText = _recentRuleNotes.Count == 0
            ? "none"
            : string.Join('\n', _recentRuleNotes);

        _statusLabel.Text =
            $"Portable core: {_tableSpec.Name}  Mode: {GetRuleModeLabel()}\n" +
            $"{BuildModeStatusLine()}\n" +
            $"Phase: {_world.Phase}  SimTime: {_world.SimulationTimeSeconds:0.000}s  FixedSteps: {_world.TotalFixedStepsExecuted}\n" +
            $"CueBall: {cueBallStatus}  Aim: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg  Speed: {_strikeSpeedMetersPerSecond:0.00} m/s  Tip: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})  Overlay: {_hardcodeOverlayVisible}\n" +
            "Controls: Tab mode  H hardcode overlay  A/D aim  W/S speed  J/L side spin  I/K follow-draw  Arrow keys move selected placement ball  Z/X cycle practice ball  Space shoot  Backspace center tip  R reset\n" +
            $"Recent shot events:\n{recentEventText}\n" +
            $"Rules/training:\n{recentRuleText}";
    }

    private string BuildModeStatusLine()
    {
        if (_ruleMode == RuleMode.Training)
        {
            var pocketed = _trainingState.PocketedObjectBallNumbers.Count == 0
                ? "none"
                : string.Join(", ", _trainingState.PocketedObjectBallNumbers);
            return $"Practice shots: {_trainingState.ShotCount}  Selected ball: {GetTrainingSelectionLabel()}  Free placement: true  Pocketed objects: {pocketed}";
        }

        var winnerText = _eightBallState.Winner.HasValue ? GetPlayerLabel(_eightBallState.Winner.Value) : "none";
        var ballInHandText = _eightBallState.BallInHandPlayer.HasValue ? GetPlayerLabel(_eightBallState.BallInHandPlayer.Value) : "none";
        return
            $"Current player: {GetPlayerLabel(_eightBallState.CurrentPlayer)}  Break shot: {_eightBallState.IsBreakShot}  Open table: {_eightBallState.OpenTable}  " +
            $"Groups: P1={_eightBallState.PlayerOneGroup} P2={_eightBallState.PlayerTwoGroup}  Ball in hand: {ballInHandText}  Winner: {winnerText}";
    }

    private string GetRuleModeLabel()
    {
        return _ruleMode == RuleMode.EightBall ? "EightBall" : "Training";
    }

    private static string GetPlayerLabel(PlayerSlot player)
    {
        return player == PlayerSlot.PlayerOne ? "Player 1" : "Player 2";
    }

    private void AddOverlaySegment(
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

        _hardcodeOverlayRoot.AddChild(segmentNode);
        SetOverlaySegment(segmentNode, start, end, height);
    }

    private void AddOverlayCircle(
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

            AddOverlaySegment($"{namePrefix}_{segmentIndex:00}", start, end, color, height);
        }
    }

    private void AddOverlayCross(
        string namePrefix,
        NumericsVector2 center,
        float armLength,
        Color color,
        float height)
    {
        AddOverlaySegment(
            $"{namePrefix}_Horizontal",
            center + new NumericsVector2(-armLength, 0.0f),
            center + new NumericsVector2(armLength, 0.0f),
            color,
            height);
        AddOverlaySegment(
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
        ((BoxMesh)guideNode.Mesh!).Size = new Vector3(AimGuideThicknessMeters, AimGuideHeightMeters, segmentLength);
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
        ((BoxMesh)guideNode.Mesh!).Size = new Vector3(OverlayLineThicknessMeters, OverlayLineHeightMeters, segmentLength);
        guideNode.LookAt(guideNode.Position + segment, Vector3.Up);
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
}
