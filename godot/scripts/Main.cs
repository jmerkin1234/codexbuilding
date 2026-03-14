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
        RailRestitution,
        RailTangentialFriction,
        RailSpinTransfer,
        SettleThreshold,
        CollisionIterations,
        BoundaryIterations
    }

    private enum RuleMode
    {
        EightBall,
        Training
    }

    private const string ImportedTableScenePath = "res://art/customtable_9ft.blend";
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
    private const float ComputerTurnThinkDelaySeconds = 0.8f;
    private const int ComputerMaxSimulationSteps = 900;
    private const int ComputerMaxTargetBallsToConsider = 4;
    private const float TrainingSelectionRingRadiusMeters = 0.044f;
    private const float TrainingSelectionRingHeightMeters = 0.02f;
    private const float TrainingSelectionRingBaseScale = 1.0f;
    private const float TrainingSelectionRingPulseAmplitude = 0.09f;
    private const float TrainingSelectionRingPulseSpeed = 4.6f;
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
    private readonly float[] _computerStrikeSpeeds = [1.4f, 1.9f, 2.4f, 2.9f, 3.4f];
    private readonly CameraPreset[] _cameraPresets =
    [
        new("Broadcast", new Vector3(-0.55f, 2.6f, 1.95f), false, 0.0f, 46.0f),
        new("TopDown", new Vector3(0.0f, 3.45f, 0.001f), true, 1.72f, 0.0f),
        new("FootRail", new Vector3(-2.35f, 1.38f, 0.0f), false, 0.0f, 38.0f),
        new("SideRail", new Vector3(0.0f, 1.82f, 2.35f), false, 0.0f, 40.0f)
    ];

    private TableSpec _tableSpec = null!;
    private SimulationConfig _config = null!;
    private SimulationWorld _world = null!;
    private Node3D _godotRoot = null!;
    private Node3D _tableRoot = null!;
    private Node3D _ballsRoot = null!;
    private Node3D _cueRoot = null!;
    private Node3D _guideRoot = null!;
    private Node3D _hardcodeOverlayRoot = null!;
    private Node3D _trainingSelectionRoot = null!;
    private Node3D _overlayClothRoot = null!;
    private Node3D _overlayCushionRoot = null!;
    private Node3D _overlayJawRoot = null!;
    private Node3D _overlayPocketRoot = null!;
    private Node3D _overlaySpotRoot = null!;
    private Camera3D _camera = null!;
    private Panel _shotBannerPanel = null!;
    private Label _shotBannerLabel = null!;
    private Panel _statusPanel = null!;
    private ColorRect _statusAccentBar = null!;
    private Label _statusHeaderLabel = null!;
    private Panel _summaryPanel = null!;
    private ColorRect _summaryAccentBar = null!;
    private Label _summaryHeaderLabel = null!;
    private Label _summaryLabel = null!;
    private Panel _debugPanel = null!;
    private Label _statusLabel = null!;
    private Label _debugLabel = null!;
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
    private bool _debugModeEnabled;
    private bool _hardcodeOverlayVisible = true;
    private bool _overlayClothVisible = true;
    private bool _overlayCushionVisible = true;
    private bool _overlayJawVisible = true;
    private bool _overlayPocketVisible = true;
    private bool _overlaySpotVisible = true;
    private DebugTuningField _selectedTuningField = DebugTuningField.SlidingFriction;
    private int _trainingSelectedBallNumber;
    private int _cameraPresetIndex = 1;
    private float _cameraZoomScale = 1.0f;
    private float _shotBannerSecondsRemaining;
    private float _computerTurnThinkSeconds;
    private float _trainingSelectionPulseSeconds;
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
        UpdateShotBanner(deltaSeconds);
        _trainingSelectionPulseSeconds += deltaSeconds;

        UpdatePlacementControls(deltaSeconds);
        UpdateShotControls(deltaSeconds);

        var result = _world.Advance(deltaSeconds);
        ProcessShotFeedbackEvents(result.Events);
        CacheRecentEvents(result.Events);
        CaptureShotFrame(result);
        SyncBallVisuals(_world.Balls);
        UpdateCueGuide();
        UpdateStatusLabel(result.Events);
        UpdateComputerTurn(deltaSeconds);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.F1:
                ToggleDebugMode();
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

        if (_world.Phase == SimulationPhase.Running)
        {
            return;
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
                    UpdateStatusLabel(Array.Empty<ShotEvent>());
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

        _trainingSelectionRoot = EnsureNode<Node3D>(_guideRoot, "TrainingSelectionRoot");
        ClearChildren(_trainingSelectionRoot);
        _trainingSelectionRoot.Visible = false;
        AddOverlayCircle(
            _trainingSelectionRoot,
            "TrainingSelectionRing",
            NumericsVector2.Zero,
            TrainingSelectionRingRadiusMeters,
            new Color(0.46f, 0.9f, 0.98f),
            TrainingSelectionRingHeightMeters);

        var hud = EnsureNode<CanvasLayer>(this, "Hud");
        _shotBannerPanel = EnsureNode<Panel>(hud, "ShotBannerPanel");
        _shotBannerPanel.AnchorLeft = 0.5f;
        _shotBannerPanel.AnchorRight = 0.5f;
        _shotBannerPanel.AnchorTop = 1.0f;
        _shotBannerPanel.AnchorBottom = 1.0f;
        _shotBannerPanel.OffsetLeft = -310.0f;
        _shotBannerPanel.OffsetTop = -114.0f;
        _shotBannerPanel.OffsetRight = 310.0f;
        _shotBannerPanel.OffsetBottom = -24.0f;
        _shotBannerPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _shotBannerPanel.Visible = false;
        _shotBannerPanel.AddThemeStyleboxOverride(
            "panel",
            CreateHudPanelStyle(
                new Color(0.08f, 0.18f, 0.22f, 0.9f),
                new Color(0.48f, 0.83f, 0.92f, 0.95f)));

        _shotBannerLabel = EnsureNode<Label>(_shotBannerPanel, "ShotBannerLabel");
        _shotBannerLabel.Position = new Vector2(18.0f, 12.0f);
        _shotBannerLabel.Size = new Vector2(584.0f, 70.0f);
        _shotBannerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _shotBannerLabel.VerticalAlignment = VerticalAlignment.Center;
        _shotBannerLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _shotBannerLabel.AddThemeFontSizeOverride("font_size", 22);
        _shotBannerLabel.Visible = false;

        _statusPanel = EnsureNode<Panel>(hud, "StatusPanel");
        _statusPanel.Position = new Vector2(18.0f, 18.0f);
        _statusPanel.Size = new Vector2(780.0f, 354.0f);
        _statusPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _statusPanel.AddThemeStyleboxOverride(
            "panel",
            CreateHudPanelStyle(
                new Color(0.02f, 0.05f, 0.07f, 0.82f),
                new Color(0.25f, 0.55f, 0.63f, 0.95f)));

        _statusAccentBar = EnsureNode<ColorRect>(_statusPanel, "StatusAccentBar");
        _statusAccentBar.Position = new Vector2(0.0f, 0.0f);
        _statusAccentBar.Size = new Vector2(780.0f, 6.0f);
        _statusAccentBar.Color = new Color(0.42f, 0.83f, 0.89f, 0.95f);

        _statusHeaderLabel = EnsureNode<Label>(_statusPanel, "StatusHeaderLabel");
        _statusHeaderLabel.Position = new Vector2(16.0f, 18.0f);
        _statusHeaderLabel.Size = new Vector2(748.0f, 34.0f);
        _statusHeaderLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusHeaderLabel.VerticalAlignment = VerticalAlignment.Center;
        _statusHeaderLabel.AddThemeFontSizeOverride("font_size", 23);
        _statusHeaderLabel.Modulate = new Color(0.9f, 0.98f, 1.0f);

        _statusLabel = EnsureNode<Label>(_statusPanel, "StatusLabel");
        _statusLabel.Position = new Vector2(16.0f, 58.0f);
        _statusLabel.Size = new Vector2(748.0f, 280.0f);
        _statusLabel.Modulate = Colors.White;
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statusLabel.VerticalAlignment = VerticalAlignment.Top;
        _statusLabel.AddThemeFontSizeOverride("font_size", 15);

        _summaryPanel = EnsureNode<Panel>(hud, "SummaryPanel");
        _summaryPanel.Position = new Vector2(18.0f, 384.0f);
        _summaryPanel.Size = new Vector2(780.0f, 208.0f);
        _summaryPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _summaryPanel.AddThemeStyleboxOverride(
            "panel",
            CreateHudPanelStyle(
                new Color(0.02f, 0.04f, 0.06f, 0.82f),
                new Color(0.31f, 0.63f, 0.72f, 0.95f)));

        _summaryAccentBar = EnsureNode<ColorRect>(_summaryPanel, "SummaryAccentBar");
        _summaryAccentBar.Position = new Vector2(0.0f, 0.0f);
        _summaryAccentBar.Size = new Vector2(780.0f, 6.0f);
        _summaryAccentBar.Color = new Color(0.42f, 0.83f, 0.89f, 0.95f);

        _summaryHeaderLabel = EnsureNode<Label>(_summaryPanel, "SummaryHeaderLabel");
        _summaryHeaderLabel.Position = new Vector2(16.0f, 16.0f);
        _summaryHeaderLabel.Size = new Vector2(748.0f, 30.0f);
        _summaryHeaderLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _summaryHeaderLabel.VerticalAlignment = VerticalAlignment.Center;
        _summaryHeaderLabel.AddThemeFontSizeOverride("font_size", 21);
        _summaryHeaderLabel.Modulate = new Color(0.9f, 0.98f, 1.0f);

        _summaryLabel = EnsureNode<Label>(_summaryPanel, "SummaryLabel");
        _summaryLabel.Position = new Vector2(16.0f, 52.0f);
        _summaryLabel.Size = new Vector2(748.0f, 138.0f);
        _summaryLabel.Modulate = Colors.White;
        _summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _summaryLabel.VerticalAlignment = VerticalAlignment.Top;
        _summaryLabel.AddThemeFontSizeOverride("font_size", 15);

        _debugPanel = EnsureNode<Panel>(hud, "DebugPanel");
        _debugPanel.Position = new Vector2(816.0f, 18.0f);
        _debugPanel.Size = new Vector2(520.0f, 760.0f);
        _debugPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _debugPanel.AddThemeStyleboxOverride(
            "panel",
            CreateHudPanelStyle(
                new Color(0.01f, 0.04f, 0.03f, 0.84f),
                new Color(0.28f, 0.73f, 0.53f, 0.95f)));
        _debugPanel.Visible = false;

        _debugLabel = EnsureNode<Label>(_debugPanel, "DebugLabel");
        _debugLabel.Position = new Vector2(16.0f, 14.0f);
        _debugLabel.Size = new Vector2(488.0f, 730.0f);
        _debugLabel.Modulate = new Color(0.84f, 0.98f, 0.89f);
        _debugLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _debugLabel.VerticalAlignment = VerticalAlignment.Top;
        _debugLabel.AddThemeFontSizeOverride("font_size", 14);
        _debugLabel.Visible = false;
    }

    private void ConfigureCameraAndLighting()
    {
        _camera = EnsureNode<Camera3D>(this, "ViewCamera");
        _camera.Current = true;
        ApplyCameraPreset();

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

        AddOverlaySegment(_overlayClothRoot, "OverlayClothTop", topLeft, topRight, new Color(0.76f, 0.92f, 0.98f), 0.020f);
        AddOverlaySegment(_overlayClothRoot, "OverlayClothBottom", bottomLeft, bottomRight, new Color(0.76f, 0.92f, 0.98f), 0.020f);
        AddOverlaySegment(_overlayClothRoot, "OverlayClothLeft", topLeft, bottomLeft, new Color(0.76f, 0.92f, 0.98f), 0.020f);
        AddOverlaySegment(_overlayClothRoot, "OverlayClothRight", topRight, bottomRight, new Color(0.76f, 0.92f, 0.98f), 0.020f);

        foreach (var cushion in _tableSpec.Cushions)
        {
            AddOverlaySegment(
                _overlayCushionRoot,
                $"Overlay_{cushion.SourceName}",
                cushion.Start,
                cushion.End,
                new Color(0.98f, 0.59f, 0.2f),
                0.024f);
        }

        foreach (var jaw in _tableSpec.JawSegments)
        {
            AddOverlaySegment(
                _overlayJawRoot,
                $"Overlay_{jaw.SourceName}",
                jaw.Start,
                jaw.End,
                new Color(0.95f, 0.31f, 0.35f),
                0.028f);
        }

        foreach (var pocket in _tableSpec.Pockets)
        {
            AddOverlayCircle(
                _overlayPocketRoot,
                $"Overlay_{pocket.SourceName}",
                pocket.Center,
                pocket.CaptureRadiusMeters,
                new Color(0.44f, 0.86f, 0.97f),
                0.032f);
        }

        AddOverlayCross(_overlaySpotRoot, "OverlayCueBallSpawn", _tableSpec.CueBallSpawn, 0.032f, new Color(0.95f, 0.95f, 0.95f), 0.036f);
        AddOverlayCross(_overlaySpotRoot, "OverlayRackApexSpot", _tableSpec.RackApexSpot, 0.032f, new Color(0.95f, 0.82f, 0.22f), 0.036f);

        UpdateOverlayVisibility();
    }

    private void ResetSessionForCurrentMode()
    {
        _world.Reset(StandardEightBallRack.Create(_tableSpec));
        _eightBallState = EightBallMatchState.CreateNew();
        _trainingState = TrainingModeState.CreateNew();
        _computerTurnThinkSeconds = 0.0f;
        _trainingSelectedBallNumber = 0;
        _capturedCueStrike = null;
        _capturedShotFrameIndex = 0;
        _shotCaptureActive = false;
        _capturedShotFrames.Clear();
        _recentFrameEvents.Clear();
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add(_ruleMode == RuleMode.Training ? "FreePlay ready." : "Eight-ball vs computer ready.");
        _aimAngleRadians = GetDefaultAimAngle();
        _strikeSpeedMetersPerSecond = 2.0f;
        _tipOffsetNormalized = Vector2.Zero;
        ResetShotSummary();
        MarkAimPreviewDirty();

        if (CanPlaceCueBall())
        {
            ResetWorldForNextTurn(cueBallInHand: true, requiresEightBallRespot: false);
        }

        ShowShotBanner(
            _ruleMode == RuleMode.Training ? "FreePlay ready." : "Eight-ball vs computer ready.",
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
        _ruleMode = _ruleMode == RuleMode.EightBall ? RuleMode.Training : RuleMode.EightBall;
        ResetSessionForCurrentMode();
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
        _debugModeEnabled = !_debugModeEnabled;
        _debugPanel.Visible = _debugModeEnabled;
        _debugLabel.Visible = _debugModeEnabled;
        UpdateOverlayVisibility();
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add(_debugModeEnabled
            ? "Debug mode enabled."
            : "Debug mode disabled.");
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private void CycleTuningField(int direction)
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
            DebugTuningField.RailRestitution => CreateAdjustedConfig(
                boundaryRestitution: AdjustFloat(
                    _config.BoundaryRestitution,
                    0.01f * stepScale * direction,
                    0.0f,
                    1.0f)),
            DebugTuningField.RailTangentialFriction => CreateAdjustedConfig(
                boundaryTangentialFrictionFactor: AdjustFloat(
                    _config.BoundaryTangentialFrictionFactor,
                    0.02f * stepScale * direction,
                    0.0f,
                    1.0f)),
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

    private void CycleCameraPreset()
    {
        _cameraPresetIndex = (_cameraPresetIndex + 1) % _cameraPresets.Length;
        ApplyCameraPreset();
        _recentRuleNotes.Clear();
        _recentRuleNotes.Add($"Camera preset: {GetActiveCameraPreset().Name}");
        UpdateStatusLabel(Array.Empty<ShotEvent>());
    }

    private void AdjustCameraZoom(float delta)
    {
        _cameraZoomScale = Mathf.Clamp(_cameraZoomScale + delta, 0.65f, 1.85f);
        ApplyCameraPreset();
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
        _recentRuleNotes.Add($"FreePlay selection: {GetTrainingSelectionLabel()}");
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
            MaximumStrikeSpeedMetersPerSecond);
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
            _ruleMode == RuleMode.Training ? "FreePlay shot started." : "Eight-ball shot started.",
            _ruleMode == RuleMode.Training ? "FreePlay shot started" : "Eight-ball shot started");
    }

    private void ExecuteShot(ShotInput shot, string recentNote, string bannerText)
    {
        if (!CanEditShot())
        {
            return;
        }

        try
        {
            var resolvedCueStrike = _world.ApplyCueStrike(shot);
            BeginShotCapture(resolvedCueStrike);
            _recentFrameEvents.Clear();
            _recentRuleNotes.Clear();
            _recentRuleNotes.Add(recentNote);
            ShowShotBanner(
                bannerText,
                new ShotBannerStyle(
                    new Color(0.09f, 0.17f, 0.26f, 0.9f),
                    new Color(0.52f, 0.74f, 0.96f, 0.95f),
                    new Color(0.94f, 0.98f, 1.0f)),
                1.4f);
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

    private void UpdateComputerTurn(float deltaSeconds)
    {
        if (!IsComputerTurnPending())
        {
            _computerTurnThinkSeconds = 0.0f;
            return;
        }

        _computerTurnThinkSeconds += deltaSeconds;
        if (_computerTurnThinkSeconds < ComputerTurnThinkDelaySeconds)
        {
            return;
        }

        _computerTurnThinkSeconds = 0.0f;
        ExecuteComputerTurn();
    }

    private bool IsComputerTurnPending()
    {
        return _ruleMode == RuleMode.EightBall &&
               _eightBallState.CurrentPlayer == PlayerSlot.PlayerTwo &&
               !_eightBallState.IsGameOver &&
               !_shotCaptureActive &&
               _world.Phase != SimulationPhase.Running;
    }

    private ComputerShotPlan? BuildComputerShotPlan()
    {
        var baseBalls = _world.Balls.ToArray();
        var placementCandidates = GetComputerPlacementCandidates(baseBalls);
        ComputerShotPlan? bestPlan = null;
        var bestScore = float.NegativeInfinity;

        foreach (var preferredPlacement in placementCandidates)
        {
            var placedBalls = preferredPlacement.HasValue
                ? ApplyBallPlacement(baseBalls.ToArray(), 0, preferredPlacement.Value, keepPocketed: false)
                : baseBalls.ToArray();
            var cueBallCandidates = placedBalls
                .Where(ball => ball.BallNumber == 0 && !ball.IsPocketed)
                .Take(1)
                .ToArray();
            if (cueBallCandidates.Length == 0)
            {
                continue;
            }
            var cueBall = cueBallCandidates[0];

            var targetBalls = GetComputerTargetBalls(placedBalls, cueBall.Position)
                .Take(ComputerMaxTargetBallsToConsider)
                .ToArray();
            if (targetBalls.Length == 0)
            {
                continue;
            }

            foreach (var candidate in BuildComputerShotCandidates(cueBall.Position, targetBalls))
            {
                var trace = SimulateReplayTrace(candidate.Shot, placedBalls);
                var turnResult = EightBallRulesEngine.ResolveShot(_eightBallState, trace);
                var score = ScoreComputerShot(turnResult, candidate.TargetBallNumber);

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestPlan = new ComputerShotPlan(
                    preferredPlacement,
                    candidate.Shot,
                    score,
                    $"at {FormatBallLabel(candidate.TargetBallNumber)}");
            }
        }

        if (bestPlan.HasValue)
        {
            return bestPlan;
        }

        var fallbackCueBallCandidates = baseBalls
            .Where(ball => ball.BallNumber == 0 && !ball.IsPocketed)
            .Take(1)
            .ToArray();
        if (fallbackCueBallCandidates.Length == 0)
        {
            return null;
        }
        var fallbackCueBall = fallbackCueBallCandidates[0];
        var fallbackTarget = GetComputerTargetBalls(baseBalls, fallbackCueBall.Position).FirstOrDefault();
        if (fallbackTarget.BallNumber == 0)
        {
            return null;
        }

        var fallbackAim = NumericsVector2.Normalize(fallbackTarget.Position - fallbackCueBall.Position);
        return new ComputerShotPlan(
            CueBallPlacement: null,
            Shot: new ShotInput(fallbackAim, 2.2f, NumericsVector2.Zero),
            Score: -1000.0f,
            Description: $"at {FormatBallLabel(fallbackTarget.BallNumber)}");
    }

    private IEnumerable<NumericsVector2?> GetComputerPlacementCandidates(IReadOnlyList<BallState> balls)
    {
        if (_eightBallState.BallInHandPlayer != PlayerSlot.PlayerTwo)
        {
            yield return null;
            yield break;
        }

        var cueSpawn = _tableSpec.CueBallSpawn;
        var center = (_tableSpec.ClothMin + _tableSpec.ClothMax) * 0.5f;
        var offsets = new[]
        {
            NumericsVector2.Zero,
            new NumericsVector2(-0.28f, 0.0f),
            new NumericsVector2(-0.18f, 0.18f),
            new NumericsVector2(-0.18f, -0.18f),
            new NumericsVector2(0.08f, 0.22f),
            new NumericsVector2(0.08f, -0.22f),
            new NumericsVector2(center.X - cueSpawn.X, 0.0f)
        };

        foreach (var offset in offsets)
        {
            yield return FindLegalPlacement(cueSpawn + offset, 0, balls);
        }
    }

    private IEnumerable<BallState> GetComputerTargetBalls(IReadOnlyList<BallState> balls, NumericsVector2 cueBallPosition)
    {
        var legalTargets = ResolveComputerLegalTargets(balls);
        return balls
            .Where(ball => legalTargets.Contains(ball.BallNumber))
            .OrderBy(ball => NumericsVector2.DistanceSquared(ball.Position, cueBallPosition));
    }

    private HashSet<int> ResolveComputerLegalTargets(IReadOnlyList<BallState> balls)
    {
        var available = balls
            .Where(ball => !ball.IsPocketed && ball.BallNumber != 0)
            .Select(ball => ball.BallNumber)
            .ToHashSet();

        if (_eightBallState.IsBreakShot || _eightBallState.OpenTable)
        {
            available.Remove(8);
            return available;
        }

        var computerGroup = _eightBallState.GetGroupForPlayer(PlayerSlot.PlayerTwo);
        if (computerGroup == BallGroup.Unassigned)
        {
            available.Remove(8);
            return available;
        }

        var groupTargets = available
            .Where(ballNumber => IsBallInGroup(ballNumber, computerGroup))
            .ToHashSet();
        if (groupTargets.Count > 0)
        {
            return groupTargets;
        }

        return available.Contains(8)
            ? new HashSet<int> { 8 }
            : new HashSet<int>();
    }

    private IEnumerable<ComputerShotCandidate> BuildComputerShotCandidates(
        NumericsVector2 cueBallPosition,
        IReadOnlyList<BallState> targetBalls)
    {
        foreach (var targetBall in targetBalls)
        {
            var directAim = targetBall.Position - cueBallPosition;
            if (directAim.LengthSquared() > 0.000001f)
            {
                foreach (var speed in _computerStrikeSpeeds)
                {
                    yield return new ComputerShotCandidate(
                        targetBall.BallNumber,
                        new ShotInput(NumericsVector2.Normalize(directAim), speed, NumericsVector2.Zero));
                }
            }

            foreach (var pocket in _tableSpec.Pockets)
            {
                var pocketDirection = pocket.Center - targetBall.Position;
                if (pocketDirection.LengthSquared() <= 0.000001f)
                {
                    continue;
                }

                var contactPoint = targetBall.Position -
                                   (NumericsVector2.Normalize(pocketDirection) * _tableSpec.BallDiameterMeters);
                var cueDirection = contactPoint - cueBallPosition;
                if (cueDirection.LengthSquared() <= 0.000001f)
                {
                    continue;
                }

                foreach (var speed in _computerStrikeSpeeds)
                {
                    yield return new ComputerShotCandidate(
                        targetBall.BallNumber,
                        new ShotInput(NumericsVector2.Normalize(cueDirection), speed, NumericsVector2.Zero));
                }
            }
        }
    }

    private SimulationReplayTrace SimulateReplayTrace(ShotInput shot, IReadOnlyList<BallState> initialBalls)
    {
        var previewWorld = new SimulationWorld(_tableSpec, _config, initialBalls.ToArray());
        var resolvedCueStrike = previewWorld.ApplyCueStrike(shot);
        var frames = new List<SimulationReplayFrame>(ComputerMaxSimulationSteps);

        for (var step = 0; step < ComputerMaxSimulationSteps; step++)
        {
            var result = previewWorld.Advance(_config.FixedStepSeconds);
            frames.Add(new SimulationReplayFrame(
                step,
                result.Phase,
                result.SimulationTimeSeconds,
                result.Balls.ToArray(),
                result.Events.ToArray()));

            if (result.Phase is SimulationPhase.Settled or SimulationPhase.Idle)
            {
                break;
            }
        }

        return new SimulationReplayTrace(
            resolvedCueStrike,
            frames,
            completed: frames.Count > 0 && frames[^1].Phase is SimulationPhase.Settled or SimulationPhase.Idle,
            maxSteps: ComputerMaxSimulationSteps);
    }

    private float ScoreComputerShot(EightBallTurnResult turnResult, int intendedTargetBall)
    {
        var summary = turnResult.Summary;
        var score = 0.0f;

        if (_eightBallState.IsBreakShot)
        {
            score += turnResult.BreakWasLegal ? 120.0f : -240.0f;
        }

        if (turnResult.NextState.Winner == PlayerSlot.PlayerTwo)
        {
            score += 10000.0f;
        }
        else if (turnResult.NextState.Winner == PlayerSlot.PlayerOne)
        {
            score -= 10000.0f;
        }

        if (turnResult.IsFoul)
        {
            score -= 420.0f + (turnResult.Fouls.Count * 90.0f);
        }

        if (summary.IsScratch)
        {
            score -= 550.0f;
        }

        if (turnResult.NextState.BallInHandPlayer == PlayerSlot.PlayerOne)
        {
            score -= 180.0f;
        }

        if (summary.FirstContactBallNumber == intendedTargetBall)
        {
            score += 140.0f;
        }
        else if (summary.FirstContactBallNumber.HasValue)
        {
            score += 30.0f;
        }

        if (summary.HasRailOrPocketAfterFirstContact)
        {
            score += 35.0f;
        }

        var computerGroup = turnResult.NextState.GetGroupForPlayer(PlayerSlot.PlayerTwo);
        var pocketedScoreBalls = summary.PocketedBallNumbers.Count(ballNumber =>
            ballNumber != 0 &&
            (computerGroup == BallGroup.Unassigned || IsBallInGroup(ballNumber, computerGroup)));
        score += pocketedScoreBalls * 220.0f;

        if (turnResult.AssignedGroup.HasValue)
        {
            score += 150.0f;
        }

        if (turnResult.PlayerContinues)
        {
            score += 120.0f;
        }

        if (turnResult.RequiresEightBallRespot)
        {
            score -= 150.0f;
        }

        score -= MathF.Abs(summary.ResolvedCueStrike.StrikeSpeedMetersPerSecond - 2.2f) * 8.0f;
        return score;
    }

    private static bool IsBallInGroup(int ballNumber, BallGroup group)
    {
        return group switch
        {
            BallGroup.Solids => ballNumber is >= 1 and <= 7,
            BallGroup.Stripes => ballNumber is >= 9 and <= 15,
            _ => false
        };
    }

    private void ExecuteComputerTurn()
    {
        try
        {
            var plan = BuildComputerShotPlan();
            if (plan == null)
            {
                _recentRuleNotes.Clear();
                _recentRuleNotes.Add("Computer could not find a playable shot.");
                UpdateStatusLabel(Array.Empty<ShotEvent>());
                return;
            }

            if (plan.Value.CueBallPlacement.HasValue)
            {
                MoveBallToPlacement(0, plan.Value.CueBallPlacement.Value, keepPocketed: false);
            }

            _aimAngleRadians = Mathf.Atan2(plan.Value.Shot.AimDirection.Y, plan.Value.Shot.AimDirection.X);
            _strikeSpeedMetersPerSecond = plan.Value.Shot.StrikeSpeedMetersPerSecond;
            _tipOffsetNormalized = new Vector2(
                plan.Value.Shot.TipOffsetNormalized.X,
                plan.Value.Shot.TipOffsetNormalized.Y);
            MarkAimPreviewDirty();
            ExecuteShot(
                plan.Value.Shot,
                $"Computer shot: {plan.Value.Description}",
                $"Computer shoots {plan.Value.Description}");
        }
        catch (Exception exception)
        {
            _recentRuleNotes.Clear();
            _recentRuleNotes.Add($"Computer shot failed: {exception.Message}");
            UpdateStatusLabel(Array.Empty<ShotEvent>());
        }
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

        ShowShotBanner(
            BuildEightBallTurnBanner(turnResult),
            ResolveEightBallBannerStyle(turnResult),
            ResolveEightBallBannerDuration(turnResult));
        ApplyEightBallShotSummary(turnResult);

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
        _recentRuleNotes.Add($"FreePlay shots: {_trainingState.ShotCount}");

        if (turnResult.Summary.PocketedBallNumbers.Count > 0)
        {
            _recentRuleNotes.Add($"Pocketed: {string.Join(", ", turnResult.Summary.PocketedBallNumbers)}");
        }

        if (turnResult.RequiresEightBallRespot)
        {
            _recentRuleNotes.Add("8-ball respot required.");
        }

        _recentRuleNotes.Add("Cue ball can be moved freely.");
        ShowShotBanner(
            BuildTrainingTurnBanner(turnResult),
            ResolveTrainingBannerStyle(turnResult),
            ResolveTrainingBannerDuration(turnResult));
        ApplyTrainingShotSummary(turnResult);

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

        if (_ruleMode == RuleMode.Training)
        {
            _trainingState = new TrainingModeState(
                shotCount: _trainingState.ShotCount,
                pocketedObjectBallNumbers: _trainingState.PocketedObjectBallNumbers.Where(number => number != ballNumber).ToArray(),
                cueBallInHand: true);
            _recentRuleNotes.Clear();
            _recentRuleNotes.Add($"FreePlay layout: moved {GetTrainingSelectionLabel()}");
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

    private void ProcessShotFeedbackEvents(IReadOnlyList<ShotEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        if (events.Any(shotEvent => shotEvent.EventType == ShotEventType.Scratch))
        {
            ShowShotBanner(
                "Scratch",
                new ShotBannerStyle(
                    new Color(0.25f, 0.08f, 0.08f, 0.92f),
                    new Color(0.96f, 0.41f, 0.34f, 0.98f),
                    new Color(1.0f, 0.93f, 0.92f)),
                2.4f);
            return;
        }

        var pocketedBalls = events
            .Where(shotEvent => shotEvent.EventType == ShotEventType.Pocketed && shotEvent.BallNumber is int ballNumber && ballNumber != 0)
            .Select(shotEvent => shotEvent.BallNumber!.Value)
            .Distinct()
            .OrderBy(ballNumber => ballNumber)
            .ToArray();

        if (pocketedBalls.Length > 0)
        {
            ShowShotBanner(
                $"Pocketed {FormatBallNumberList(pocketedBalls)}",
                new ShotBannerStyle(
                    new Color(0.07f, 0.19f, 0.1f, 0.92f),
                    new Color(0.45f, 0.88f, 0.53f, 0.98f),
                    new Color(0.93f, 1.0f, 0.94f)),
                2.1f);
            return;
        }

        var firstContact = events.LastOrDefault(shotEvent => shotEvent.EventType == ShotEventType.FirstContact && shotEvent.BallNumber.HasValue);
        if (firstContact?.BallNumber.HasValue == true)
        {
            ShowShotBanner(
                $"First contact: {FormatBallLabel(firstContact.BallNumber.Value)}",
                new ShotBannerStyle(
                    new Color(0.08f, 0.14f, 0.23f, 0.9f),
                    new Color(0.41f, 0.72f, 0.96f, 0.95f),
                    new Color(0.94f, 0.98f, 1.0f)),
                1.3f);
        }
    }

    private void SyncBallVisuals(IReadOnlyList<BallState> balls)
    {
        var ballRadiusMeters = _tableSpec.BallDiameterMeters * 0.5f;
        BallState? selectedTrainingBall = null;

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
            ballNode.Scale = Vector3.One;

            if (_ruleMode == RuleMode.Training && ball.BallNumber == _trainingSelectedBallNumber)
            {
                selectedTrainingBall = ball;
            }
        }

        UpdateTrainingSelectionHighlight(selectedTrainingBall, ballRadiusMeters);
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

        _statusHeaderLabel.Text = BuildStatusHeaderText();
        _statusHeaderLabel.Modulate = ResolveStatusAccentColor();
        _statusAccentBar.Color = ResolveStatusAccentColor();

        _statusLabel.Text =
            $"Portable core: {_tableSpec.Name}\n" +
            $"{BuildModeStatusLine()}\n" +
            $"Phase: {_world.Phase}  SimTime: {_world.SimulationTimeSeconds:0.000}s  FixedSteps: {_world.TotalFixedStepsExecuted}\n" +
            $"CueBall: {cueBallStatus}  Aim: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg  Speed: {_strikeSpeedMetersPerSecond:0.00} m/s  Tip: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})  Overlay: {BuildOverlaySummary()}\n" +
            $"Camera: {GetActiveCameraPreset().Name}  Zoom: {_cameraZoomScale:0.00}x\n" +
            $"Debug tuning: {GetTuningFieldLabel(_selectedTuningField)} = {GetSelectedTuningValueText()}\n" +
            "Controls: F1 debug  F2/F3 tune select  F4/F5 tune -/+ (Shift coarse)  Tab mode  H hardcode overlay  1 cloth  2 cushions  3 jaws  4 pockets  5 spots  C camera preset  Q/E zoom  A/D aim  W/S speed  J/L side spin  I/K follow-draw  Arrow keys move selected placement ball  Z/X cycle practice ball  Space shoot  Backspace center tip  R reset\n" +
            $"Recent shot events:\n{recentEventText}\n" +
            $"Rules/training:\n{recentRuleText}";

        UpdateDebugPanel();
    }

    private void ResetShotSummary()
    {
        if (_ruleMode == RuleMode.Training)
        {
            SetShotSummary(
                "Last FreePlay Shot | Ready",
                "No completed freeplay shot yet.\nCue ball placement is free, and FreePlay keeps the table open for layout setup.",
                new Color(0.47f, 0.86f, 0.88f, 0.98f));
            return;
        }

        SetShotSummary(
            "Last EightBall Shot | Ready",
            $"No completed shot yet.\n{GetPlayerLabel(_eightBallState.CurrentPlayer)} is on the break and the table is open.",
            new Color(0.45f, 0.76f, 0.98f, 0.98f));
    }

    private void ApplyEightBallShotSummary(EightBallTurnResult turnResult)
    {
        var summary = turnResult.Summary;
        var header = turnResult.NextState.Winner.HasValue
            ? $"Last EightBall Shot | Winner: {GetPlayerLabel(turnResult.NextState.Winner.Value)}"
            : turnResult.IsFoul
                ? "Last EightBall Shot | Foul"
                : turnResult.PlayerContinues
                    ? "Last EightBall Shot | Table Run"
                    : "Last EightBall Shot | Turn End";
        var summaryText =
            $"Shooter: {GetPlayerLabel(turnResult.ShootingPlayer)}  Shot: {turnResult.NextState.ShotNumber}\n" +
            $"Outcome: {BuildEightBallOutcomeSummary(turnResult)}\n" +
            $"First contact: {FormatOptionalBallLabel(summary.FirstContactBallNumber)}  Pocketed: {FormatBallNumberListOrNone(summary.PocketedBallNumbers)}\n" +
            $"Object rails: {FormatBallNumberListOrNone(summary.DistinctObjectBallRailContacts)}  Rail/pocket after contact: {FormatYesNo(summary.HasRailOrPocketAfterFirstContact)}  Scratch: {FormatYesNo(summary.IsScratch)}";

        SetShotSummary(
            header,
            summaryText,
            ResolveEightBallBannerStyle(turnResult).BorderColor);
    }

    private void ApplyTrainingShotSummary(TrainingTurnResult turnResult)
    {
        var summary = turnResult.Summary;
        var header = summary.IsScratch
            ? "Last FreePlay Shot | Scratch"
            : summary.PocketedBallNumbers.Count > 0
                ? "Last FreePlay Shot | Pocketed"
                : "Last FreePlay Shot | Settled";
        var summaryText =
            $"FreePlay shot: {turnResult.NextState.ShotCount}\n" +
            $"Outcome: {BuildTrainingOutcomeSummary(turnResult)}\n" +
            $"First contact: {FormatOptionalBallLabel(summary.FirstContactBallNumber)}  Pocketed: {FormatBallNumberListOrNone(summary.PocketedBallNumbers)}\n" +
            $"Object rails: {FormatBallNumberListOrNone(summary.DistinctObjectBallRailContacts)}  Rail/pocket after contact: {FormatYesNo(summary.HasRailOrPocketAfterFirstContact)}  Scratch: {FormatYesNo(summary.IsScratch)}";

        SetShotSummary(
            header,
            summaryText,
            ResolveTrainingBannerStyle(turnResult).BorderColor);
    }

    private void SetShotSummary(string header, string text, Color accentColor)
    {
        _summaryHeaderLabel.Text = header;
        _summaryHeaderLabel.Modulate = accentColor;
        _summaryAccentBar.Color = accentColor;
        _summaryLabel.Text = text;
    }

    private string BuildEightBallOutcomeSummary(EightBallTurnResult turnResult)
    {
        var parts = new List<string>();
        if (turnResult.NextState.ShotNumber == 1)
        {
            parts.Add(turnResult.BreakWasLegal ? "legal break" : "illegal break");
        }

        if (turnResult.IsFoul)
        {
            parts.Add($"foul: {string.Join(", ", turnResult.Fouls)}");
        }

        if (turnResult.AssignedGroup.HasValue)
        {
            parts.Add($"claimed {turnResult.AssignedGroup.Value}");
        }

        if (turnResult.RequiresEightBallRespot)
        {
            parts.Add("8-ball respot");
        }

        if (turnResult.NextState.Winner.HasValue)
        {
            parts.Add($"winner {GetPlayerLabel(turnResult.NextState.Winner.Value)}");
        }
        else if (turnResult.PlayerContinues)
        {
            parts.Add($"{GetPlayerLabel(turnResult.ShootingPlayer)} continues");
        }
        else
        {
            parts.Add($"next: {GetPlayerLabel(turnResult.NextState.CurrentPlayer)}");
        }

        if (turnResult.NextState.BallInHandPlayer.HasValue)
        {
            parts.Add($"ball in hand {GetPlayerLabel(turnResult.NextState.BallInHandPlayer.Value)}");
        }

        return string.Join(" | ", parts);
    }

    private string BuildTrainingOutcomeSummary(TrainingTurnResult turnResult)
    {
        var parts = new List<string>();

        if (turnResult.Summary.PocketedBallNumbers.Count > 0)
        {
            parts.Add($"pocketed {FormatBallNumberList(turnResult.Summary.PocketedBallNumbers)}");
        }
        else
        {
            parts.Add("no balls pocketed");
        }

        if (turnResult.RequiresEightBallRespot)
        {
            parts.Add("8-ball respot");
        }

        if (turnResult.CanRepositionCueBallAnywhere)
        {
            parts.Add("cue ball free placement");
        }

        return string.Join(" | ", parts);
    }

    private string BuildModeStatusLine()
    {
        if (_ruleMode == RuleMode.Training)
        {
            var pocketed = _trainingState.PocketedObjectBallNumbers.Count == 0
                ? "none"
                : string.Join(", ", _trainingState.PocketedObjectBallNumbers);
            return $"FreePlay shots: {_trainingState.ShotCount}  Selected ball: {GetTrainingSelectionLabel()}  Free placement: true  Pocketed objects: {pocketed}";
        }

        var winnerText = _eightBallState.Winner.HasValue ? GetPlayerLabel(_eightBallState.Winner.Value) : "none";
        var ballInHandText = _eightBallState.BallInHandPlayer.HasValue ? GetPlayerLabel(_eightBallState.BallInHandPlayer.Value) : "none";
        return
            $"Current player: {GetPlayerLabel(_eightBallState.CurrentPlayer)}  Break shot: {_eightBallState.IsBreakShot}  Open table: {_eightBallState.OpenTable}  " +
            $"Groups: P1={_eightBallState.PlayerOneGroup} P2={_eightBallState.PlayerTwoGroup}  Ball in hand: {ballInHandText}  Winner: {winnerText}";
    }

    private string BuildStatusHeaderText()
    {
        if (_ruleMode == RuleMode.Training)
        {
            return $"FreePlay | Selected {GetTrainingSelectionLabel()} | Cue Ball In Hand";
        }

        if (_eightBallState.Winner.HasValue)
        {
            return $"EightBall | Winner: {GetPlayerLabel(_eightBallState.Winner.Value)}";
        }

        var groupText = _eightBallState.OpenTable
            ? "Open Table"
            : $"P1 {_eightBallState.PlayerOneGroup} / P2 {_eightBallState.PlayerTwoGroup}";
        var ballInHandText = _eightBallState.BallInHandPlayer == _eightBallState.CurrentPlayer
            ? " | Ball In Hand"
            : string.Empty;
        return $"EightBall | {GetPlayerLabel(_eightBallState.CurrentPlayer)} To Shoot | {groupText}{ballInHandText}";
    }

    private Color ResolveStatusAccentColor()
    {
        if (_ruleMode == RuleMode.Training)
        {
            return new Color(0.47f, 0.86f, 0.88f, 0.98f);
        }

        if (_eightBallState.Winner.HasValue)
        {
            return new Color(0.99f, 0.85f, 0.31f, 0.98f);
        }

        if (HasRecentRulePrefix("Foul:"))
        {
            return new Color(0.97f, 0.42f, 0.38f, 0.98f);
        }

        if (_eightBallState.BallInHandPlayer == _eightBallState.CurrentPlayer)
        {
            return new Color(0.98f, 0.68f, 0.34f, 0.98f);
        }

        return _eightBallState.CurrentPlayer == PlayerSlot.PlayerOne
            ? new Color(0.45f, 0.76f, 0.98f, 0.98f)
            : new Color(0.98f, 0.62f, 0.36f, 0.98f);
    }

    private bool HasRecentRulePrefix(string prefix)
    {
        return _recentRuleNotes.Any(note => note.StartsWith(prefix, StringComparison.Ordinal));
    }

    private string GetRuleModeLabel()
    {
        return _ruleMode == RuleMode.EightBall ? "EightBall" : "FreePlay";
    }

    private static string GetPlayerLabel(PlayerSlot player)
    {
        return player == PlayerSlot.PlayerOne ? "Player 1" : "Player 2";
    }

    private void UpdateTrainingSelectionHighlight(BallState? selectedBall, float ballRadiusMeters)
    {
        if (_trainingSelectionRoot == null)
        {
            return;
        }

        var canShowHighlight = _ruleMode == RuleMode.Training &&
                               CanAdjustPlacement() &&
                               selectedBall is { IsPocketed: false };
        _trainingSelectionRoot.Visible = canShowHighlight;
        if (!canShowHighlight || selectedBall == null)
        {
            return;
        }

        var pulse = TrainingSelectionRingBaseScale +
                    (Mathf.Sin(_trainingSelectionPulseSeconds * TrainingSelectionRingPulseSpeed) * TrainingSelectionRingPulseAmplitude);
        _trainingSelectionRoot.Position = ToGodotPoint(selectedBall.Value.Position, ballRadiusMeters + 0.004f);
        _trainingSelectionRoot.Scale = new Vector3(pulse, 1.0f, pulse);
    }

    private string FormatOptionalBallLabel(int? ballNumber)
    {
        return ballNumber.HasValue ? FormatBallLabel(ballNumber.Value) : "none";
    }

    private string FormatBallNumberListOrNone(IEnumerable<int> ballNumbers)
    {
        var values = ballNumbers.ToArray();
        return values.Length == 0 ? "none" : FormatBallNumberList(values);
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "yes" : "no";
    }

    private void UpdateOverlayVisibility()
    {
        _hardcodeOverlayRoot.Visible = _hardcodeOverlayVisible || _debugModeEnabled;
        _overlayClothRoot.Visible = _overlayClothVisible;
        _overlayCushionRoot.Visible = _overlayCushionVisible;
        _overlayJawRoot.Visible = _overlayJawVisible;
        _overlayPocketRoot.Visible = _overlayPocketVisible;
        _overlaySpotRoot.Visible = _overlaySpotVisible;
    }

    private void UpdateDebugPanel()
    {
        _debugPanel.Visible = _debugModeEnabled;
        _debugLabel.Visible = _debugModeEnabled;
        if (!_debugModeEnabled)
        {
            return;
        }

        _debugLabel.Text = BuildDebugText();
    }

    private string BuildDebugText()
    {
        var cueBall = _world.Balls.FirstOrDefault(ball => ball.BallNumber == 0);
        var selectedBall = _world.Balls.FirstOrDefault(ball => ball.BallNumber == GetPlacementBallNumber());
        var movingBalls = _world.Balls
            .Where(ball => !ball.IsPocketed && ball.Velocity.LengthSquared() > 0.000001f)
            .OrderBy(ball => ball.BallNumber)
            .ToArray();
        var pocketedCount = _world.Balls.Count(ball => ball.IsPocketed);
        var preview = _cachedAimPreview;
        var previewPrimary = preview == null ? 0.0f : NumericsVector2.Distance(preview.CueStart, preview.PrimaryCueEnd);
        var previewSecondary = preview?.SecondaryCueEnd is NumericsVector2 secondaryCueEnd
            ? NumericsVector2.Distance(preview.PrimaryCueEnd, secondaryCueEnd)
            : 0.0f;
        var previewTarget = preview?.TargetStart is NumericsVector2 targetStart && preview.TargetEnd is NumericsVector2 targetEnd
            ? NumericsVector2.Distance(targetStart, targetEnd)
            : 0.0f;
        var movingSummary = movingBalls.Length == 0
            ? "none"
            : string.Join(", ", movingBalls.Take(6).Select(ball => $"{ball.BallNumber}:{ball.Velocity.Length():0.000}"));

        return
            "Debug Mode\n" +
            $"Table: {_tableSpec.Name}  Source: {_tableSpec.SourceBlendPath}\n" +
            $"Cloth: min={FormatVector(_tableSpec.ClothMin)} max={FormatVector(_tableSpec.ClothMax)}  BallD={_tableSpec.BallDiameterMeters:0.00000}\n" +
            $"Geometry: cushions={_tableSpec.Cushions.Count} jaws={_tableSpec.JawSegments.Count} pockets={_tableSpec.Pockets.Count} overlay={BuildOverlaySummary()}\n" +
            $"Config: dt={_config.FixedStepSeconds:0.000000} settle={_config.SettleSpeedThresholdMetersPerSecond:0.0000} slide={_config.SlidingFrictionAccelerationMetersPerSecondSquared:0.000} roll={_config.RollingFrictionAccelerationMetersPerSecondSquared:0.000}\n" +
            $"Config: spin_decay={_config.SpinDecayRpsPerSecond:0.000} side_curve={_config.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps:0.0000} move_side_decay={_config.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond:0.000}\n" +
            $"Config: ball_rest={_config.BallCollisionRestitution:0.00} ball_tangent={_config.BallCollisionTangentialTransferFactor:0.00} ball_spin={_config.BallCollisionSpinTransferFactor:0.00}\n" +
            $"Config: rail_rest={_config.BoundaryRestitution:0.00} rail_friction={_config.BoundaryTangentialFrictionFactor:0.00} rail_spin={_config.BoundarySpinTransferFactor:0.00} pair_iter={_config.MaxCollisionIterationsPerStep} rail_iter={_config.MaxBoundaryIterationsPerStep}\n" +
            $"Tuning: selected={GetTuningFieldLabel(_selectedTuningField)} value={GetSelectedTuningValueText()} controls=F2/F3 select, F4/F5 adjust, Shift=coarse\n" +
            $"World: phase={_world.Phase} sim_t={_world.SimulationTimeSeconds:0.000} acc={_world.AccumulatorSeconds:0.000000} steps={_world.TotalFixedStepsExecuted} capture={_shotCaptureActive} frames={_capturedShotFrames.Count}\n" +
            $"Camera: preset={GetActiveCameraPreset().Name} zoom={_cameraZoomScale:0.00} pos=({_camera.Position.X:0.000},{_camera.Position.Y:0.000},{_camera.Position.Z:0.000})\n" +
            $"Rules: mode={GetRuleModeLabel()} debug_overlay_manual={_hardcodeOverlayVisible} debug_enabled={_debugModeEnabled}\n" +
            $"Cue: pos={FormatVector(cueBall.Position)} vel={FormatVector(cueBall.Velocity)} spin={FormatSpin(cueBall.Spin)} pocketed={cueBall.IsPocketed}\n" +
            $"Selected: {GetTrainingSelectionLabel()} pos={FormatVector(selectedBall.Position)} vel={FormatVector(selectedBall.Velocity)} spin={FormatSpin(selectedBall.Spin)} pocketed={selectedBall.IsPocketed}\n" +
            $"Balls: moving={movingBalls.Length} pocketed={pocketedCount} moving_list={movingSummary}\n" +
            $"Preview: dirty={_aimPreviewDirty} primary={previewPrimary:0.000} secondary={previewSecondary:0.000} target={previewTarget:0.000}\n" +
            $"State: {BuildDebugStateLine()}";
    }

    private void RebuildWorldWithCurrentState()
    {
        var preservedBalls = _world.Balls.ToArray();
        _world = new SimulationWorld(_tableSpec, _config, preservedBalls);
        MarkAimPreviewDirty();
        SyncBallVisuals(_world.Balls);
        UpdateCueGuide();
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
        int? maxCollisionIterationsPerStep = null,
        float? boundaryRestitution = null,
        float? boundaryTangentialFrictionFactor = null,
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
            maxCollisionIterationsPerStep: maxCollisionIterationsPerStep ?? _config.MaxCollisionIterationsPerStep,
            boundaryRestitution: boundaryRestitution ?? _config.BoundaryRestitution,
            boundaryTangentialFrictionFactor: boundaryTangentialFrictionFactor ?? _config.BoundaryTangentialFrictionFactor,
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
            DebugTuningField.RailRestitution => $"{_config.BoundaryRestitution:0.000}",
            DebugTuningField.RailTangentialFriction => $"{_config.BoundaryTangentialFrictionFactor:0.000}",
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
            DebugTuningField.RailRestitution => "Rail Restitution",
            DebugTuningField.RailTangentialFriction => "Rail Tangential Friction",
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
               left.MaxCollisionIterationsPerStep == right.MaxCollisionIterationsPerStep &&
               left.BoundaryRestitution == right.BoundaryRestitution &&
               left.BoundaryTangentialFrictionFactor == right.BoundaryTangentialFrictionFactor &&
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

    private string BuildDebugStateLine()
    {
        if (_ruleMode == RuleMode.Training)
        {
            return $"freeplay_shots={_trainingState.ShotCount} cue_ball_in_hand={_trainingState.CueBallInHand} selected={GetTrainingSelectionLabel()}";
        }

        return
            $"current={GetPlayerLabel(_eightBallState.CurrentPlayer)} open={_eightBallState.OpenTable} groups=P1:{_eightBallState.PlayerOneGroup}/P2:{_eightBallState.PlayerTwoGroup} " +
            $"break={_eightBallState.IsBreakShot} winner={_eightBallState.Winner?.ToString() ?? "none"} bih={_eightBallState.BallInHandPlayer?.ToString() ?? "none"}";
    }

    private static string FormatVector(NumericsVector2 value)
    {
        return $"({value.X:0.000},{value.Y:0.000})";
    }

    private static string FormatSpin(SpinState spin)
    {
        return $"({spin.SideSpinRps:0.000},{spin.ForwardSpinRps:0.000},{spin.VerticalSpinRps:0.000})";
    }

    private void UpdateShotBanner(float deltaSeconds)
    {
        if (_shotBannerSecondsRemaining <= 0.0f)
        {
            return;
        }

        _shotBannerSecondsRemaining = Mathf.Max(0.0f, _shotBannerSecondsRemaining - deltaSeconds);
        if (_shotBannerSecondsRemaining > 0.0f)
        {
            return;
        }

        _shotBannerPanel.Visible = false;
        _shotBannerLabel.Visible = false;
        _shotBannerLabel.Text = string.Empty;
    }

    private void ShowShotBanner(string text, ShotBannerStyle style, float durationSeconds)
    {
        _shotBannerSecondsRemaining = Mathf.Max(durationSeconds, 0.2f);
        _shotBannerPanel.Visible = true;
        _shotBannerLabel.Visible = true;
        _shotBannerLabel.Text = text;
        _shotBannerLabel.Modulate = style.TextColor;
        _shotBannerPanel.AddThemeStyleboxOverride(
            "panel",
            CreateHudPanelStyle(style.BackgroundColor, style.BorderColor));
    }

    private Vector3 GetTableCenter3D()
    {
        var center = (_tableSpec.ClothMin + _tableSpec.ClothMax) * 0.5f;
        return new Vector3(center.X, 0.0f, center.Y);
    }

    private string BuildOverlaySummary()
    {
        return
            $"{(_hardcodeOverlayRoot.Visible ? "on" : "off")} " +
            $"[cloth={_overlayClothVisible} cushions={_overlayCushionVisible} jaws={_overlayJawVisible} pockets={_overlayPocketVisible} spots={_overlaySpotVisible}]";
    }

    private string BuildEightBallTurnBanner(EightBallTurnResult turnResult)
    {
        if (turnResult.NextState.Winner.HasValue)
        {
            return $"Winner: {GetPlayerLabel(turnResult.NextState.Winner.Value)}";
        }

        if (turnResult.IsFoul)
        {
            return $"Foul: {string.Join(", ", turnResult.Fouls)}";
        }

        if (turnResult.RequiresEightBallRespot)
        {
            return "8-ball respot required";
        }

        if (turnResult.AssignedGroup.HasValue)
        {
            return $"{GetPlayerLabel(turnResult.ShootingPlayer)} claims {turnResult.AssignedGroup.Value}";
        }

        if (turnResult.PlayerContinues)
        {
            return $"{GetPlayerLabel(turnResult.ShootingPlayer)} continues";
        }

        if (turnResult.NextState.BallInHandPlayer.HasValue)
        {
            return $"Ball in hand: {GetPlayerLabel(turnResult.NextState.BallInHandPlayer.Value)}";
        }

        return $"Turn to {GetPlayerLabel(turnResult.NextState.CurrentPlayer)}";
    }

    private static ShotBannerStyle ResolveEightBallBannerStyle(EightBallTurnResult turnResult)
    {
        if (turnResult.NextState.Winner.HasValue)
        {
            return new ShotBannerStyle(
                new Color(0.22f, 0.17f, 0.03f, 0.94f),
                new Color(0.98f, 0.84f, 0.29f, 0.98f),
                new Color(1.0f, 0.98f, 0.9f));
        }

        if (turnResult.IsFoul || turnResult.RequiresEightBallRespot)
        {
            return new ShotBannerStyle(
                new Color(0.24f, 0.1f, 0.06f, 0.94f),
                new Color(0.98f, 0.54f, 0.28f, 0.98f),
                new Color(1.0f, 0.95f, 0.92f));
        }

        return new ShotBannerStyle(
            new Color(0.08f, 0.18f, 0.1f, 0.92f),
            new Color(0.48f, 0.86f, 0.54f, 0.96f),
            new Color(0.94f, 1.0f, 0.95f));
    }

    private static float ResolveEightBallBannerDuration(EightBallTurnResult turnResult)
    {
        if (turnResult.NextState.Winner.HasValue)
        {
            return 3.6f;
        }

        if (turnResult.IsFoul || turnResult.RequiresEightBallRespot)
        {
            return 2.8f;
        }

        return 2.2f;
    }

    private string BuildTrainingTurnBanner(TrainingTurnResult turnResult)
    {
        var pocketedObjectBalls = turnResult.Summary.PocketedBallNumbers
            .Where(ballNumber => ballNumber != 0)
            .ToArray();

        if (turnResult.RequiresEightBallRespot)
        {
            return "FreePlay: 8-ball respot required";
        }

        if (pocketedObjectBalls.Length > 0)
        {
            return $"FreePlay pocketed {FormatBallNumberList(pocketedObjectBalls)}";
        }

        return $"FreePlay shot {_trainingState.ShotCount} settled";
    }

    private static ShotBannerStyle ResolveTrainingBannerStyle(TrainingTurnResult turnResult)
    {
        var pocketedObjectBalls = turnResult.Summary.PocketedBallNumbers
            .Where(ballNumber => ballNumber != 0)
            .ToArray();

        if (turnResult.RequiresEightBallRespot)
        {
            return new ShotBannerStyle(
                new Color(0.24f, 0.1f, 0.06f, 0.94f),
                new Color(0.98f, 0.54f, 0.28f, 0.98f),
                new Color(1.0f, 0.95f, 0.92f));
        }

        if (pocketedObjectBalls.Length > 0)
        {
            return new ShotBannerStyle(
                new Color(0.07f, 0.19f, 0.1f, 0.92f),
                new Color(0.45f, 0.88f, 0.53f, 0.98f),
                new Color(0.93f, 1.0f, 0.94f));
        }

        return new ShotBannerStyle(
            new Color(0.08f, 0.18f, 0.22f, 0.9f),
            new Color(0.48f, 0.83f, 0.92f, 0.95f),
            new Color(0.95f, 0.99f, 1.0f));
    }

    private static float ResolveTrainingBannerDuration(TrainingTurnResult turnResult)
    {
        return turnResult.RequiresEightBallRespot ? 2.6f : 2.0f;
    }

    private string FormatBallNumberList(IEnumerable<int> ballNumbers)
    {
        return string.Join(", ", ballNumbers.Select(FormatBallLabel));
    }

    private string FormatBallLabel(int ballNumber)
    {
        return ballNumber == 0 ? "CueBall" : $"Ball_{ballNumber:00}";
    }

    private void AddOverlaySegment(
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

    private readonly record struct ComputerShotCandidate(
        int TargetBallNumber,
        ShotInput Shot);

    private readonly record struct ComputerShotPlan(
        NumericsVector2? CueBallPlacement,
        ShotInput Shot,
        float Score,
        string Description);
}
