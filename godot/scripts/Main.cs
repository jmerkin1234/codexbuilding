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
	private const string ImportedTableSourceNodeName = "ImportedTableSource";
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
	private const float AimGuideThicknessMeters = 0.008f;
	private const float AimGuideHeightMeters = 0.01f;
	private const float ComputerTurnThinkDelaySeconds = 0.8f;
	private const int ComputerMaxSimulationSteps = 900;
	private const int ComputerMaxTargetBallsToConsider = 4;
	private const float OverlayLineThicknessMeters = 0.01f;
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
	private const float CueStickPowerPullbackMeters = 0.18f;
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
	private readonly Dictionary<int, Quaternion> _ballVisualBaseRotations = new();
	private readonly Dictionary<int, Vector3> _ballVisualLastPositions = new();
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
	private int _cameraPresetIndex = 1;
	private float _cameraZoomScale = 1.0f;
	private float _overlayLineThicknessMeters = OverlayLineThicknessMeters;
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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
		{
			HandleMouseButtonInput(mouseButtonEvent);
			return;
		}

		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.Keycode == Key.Escape)
		{
			ToggleMenuVisibility();
			return;
		}

		switch (keyEvent.Keycode)
		{
			case Key.F1:
				ToggleDebugMode();
				return;
			case Key.F6:
				ToggleHelpPanel();
				return;
			case Key.F7:
				ToggleHudVisibility();
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

		if (_menuVisible)
		{
			switch (keyEvent.Keycode)
			{
				case Key.Key1:
					StartMenuSelection(RuleMode.EightBall);
					break;
				case Key.Key2:
					StartMenuSelection(RuleMode.Training);
					break;
				case Key.Key3:
					StartMenuSelection(RuleMode.Calibration);
					break;
			}

			return;
		}

		if (_world.Phase == SimulationPhase.Running)
		{
			return;
		}

		if (_ruleMode == RuleMode.Calibration)
		{
			switch (keyEvent.Keycode)
			{
				case Key.Comma:
					CycleCalibrationField(-1, keyEvent.ShiftPressed);
					return;
				case Key.Period:
					CycleCalibrationField(1, keyEvent.ShiftPressed);
					return;
				case Key.Minus:
					AdjustSelectedCalibrationField(-1, keyEvent.ShiftPressed);
					return;
				case Key.Equal:
					AdjustSelectedCalibrationField(1, keyEvent.ShiftPressed);
					return;
				case Key.P:
					SaveCalibrationProfile();
					return;
				case Key.O:
					ReloadCalibrationProfile();
					return;
				case Key.U:
					ResetCalibrationProfile();
					return;
			}
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
		var importedTableSource = GetNodeOrNull<Node>(ImportedTableSourceNodeName);
		if (importedTableSource?.GetNodeOrNull<Node3D>("GodotRoot") is { } importedGodotRoot)
		{
			_godotRoot = importedGodotRoot;

			if (importedTableSource.GetNodeOrNull<Camera3D>("Camera") is { } importedCamera)
			{
				importedCamera.Current = false;
				importedCamera.Visible = false;
			}

		}
		else
		{
			_godotRoot = EnsureNode<Node3D>(this, "GodotRoot");
		}

		_tableRoot = EnsureNode<Node3D>(_godotRoot, "TableRoot");
		_ballsRoot = EnsureNode<Node3D>(_godotRoot, "BallsRoot");
		_cueRoot = EnsureNode<Node3D>(_godotRoot, "CueRoot");
		_guideRoot = EnsureNode<Node3D>(_godotRoot, "GuideRoot");
		_hardcodeOverlayRoot = EnsureNode<Node3D>(_godotRoot, "HardcodeOverlayRoot");

		_cueGuide = EnsureNode<MeshInstance3D>(_guideRoot, "CueGuide");
		_cueGuide.Mesh = new BoxMesh();
		_cueGuide.MaterialOverride = CreateMaterial(new Color(0.92f, 0.84f, 0.58f), roughness: 0.65f);
		_cueGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		_importedCueStick = _cueRoot.GetNodeOrNull<Node3D>("CueStick");
		if (_importedCueStick != null)
		{
			_importedCueStick.Visible = true;
			_cueStickHeightMeters = _importedCueStick.Position.Y;
			var originalCueRotation = _importedCueStick.Quaternion;

			if (_ballsRoot.GetNodeOrNull<Node3D>("CueBall") is { } importedCueBall)
			{
				var cueToBall = new Vector3(
					importedCueBall.Position.X - _importedCueStick.Position.X,
					0.0f,
					importedCueBall.Position.Z - _importedCueStick.Position.Z);

				if (cueToBall.LengthSquared() > 0.000001f)
				{
					_cueStickBaseOffsetMeters = cueToBall.Length();
					var lookProbe = new Node3D();
					_cueRoot.AddChild(lookProbe);
					lookProbe.Position = _importedCueStick.Position;
					lookProbe.LookAt(lookProbe.Position + cueToBall.Normalized(), Vector3.Up);
					_cueStickLookCorrection = lookProbe.Quaternion.Inverse() * originalCueRotation;
					lookProbe.QueueFree();
				}
				else
				{
					_cueStickLookCorrection = originalCueRotation;
				}
			}
			else
			{
				_cueStickLookCorrection = originalCueRotation;
			}
		}

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

		var hud = EnsureNode<CanvasLayer>(this, "Hud");
		_menuOverlay = EnsureNode<ColorRect>(hud, "MenuOverlay");
		_menuOverlay.AnchorLeft = 0.0f;
		_menuOverlay.AnchorTop = 0.0f;
		_menuOverlay.AnchorRight = 1.0f;
		_menuOverlay.AnchorBottom = 1.0f;
		_menuOverlay.OffsetLeft = 0.0f;
		_menuOverlay.OffsetTop = 0.0f;
		_menuOverlay.OffsetRight = 0.0f;
		_menuOverlay.OffsetBottom = 0.0f;
		_menuOverlay.Color = new Color(0.01f, 0.02f, 0.04f, 0.72f);

		_menuPanel = EnsureNode<Panel>(_menuOverlay, "MenuPanel");
		_menuPanel.AnchorLeft = 0.5f;
		_menuPanel.AnchorRight = 0.5f;
		_menuPanel.AnchorTop = 0.5f;
		_menuPanel.AnchorBottom = 0.5f;
		_menuPanel.OffsetLeft = -250.0f;
		_menuPanel.OffsetTop = -260.0f;
		_menuPanel.OffsetRight = 250.0f;
		_menuPanel.OffsetBottom = 260.0f;
		_menuPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.03f, 0.06f, 0.09f, 0.95f),
				new Color(0.46f, 0.78f, 0.94f, 0.98f)));

		_menuTitleLabel = EnsureNode<Label>(_menuPanel, "MenuTitleLabel");
		_menuTitleLabel.Position = new Vector2(24.0f, 20.0f);
		_menuTitleLabel.Size = new Vector2(452.0f, 38.0f);
		_menuTitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_menuTitleLabel.AddThemeFontSizeOverride("font_size", 28);
		_menuTitleLabel.Modulate = new Color(0.95f, 0.99f, 1.0f);
		_menuTitleLabel.Text = "CodexBuilding Billiards";

		_menuSubtitleLabel = EnsureNode<Label>(_menuPanel, "MenuSubtitleLabel");
		_menuSubtitleLabel.Position = new Vector2(28.0f, 62.0f);
		_menuSubtitleLabel.Size = new Vector2(444.0f, 82.0f);
		_menuSubtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_menuSubtitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_menuSubtitleLabel.AddThemeFontSizeOverride("font_size", 15);
		_menuSubtitleLabel.Modulate = new Color(0.84f, 0.93f, 0.98f);

		_menuModeLabel = EnsureNode<Label>(_menuPanel, "MenuModeLabel");
		_menuModeLabel.Position = new Vector2(28.0f, 144.0f);
		_menuModeLabel.Size = new Vector2(444.0f, 28.0f);
		_menuModeLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_menuModeLabel.AddThemeFontSizeOverride("font_size", 16);
		_menuModeLabel.Modulate = new Color(0.98f, 0.79f, 0.31f);

		_menuPlayEightBallButton = CreateMenuButton(_menuPanel, "MenuPlayEightBallButton", "Play 8-Ball vs Computer", 188.0f);
		_menuPlayEightBallButton.Pressed += () => StartMenuSelection(RuleMode.EightBall);

		_menuPlayFreePlayButton = CreateMenuButton(_menuPanel, "MenuPlayFreePlayButton", "Open FreePlay", 242.0f);
		_menuPlayFreePlayButton.Pressed += () => StartMenuSelection(RuleMode.Training);

		_menuOpenTuningButton = CreateMenuButton(_menuPanel, "MenuOpenTuningButton", "Open Tuning Mode", 296.0f);
		_menuOpenTuningButton.Pressed += () => StartMenuSelection(RuleMode.Calibration);

		_menuResumeButton = CreateMenuButton(_menuPanel, "MenuResumeButton", "Resume Current Table", 350.0f);
		_menuResumeButton.Pressed += CloseMenu;

		_menuResetButton = CreateMenuButton(_menuPanel, "MenuResetButton", "Reset Current Mode", 404.0f);
		_menuResetButton.Pressed += ResetCurrentModeFromMenu;

		_menuReturnToMenuButton = CreateMenuButton(_menuPanel, "MenuReturnToMenuButton", "Return To Start Screen", 458.0f);
		_menuReturnToMenuButton.Pressed += ReturnToStartMenu;

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
		_statusPanel.Size = new Vector2(860.0f, 246.0f);
		_statusPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_statusPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.02f, 0.05f, 0.07f, 0.82f),
				new Color(0.25f, 0.55f, 0.63f, 0.95f)));

		_statusAccentBar = EnsureNode<ColorRect>(_statusPanel, "StatusAccentBar");
		_statusAccentBar.Position = new Vector2(0.0f, 0.0f);
		_statusAccentBar.Size = new Vector2(860.0f, 6.0f);
		_statusAccentBar.Color = new Color(0.42f, 0.83f, 0.89f, 0.95f);

		_statusHeaderLabel = EnsureNode<Label>(_statusPanel, "StatusHeaderLabel");
		_statusHeaderLabel.Position = new Vector2(16.0f, 18.0f);
		_statusHeaderLabel.Size = new Vector2(828.0f, 34.0f);
		_statusHeaderLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_statusHeaderLabel.VerticalAlignment = VerticalAlignment.Center;
		_statusHeaderLabel.AddThemeFontSizeOverride("font_size", 23);
		_statusHeaderLabel.Modulate = new Color(0.9f, 0.98f, 1.0f);

		_statusLabel = EnsureNode<Label>(_statusPanel, "StatusLabel");
		_statusLabel.Position = new Vector2(16.0f, 58.0f);
		_statusLabel.Size = new Vector2(828.0f, 170.0f);
		_statusLabel.Modulate = Colors.White;
		_statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_statusLabel.VerticalAlignment = VerticalAlignment.Top;
		_statusLabel.AddThemeFontSizeOverride("font_size", 15);

		_summaryPanel = EnsureNode<Panel>(hud, "SummaryPanel");
		_summaryPanel.Position = new Vector2(18.0f, 282.0f);
		_summaryPanel.Size = new Vector2(860.0f, 208.0f);
		_summaryPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_summaryPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.02f, 0.04f, 0.06f, 0.82f),
				new Color(0.31f, 0.63f, 0.72f, 0.95f)));

		_summaryAccentBar = EnsureNode<ColorRect>(_summaryPanel, "SummaryAccentBar");
		_summaryAccentBar.Position = new Vector2(0.0f, 0.0f);
		_summaryAccentBar.Size = new Vector2(860.0f, 6.0f);
		_summaryAccentBar.Color = new Color(0.42f, 0.83f, 0.89f, 0.95f);

		_summaryHeaderLabel = EnsureNode<Label>(_summaryPanel, "SummaryHeaderLabel");
		_summaryHeaderLabel.Position = new Vector2(16.0f, 16.0f);
		_summaryHeaderLabel.Size = new Vector2(828.0f, 30.0f);
		_summaryHeaderLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_summaryHeaderLabel.VerticalAlignment = VerticalAlignment.Center;
		_summaryHeaderLabel.AddThemeFontSizeOverride("font_size", 21);
		_summaryHeaderLabel.Modulate = new Color(0.9f, 0.98f, 1.0f);

		_summaryLabel = EnsureNode<Label>(_summaryPanel, "SummaryLabel");
		_summaryLabel.Position = new Vector2(16.0f, 52.0f);
		_summaryLabel.Size = new Vector2(828.0f, 138.0f);
		_summaryLabel.Modulate = Colors.White;
		_summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_summaryLabel.VerticalAlignment = VerticalAlignment.Top;
		_summaryLabel.AddThemeFontSizeOverride("font_size", 15);

		_aimPanel = EnsureNode<Panel>(hud, "AimPanel");
		_aimPanel.Position = new Vector2(896.0f, 18.0f);
		_aimPanel.Size = new Vector2(440.0f, 220.0f);
		_aimPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_aimPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.04f, 0.05f, 0.09f, 0.84f),
				new Color(0.43f, 0.76f, 0.97f, 0.95f)));

		_aimHeaderLabel = EnsureNode<Label>(_aimPanel, "AimHeaderLabel");
		_aimHeaderLabel.Position = new Vector2(16.0f, 14.0f);
		_aimHeaderLabel.Size = new Vector2(408.0f, 30.0f);
		_aimHeaderLabel.AddThemeFontSizeOverride("font_size", 20);
		_aimHeaderLabel.Modulate = new Color(0.92f, 0.97f, 1.0f);

		_aimMetricsLabel = EnsureNode<Label>(_aimPanel, "AimMetricsLabel");
		_aimMetricsLabel.Position = new Vector2(16.0f, 46.0f);
		_aimMetricsLabel.Size = new Vector2(220.0f, 154.0f);
		_aimMetricsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_aimMetricsLabel.VerticalAlignment = VerticalAlignment.Top;
		_aimMetricsLabel.AddThemeFontSizeOverride("font_size", 15);
		_aimMetricsLabel.Modulate = new Color(0.89f, 0.95f, 1.0f);

		_aimSpeedTrack = EnsureNode<ColorRect>(_aimPanel, "AimSpeedTrack");
		_aimSpeedTrack.Position = new Vector2(16.0f, 166.0f);
		_aimSpeedTrack.Size = new Vector2(220.0f, 12.0f);
		_aimSpeedTrack.Color = new Color(0.14f, 0.18f, 0.23f, 0.95f);

		_aimSpeedFill = EnsureNode<ColorRect>(_aimSpeedTrack, "AimSpeedFill");
		_aimSpeedFill.Position = Vector2.Zero;
		_aimSpeedFill.Size = new Vector2(110.0f, 12.0f);
		_aimSpeedFill.Color = new Color(0.43f, 0.82f, 0.98f, 0.98f);

		_aimTipPad = EnsureNode<Panel>(_aimPanel, "AimTipPad");
		_aimTipPad.Position = new Vector2(268.0f, 48.0f);
		_aimTipPad.Size = new Vector2(140.0f, 140.0f);
		_aimTipPad.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.05f, 0.08f, 0.12f, 0.95f),
				new Color(0.31f, 0.63f, 0.89f, 0.95f)));

		_aimTipHorizontal = EnsureNode<ColorRect>(_aimTipPad, "AimTipHorizontal");
		_aimTipHorizontal.Position = new Vector2(18.0f, 69.0f);
		_aimTipHorizontal.Size = new Vector2(104.0f, 2.0f);
		_aimTipHorizontal.Color = new Color(0.44f, 0.76f, 0.95f, 0.8f);

		_aimTipVertical = EnsureNode<ColorRect>(_aimTipPad, "AimTipVertical");
		_aimTipVertical.Position = new Vector2(69.0f, 18.0f);
		_aimTipVertical.Size = new Vector2(2.0f, 104.0f);
		_aimTipVertical.Color = new Color(0.44f, 0.76f, 0.95f, 0.8f);

		_aimTipIndicator = EnsureNode<ColorRect>(_aimTipPad, "AimTipIndicator");
		_aimTipIndicator.Position = new Vector2(64.0f, 64.0f);
		_aimTipIndicator.Size = new Vector2(12.0f, 12.0f);
		_aimTipIndicator.Color = new Color(0.99f, 0.79f, 0.28f, 0.98f);

		_helpPanel = EnsureNode<Panel>(hud, "HelpPanel");
		_helpPanel.Position = new Vector2(896.0f, 256.0f);
		_helpPanel.Size = new Vector2(440.0f, 234.0f);
		_helpPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_helpPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.04f, 0.06f, 0.08f, 0.84f),
				new Color(0.4f, 0.67f, 0.78f, 0.95f)));

		_helpHeaderLabel = EnsureNode<Label>(_helpPanel, "HelpHeaderLabel");
		_helpHeaderLabel.Position = new Vector2(16.0f, 14.0f);
		_helpHeaderLabel.Size = new Vector2(408.0f, 28.0f);
		_helpHeaderLabel.AddThemeFontSizeOverride("font_size", 19);
		_helpHeaderLabel.Modulate = new Color(0.92f, 0.97f, 1.0f);
		_helpHeaderLabel.Text = "Controls";

		_helpLabel = EnsureNode<Label>(_helpPanel, "HelpLabel");
		_helpLabel.Position = new Vector2(16.0f, 46.0f);
		_helpLabel.Size = new Vector2(408.0f, 172.0f);
		_helpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_helpLabel.VerticalAlignment = VerticalAlignment.Top;
		_helpLabel.AddThemeFontSizeOverride("font_size", 14);
		_helpLabel.Modulate = new Color(0.9f, 0.95f, 0.98f);

		_debugWindow = EnsureNode<Window>(this, "DebugWindow");
		_debugWindow.Title = "CodexBuilding Debug";
		_debugWindow.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen;
		_debugWindow.Size = DefaultDebugWindowSize;
		_debugWindow.MinSize = new Vector2I(520, 620);
		_debugWindow.Unresizable = false;
		_debugWindow.WrapControls = true;
		_debugWindow.Exclusive = false;
		_debugWindow.Transient = false;
		_debugWindow.Visible = false;
		_debugWindow.CloseRequested += OnDebugWindowCloseRequested;

		_debugPanel = EnsureNode<Panel>(_debugWindow, "DebugPanel");
		_debugPanel.Position = new Vector2(0.0f, 0.0f);
		_debugPanel.Size = new Vector2(DefaultDebugWindowSize.X, DefaultDebugWindowSize.Y);
		_debugPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.01f, 0.04f, 0.03f, 0.84f),
				new Color(0.28f, 0.73f, 0.53f, 0.95f)));
		_debugPanel.Visible = false;

		_debugHeaderLabel = EnsureNode<Label>(_debugPanel, "DebugHeaderLabel");
		_debugHeaderLabel.Position = new Vector2(16.0f, 12.0f);
		_debugHeaderLabel.Size = new Vector2(608.0f, 52.0f);
		_debugHeaderLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_debugHeaderLabel.VerticalAlignment = VerticalAlignment.Center;
		_debugHeaderLabel.AddThemeFontSizeOverride("font_size", 16);
		_debugHeaderLabel.Modulate = new Color(0.84f, 0.98f, 0.89f);
		_debugHeaderLabel.Visible = false;

		_debugLabel = EnsureNode<TextEdit>(_debugPanel, "DebugLabel");
		_debugLabel.Position = new Vector2(16.0f, 72.0f);
		_debugLabel.Size = new Vector2(608.0f, 772.0f);
		_debugLabel.Editable = false;
		_debugLabel.ContextMenuEnabled = true;
		_debugLabel.ShortcutKeysEnabled = false;
		_debugLabel.HighlightCurrentLine = false;
		_debugLabel.CaretBlink = false;
		_debugLabel.SelectingEnabled = true;
		_debugLabel.FocusMode = Control.FocusModeEnum.Click;
		_debugLabel.AddThemeFontSizeOverride("font_size", 14);
		_debugLabel.AddThemeColorOverride("font_color", new Color(0.84f, 0.98f, 0.89f));
		_debugLabel.Visible = false;

		UpdateHudVisibility();
	}

	private void ConfigureCameraAndLighting()
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
			if (_ballsRoot.GetNodeOrNull<MeshInstance3D>(GetBallNodeName(ball)) is { } existingBallNode)
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

		AddOverlaySegment(_overlayClothRoot, "OverlayClothTop", topLeft, topRight, ResolveOverlayColor("OverlayClothTop", new Color(0.76f, 0.92f, 0.98f)), 0.020f);
		AddOverlaySegment(_overlayClothRoot, "OverlayClothBottom", bottomLeft, bottomRight, ResolveOverlayColor("OverlayClothBottom", new Color(0.76f, 0.92f, 0.98f)), 0.020f);
		AddOverlaySegment(_overlayClothRoot, "OverlayClothLeft", topLeft, bottomLeft, ResolveOverlayColor("OverlayClothLeft", new Color(0.76f, 0.92f, 0.98f)), 0.020f);
		AddOverlaySegment(_overlayClothRoot, "OverlayClothRight", topRight, bottomRight, ResolveOverlayColor("OverlayClothRight", new Color(0.76f, 0.92f, 0.98f)), 0.020f);

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
				ResolveOverlayColor($"Overlay_{pocket.SourceName}", new Color(0.44f, 0.86f, 0.97f)),
				0.032f);
		}

		AddOverlayCross(_overlaySpotRoot, "OverlayCueBallSpawn", _tableSpec.CueBallSpawn, 0.032f, ResolveOverlayColor("OverlayCueBallSpawn", new Color(0.95f, 0.95f, 0.95f)), 0.036f);
		AddOverlayCross(_overlaySpotRoot, "OverlayRackApexSpot", _tableSpec.RackApexSpot, 0.032f, ResolveOverlayColor("OverlayRackApexSpot", new Color(0.95f, 0.82f, 0.22f)), 0.036f);

		UpdateOverlayVisibility();
	}

	private void ResetSessionForCurrentMode()
	{
		_world.Reset(StandardEightBallRack.Create(_tableSpec));
		_eightBallState = EightBallMatchState.CreateNew();
		_trainingState = TrainingModeState.CreateNew();
		_sessionStarted = true;
		_computerTurnThinkSeconds = 0.0f;
		_trainingSelectedBallNumber = 0;
		_capturedCueStrike = null;
		_capturedShotFrameIndex = 0;
		_shotCaptureActive = false;
		_capturedShotFrames.Clear();
		_recentFrameEvents.Clear();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(GetModeReadyText());
		_aimAngleRadians = GetDefaultAimAngle();
		_strikeSpeedMetersPerSecond = DefaultStrikeSpeedMetersPerSecond;
		_tipOffsetNormalized = Vector2.Zero;
		if (_ruleMode == RuleMode.Calibration)
		{
			_hardcodeOverlayVisible = true;
		}
		ResetShotSummary();
		MarkAimPreviewDirty();

		if (CanPlaceCueBall())
		{
			ResetWorldForNextTurn(cueBallInHand: true, requiresEightBallRespot: false);
		}

		ShowShotBanner(
			GetModeReadyText(),
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
		_ruleMode = _ruleMode switch
		{
			RuleMode.EightBall => RuleMode.Training,
			RuleMode.Training => RuleMode.Calibration,
			_ => RuleMode.EightBall
		};
		ResetSessionForCurrentMode();
	}

	private void StartMenuSelection(RuleMode mode)
	{
		_ruleMode = mode;
		ResetSessionForCurrentMode();
		CloseMenu();
	}

	private void ResetCurrentModeFromMenu()
	{
		ResetSessionForCurrentMode();
		CloseMenu();
	}

	private void ReturnToStartMenu()
	{
		_menuVisible = true;
		_sessionStarted = false;
		_shotBannerSecondsRemaining = 0.0f;
		_shotBannerPanel.Visible = false;
		_shotBannerLabel.Visible = false;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add("Start menu opened.");
		UpdateMenuState();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleMenuVisibility()
	{
		if (_menuVisible)
		{
			if (_sessionStarted)
			{
				CloseMenu();
			}

			return;
		}

		_menuVisible = true;
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add("Menu opened.");
		UpdateMenuState();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void CloseMenu()
	{
		_menuVisible = false;
		UpdateMenuState();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void UpdateMenuState()
	{
		_menuOverlay.Visible = _menuVisible;
		_menuPanel.Visible = _menuVisible;
		UpdateHudVisibility();

		if (!_menuVisible)
		{
			return;
		}

		var activeMode = GetRuleModeLabel();
		_menuSubtitleLabel.Text = _sessionStarted
			? "Pause the table, swap modes, or reset without digging through hotkeys. The portable core stays live underneath this menu."
			: "Choose how you want to play. Eight-ball uses the simple computer opponent; FreePlay leaves the whole table open for practice and layout work; Tuning mode calibrates the hardcoded table geometry.";
		_menuModeLabel.Text = _sessionStarted
			? $"Current mode: {activeMode}  |  Esc closes menu"
			: $"Start mode: {activeMode}  |  Keyboard: 1 = EightBall, 2 = FreePlay, 3 = Tuning";

		_menuResumeButton.Visible = _sessionStarted;
		_menuResumeButton.Disabled = !_sessionStarted;
		_menuResetButton.Visible = _sessionStarted;
		_menuResetButton.Disabled = !_sessionStarted;
		_menuReturnToMenuButton.Visible = _sessionStarted;
		_menuReturnToMenuButton.Disabled = !_sessionStarted;
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
		SetDebugModeEnabled(!_debugModeEnabled);
	}

	private void ToggleHelpPanel()
	{
		_helpPanelVisible = !_helpPanelVisible;
		UpdateAuxiliaryPanelVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_helpPanelVisible ? "Controls panel visible." : "Controls panel hidden.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void ToggleHudVisibility()
	{
		_hudVisible = !_hudVisible;
		UpdateHudVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_hudVisible ? "Gameplay HUD visible." : "Gameplay HUD hidden.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void HandleMouseButtonInput(InputEventMouseButton mouseButtonEvent)
	{
		if (_menuVisible || _world.Phase == SimulationPhase.Running || IsComputerTurnPending())
		{
			return;
		}

		var direction = mouseButtonEvent.ButtonIndex switch
		{
			MouseButton.WheelUp => -1,
			MouseButton.WheelDown => 1,
			_ => 0
		};

		if (direction == 0)
		{
			return;
		}

		if ((_debugModeEnabled || _ruleMode == RuleMode.Calibration) && mouseButtonEvent.CtrlPressed)
		{
			AdjustOverlayThickness(direction, mouseButtonEvent.ShiftPressed);
			return;
		}

		AdjustAimWithMouseWheel(direction, mouseButtonEvent.ShiftPressed);
	}

	private void SetDebugModeEnabled(bool enabled)
	{
		if (_debugModeEnabled == enabled)
		{
			UpdateAuxiliaryPanelVisibility();
			return;
		}

		_debugModeEnabled = enabled;
		UpdateAuxiliaryPanelVisibility();
		UpdateOverlayVisibility();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(_debugModeEnabled
			? "Debug window opened. Move it to the second monitor if you want."
			: "Debug window closed.");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void OnDebugWindowCloseRequested()
	{
		SetDebugModeEnabled(false);
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

		var step = coarse ? 0.0035f : 0.0012f;
		var updatedThickness = AdjustFloat(
			_overlayLineThicknessMeters,
			step * direction,
			0.0035f,
			0.05f);

		if (Mathf.IsEqualApprox(updatedThickness, _overlayLineThicknessMeters))
		{
			return;
		}

		_overlayLineThicknessMeters = updatedThickness;
		BuildHardcodeOverlay();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Overlay thickness: {_overlayLineThicknessMeters:0.0000} m");
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
			Shot: new ShotInput(fallbackAim, GetComputerTargetStrikeSpeedMetersPerSecond(), NumericsVector2.Zero),
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
		var strikeSpeeds = GetComputerStrikeSpeeds();

		foreach (var targetBall in targetBalls)
		{
			var directAim = targetBall.Position - cueBallPosition;
			if (directAim.LengthSquared() > 0.000001f)
			{
				foreach (var speed in strikeSpeeds)
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

				foreach (var speed in strikeSpeeds)
				{
					yield return new ComputerShotCandidate(
						targetBall.BallNumber,
						new ShotInput(NumericsVector2.Normalize(cueDirection), speed, NumericsVector2.Zero));
				}
			}
		}
	}

	private float[] GetComputerStrikeSpeeds()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsBreakShot
			? _computerBreakStrikeSpeeds
			: _computerRegularStrikeSpeeds;
	}

	private float GetComputerTargetStrikeSpeedMetersPerSecond()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsBreakShot
			? 6.8f
			: 2.3f;
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

		score -= MathF.Abs(
			summary.ResolvedCueStrike.StrikeSpeedMetersPerSecond - GetComputerTargetStrikeSpeedMetersPerSecond()) * 8.0f;
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
				HandleComputerTurnFailure("Computer could not find a playable shot.");
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
			HandleComputerTurnFailure($"Computer shot failed: {exception.Message}");
		}
	}

	private void HandleComputerTurnFailure(string message)
	{
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add(message);

		if (_ruleMode != RuleMode.EightBall || _eightBallState.IsGameOver)
		{
			UpdateStatusLabel(Array.Empty<ShotEvent>());
			return;
		}

		var opponent = _eightBallState.CurrentPlayer == PlayerSlot.PlayerOne
			? PlayerSlot.PlayerTwo
			: PlayerSlot.PlayerOne;
		_eightBallState = new EightBallMatchState(
			currentPlayer: opponent,
			breakingPlayer: _eightBallState.BreakingPlayer,
			playerOneGroup: _eightBallState.PlayerOneGroup,
			playerTwoGroup: _eightBallState.PlayerTwoGroup,
			isBreakShot: false,
			shotNumber: _eightBallState.ShotNumber + 1,
			isGameOver: false,
			winner: null,
			ballInHandPlayer: opponent,
			pocketedObjectBallNumbers: _eightBallState.PocketedObjectBallNumbers.ToArray());
		_recentRuleNotes.Add($"Computer forfeited turn. Ball in hand: {GetPlayerLabel(opponent)}");
		ShowShotBanner(
			$"Computer turn failed. {GetPlayerLabel(opponent)} gets ball in hand.",
			new ShotBannerStyle(
				new Color(0.24f, 0.1f, 0.06f, 0.94f),
				new Color(0.98f, 0.54f, 0.28f, 0.98f),
				new Color(1.0f, 0.95f, 0.92f)),
			2.8f);
		ResetWorldForNextTurn(cueBallInHand: true, requiresEightBallRespot: false);
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
			RuleMode.Calibration => true,
			_ => false
		};
	}

	private bool CanPlaceCueBall()
	{
		return CanAdjustPlacement();
	}

	private int GetPlacementBallNumber()
	{
		return _ruleMode is RuleMode.Training or RuleMode.Calibration ? _trainingSelectedBallNumber : 0;
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
		_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
			? $"Tuning shots: {_trainingState.ShotCount}"
			: $"FreePlay shots: {_trainingState.ShotCount}");

		if (turnResult.Summary.PocketedBallNumbers.Count > 0)
		{
			_recentRuleNotes.Add($"Pocketed: {string.Join(", ", turnResult.Summary.PocketedBallNumbers)}");
		}

		if (turnResult.RequiresEightBallRespot)
		{
			_recentRuleNotes.Add("8-ball respot required.");
		}

		_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
			? "Tuning mode keeps cue ball free and table geometry live."
			: "Cue ball can be moved freely.");
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

		if (_ruleMode is RuleMode.Training or RuleMode.Calibration)
		{
			_trainingState = new TrainingModeState(
				shotCount: _trainingState.ShotCount,
				pocketedObjectBallNumbers: _trainingState.PocketedObjectBallNumbers.Where(number => number != ballNumber).ToArray(),
				cueBallInHand: true);
			_recentRuleNotes.Clear();
			_recentRuleNotes.Add(_ruleMode == RuleMode.Calibration
				? $"Tuning layout: moved {GetTrainingSelectionLabel()}"
				: $"FreePlay layout: moved {GetTrainingSelectionLabel()}");
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

	private void SyncBallVisuals(IReadOnlyList<BallState> balls, float deltaSeconds = 0.0f)
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
		var guideLength = 0.45f + (speedNormalized * 0.82f);
		var end = start + (aimDirection * guideLength);
		var midpoint = (start + end) * 0.5f;

		if (_importedCueStick != null)
		{
			var cueBallLookPoint = new Vector3(start.X, _cueStickHeightMeters, start.Z);
			var cueStickPosition = cueBallLookPoint - (aimDirection * (_cueStickBaseOffsetMeters + (speedNormalized * CueStickPowerPullbackMeters)));
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
			$"Core: {_tableSpec.Name}\n" +
			$"{BuildModeStatusLine()}\n" +
			$"Phase: {_world.Phase}  SimTime: {_world.SimulationTimeSeconds:0.000}s  FixedSteps: {_world.TotalFixedStepsExecuted}  Camera: {GetActiveCameraPreset().Name} @ {_cameraZoomScale:0.00}x\n" +
			$"CueBall: {cueBallStatus}  Overlay: {BuildOverlaySummary()}\n" +
			$"Recent shot events:\n{recentEventText}\n" +
			$"Rules/training:\n{recentRuleText}";

		UpdateAimPanel(cueBall);
		UpdateHelpPanel();
		UpdateDebugPanel();
	}

	private void UpdateAimPanel(BallState cueBall)
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			var selectedField = GetSelectedCalibrationField();
			_aimHeaderLabel.Text = "Tuning Mode";
			_aimMetricsLabel.Text =
				$"Selected: {selectedField.Label}\n" +
				$"Current: {selectedField.GetFormattedValue(_tableCalibrationProfile)}\n" +
				$"Range: {selectedField.Minimum:0.0000} to {selectedField.Maximum:0.0000}\n" +
				$"Overlay thickness: {_overlayLineThicknessMeters:0.0000} m\n" +
				$"Profile: {CalibrationProfilePath}\n" +
				$"Save: P  Reload: O  Reset: U";
			_aimSpeedFill.Size = new Vector2(220.0f, _aimSpeedFill.Size.Y);
			_aimSpeedFill.Color = new Color(0.96f, 0.74f, 0.32f, 0.98f);
			_aimTipIndicator.Position = new Vector2(64.0f, 64.0f);
			return;
		}

		var activeMaximumStrikeSpeed = GetCurrentMaximumStrikeSpeedMetersPerSecond();
		var speedNormalized = Mathf.InverseLerp(
			MinimumStrikeSpeedMetersPerSecond,
			activeMaximumStrikeSpeed,
			_strikeSpeedMetersPerSecond);
		var powerPercent = speedNormalized * 100.0f;
		_aimHeaderLabel.Text = IsComputerTurnPending() ? "Computer Shot Planning" : "Shot Setup";
		_aimMetricsLabel.Text =
			$"Aim angle: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg\n" +
			$"Strike speed: {_strikeSpeedMetersPerSecond:0.00} / {activeMaximumStrikeSpeed:0.00} m/s\n" +
			$"Power: {powerPercent:0}%\n" +
			$"Tip offset: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})\n" +
			$"Cue ball speed: {cueBall.Velocity.Length():0.000} m/s\n" +
			$"Selected tune: {GetTuningFieldLabel(_selectedTuningField)}\n" +
			$"Tune value: {GetSelectedTuningValueText()}";

		_aimSpeedFill.Size = new Vector2(Mathf.Max(8.0f, 220.0f * speedNormalized), _aimSpeedFill.Size.Y);
		_aimSpeedFill.Color = ResolveAimSpeedColor(speedNormalized);

		var indicatorRadius = 48.0f;
		var center = new Vector2(70.0f, 70.0f);
		var indicatorCenter = center + new Vector2(_tipOffsetNormalized.X, -_tipOffsetNormalized.Y) * indicatorRadius;
		_aimTipIndicator.Position = indicatorCenter - new Vector2(6.0f, 6.0f);
	}

	private void UpdateHelpPanel()
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			_helpHeaderLabel.Text = "Controls | Tuning";
			_helpLabel.Text =
				"Tuning: ,/. field  Shift+,/. section jump  -/= adjust  Shift+-/= coarse\n" +
				"Profile: P save  O reload  U reset profile  Ctrl+wheel overlay thickness\n" +
				"View: C camera preset  Q/E zoom  H hardcode overlay  1-5 overlay layers\n" +
				"Shot: A/D aim  Mouse wheel fine aim  W/S speed  J/L side spin  I/K follow-draw  Space shoot\n" +
				"Modes: Esc menu  Tab quick-switch mode  R reset mode  F1 debug window  F6 help  F7 HUD";
			return;
		}

		_helpHeaderLabel.Text = _ruleMode == RuleMode.Training ? "Controls | FreePlay" : "Controls | EightBall";
		_helpLabel.Text =
			"Shot: Space shoot  A/D aim  Mouse wheel fine aim  W/S speed  J/L side spin  I/K follow-draw  Backspace center tip\n" +
			"View: C camera preset  Q/E zoom  H hardcode overlay  1-5 overlay layers\n" +
			"Modes: Esc menu  Tab quick-switch mode  R reset rack/layout  F1 debug window  F6 help  F7 HUD\n" +
			"Placement: Arrow keys move selected ball when placement is active  Z/X cycle freeplay ball\n" +
			"Debug tune: F2/F3 choose value  F4/F5 adjust  Shift+F4/F5 coarse  Ctrl+wheel overlay thickness";
	}

	private void ResetShotSummary()
	{
		if (_ruleMode == RuleMode.Calibration)
		{
			var selectedField = GetSelectedCalibrationField();
			SetShotSummary(
				"Tuning Mode | Table Calibration",
				$"Selected field: {selectedField.Label}\nCurrent value: {selectedField.GetFormattedValue(_tableCalibrationProfile)}\nUse ,/. to select fields, -/= to adjust, P to save, O to reload, and U to reset the profile.",
				new Color(0.98f, 0.82f, 0.36f, 0.98f));
			return;
		}

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
		var modeLabel = GetNonMatchModeLabel();
		var header = summary.IsScratch
			? $"Last {modeLabel} Shot | Scratch"
			: summary.PocketedBallNumbers.Count > 0
				? $"Last {modeLabel} Shot | Pocketed"
				: $"Last {modeLabel} Shot | Settled";
		var summaryText =
			$"{modeLabel} shot: {turnResult.NextState.ShotCount}\n" +
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
		if (_ruleMode == RuleMode.Calibration)
		{
			var selectedField = GetSelectedCalibrationField();
			return
				$"Tuning field: {selectedField.Label} ({_selectedCalibrationFieldIndex + 1}/{_calibrationFields.Count})  " +
				$"Value: {selectedField.GetFormattedValue(_tableCalibrationProfile)}  Overlay thickness: {_overlayLineThicknessMeters:0.0000} m";
		}

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
		if (_menuVisible)
		{
			return _sessionStarted
				? $"Menu Open | {GetRuleModeLabel()} paused"
				: "Start Menu | Choose a mode";
		}

		if (_ruleMode == RuleMode.Calibration)
		{
			return $"Tuning | {GetSelectedCalibrationField().Label}";
		}

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
		if (_ruleMode == RuleMode.Calibration)
		{
			return new Color(0.98f, 0.82f, 0.36f, 0.98f);
		}

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

	private static Color ResolveAimSpeedColor(float normalizedSpeed)
	{
		normalizedSpeed = Mathf.Clamp(normalizedSpeed, 0.0f, 1.0f);
		return normalizedSpeed switch
		{
			< 0.35f => new Color(0.4f, 0.78f, 0.96f, 0.98f),
			< 0.7f => new Color(0.52f, 0.86f, 0.48f, 0.98f),
			_ => new Color(0.97f, 0.69f, 0.3f, 0.98f)
		};
	}

	private bool HasRecentRulePrefix(string prefix)
	{
		return _recentRuleNotes.Any(note => note.StartsWith(prefix, StringComparison.Ordinal));
	}

	private string GetRuleModeLabel()
	{
		return _ruleMode switch
		{
			RuleMode.EightBall => "EightBall",
			RuleMode.Training => "FreePlay",
			RuleMode.Calibration => "Tuning",
			_ => "Unknown"
		};
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

		_trainingSelectionRoot.Visible = false;
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
		_hardcodeOverlayRoot.Visible = _hardcodeOverlayVisible || _debugModeEnabled || _ruleMode == RuleMode.Calibration;
		_overlayClothRoot.Visible = _overlayClothVisible;
		_overlayCushionRoot.Visible = _overlayCushionVisible;
		_overlayJawRoot.Visible = _overlayJawVisible;
		_overlayPocketRoot.Visible = _overlayPocketVisible;
		_overlaySpotRoot.Visible = _overlaySpotVisible;
	}

	private void UpdateHudVisibility()
	{
		var gameplayHudVisible = _hudVisible && !_menuVisible;
		_statusPanel.Visible = gameplayHudVisible;
		_summaryPanel.Visible = gameplayHudVisible;
		_aimPanel.Visible = gameplayHudVisible;
		_shotBannerPanel.Visible = gameplayHudVisible && _shotBannerSecondsRemaining > 0.0f;
		_shotBannerLabel.Visible = _shotBannerPanel.Visible;
		UpdateAuxiliaryPanelVisibility();
	}

	private void UpdateAuxiliaryPanelVisibility()
	{
		var gameplayHudVisible = _hudVisible && !_menuVisible;
		_debugWindow.Visible = _debugModeEnabled;
		_debugPanel.Visible = _debugModeEnabled;
		_debugHeaderLabel.Visible = _debugModeEnabled;
		_debugLabel.Visible = _debugModeEnabled;
		_helpPanel.Visible = gameplayHudVisible && _helpPanelVisible;
		_helpHeaderLabel.Visible = _helpPanel.Visible;
		_helpLabel.Visible = _helpPanel.Visible;
	}

	private void UpdateDebugPanel()
	{
		UpdateAuxiliaryPanelVisibility();
		if (!_debugModeEnabled)
		{
			return;
		}

		_debugWindow.Title = $"CodexBuilding Debug | {GetRuleModeLabel()} | {_world.Phase}";
		_debugHeaderLabel.Text =
			"Portable Engine Debug\n" +
			"F1 closes this window. F2/F3 select a tuning field. F4/F5 adjust it. Hold Shift for coarse changes.";
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
		var builder = new StringBuilder(2048);
		builder.AppendLine("OVERVIEW");
		builder.AppendLine($"  Mode: {GetRuleModeLabel()}");
		builder.AppendLine($"  Match state: {BuildModeStatusLine()}");
		builder.AppendLine($"  Phase: {_world.Phase} | Sim time: {_world.SimulationTimeSeconds:0.000}s | Fixed steps: {_world.TotalFixedStepsExecuted}");
		builder.AppendLine($"  Shot capture: {FormatYesNo(_shotCaptureActive)} | Captured frames: {_capturedShotFrames.Count}");
		builder.AppendLine($"  Recent state: {BuildDebugStateLine()}");
		builder.AppendLine();

		builder.AppendLine("TABLE GEOMETRY");
		builder.AppendLine($"  Table spec: {_tableSpec.Name}");
		builder.AppendLine($"  Source blend: {_tableSpec.SourceBlendPath}");
		builder.AppendLine($"  Cloth min/max: {FormatVector(_tableSpec.ClothMin)} -> {FormatVector(_tableSpec.ClothMax)}");
		builder.AppendLine($"  Ball diameter: {_tableSpec.BallDiameterMeters:0.00000} m");
		builder.AppendLine($"  Geometry counts: cushions={_tableSpec.Cushions.Count}, jaws={_tableSpec.JawSegments.Count}, pockets={_tableSpec.Pockets.Count}");
		builder.AppendLine($"  Overlay layers: {BuildOverlaySummary()}");
		builder.AppendLine($"  Overlay thickness: {_overlayLineThicknessMeters:0.0000} m");
		builder.AppendLine();

		builder.AppendLine("ACTIVE TUNING");
		builder.AppendLine($"  Selected field: {GetTuningFieldLabel(_selectedTuningField)}");
		builder.AppendLine($"  Selected value: {GetSelectedTuningValueText()}");
		builder.AppendLine("  Controls: F2/F3 choose field | F4/F5 adjust | Shift = coarse step | Ctrl+wheel = overlay thickness");
		builder.AppendLine($"  Fixed step: {_config.FixedStepSeconds:0.000000} s");
		builder.AppendLine($"  Settle threshold: {_config.SettleSpeedThresholdMetersPerSecond:0.0000} m/s");
		builder.AppendLine($"  Cloth friction: slide={_config.SlidingFrictionAccelerationMetersPerSecondSquared:0.000}, roll={_config.RollingFrictionAccelerationMetersPerSecondSquared:0.000}");
		builder.AppendLine($"  Spin tuning: decay={_config.SpinDecayRpsPerSecond:0.000}, side curve={_config.SideSpinCurveAccelerationMetersPerSecondSquaredPerRps:0.0000}, moving side decay={_config.MovingSideSpinDecayRpsPerSecondPerMetersPerSecond:0.000}");
		builder.AppendLine($"  Ball contact: restitution={_config.BallCollisionRestitution:0.00}, tangent={_config.BallCollisionTangentialTransferFactor:0.00}, spin={_config.BallCollisionSpinTransferFactor:0.00}, follow/draw={_config.BallCollisionForwardSpinCarryFactor:0.00}");
		builder.AppendLine($"  Rail contact: restitution={_config.BoundaryRestitution:0.00}, glancing={_config.BoundaryGlancingRestitution:0.00}, tangential keep={_config.BoundaryTangentialVelocityRetention:0.00}, tangential friction={_config.BoundaryTangentialFrictionFactor:0.00}, english={_config.BoundaryEnglishTransferFactor:0.00}, spin={_config.BoundarySpinTransferFactor:0.00}");
		builder.AppendLine($"  Solver iterations: ball pairs={_config.MaxCollisionIterationsPerStep}, rails={_config.MaxBoundaryIterationsPerStep}");
		builder.AppendLine();

		builder.AppendLine("SHOT SETUP");
		builder.AppendLine($"  Strike speed: {_strikeSpeedMetersPerSecond:0.00} / {GetCurrentMaximumStrikeSpeedMetersPerSecond():0.00} m/s");
		builder.AppendLine($"  Aim angle: {Mathf.RadToDeg(_aimAngleRadians):0.0} deg");
		builder.AppendLine($"  Tip offset: ({_tipOffsetNormalized.X:0.00}, {_tipOffsetNormalized.Y:0.00})");
		builder.AppendLine($"  Camera: {GetActiveCameraPreset().Name} | Zoom: {_cameraZoomScale:0.00}x | Pos: ({_camera.Position.X:0.000}, {_camera.Position.Y:0.000}, {_camera.Position.Z:0.000})");
		builder.AppendLine($"  Manual overlay toggle: {FormatYesNo(_hardcodeOverlayVisible)} | Debug enabled: {FormatYesNo(_debugModeEnabled)}");
		builder.AppendLine();

		builder.AppendLine("CUE BALL");
		builder.AppendLine($"  Position: {FormatVector(cueBall.Position)}");
		builder.AppendLine($"  Velocity: {FormatVector(cueBall.Velocity)}");
		builder.AppendLine($"  Spin (side, forward, vertical): {FormatSpin(cueBall.Spin)}");
		builder.AppendLine($"  Pocketed: {FormatYesNo(cueBall.IsPocketed)}");
		builder.AppendLine();

		builder.AppendLine("SELECTED BALL");
		builder.AppendLine($"  Label: {GetTrainingSelectionLabel()}");
		builder.AppendLine($"  Position: {FormatVector(selectedBall.Position)}");
		builder.AppendLine($"  Velocity: {FormatVector(selectedBall.Velocity)}");
		builder.AppendLine($"  Spin (side, forward, vertical): {FormatSpin(selectedBall.Spin)}");
		builder.AppendLine($"  Pocketed: {FormatYesNo(selectedBall.IsPocketed)}");
		builder.AppendLine();

		builder.AppendLine("BALL SUMMARY");
		builder.AppendLine($"  Moving balls: {movingBalls.Length}");
		builder.AppendLine($"  Pocketed balls: {pocketedCount}");
		builder.AppendLine($"  Moving list: {movingSummary}");
		builder.AppendLine();

		builder.AppendLine("AIM PREVIEW");
		builder.AppendLine($"  Dirty: {FormatYesNo(_aimPreviewDirty)}");
		builder.AppendLine($"  Primary path: {previewPrimary:0.000} m");
		builder.AppendLine($"  Secondary path: {previewSecondary:0.000} m");
		builder.AppendLine($"  Target path: {previewTarget:0.000} m");

		return builder.ToString();
	}

	private float GetCurrentMaximumStrikeSpeedMetersPerSecond()
	{
		return _ruleMode == RuleMode.EightBall && _eightBallState.IsBreakShot
			? MaximumBreakStrikeSpeedMetersPerSecond
			: MaximumRegularStrikeSpeedMetersPerSecond;
	}

	private string GetCalibrationProfileAbsolutePath()
	{
		return ProjectSettings.GlobalizePath(CalibrationProfilePath);
	}

	private string GetModeReadyText()
	{
		return _ruleMode switch
		{
			RuleMode.EightBall => "Eight-ball vs computer ready.",
			RuleMode.Training => "FreePlay ready.",
			RuleMode.Calibration => "Tuning mode ready.",
			_ => "Mode ready."
		};
	}

	private string GetNonMatchModeLabel()
	{
		return _ruleMode == RuleMode.Calibration ? "Tuning" : "FreePlay";
	}

	private string GetShotStartedNote()
	{
		return _ruleMode switch
		{
			RuleMode.EightBall => "Eight-ball shot started.",
			RuleMode.Calibration => "Tuning shot started.",
			_ => "FreePlay shot started."
		};
	}

	private void BuildCalibrationFields()
	{
		_calibrationFields.Clear();

		AddCalibrationField("Cloth", "Cloth Min X", "OverlayClothLeft", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMinOffset.X,
			value => _tableCalibrationProfile.ClothMinOffset.X = value);
		AddCalibrationField("Cloth", "Cloth Min Y", "OverlayClothTop", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMinOffset.Y,
			value => _tableCalibrationProfile.ClothMinOffset.Y = value);
		AddCalibrationField("Cloth", "Cloth Max X", "OverlayClothRight", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMaxOffset.X,
			value => _tableCalibrationProfile.ClothMaxOffset.X = value);
		AddCalibrationField("Cloth", "Cloth Max Y", "OverlayClothBottom", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMaxOffset.Y,
			value => _tableCalibrationProfile.ClothMaxOffset.Y = value);

		AddCalibrationField("Spots", "Cue Ball Spawn X", "OverlayCueBallSpawn", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.CueBallSpawnOffset.X,
			value => _tableCalibrationProfile.CueBallSpawnOffset.X = value);
		AddCalibrationField("Spots", "Cue Ball Spawn Y", "OverlayCueBallSpawn", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.CueBallSpawnOffset.Y,
			value => _tableCalibrationProfile.CueBallSpawnOffset.Y = value);
		AddCalibrationField("Spots", "Rack Apex X", "OverlayRackApexSpot", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.RackApexSpotOffset.X,
			value => _tableCalibrationProfile.RackApexSpotOffset.X = value);
		AddCalibrationField("Spots", "Rack Apex Y", "OverlayRackApexSpot", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.RackApexSpotOffset.Y,
			value => _tableCalibrationProfile.RackApexSpotOffset.Y = value);

		foreach (var cushion in _baseTableSpec.Cushions)
		{
			var sourceName = cushion.SourceName;
			AddCalibrationField("Cushions", $"{sourceName} Start X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].StartOffset.X,
				value => _tableCalibrationProfile.Cushions[sourceName].StartOffset.X = value);
			AddCalibrationField("Cushions", $"{sourceName} Start Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].StartOffset.Y,
				value => _tableCalibrationProfile.Cushions[sourceName].StartOffset.Y = value);
			AddCalibrationField("Cushions", $"{sourceName} End X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].EndOffset.X,
				value => _tableCalibrationProfile.Cushions[sourceName].EndOffset.X = value);
			AddCalibrationField("Cushions", $"{sourceName} End Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].EndOffset.Y,
				value => _tableCalibrationProfile.Cushions[sourceName].EndOffset.Y = value);
		}

		foreach (var jaw in _baseTableSpec.JawSegments)
		{
			var sourceName = jaw.SourceName;
			AddCalibrationField("Jaws", $"{sourceName} Start X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].StartOffset.X,
				value => _tableCalibrationProfile.Jaws[sourceName].StartOffset.X = value);
			AddCalibrationField("Jaws", $"{sourceName} Start Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].StartOffset.Y,
				value => _tableCalibrationProfile.Jaws[sourceName].StartOffset.Y = value);
			AddCalibrationField("Jaws", $"{sourceName} End X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].EndOffset.X,
				value => _tableCalibrationProfile.Jaws[sourceName].EndOffset.X = value);
			AddCalibrationField("Jaws", $"{sourceName} End Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].EndOffset.Y,
				value => _tableCalibrationProfile.Jaws[sourceName].EndOffset.Y = value);
		}

		foreach (var pocket in _baseTableSpec.Pockets)
		{
			var sourceName = pocket.SourceName;
			AddCalibrationField("Pockets", $"{sourceName} Center X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.X,
				value => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.X = value);
			AddCalibrationField("Pockets", $"{sourceName} Center Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.Y,
				value => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.Y = value);
			AddCalibrationField("Pockets", $"{sourceName} Capture Radius", $"Overlay_{sourceName}", -0.05f, 0.05f, 0.0005f, 0.003f,
				() => _tableCalibrationProfile.Pockets[sourceName].CaptureRadiusOffset,
				value => _tableCalibrationProfile.Pockets[sourceName].CaptureRadiusOffset = value);
			AddCalibrationField("Pockets", $"{sourceName} Drop Radius", $"Overlay_{sourceName}", -0.05f, 0.05f, 0.0005f, 0.003f,
				() => _tableCalibrationProfile.Pockets[sourceName].DropRadiusOffset,
				value => _tableCalibrationProfile.Pockets[sourceName].DropRadiusOffset = value);
		}

		if (_calibrationFields.Count == 0)
		{
			throw new InvalidOperationException("Calibration mode requires at least one calibration field.");
		}

		_selectedCalibrationFieldIndex = Mathf.Clamp(_selectedCalibrationFieldIndex, 0, _calibrationFields.Count - 1);
	}

	private void AddCalibrationField(
		string section,
		string label,
		string overlayTarget,
		float minimum,
		float maximum,
		float fineStep,
		float coarseStep,
		Func<float> getter,
		Action<float> setter)
	{
		_calibrationFields.Add(new CalibrationField(section, label, overlayTarget, minimum, maximum, fineStep, coarseStep, getter, setter));
	}

	private CalibrationField GetSelectedCalibrationField()
	{
		return _calibrationFields[Mathf.Clamp(_selectedCalibrationFieldIndex, 0, _calibrationFields.Count - 1)];
	}

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
		ApplyCalibrationProfile("Reloaded tuning profile.");
	}

	private void ResetCalibrationProfile()
	{
		_tableCalibrationProfile = TableCalibrationProfile.CreateDefault(_baseTableSpec);
		BuildCalibrationFields();
		ApplyCalibrationProfile("Reset tuning profile to source values.");
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
		_shotBannerLabel.Text = text;
		_shotBannerLabel.Modulate = style.TextColor;
		_shotBannerPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(style.BackgroundColor, style.BorderColor));
		UpdateHudVisibility();
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
		var modeLabel = GetNonMatchModeLabel();
		var pocketedObjectBalls = turnResult.Summary.PocketedBallNumbers
			.Where(ballNumber => ballNumber != 0)
			.ToArray();

		if (turnResult.RequiresEightBallRespot)
		{
			return $"{modeLabel}: 8-ball respot required";
		}

		if (pocketedObjectBalls.Length > 0)
		{
			return $"{modeLabel} pocketed {FormatBallNumberList(pocketedObjectBalls)}";
		}

		return $"{modeLabel} shot {_trainingState.ShotCount} settled";
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
		((BoxMesh)guideNode.Mesh!).Size = new Vector3(_overlayLineThicknessMeters, OverlayLineHeightMeters, segmentLength);
		guideNode.LookAt(guideNode.Position + segment, Vector3.Up);
	}

	private Color ResolveOverlayColor(string overlayName, Color defaultColor)
	{
		if (_ruleMode != RuleMode.Calibration || _calibrationFields.Count == 0)
		{
			return defaultColor;
		}

		var selectedField = GetSelectedCalibrationField();
		if (selectedField.OverlayTarget != overlayName)
		{
			return defaultColor;
		}

		return defaultColor.Lerp(new Color(1.0f, 0.98f, 0.52f), 0.45f);
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

	private sealed class CalibrationField
	{
		public CalibrationField(
			string section,
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

	private readonly record struct ComputerShotCandidate(
		int TargetBallNumber,
		ShotInput Shot);

	private readonly record struct ComputerShotPlan(
		NumericsVector2? CueBallPlacement,
		ShotInput Shot,
		float Score,
		string Description);
}
