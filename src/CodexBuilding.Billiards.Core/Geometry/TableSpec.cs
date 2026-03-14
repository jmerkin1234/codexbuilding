using System.Numerics;

namespace CodexBuilding.Billiards.Core.Geometry;

public sealed class TableSpec
{
    public TableSpec(
        string name,
        string sourceBlendPath,
        Vector2 clothMin,
        Vector2 clothMax,
        float ballDiameterMeters,
        Vector2 cueBallSpawn,
        Vector2 rackApexSpot,
        IReadOnlyList<CushionSegment> cushions,
        IReadOnlyList<PocketSpec> pockets)
    {
        Name = name;
        SourceBlendPath = sourceBlendPath;
        ClothMin = clothMin;
        ClothMax = clothMax;
        BallDiameterMeters = ballDiameterMeters;
        CueBallSpawn = cueBallSpawn;
        RackApexSpot = rackApexSpot;
        Cushions = cushions;
        Pockets = pockets;
    }

    public string Name { get; }

    public string SourceBlendPath { get; }

    public Vector2 ClothMin { get; }

    public Vector2 ClothMax { get; }

    public float BallDiameterMeters { get; }

    public Vector2 CueBallSpawn { get; }

    public Vector2 RackApexSpot { get; }

    public IReadOnlyList<CushionSegment> Cushions { get; }

    public IReadOnlyList<PocketSpec> Pockets { get; }
}
