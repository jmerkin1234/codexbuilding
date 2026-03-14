using System.Numerics;

namespace CodexBuilding.Billiards.Core.Geometry;

public sealed class PocketSpec
{
    public PocketSpec(string sourceName, PocketKind kind, Vector2 center, float captureRadiusMeters)
    {
        SourceName = sourceName;
        Kind = kind;
        Center = center;
        CaptureRadiusMeters = captureRadiusMeters;
    }

    public string SourceName { get; }

    public PocketKind Kind { get; }

    public Vector2 Center { get; }

    public float CaptureRadiusMeters { get; }
}
