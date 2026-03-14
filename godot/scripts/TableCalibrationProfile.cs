using System.Text.Json;
using CodexBuilding.Billiards.Core.Geometry;
using NumericsVector2 = System.Numerics.Vector2;

namespace CodexBuilding.Billiards.Godot46;

internal sealed class TableCalibrationProfile
{
	public Vector2Calibration ClothMinOffset { get; set; } = new();
	public Vector2Calibration ClothMaxOffset { get; set; } = new();
	public Vector2Calibration CueBallSpawnOffset { get; set; } = new();
	public Vector2Calibration RackApexSpotOffset { get; set; } = new();
	public Dictionary<string, SegmentCalibration> Cushions { get; set; } = new(StringComparer.Ordinal);
	public Dictionary<string, SegmentCalibration> Jaws { get; set; } = new(StringComparer.Ordinal);
	public Dictionary<string, PocketCalibration> Pockets { get; set; } = new(StringComparer.Ordinal);

	public static TableCalibrationProfile CreateDefault(TableSpec baseSpec)
	{
		var profile = new TableCalibrationProfile();
		profile.EnsureKeys(baseSpec);
		return profile;
	}

	public void EnsureKeys(TableSpec baseSpec)
	{
		foreach (var cushion in baseSpec.Cushions)
		{
			if (!Cushions.ContainsKey(cushion.SourceName))
			{
				Cushions[cushion.SourceName] = new SegmentCalibration();
			}
		}

		foreach (var jaw in baseSpec.JawSegments)
		{
			if (!Jaws.ContainsKey(jaw.SourceName))
			{
				Jaws[jaw.SourceName] = new SegmentCalibration();
			}
		}

		foreach (var pocket in baseSpec.Pockets)
		{
			if (!Pockets.ContainsKey(pocket.SourceName))
			{
				Pockets[pocket.SourceName] = new PocketCalibration();
			}
		}
	}

	public static TableCalibrationProfile LoadOrDefault(string absolutePath, TableSpec baseSpec)
	{
		try
		{
			if (!File.Exists(absolutePath))
			{
				return CreateDefault(baseSpec);
			}

			var json = File.ReadAllText(absolutePath);
			var profile = JsonSerializer.Deserialize<TableCalibrationProfile>(json) ?? CreateDefault(baseSpec);
			profile.EnsureKeys(baseSpec);
			return profile;
		}
		catch
		{
			return CreateDefault(baseSpec);
		}
	}

	public void Save(string absolutePath)
	{
		var directoryPath = Path.GetDirectoryName(absolutePath);
		if (!string.IsNullOrWhiteSpace(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			WriteIndented = true
		});
		File.WriteAllText(absolutePath, json);
	}
}

internal sealed class SegmentCalibration
{
	public Vector2Calibration StartOffset { get; set; } = new();
	public Vector2Calibration EndOffset { get; set; } = new();
}

internal sealed class PocketCalibration
{
	public Vector2Calibration CenterOffset { get; set; } = new();
	public float CaptureRadiusOffset { get; set; }
	public float DropRadiusOffset { get; set; }
}

internal sealed class Vector2Calibration
{
	public float X { get; set; }
	public float Y { get; set; }

	public NumericsVector2 ToNumerics()
	{
		return new NumericsVector2(X, Y);
	}
}

internal static class TableCalibrationBuilder
{
	public static TableSpec Apply(TableSpec baseSpec, TableCalibrationProfile profile)
	{
		var clothMin = baseSpec.ClothMin + profile.ClothMinOffset.ToNumerics();
		var clothMax = baseSpec.ClothMax + profile.ClothMaxOffset.ToNumerics();
		var cueBallSpawn = baseSpec.CueBallSpawn + profile.CueBallSpawnOffset.ToNumerics();
		var rackApexSpot = baseSpec.RackApexSpot + profile.RackApexSpotOffset.ToNumerics();
		var tableCenter = (clothMin + clothMax) * 0.5f;

		var cushions = baseSpec.Cushions
			.Select(cushion => ApplySegment(cushion, profile.Cushions, tableCenter))
			.ToArray();
		var jaws = baseSpec.JawSegments
			.Select(jaw => ApplySegment(jaw, profile.Jaws, tableCenter))
			.ToArray();
		var pockets = baseSpec.Pockets
			.Select(pocket => ApplyPocket(pocket, profile.Pockets, jaws))
			.ToArray();

		return new TableSpec(
			baseSpec.Name,
			baseSpec.SourceBlendPath,
			clothMin,
			clothMax,
			baseSpec.BallDiameterMeters,
			cueBallSpawn,
			rackApexSpot,
			cushions,
			jaws,
			pockets);
	}

	private static CushionSegment ApplySegment(
		CushionSegment baseSegment,
		IReadOnlyDictionary<string, SegmentCalibration> calibrations,
		NumericsVector2 tableCenter)
	{
		calibrations.TryGetValue(baseSegment.SourceName, out var calibration);
		var start = baseSegment.Start + (calibration?.StartOffset.ToNumerics() ?? NumericsVector2.Zero);
		var end = baseSegment.End + (calibration?.EndOffset.ToNumerics() ?? NumericsVector2.Zero);
		return new CushionSegment(
			baseSegment.SourceName,
			start,
			end,
			ResolveInwardNormal(start, end, tableCenter));
	}

	private static PocketSpec ApplyPocket(
		PocketSpec basePocket,
		IReadOnlyDictionary<string, PocketCalibration> calibrations,
		IReadOnlyList<CushionSegment> adjustedJaws)
	{
		calibrations.TryGetValue(basePocket.SourceName, out var calibration);
		var centerOffset = calibration?.CenterOffset.ToNumerics() ?? NumericsVector2.Zero;
		var center = basePocket.Center + centerOffset;
		var captureRadius = MathF.Max(0.01f, basePocket.CaptureRadiusMeters + (calibration?.CaptureRadiusOffset ?? 0.0f));
		var dropRadius = Math.Clamp(
			basePocket.DropRadiusMeters + (calibration?.DropRadiusOffset ?? 0.0f),
			0.005f,
			captureRadius);

		var relatedJaws = adjustedJaws
			.Where(jaw => jaw.SourceName.StartsWith(basePocket.SourceName + "_jaw_", StringComparison.Ordinal))
			.Take(2)
			.ToArray();

		var mouthCenter = relatedJaws.Length == 2
			? (relatedJaws[0].Start + relatedJaws[1].Start) * 0.5f
			: basePocket.MouthCenter + centerOffset;
		var mouthHalfWidth = relatedJaws.Length == 2
			? NumericsVector2.Distance(relatedJaws[0].Start, relatedJaws[1].Start) * 0.5f
			: basePocket.MouthHalfWidthMeters;

		return new PocketSpec(
			basePocket.SourceName,
			basePocket.Kind,
			center,
			captureRadius,
			mouthCenter,
			MathF.Max(0.005f, mouthHalfWidth),
			dropRadius,
			basePocket.MaxEntrySpeedMetersPerSecond);
	}

	private static NumericsVector2 ResolveInwardNormal(NumericsVector2 start, NumericsVector2 end, NumericsVector2 tableCenter)
	{
		var direction = NumericsVector2.Normalize(end - start);
		var leftNormal = new NumericsVector2(-direction.Y, direction.X);
		var midpoint = (start + end) * 0.5f;
		return NumericsVector2.Dot(leftNormal, tableCenter - midpoint) >= 0.0f
			? leftNormal
			: -leftNormal;
	}
}
