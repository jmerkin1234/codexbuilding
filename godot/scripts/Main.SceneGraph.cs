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
{
	private void ConfigureSceneGraph()
	{
		_godotRoot = EnsureNode<Node3D>(this, "GodotRoot");

		_tableRoot = EnsureNode<Node3D>(_godotRoot, "TableRoot");
		_ballsRoot = EnsureNode<Node3D>(_godotRoot, "BallsRoot");
		_cueRoot = EnsureNode<Node3D>(_godotRoot, "CueRoot");
		_guideRoot = EnsureNode<Node3D>(_godotRoot, "GuideRoot");
		_hardcodeOverlayRoot = EnsureNode<Node3D>(_godotRoot, "HardcodeOverlayRoot");

		_cueGuide = EnsureNode<MeshInstance3D>(_guideRoot, "CueGuide");
		_cueGuide.Mesh = new BoxMesh();
		_cueGuide.MaterialOverride = CreateMaterial(new Color(0.92f, 0.84f, 0.58f), roughness: 0.65f);
		_cueGuide.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		_importedCueStick = FindNodeRecursive<Node3D>(_cueRoot, "CueStick") ?? _cueRoot;
		if (_importedCueStick != null)
		{
			_importedCueStick.Visible = true;
			_cueStickHeightMeters = _importedCueStick.Position.Y;
			var originalCueRotation = _importedCueStick.Quaternion;

			if (FindNodeRecursive<Node3D>(_ballsRoot, "CueBall") is { } importedCueBall)
			{
				var cueToBall = new Vector3(
					importedCueBall.Position.X - _importedCueStick.Position.X,
					0.0f,
					importedCueBall.Position.Z - _importedCueStick.Position.Z);

				if (cueToBall.LengthSquared() > 0.000001f)
				{
					var desiredCueCenterOffset = cueToBall.Length();
					if (desiredCueCenterOffset <= 0.0001f &&
						TryResolveCueStickTipOffsetMeters(_importedCueStick, originalCueRotation, cueToBall.Normalized(), out var cueStickTipOffsetMeters))
					{
						desiredCueCenterOffset = cueStickTipOffsetMeters + (_tableSpec.BallDiameterMeters * 0.5f) + CueStickTipGapMeters;
					}

					_cueStickBaseOffsetMeters = desiredCueCenterOffset;
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
		_aimPanel.Size = new Vector2(440.0f, 398.0f);
		_aimPanel.MouseFilter = Control.MouseFilterEnum.Pass;
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
		_aimHeaderLabel.MouseFilter = Control.MouseFilterEnum.Stop;
		_aimHeaderLabel.GuiInput += OnAimHeaderGuiInput;

		_aimMetricsLabel = EnsureNode<Label>(_aimPanel, "AimMetricsLabel");
		_aimMetricsLabel.Position = new Vector2(16.0f, 46.0f);
		_aimMetricsLabel.Size = new Vector2(220.0f, 312.0f);
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
		_helpPanel.Position = new Vector2(896.0f, 434.0f);
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

		_tuningWindow = EnsureNode<Window>(this, "TuningWindow");
		_tuningWindow.Title = "CodexBuilding Tuning";
		_tuningWindow.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen;
		_tuningWindow.Size = DefaultTuningWindowSize;
		_tuningWindow.MinSize = new Vector2I(700, 720);
		_tuningWindow.Unresizable = false;
		_tuningWindow.WrapControls = true;
		_tuningWindow.Exclusive = false;
		_tuningWindow.Transient = false;
		_tuningWindow.Visible = false;
		_tuningWindow.CloseRequested += OnTuningWindowCloseRequested;

		var tuningRoot = EnsureNode<VBoxContainer>(_tuningWindow, "TuningRoot");
		tuningRoot.AnchorRight = 1.0f;
		tuningRoot.AnchorBottom = 1.0f;
		tuningRoot.OffsetLeft = 16.0f;
		tuningRoot.OffsetTop = 16.0f;
		tuningRoot.OffsetRight = -16.0f;
		tuningRoot.OffsetBottom = -16.0f;
		tuningRoot.AddThemeConstantOverride("separation", 10);

		var tuningHeaderRow = EnsureNode<HBoxContainer>(tuningRoot, "TuningHeaderRow");
		tuningHeaderRow.AddThemeConstantOverride("separation", 10);

		_tuningWindowHeaderLabel = EnsureNode<Label>(tuningHeaderRow, "TuningWindowHeaderLabel");
		_tuningWindowHeaderLabel.Text = "Table Tuning";
		_tuningWindowHeaderLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningWindowHeaderLabel.AddThemeFontSizeOverride("font_size", 24);
		_tuningWindowHeaderLabel.Modulate = new Color(0.98f, 0.92f, 0.74f);

		_tuningInfoToggleButton = EnsureNode<Button>(tuningHeaderRow, "TuningInfoToggleButton");
		_tuningInfoToggleButton.Text = "Hide Info";
		_tuningInfoToggleButton.AddThemeFontSizeOverride("font_size", 14);
		_tuningInfoToggleButton.Pressed += ToggleTuningInfoVisibility;

		_tuningWindowInfoLabel = EnsureNode<Label>(tuningRoot, "TuningWindowInfoLabel");
		_tuningWindowInfoLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningWindowInfoLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_tuningWindowInfoLabel.AddThemeFontSizeOverride("font_size", 14);
		_tuningWindowInfoLabel.Modulate = new Color(0.9f, 0.95f, 0.98f);

		_tuningFieldSelector = EnsureNode<OptionButton>(tuningRoot, "TuningObjectSelector");
		_tuningFieldSelector.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningFieldSelector.ItemSelected += OnTuningFieldSelected;

		_tuningObjectDetailsPanel = EnsureNode<PanelContainer>(tuningRoot, "TuningObjectDetailsPanel");
		_tuningObjectDetailsPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningObjectDetailsPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.08f, 0.08f, 0.11f, 0.94f),
				new Color(0.93f, 0.73f, 0.34f, 0.95f)));

		var tuningObjectDetailsBox = EnsureNode<VBoxContainer>(_tuningObjectDetailsPanel, "TuningObjectDetailsBox");
		tuningObjectDetailsBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		tuningObjectDetailsBox.AddThemeConstantOverride("separation", 8);

		_tuningObjectDetailsLabel = EnsureNode<Label>(tuningObjectDetailsBox, "TuningObjectDetailsLabel");
		_tuningObjectDetailsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_tuningObjectDetailsLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningObjectDetailsLabel.AddThemeFontSizeOverride("font_size", 15);
		_tuningObjectDetailsLabel.Modulate = new Color(0.98f, 0.95f, 0.84f);

		_tuningObjectMiniPanelGrid = EnsureNode<GridContainer>(tuningObjectDetailsBox, "TuningObjectMiniPanelGrid");
		_tuningObjectMiniPanelGrid.Columns = 2;
		_tuningObjectMiniPanelGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningObjectMiniPanelGrid.AddThemeConstantOverride("h_separation", 10);
		_tuningObjectMiniPanelGrid.AddThemeConstantOverride("v_separation", 10);

		var tuningContentRow = EnsureNode<HBoxContainer>(tuningRoot, "TuningContentRow");
		tuningContentRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		tuningContentRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		tuningContentRow.AddThemeConstantOverride("separation", 12);

		var tuningControlsColumn = EnsureNode<VBoxContainer>(tuningContentRow, "TuningControlsColumn");
		tuningControlsColumn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		tuningControlsColumn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		tuningControlsColumn.AddThemeConstantOverride("separation", 10);

		_tuningScrollContainer = EnsureNode<ScrollContainer>(tuningControlsColumn, "TuningScrollContainer");
		_tuningScrollContainer.CustomMinimumSize = new Vector2(0.0f, 520.0f);
		_tuningScrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_tuningScrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
		_tuningScrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways;

		_tuningFieldsContainer = EnsureNode<VBoxContainer>(_tuningScrollContainer, "TuningFieldsContainer");
		_tuningFieldsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningFieldsContainer.AddThemeConstantOverride("separation", 8);
		ConfigureScrollBarAppearance(_tuningScrollContainer);

		_tuningOverlayLabel = EnsureNode<Label>(tuningControlsColumn, "TuningOverlayLabel");
		_tuningOverlayLabel.AddThemeFontSizeOverride("font_size", 14);
		_tuningOverlayLabel.Modulate = new Color(0.89f, 0.95f, 1.0f);

		_tuningOverlaySlider = EnsureNode<HSlider>(tuningControlsColumn, "TuningOverlaySlider");
		_tuningOverlaySlider.MinValue = MinOverlayThicknessPixels;
		_tuningOverlaySlider.MaxValue = MaxOverlayThicknessPixels;
		_tuningOverlaySlider.Step = 0.05;
		_tuningOverlaySlider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningOverlaySlider.ValueChanged += OnTuningOverlayThicknessChanged;

		var tuningButtonRow = EnsureNode<HBoxContainer>(tuningControlsColumn, "TuningButtonRow");
		tuningButtonRow.AddThemeConstantOverride("separation", 10);

		_tuningSaveButton = EnsureNode<Button>(tuningButtonRow, "TuningSaveButton");
		_tuningSaveButton.Text = "Save";
		_tuningSaveButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningSaveButton.AddThemeFontSizeOverride("font_size", 14);
		_tuningSaveButton.Pressed += SaveCalibrationProfile;

		_tuningReloadButton = EnsureNode<Button>(tuningButtonRow, "TuningReloadButton");
		_tuningReloadButton.Text = "Reload";
		_tuningReloadButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningReloadButton.AddThemeFontSizeOverride("font_size", 14);
		_tuningReloadButton.Pressed += ReloadCalibrationProfile;

		_tuningResetButton = EnsureNode<Button>(tuningButtonRow, "TuningResetButton");
		_tuningResetButton.Text = "Reset";
		_tuningResetButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningResetButton.AddThemeFontSizeOverride("font_size", 14);
		_tuningResetButton.Pressed += ResetCalibrationProfile;

		_tuningLegendPanel = EnsureNode<PanelContainer>(tuningContentRow, "TuningLegendPanel");
		_tuningLegendPanel.CustomMinimumSize = new Vector2(220.0f, 0.0f);
		_tuningLegendPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_tuningLegendPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.05f, 0.07f, 0.1f, 0.94f),
				new Color(0.34f, 0.58f, 0.72f, 0.96f)));

		var tuningLegendBox = EnsureNode<VBoxContainer>(_tuningLegendPanel, "TuningLegendBox");
		tuningLegendBox.AddThemeConstantOverride("separation", 8);

		_tuningLegendHeaderLabel = EnsureNode<Label>(tuningLegendBox, "TuningLegendHeaderLabel");
		_tuningLegendHeaderLabel.Text = "Overlay Legend";
		_tuningLegendHeaderLabel.AddThemeFontSizeOverride("font_size", 18);
		_tuningLegendHeaderLabel.Modulate = new Color(0.93f, 0.97f, 1.0f);

		var tuningLegendSubtitle = EnsureNode<Label>(tuningLegendBox, "TuningLegendSubtitle");
		tuningLegendSubtitle.Text = "These colors map the hardcoded table and the live shot guides.";
		tuningLegendSubtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		tuningLegendSubtitle.AddThemeFontSizeOverride("font_size", 13);
		tuningLegendSubtitle.Modulate = new Color(0.83f, 0.91f, 0.98f);

		_tuningLegendScrollContainer = EnsureNode<ScrollContainer>(tuningLegendBox, "TuningLegendScrollContainer");
		_tuningLegendScrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningLegendScrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_tuningLegendScrollContainer.CustomMinimumSize = new Vector2(0.0f, 520.0f);
		_tuningLegendScrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
		_tuningLegendScrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways;
		ConfigureScrollBarAppearance(_tuningLegendScrollContainer);

		_tuningLegendRows = EnsureNode<VBoxContainer>(_tuningLegendScrollContainer, "TuningLegendRows");
		_tuningLegendRows.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_tuningLegendRows.AddThemeConstantOverride("separation", 6);
		BuildTuningLegendRows();

		PopulateCalibrationFieldSelector();
		SyncCalibrationControls();
		UpdateHudVisibility();
	}
}
