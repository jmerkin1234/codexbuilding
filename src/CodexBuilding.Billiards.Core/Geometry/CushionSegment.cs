using System.Numerics;

namespace CodexBuilding.Billiards.Core.Geometry;

public sealed class CushionSegment
{
    public CushionSegment(string sourceName, Vector2 start, Vector2 end, Vector2 inwardNormal)
    {
        SourceName = sourceName;
        Start = start;
        End = end;
        InwardNormal = Vector2.Normalize(inwardNormal);
    }

    public string SourceName { get; }

    public Vector2 Start { get; }

    public Vector2 End { get; }

    public Vector2 Direction => Vector2.Normalize(End - Start);

    public Vector2 InwardNormal { get; }
}
