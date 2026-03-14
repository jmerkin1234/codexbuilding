using System;
using System.Collections.Generic;
using System.Linq;
using CodexBuilding.Billiards.Core.Geometry;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

public partial class Main
{
	private void BuildCalibrationFields()
	{
		_calibrationFields.Clear();

		AddCalibrationField("PlayArea", "PlayArea", "Play Area", "Play Area Min X", "OverlayClothLeft", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMinOffset.X,
			value => _tableCalibrationProfile.ClothMinOffset.X = value);
		AddCalibrationField("PlayArea", "PlayArea", "Play Area", "Play Area Min Y", "OverlayClothTop", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMinOffset.Y,
			value => _tableCalibrationProfile.ClothMinOffset.Y = value);
		AddCalibrationField("PlayArea", "PlayArea", "Play Area", "Play Area Max X", "OverlayClothRight", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMaxOffset.X,
			value => _tableCalibrationProfile.ClothMaxOffset.X = value);
		AddCalibrationField("PlayArea", "PlayArea", "Play Area", "Play Area Max Y", "OverlayClothBottom", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.ClothMaxOffset.Y,
			value => _tableCalibrationProfile.ClothMaxOffset.Y = value);

		AddCalibrationField("Spots", "CueBallSpawn", "Cue Ball Spawn", "Cue Ball Spawn X", "OverlayCueBallSpawn", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.CueBallSpawnOffset.X,
			value => _tableCalibrationProfile.CueBallSpawnOffset.X = value);
		AddCalibrationField("Spots", "CueBallSpawn", "Cue Ball Spawn", "Cue Ball Spawn Y", "OverlayCueBallSpawn", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.CueBallSpawnOffset.Y,
			value => _tableCalibrationProfile.CueBallSpawnOffset.Y = value);
		AddCalibrationField("Spots", "RackApexSpot", "Rack Apex Spot", "Rack Apex X", "OverlayRackApexSpot", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.RackApexSpotOffset.X,
			value => _tableCalibrationProfile.RackApexSpotOffset.X = value);
		AddCalibrationField("Spots", "RackApexSpot", "Rack Apex Spot", "Rack Apex Y", "OverlayRackApexSpot", -0.4f, 0.4f, 0.0005f, 0.005f,
			() => _tableCalibrationProfile.RackApexSpotOffset.Y,
			value => _tableCalibrationProfile.RackApexSpotOffset.Y = value);

		foreach (var cushion in _baseTableSpec.Cushions)
		{
			var sourceName = cushion.SourceName;
			var startJawSourceName = FindClosestJawSourceName(cushion.Start);
			var endJawSourceName = FindClosestJawSourceName(cushion.End);
			AddCalibrationField("Cushions", sourceName, sourceName, $"{sourceName} Start X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].StartOffset.X,
				value => _tableCalibrationProfile.Cushions[sourceName].StartOffset.X = value);
			AddCalibrationField("Cushions", sourceName, sourceName, $"{sourceName} Start Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].StartOffset.Y,
				value => _tableCalibrationProfile.Cushions[sourceName].StartOffset.Y = value);
			AddCalibrationField("Cushions", sourceName, sourceName, $"{sourceName} End X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].EndOffset.X,
				value => _tableCalibrationProfile.Cushions[sourceName].EndOffset.X = value);
			AddCalibrationField("Cushions", sourceName, sourceName, $"{sourceName} End Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Cushions[sourceName].EndOffset.Y,
				value => _tableCalibrationProfile.Cushions[sourceName].EndOffset.Y = value);
			AddCalibrationField("Cushions", sourceName, sourceName, $"{sourceName} Start Pocket Angle", $"Overlay_{startJawSourceName}", -180.0f, 180.0f, 0.1f, 1.0f,
				() => GetAdjustedSegmentAngleDegrees(_baseTableSpec.JawSegments, _tableCalibrationProfile.Jaws, startJawSourceName),
				value => SetAdjustedSegmentAngleDegrees(_baseTableSpec.JawSegments, _tableCalibrationProfile.Jaws, startJawSourceName, value));
			AddCalibrationField("Cushions", sourceName, sourceName, $"{sourceName} End Pocket Angle", $"Overlay_{endJawSourceName}", -180.0f, 180.0f, 0.1f, 1.0f,
				() => GetAdjustedSegmentAngleDegrees(_baseTableSpec.JawSegments, _tableCalibrationProfile.Jaws, endJawSourceName),
				value => SetAdjustedSegmentAngleDegrees(_baseTableSpec.JawSegments, _tableCalibrationProfile.Jaws, endJawSourceName, value));
		}

		foreach (var jaw in _baseTableSpec.JawSegments)
		{
			var sourceName = jaw.SourceName;
			AddCalibrationField("Jaws", sourceName, sourceName, $"{sourceName} Start X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].StartOffset.X,
				value => _tableCalibrationProfile.Jaws[sourceName].StartOffset.X = value);
			AddCalibrationField("Jaws", sourceName, sourceName, $"{sourceName} Start Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].StartOffset.Y,
				value => _tableCalibrationProfile.Jaws[sourceName].StartOffset.Y = value);
			AddCalibrationField("Jaws", sourceName, sourceName, $"{sourceName} End X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].EndOffset.X,
				value => _tableCalibrationProfile.Jaws[sourceName].EndOffset.X = value);
			AddCalibrationField("Jaws", sourceName, sourceName, $"{sourceName} End Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Jaws[sourceName].EndOffset.Y,
				value => _tableCalibrationProfile.Jaws[sourceName].EndOffset.Y = value);
			AddCalibrationField("Jaws", sourceName, sourceName, $"{sourceName} Angle", $"Overlay_{sourceName}", -180.0f, 180.0f, 0.1f, 1.0f,
				() => GetAdjustedSegmentAngleDegrees(_baseTableSpec.JawSegments, _tableCalibrationProfile.Jaws, sourceName),
				value => SetAdjustedSegmentAngleDegrees(_baseTableSpec.JawSegments, _tableCalibrationProfile.Jaws, sourceName, value));
		}

		foreach (var pocket in _baseTableSpec.Pockets)
		{
			var sourceName = pocket.SourceName;
			AddCalibrationField("Pockets", sourceName, sourceName, $"{sourceName} Center X", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.X,
				value => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.X = value);
			AddCalibrationField("Pockets", sourceName, sourceName, $"{sourceName} Center Y", $"Overlay_{sourceName}", -0.4f, 0.4f, 0.0005f, 0.005f,
				() => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.Y,
				value => _tableCalibrationProfile.Pockets[sourceName].CenterOffset.Y = value);
			AddCalibrationField("Pockets", sourceName, sourceName, $"{sourceName} Capture Radius", $"Overlay_{sourceName}", -0.05f, 0.05f, 0.0005f, 0.003f,
				() => _tableCalibrationProfile.Pockets[sourceName].CaptureRadiusOffset,
				value => _tableCalibrationProfile.Pockets[sourceName].CaptureRadiusOffset = value);
			AddCalibrationField("Pockets", sourceName, sourceName, $"{sourceName} Drop Radius", $"Overlay_{sourceName}", -0.05f, 0.05f, 0.0005f, 0.003f,
				() => _tableCalibrationProfile.Pockets[sourceName].DropRadiusOffset,
				value => _tableCalibrationProfile.Pockets[sourceName].DropRadiusOffset = value);
		}

		if (_calibrationFields.Count == 0)
		{
			throw new InvalidOperationException("Calibration mode requires at least one calibration field.");
		}

		_calibrationObjects.Clear();
		_calibrationObjects.AddRange(_calibrationFields
			.GroupBy(field => field.ObjectKey, StringComparer.Ordinal)
			.Select(group => new CalibrationObjectEntry(group.Key, group.First().ObjectLabel))
			.OrderBy(entry => entry.Label, StringComparer.Ordinal));

		if (string.IsNullOrWhiteSpace(_selectedCalibrationObjectKey) ||
			!_calibrationObjects.Any(entry => entry.Key == _selectedCalibrationObjectKey))
		{
			_selectedCalibrationObjectKey = _calibrationObjects[0].Key;
		}

		_selectedCalibrationFieldIndex = _calibrationFields.FindIndex(field => field.ObjectKey == _selectedCalibrationObjectKey);
		_selectedCalibrationFieldIndex = Mathf.Clamp(_selectedCalibrationFieldIndex, 0, _calibrationFields.Count - 1);
	}

