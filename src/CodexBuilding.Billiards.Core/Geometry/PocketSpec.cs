using System.Numerics;

namespace CodexBuilding.Billiards.Core.Geometry;

public sealed class PocketSpec
{
    public PocketSpec(
        string sourceName,
        PocketKind kind,
        Vector2 center,
        float captureRadiusMeters,
        Vector2 mouthCenter,
        float mouthHalfWidthMeters,
        float dropRadiusMeters,
        float maxEntrySpeedMetersPerSecond)
    {
        if (captureRadiusMeters <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(captureRadiusMeters), "Capture radius must be positive.");
        }

        if (mouthHalfWidthMeters <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(mouthHalfWidthMeters), "Mouth half-width must be positive.");
        }

        if (dropRadiusMeters <= 0.0f || dropRadiusMeters > captureRadiusMeters)
        {
            throw new ArgumentOutOfRangeException(nameof(dropRadiusMeters), "Drop radius must be positive and no larger than the capture radius.");
        }

        if (maxEntrySpeedMetersPerSecond <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntrySpeedMetersPerSecond), "Max entry speed must be positive.");
        }

        SourceName = sourceName;
        Kind = kind;
        Center = center;
        CaptureRadiusMeters = captureRadiusMeters;
        MouthCenter = mouthCenter;
        MouthHalfWidthMeters = mouthHalfWidthMeters;
        DropRadiusMeters = dropRadiusMeters;
        MaxEntrySpeedMetersPerSecond = maxEntrySpeedMetersPerSecond;
    }

    public string SourceName { get; }

    public PocketKind Kind { get; }

    public Vector2 Center { get; }

    public float CaptureRadiusMeters { get; }

    public Vector2 MouthCenter { get; }

    public float MouthHalfWidthMeters { get; }

    public float DropRadiusMeters { get; }

    public float MaxEntrySpeedMetersPerSecond { get; }

    public Vector2 EntryDirection
    {
        get
        {
            var direction = Center - MouthCenter;
            return direction.LengthSquared() <= float.Epsilon
                ? Vector2.UnitY
                : Vector2.Normalize(direction);
        }
    }

    public float FunnelDepthMeters => Vector2.Distance(MouthCenter, Center);
}
