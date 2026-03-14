using System.Linq;
using System.Text;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Rules;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;
using GodotEnvironment = Godot.Environment;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main : Node3D
{
	private static readonly Vector2I DefaultWindowSize = new(1920, 1080);
	private static readonly Vector2I DefaultDebugWindowSize = new(640, 860);
	private static readonly Vector2I DefaultTuningWindowSize = new(860, 980);
	private const string ImportedTableSourceNodeName = "ImportedTableSource";

	private const string ImportedTableScenePath = "res://art/customtable_9ft.blend";
	private const string CalibrationProfilePath = "user://table_calibration.json";
	private const float FeltThicknessMeters = 0.04f;
	private const float FrameThicknessMeters = 0.1f;
	private const float FrameOverhangMeters = 0.22f;
	private const float BottomThicknessMeters = 0.08f;
	private const float RailVisualWidthMeters = 0.12f;
	private const float RailVisualHeightMeters = 0.1f;
	private const float PocketDepthMeters = 0.08f;
	private const float CueGuideThicknessMeters = 0.012f;
	private const float CueGuideHeightMeters = 0.012f;
	private const float AimGuideThicknessPixels = 2.0f;
	private const float AimGuideHeightMeters = 0.01f;
	private const float ComputerTurnThinkDelaySeconds = 0.8f;
	private const int ComputerMaxSimulationSteps = 900;
	private const int ComputerMaxTargetBallsToConsider = 4;
	private const float OverlayLineThicknessPixels = 1.5f;
	private const float MinOverlayThicknessPixels = 0.5f;
	private const float MaxOverlayThicknessPixels = 2.0f;
	private const float OverlayLineHeightMeters = 0.008f;
	private const int OverlayPocketSegments = 20;
	private const float AimTurnRadiansPerSecond = 1.8f;
	private const float MouseWheelAimStepDegrees = 0.2f;
	private const float StrikeSpeedAdjustPerSecond = 1.5f;
	private const float TipAdjustPerSecond = 1.2f;
	private const float CueBallPlacementMetersPerSecond = 0.9f;
	private const float MinimumStrikeSpeedMetersPerSecond = 0.3f;
	private const float MaximumRegularStrikeSpeedMetersPerSecond = 5.0f;
	private const float MaximumBreakStrikeSpeedMetersPerSecond = 8.0f;
	private const float DefaultStrikeSpeedMetersPerSecond = 2.2f;
	private const float BallVisualTeleportResetMeters = 0.4f;
	private const float CueStickTipGapMeters = 0.03f;
	private const float CueStickPowerPullbackMeters = 0.18f;
	private const int AimPreviewPostInteractionFrames = 48;
	private const int AimPreviewMaxSteps = 360;

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
	private readonly Dictionary<int, Quaternion> _ballVisualBaseRotations = new();
	private readonly Dictionary<int, Vector3> _ballVisualLastPositions = new();
	private readonly List<CalibrationObjectEntry> _calibrationObjects = new();
	private readonly float[] _computerRegularStrikeSpeeds = [1.4f, 2.1f, 2.9f, 3.8f, 4.8f];
	private readonly float[] _computerBreakStrikeSpeeds = [5.4f, 6.4f, 7.4f];
	private readonly CameraPreset[] _cameraPresets =
	[
		new("Broadcast", new Vector3(-0.55f, 2.6f, 1.95f), false, 0.0f, 46.0f),
		new("TopDown", new Vector3(0.0f, 3.45f, 0.001f), true, 1.72f, 0.0f),
		new("FootRail", new Vector3(-2.35f, 1.38f, 0.0f), false, 0.0f, 38.0f),
		new("SideRail", new Vector3(0.0f, 1.82f, 2.35f), false, 0.0f, 40.0f)
	];

	private readonly List<CalibrationField> _calibrationFields = new();
	private TableSpec _baseTableSpec = null!;
	private TableSpec _tableSpec = null!;
	private TableCalibrationProfile _tableCalibrationProfile = null!;
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
	private Node3D? _importedCueStick;
	private Camera3D _camera = null!;
	private Panel _shotBannerPanel = null!;
	private Label _shotBannerLabel = null!;
	private ColorRect _menuOverlay = null!;
	private Panel _menuPanel = null!;
	private Label _menuTitleLabel = null!;
	private Label _menuSubtitleLabel = null!;
	private Label _menuModeLabel = null!;
	private Button _menuPlayEightBallButton = null!;
	private Button _menuPlayFreePlayButton = null!;
	private Button _menuOpenTuningButton = null!;
	private Button _menuResumeButton = null!;
	private Button _menuResetButton = null!;
	private Button _menuReturnToMenuButton = null!;
	private Panel _statusPanel = null!;
	private ColorRect _statusAccentBar = null!;
	private Label _statusHeaderLabel = null!;
	private Panel _aimPanel = null!;
	private Label _aimHeaderLabel = null!;
	private Label _aimMetricsLabel = null!;
	private ColorRect _aimSpeedTrack = null!;
	private ColorRect _aimSpeedFill = null!;
	private Panel _aimTipPad = null!;
	private ColorRect _aimTipHorizontal = null!;
	private ColorRect _aimTipVertical = null!;
	private ColorRect _aimTipIndicator = null!;
	private Window _tuningWindow = null!;
	private Label _tuningWindowHeaderLabel = null!;
	private Button _tuningInfoToggleButton = null!;
	private Label _tuningWindowInfoLabel = null!;
	private OptionButton _tuningFieldSelector = null!;
	private PanelContainer _tuningObjectDetailsPanel = null!;
	private Label _tuningObjectDetailsLabel = null!;
	private GridContainer _tuningObjectMiniPanelGrid = null!;
	private readonly List<TuningMiniPanel> _tuningMiniPanels = new();
	private PanelContainer _tuningLegendPanel = null!;
	private Label _tuningLegendHeaderLabel = null!;
	private ScrollContainer _tuningLegendScrollContainer = null!;
	private VBoxContainer _tuningLegendRows = null!;
	private ScrollContainer _tuningScrollContainer = null!;
	private VBoxContainer _tuningFieldsContainer = null!;
	private readonly List<TuningFieldRow> _tuningFieldRows = new();
	private Label _tuningOverlayLabel = null!;
	private HSlider _tuningOverlaySlider = null!;
	private Button _tuningSaveButton = null!;
	private Button _tuningReloadButton = null!;
	private Button _tuningResetButton = null!;
	private Panel _helpPanel = null!;
	private Label _helpHeaderLabel = null!;
	private Label _helpLabel = null!;
	private Panel _summaryPanel = null!;
	private ColorRect _summaryAccentBar = null!;
	private Label _summaryHeaderLabel = null!;
	private Label _summaryLabel = null!;
	private Window _debugWindow = null!;
	private Panel _debugPanel = null!;
	private Label _debugHeaderLabel = null!;
	private Label _statusLabel = null!;
	private TextEdit _debugLabel = null!;
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
	private bool _tuningInfoVisible = true;
	private bool _hardcodeOverlayVisible = true;
	private bool _overlayClothVisible = true;
	private bool _overlayCushionVisible = true;
	private bool _overlayJawVisible = true;
	private bool _overlayPocketVisible = true;
	private bool _overlaySpotVisible = true;
	private bool _helpPanelVisible = true;
	private bool _hudVisible = true;
	private bool _menuVisible = true;
	private bool _sessionStarted;
	private DebugTuningField _selectedTuningField = DebugTuningField.SlidingFriction;
	private int _selectedCalibrationFieldIndex;
	private int _trainingSelectedBallNumber;
	private bool _syncingTuningControls;
	private string _selectedCalibrationObjectKey = string.Empty;
	private string _selectedMiniPanelObjectKey = string.Empty;
	private bool _draggingAimPanel;
	private Vector2 _aimPanelDragOffset = Vector2.Zero;
	private int _cameraPresetIndex = 1;
	private float _cameraZoomScale = 1.0f;
	private float _overlayLineThicknessPixels = OverlayLineThicknessPixels;
	private float _shotBannerSecondsRemaining;
	private float _computerTurnThinkSeconds;
	private float _aimAngleRadians;
	private float _cueStickBaseOffsetMeters = 0.8f;
	private float _cueStickHeightMeters = 0.066f;
	private Quaternion _cueStickLookCorrection;
	private float _strikeSpeedMetersPerSecond = DefaultStrikeSpeedMetersPerSecond;
	private Vector2 _tipOffsetNormalized = Vector2.Zero;

	public override void _Ready()
	{
		ConfigureWindowDefaults();
		_baseTableSpec = CustomTable9FtSpec.Create();
		_tableCalibrationProfile = TableCalibrationProfile.LoadOrDefault(GetCalibrationProfileAbsolutePath(), _baseTableSpec);
		_tableSpec = TableCalibrationBuilder.Apply(_baseTableSpec, _tableCalibrationProfile);
		BuildCalibrationFields();
		_config = SimulationConfig.Default;
		_world = new SimulationWorld(_tableSpec, _config, StandardEightBallRack.Create(_tableSpec));

		ConfigureSceneGraph();
		ConfigureCameraAndLighting();
		BuildTableVisual();
		BuildHardcodeOverlay();
		BuildBallVisuals();
		ResetSessionForCurrentMode();
		ReturnToStartMenu();
	}

	private static void ConfigureWindowDefaults()
	{
		if (DisplayServer.GetName() == "headless")
		{
			return;
		}

		DisplayServer.WindowSetSize(DefaultWindowSize);
	}

	public override void _Process(double delta)
	{
		var deltaSeconds = (float)delta;
		UpdateShotBanner(deltaSeconds);

		if (_menuVisible)
		{
			UpdateStatusLabel(Array.Empty<ShotEvent>());
			return;
		}

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
}