	private void AddCalibrationField(
		string section,
		string objectKey,
		string objectLabel,
		string label,
		string overlayTarget,
		float minimum,
		float maximum,
		float fineStep,
		float coarseStep,
		Func<float> getter,
		Action<float> setter)
	{
		_calibrationFields.Add(new CalibrationField(section, objectKey, objectLabel, label, overlayTarget, minimum, maximum, fineStep, coarseStep, getter, setter));
	}

	private string FindClosestJawSourceName(NumericsVector2 point)
	{
		if (_baseTableSpec.JawSegments.Count == 0)
		{
			throw new InvalidOperationException("No jaw segments are available for tuning.");
		}

		string? closestSourceName = null;
		var closestDistanceSquared = float.MaxValue;
		foreach (var jaw in _baseTableSpec.JawSegments)
		{
			var startDistanceSquared = NumericsVector2.DistanceSquared(point, jaw.Start);
			if (startDistanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = startDistanceSquared;
				closestSourceName = jaw.SourceName;
			}

			var endDistanceSquared = NumericsVector2.DistanceSquared(point, jaw.End);
			if (endDistanceSquared < closestDistanceSquared)
			{
				closestDistanceSquared = endDistanceSquared;
				closestSourceName = jaw.SourceName;
			}
		}

		return closestSourceName ?? throw new InvalidOperationException("Unable to resolve a jaw segment for the selected cushion endpoint.");
	}

