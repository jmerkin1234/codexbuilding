using System;
using System.Linq;
using CodexBuilding.Billiards.Core.Simulation;
using Godot;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{
	private void PopulateCalibrationFieldSelector()
	{
		if (_tuningFieldSelector == null)
		{
			return;
		}

		_syncingTuningControls = true;
		_tuningFieldSelector.Clear();
		for (var index = 0; index < _calibrationObjects.Count; index++)
		{
			var calibrationObject = _calibrationObjects[index];
			_tuningFieldSelector.AddItem(calibrationObject.Label, index);
		}

		if (_calibrationObjects.Count > 0)
		{
			var selectedObjectIndex = _calibrationObjects.FindIndex(entry => entry.Key == _selectedCalibrationObjectKey);
			_tuningFieldSelector.Select(Mathf.Clamp(selectedObjectIndex, 0, _calibrationObjects.Count - 1));
		}

		_syncingTuningControls = false;
	}

	private void RebuildTuningFieldRows()
	{
		if (_tuningFieldsContainer == null)
		{
			return;
		}

		_tuningFieldRows.Clear();
		ClearChildren(_tuningFieldsContainer);
		for (var fieldIndex = 0; fieldIndex < _calibrationFields.Count; fieldIndex++)
		{
			var rowPanel = new PanelContainer
			{
				Name = $"TuningFieldRow_{fieldIndex}",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0.0f, 74.0f)
			};
			rowPanel.AddThemeStyleboxOverride(
				"panel",
				CreateHudPanelStyle(
					new Color(0.06f, 0.08f, 0.12f, 0.92f),
					new Color(0.22f, 0.33f, 0.44f, 0.95f)));
			_tuningFieldsContainer.AddChild(rowPanel);

			var rowBox = new VBoxContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			rowBox.AddThemeConstantOverride("separation", 6);
			rowPanel.AddChild(rowBox);

			var rowLabel = new Label
			{
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			rowLabel.AddThemeFontSizeOverride("font_size", 14);
			rowBox.AddChild(rowLabel);

			var sliderRow = new HBoxContainer();
			sliderRow.AddThemeConstantOverride("separation", 8);
			rowBox.AddChild(sliderRow);

			var slider = new HSlider
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			var capturedFieldIndex = fieldIndex;
			slider.ValueChanged += value => OnTuningRowValueChanged(capturedFieldIndex, value);
			sliderRow.AddChild(slider);

			var valueLabel = new Label
			{
				Size = new Vector2(110.0f, 24.0f),
				CustomMinimumSize = new Vector2(110.0f, 24.0f),
				HorizontalAlignment = HorizontalAlignment.Right
			};
			valueLabel.AddThemeFontSizeOverride("font_size", 13);
			sliderRow.AddChild(valueLabel);

			_tuningFieldRows.Add(new TuningFieldRow(fieldIndex, rowPanel, rowLabel, slider, valueLabel));
		}

		var estimatedHeight = (_tuningFieldRows.Count * 74.0f) + (Mathf.Max(0, _tuningFieldRows.Count - 1) * 8.0f) + 12.0f;
		_tuningFieldsContainer.CustomMinimumSize = new Vector2(0.0f, estimatedHeight);
		ConfigureScrollBarAppearance(_tuningScrollContainer);
	}

	private static void ConfigureScrollBarAppearance(ScrollContainer? scrollContainer)
	{
		if (scrollContainer == null)
		{
			return;
		}

		scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.ShowNever;
		scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways;

		var verticalScrollBar = scrollContainer.GetVScrollBar();
		if (verticalScrollBar == null)
		{
			return;
		}

		verticalScrollBar.CustomMinimumSize = new Vector2(18.0f, 0.0f);
		verticalScrollBar.AddThemeStyleboxOverride(
			"scroll",
			CreateScrollBarStyle(new Color(0.05f, 0.08f, 0.12f, 0.94f), new Color(0.28f, 0.39f, 0.49f, 0.96f)));
		verticalScrollBar.AddThemeStyleboxOverride(
			"scroll_focus",
			CreateScrollBarStyle(new Color(0.05f, 0.08f, 0.12f, 0.94f), new Color(0.48f, 0.66f, 0.8f, 0.98f)));
		verticalScrollBar.AddThemeStyleboxOverride(
			"grabber",
			CreateScrollBarStyle(new Color(0.7f, 0.84f, 0.96f, 0.98f), new Color(0.94f, 0.98f, 1.0f, 0.98f)));
		verticalScrollBar.AddThemeStyleboxOverride(
			"grabber_highlight",
			CreateScrollBarStyle(new Color(0.86f, 0.93f, 1.0f, 1.0f), new Color(1.0f, 1.0f, 1.0f, 1.0f)));
		verticalScrollBar.AddThemeStyleboxOverride(
			"grabber_pressed",
			CreateScrollBarStyle(new Color(0.99f, 0.82f, 0.42f, 1.0f), new Color(1.0f, 0.95f, 0.76f, 1.0f)));
	}

	private void RebuildSelectedCalibrationMiniPanels(IReadOnlyList<CalibrationField> objectFields)
	{
		if (_tuningObjectMiniPanelGrid == null)
		{
			return;
		}

		_tuningMiniPanels.Clear();
		ClearChildren(_tuningObjectMiniPanelGrid);
		for (var objectFieldIndex = 0; objectFieldIndex < objectFields.Count; objectFieldIndex++)
		{
			var field = objectFields[objectFieldIndex];
			var panel = new PanelContainer
			{
				Name = $"TuningMiniPanel_{field.ObjectKey}_{objectFieldIndex}",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0.0f, 92.0f)
			};
			panel.AddThemeStyleboxOverride(
				"panel",
				CreateHudPanelStyle(
					new Color(0.11f, 0.11f, 0.14f, 0.96f),
					new Color(0.96f, 0.76f, 0.38f, 0.95f)));
			_tuningObjectMiniPanelGrid.AddChild(panel);

			var box = new VBoxContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			box.AddThemeConstantOverride("separation", 6);
			panel.AddChild(box);

			var label = new Label
			{
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			label.AddThemeFontSizeOverride("font_size", 14);
			label.Modulate = new Color(0.99f, 0.95f, 0.82f);
			box.AddChild(label);

			var sliderRow = new HBoxContainer();
			sliderRow.AddThemeConstantOverride("separation", 8);
			box.AddChild(sliderRow);

			var slider = new HSlider
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			var capturedFieldIndex = _calibrationFields.IndexOf(field);
			slider.ValueChanged += value => OnTuningRowValueChanged(capturedFieldIndex, value);
			sliderRow.AddChild(slider);

			var valueLabel = new Label
			{
				CustomMinimumSize = new Vector2(96.0f, 24.0f),
				HorizontalAlignment = HorizontalAlignment.Right
			};
			valueLabel.AddThemeFontSizeOverride("font_size", 13);
			valueLabel.Modulate = new Color(0.99f, 0.95f, 0.82f);
			sliderRow.AddChild(valueLabel);

			_tuningMiniPanels.Add(new TuningMiniPanel(capturedFieldIndex, panel, label, slider, valueLabel));
		}

		_selectedMiniPanelObjectKey = _selectedCalibrationObjectKey;
	}

	private static string GetMiniPanelFieldLabel(CalibrationField field)
	{
		var label = field.Label;
		if (label.StartsWith(field.ObjectLabel + " ", StringComparison.Ordinal))
		{
			label = label[(field.ObjectLabel.Length + 1)..];
		}

		return label;
	}

	private void BuildTuningLegendRows()
	{
		if (_tuningLegendRows == null)
		{
			return;
		}

		ClearChildren(_tuningLegendRows);
		AddTuningLegendRow("Play area", new Color(0.38f, 0.88f, 0.46f), "Green box for the nose-to-nose play area reference.");
		AddTuningLegendRow("Cushions", new Color(0.98f, 0.59f, 0.2f), "Orange rebound lines. Balls should bounce on these, not across the pocket gaps.");
		AddTuningLegendRow("Jaws", new Color(0.95f, 0.31f, 0.35f), "Red angled pocket-entry faces at the ends of the rails.");
		AddTuningLegendRow("Pocket capture", new Color(0.18f, 0.52f, 0.98f), "Blue circles showing the hardcoded pocket capture zones.");
		AddTuningLegendRow("Cue spawn", new Color(0.95f, 0.95f, 0.95f), "White cross for the cue-ball reference spot.");
		AddTuningLegendRow("Rack apex", new Color(0.95f, 0.82f, 0.22f), "Gold cross for the rack reference spot.");
		AddTuningLegendRow("Aim primary", new Color(0.94f, 0.97f, 0.98f), "White shot line from the cue ball.");
		AddTuningLegendRow("Aim continuation", new Color(0.39f, 0.84f, 0.94f), "Cyan continuation after the first bounce or contact.");
		AddTuningLegendRow("Aim target", new Color(0.98f, 0.72f, 0.24f), "Orange object-ball path after first contact.");
		AddTuningLegendRow("Selected target", new Color(1.0f, 0.98f, 0.52f), "Any tuned overlay brightens toward pale yellow when it is the active tuning object.");
		var estimatedHeight = (10 * 72.0f) + (9 * 6.0f) + 12.0f;
		_tuningLegendRows.CustomMinimumSize = new Vector2(0.0f, estimatedHeight);
		ConfigureScrollBarAppearance(_tuningLegendScrollContainer);
	}

	private void AddTuningLegendRow(string labelText, Color swatchColor, string description)
	{
		if (_tuningLegendRows == null)
		{
			return;
		}

		var rowPanel = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		rowPanel.AddThemeStyleboxOverride(
			"panel",
			CreateHudPanelStyle(
				new Color(0.07f, 0.09f, 0.13f, 0.94f),
				new Color(0.2f, 0.3f, 0.4f, 0.96f)));
		_tuningLegendRows.AddChild(rowPanel);

		var rowBox = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		rowBox.AddThemeConstantOverride("separation", 4);
		rowPanel.AddChild(rowBox);

		var headerRow = new HBoxContainer();
		headerRow.AddThemeConstantOverride("separation", 8);
		rowBox.AddChild(headerRow);

		var swatch = new ColorRect
		{
			CustomMinimumSize = new Vector2(22.0f, 14.0f),
			Color = swatchColor
		};
		headerRow.AddChild(swatch);

		var label = new Label
		{
			Text = labelText,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", 14);
		label.Modulate = new Color(0.96f, 0.98f, 1.0f);
		headerRow.AddChild(label);

		var descriptionLabel = new Label
		{
			Text = description,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		descriptionLabel.AddThemeFontSizeOverride("font_size", 12);
		descriptionLabel.Modulate = new Color(0.84f, 0.91f, 0.97f);
		rowBox.AddChild(descriptionLabel);
	}

	private void SyncCalibrationControls()
	{
		if (_tuningFieldSelector == null ||
			_tuningWindow == null ||
			_tuningWindowHeaderLabel == null ||
			_tuningInfoToggleButton == null ||
			_tuningWindowInfoLabel == null ||
			_tuningObjectDetailsPanel == null ||
			_tuningObjectDetailsLabel == null ||
			_tuningObjectMiniPanelGrid == null ||
			_tuningLegendPanel == null ||
			_tuningLegendHeaderLabel == null ||
			_tuningLegendRows == null ||
			_tuningScrollContainer == null ||
			_tuningFieldsContainer == null ||
			_tuningOverlayLabel == null ||
			_tuningOverlaySlider == null ||
			_tuningSaveButton == null ||
			_tuningReloadButton == null ||
			_tuningResetButton == null ||
			_aimSpeedTrack == null ||
			_aimTipPad == null)
		{
			return;
		}

		var calibrationVisible = _ruleMode == RuleMode.Calibration;
		SetWindowVisible(_tuningWindow, calibrationVisible && !_menuVisible);
		_aimSpeedTrack.Visible = !calibrationVisible;
		_aimTipPad.Visible = !calibrationVisible;

		if (!calibrationVisible || _calibrationFields.Count == 0)
		{
			return;
		}

		if (_tuningFieldRows.Count != _calibrationFields.Count)
		{
			RebuildTuningFieldRows();
		}

		var selectedObjectLabel = GetSelectedCalibrationObjectLabel();
		var objectFields = GetSelectedCalibrationObjectFields();
		if (_selectedMiniPanelObjectKey != _selectedCalibrationObjectKey || _tuningMiniPanels.Count != objectFields.Count)
		{
			RebuildSelectedCalibrationMiniPanels(objectFields);
		}

		_syncingTuningControls = true;
		var selectedObjectIndex = _calibrationObjects.FindIndex(entry => entry.Key == _selectedCalibrationObjectKey);
		_tuningFieldSelector.Select(Mathf.Clamp(selectedObjectIndex, 0, _calibrationObjects.Count - 1));
		_tuningWindowHeaderLabel.Text = $"Table Tuning | {selectedObjectLabel}";
		_tuningInfoToggleButton.Text = _tuningInfoVisible ? "Hide Info" : "Show Info";
		_tuningWindowInfoLabel.Text =
			"Use the mini-panels for the selected object when you want direct X/Y/Angle-style control.\n" +
			"The flat list below stays visible for full-table context.";
		_tuningWindowInfoLabel.Visible = _tuningInfoVisible;
		_tuningObjectDetailsLabel.Text =
			$"Selected object: {selectedObjectLabel}\n" +
			$"Literal controls: {objectFields.Count}  Jump selector only changes which object these mini-panels target.";
		_tuningObjectDetailsLabel.Visible = _tuningInfoVisible;
		for (var miniPanelIndex = 0; miniPanelIndex < _tuningMiniPanels.Count; miniPanelIndex++)
		{
			var miniPanel = _tuningMiniPanels[miniPanelIndex];
			var field = _calibrationFields[miniPanel.FieldIndex];
			miniPanel.Label.Text = GetMiniPanelFieldLabel(field);
			miniPanel.Slider.MinValue = field.Minimum;
			miniPanel.Slider.MaxValue = field.Maximum;
			miniPanel.Slider.Step = field.FineStep;
			miniPanel.Slider.Value = field.GetValue();
			miniPanel.ValueLabel.Text = field.GetFormattedValue(_tableCalibrationProfile);
		}

		for (var rowIndex = 0; rowIndex < _tuningFieldRows.Count; rowIndex++)
		{
			var row = _tuningFieldRows[rowIndex];
			var field = _calibrationFields[row.FieldIndex];
			var isSelectedObject = field.ObjectKey == _selectedCalibrationObjectKey;
			row.RowLabel.Text =
				$"{field.ObjectLabel} | {field.Label}: {field.GetFormattedValue(_tableCalibrationProfile)}  [{field.Minimum:0.0000} .. {field.Maximum:0.0000}]";
			row.RowLabel.Modulate = isSelectedObject
				? new Color(1.0f, 0.95f, 0.76f)
				: new Color(0.89f, 0.95f, 1.0f);
			row.ValueLabel.Text = field.GetFormattedValue(_tableCalibrationProfile);
			row.ValueLabel.Modulate = row.RowLabel.Modulate;
			row.Slider.MinValue = field.Minimum;
			row.Slider.MaxValue = field.Maximum;
			row.Slider.Step = field.FineStep;
			row.Slider.Value = field.GetValue();
			row.Slider.Modulate = isSelectedObject
				? new Color(1.0f, 0.97f, 0.82f)
				: Colors.White;
			row.Panel.Modulate = isSelectedObject
				? new Color(1.0f, 0.98f, 0.92f)
				: Colors.White;
		}

		_tuningOverlayLabel.Text = $"Overlay thickness: {_overlayLineThicknessPixels:0.0} px";
		_tuningOverlaySlider.Value = _overlayLineThicknessPixels;
		_tuningOverlayLabel.Visible = true;
		_tuningOverlaySlider.Visible = true;
		_tuningSaveButton.Visible = true;
		_tuningReloadButton.Visible = true;
		_tuningResetButton.Visible = true;
		_syncingTuningControls = false;
	}

	private void OnTuningFieldSelected(long selectedIndex)
	{
		if (_syncingTuningControls || selectedIndex < 0 || selectedIndex >= _calibrationObjects.Count)
		{
			return;
		}

		_selectedCalibrationObjectKey = _calibrationObjects[(int)selectedIndex].Key;
		_selectedCalibrationFieldIndex = _calibrationFields.FindIndex(field => field.ObjectKey == _selectedCalibrationObjectKey);
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Tuning object: {GetSelectedCalibrationObjectLabel()}");
		BuildHardcodeOverlay();
		SyncCalibrationControls();
		EnsureSelectedCalibrationObjectVisible();
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void OnTuningRowValueChanged(int fieldIndex, double value)
	{
		if (_syncingTuningControls || _ruleMode != RuleMode.Calibration)
		{
			return;
		}

		if (_world.Phase == SimulationPhase.Running || _shotCaptureActive)
		{
			SyncCalibrationControls();
			return;
		}

		if (fieldIndex < 0 || fieldIndex >= _calibrationFields.Count)
		{
			return;
		}

		var field = _calibrationFields[fieldIndex];
		var updatedValue = Mathf.Clamp((float)value, field.Minimum, field.Maximum);
		if (Mathf.IsEqualApprox(updatedValue, field.GetValue()))
		{
			return;
		}

		_selectedCalibrationObjectKey = field.ObjectKey;
		_selectedCalibrationFieldIndex = fieldIndex;
		field.SetValue(updatedValue);
		ApplyCalibrationProfile($"Tuned {field.Label} -> {field.GetFormattedValue(_tableCalibrationProfile)}");
	}

	private void OnTuningOverlayThicknessChanged(double value)
	{
		if (_syncingTuningControls || _ruleMode != RuleMode.Calibration)
		{
			return;
		}

		var updatedThickness = Mathf.Clamp((float)value, MinOverlayThicknessPixels, MaxOverlayThicknessPixels);
		if (Mathf.IsEqualApprox(updatedThickness, _overlayLineThicknessPixels))
		{
			return;
		}

		_overlayLineThicknessPixels = updatedThickness;
		BuildHardcodeOverlay();
		_recentRuleNotes.Clear();
		_recentRuleNotes.Add($"Overlay thickness: {_overlayLineThicknessPixels:0.0} px");
		UpdateStatusLabel(Array.Empty<ShotEvent>());
	}

	private void EnsureSelectedCalibrationObjectVisible()
	{
		if (_tuningScrollContainer == null || _tuningFieldRows.Count == 0)
		{
			return;
		}

		var targetRow = _tuningFieldRows.FirstOrDefault(row =>
			_calibrationFields[row.FieldIndex].ObjectKey == _selectedCalibrationObjectKey);
		if (targetRow == null)
		{
			return;
		}

		_tuningScrollContainer.EnsureControlVisible(targetRow.Panel);
	}

	private CalibrationField GetSelectedCalibrationField()
	{
		return _calibrationFields[Mathf.Clamp(_selectedCalibrationFieldIndex, 0, _calibrationFields.Count - 1)];
	}

	private IReadOnlyList<CalibrationField> GetSelectedCalibrationObjectFields()
	{
		if (string.IsNullOrWhiteSpace(_selectedCalibrationObjectKey))
		{
			return Array.Empty<CalibrationField>();
		}

		return _calibrationFields
			.Where(field => field.ObjectKey == _selectedCalibrationObjectKey)
			.ToArray();
	}

	private string GetSelectedCalibrationObjectLabel()
	{
		var selectedIndex = _calibrationObjects.FindIndex(entry => entry.Key == _selectedCalibrationObjectKey);
		return selectedIndex >= 0 ? _calibrationObjects[selectedIndex].Label : "None";
	}
}