	private static float GetAdjustedSegmentAngleDegrees(
		IReadOnlyList<CushionSegment> baseSegments,
		IReadOnlyDictionary<string, SegmentCalibration> calibrations,
		string sourceName)
	{
		var baseSegment = baseSegments.First(segment => segment.SourceName == sourceName);
		calibrations.TryGetValue(sourceName, out var calibration);
		var start = baseSegment.Start + (calibration?.StartOffset.ToNumerics() ?? NumericsVector2.Zero);
		var end = baseSegment.End + (calibration?.EndOffset.ToNumerics() ?? NumericsVector2.Zero);
		var direction = end - start;
		if (direction.LengthSquared() <= 0.000001f)
		{
			return 0.0f;
		}

		return Mathf.RadToDeg(MathF.Atan2(direction.Y, direction.X));
	}

	private static void SetAdjustedSegmentAngleDegrees(
		IReadOnlyList<CushionSegment> baseSegments,
		IDictionary<string, SegmentCalibration> calibrations,
		string sourceName,
		float angleDegrees)
	{
		var baseSegment = baseSegments.First(segment => segment.SourceName == sourceName);
		if (!calibrations.TryGetValue(sourceName, out var calibration))
		{
			calibration = new SegmentCalibration();
			calibrations[sourceName] = calibration;
		}

		var currentStart = baseSegment.Start + calibration.StartOffset.ToNumerics();
		var currentEnd = baseSegment.End + calibration.EndOffset.ToNumerics();
		var midpoint = (currentStart + currentEnd) * 0.5f;
		var halfLength = NumericsVector2.Distance(currentStart, currentEnd) * 0.5f;
		var angleRadians = Mathf.DegToRad(angleDegrees);
		var direction = new NumericsVector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
		var newStart = midpoint - (direction * halfLength);
		var newEnd = midpoint + (direction * halfLength);
		calibration.StartOffset.X = newStart.X - baseSegment.Start.X;
		calibration.StartOffset.Y = newStart.Y - baseSegment.Start.Y;
		calibration.EndOffset.X = newEnd.X - baseSegment.End.X;
		calibration.EndOffset.Y = newEnd.Y - baseSegment.End.Y;
	}
}
