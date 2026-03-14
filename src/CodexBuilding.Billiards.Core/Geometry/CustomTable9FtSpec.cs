using System.Numerics;

namespace CodexBuilding.Billiards.Core.Geometry;

public static class CustomTable9FtSpec
{
    public static TableSpec Create()
    {
        var clothMin = new Vector2(-1.2699999f, -0.63499993f);
        var clothMax = new Vector2(1.2699999f, 0.63499993f);
        var tableCenter = (clothMin + clothMax) * 0.5f;

        var cushions = new[]
        {
            CreateSegment(
                "rail_head",
                new Vector2(-1.2249266f, -0.56977034f),
                new Vector2(-1.2249266f, 0.57038474f),
                tableCenter),
            CreateSegment(
                "rail_foot",
                new Vector2(1.2258036f, -0.5681504f),
                new Vector2(1.2258036f, 0.5702276f),
                tableCenter),
            CreateSegment(
                "rail_upper_left",
                new Vector2(-1.2068906f, -0.59292513f),
                new Vector2(-0.050392687f, -0.59292513f),
                tableCenter),
            CreateSegment(
                "rail_upper_right",
                new Vector2(0.050399005f, -0.59292495f),
                new Vector2(1.2069001f, -0.59292495f),
                tableCenter),
            CreateSegment(
                "rail_bottom_left",
                new Vector2(-1.2068906f, 0.5953581f),
                new Vector2(-0.050392687f, 0.5953581f),
                tableCenter),
            CreateSegment(
                "rail_bottom_right",
                new Vector2(0.050398767f, 0.5953247f),
                new Vector2(1.2068999f, 0.5953247f),
                tableCenter)
        };

        var jaws = new[]
        {
            CreateCornerJaw("pocket_BR4_jaw_vertical", new Vector2(-1.2249266f, -0.56977034f), new Vector2(-1.2508456f, -0.6167401f), tableCenter),
            CreateCornerJaw("pocket_BR4_jaw_horizontal", new Vector2(-1.2068906f, -0.59292513f), new Vector2(-1.2508456f, -0.6167401f), tableCenter),
            CreateCornerJaw("pocket_BL2_jaw_vertical", new Vector2(1.2258036f, -0.5681504f), new Vector2(1.2508553f, -0.6167401f), tableCenter),
            CreateCornerJaw("pocket_BL2_jaw_horizontal", new Vector2(1.2069001f, -0.59292495f), new Vector2(1.2508553f, -0.6167401f), tableCenter),
            CreateCornerJaw("Pocket_TR5_jaw_vertical", new Vector2(-1.2249266f, 0.57038474f), new Vector2(-1.2508456f, 0.61673915f), tableCenter),
            CreateCornerJaw("Pocket_TR5_jaw_horizontal", new Vector2(-1.2068906f, 0.5953581f), new Vector2(-1.2508456f, 0.61673915f), tableCenter),
            CreateCornerJaw("pockett_TL1_jaw_vertical", new Vector2(1.2258036f, 0.5702276f), new Vector2(1.2640905f, 0.49089444f), tableCenter),
            CreateCornerJaw("pockett_TL1_jaw_horizontal", new Vector2(1.2068999f, 0.5953247f), new Vector2(1.2640905f, 0.49089444f), tableCenter),
            CreateSideJaw("pocket_BM3_jaw_left", new Vector2(-0.050392687f, -0.59292513f), new Vector2(0.0000029f, -0.66602194f), tableCenter),
            CreateSideJaw("pocket_BM3_jaw_right", new Vector2(0.050399005f, -0.59292495f), new Vector2(0.0000029f, -0.66602194f), tableCenter),
            CreateSideJaw("Pocket_TM6_jaw_left", new Vector2(-0.050392687f, 0.5953581f), new Vector2(0.0000029f, 0.66556805f), tableCenter),
            CreateSideJaw("Pocket_TM6_jaw_right", new Vector2(0.050398767f, 0.5953247f), new Vector2(0.0000029f, 0.66556805f), tableCenter)
        };

        var pockets = new[]
        {
            CreatePocketFromJawStarts(
                "pockett_TL1",
                PocketKind.Corner,
                new Vector2(1.2640905f, 0.49089444f),
                0.0584f,
                FindSegment("pockett_TL1_jaw_vertical", jaws),
                FindSegment("pockett_TL1_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "pocket_BL2",
                PocketKind.Corner,
                new Vector2(1.2508553f, -0.6167401f),
                0.0584f,
                FindSegment("pocket_BL2_jaw_vertical", jaws),
                FindSegment("pocket_BL2_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "pocket_BM3",
                PocketKind.Side,
                new Vector2(0.0000029f, -0.66602194f),
                0.0614f,
                FindSegment("pocket_BM3_jaw_left", jaws),
                FindSegment("pocket_BM3_jaw_right", jaws),
                dropRadiusMeters: 0.044f,
                maxEntrySpeedMetersPerSecond: 1.0f),
            CreatePocketFromJawStarts(
                "pocket_BR4",
                PocketKind.Corner,
                new Vector2(-1.2508456f, -0.6167401f),
                0.0584f,
                FindSegment("pocket_BR4_jaw_vertical", jaws),
                FindSegment("pocket_BR4_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "Pocket_TR5",
                PocketKind.Corner,
                new Vector2(-1.2508456f, 0.61673915f),
                0.0584f,
                FindSegment("Pocket_TR5_jaw_vertical", jaws),
                FindSegment("Pocket_TR5_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "Pocket_TM6",
                PocketKind.Side,
                new Vector2(0.0000029f, 0.66556805f),
                0.0614f,
                FindSegment("Pocket_TM6_jaw_left", jaws),
                FindSegment("Pocket_TM6_jaw_right", jaws),
                dropRadiusMeters: 0.044f,
                maxEntrySpeedMetersPerSecond: 1.0f)
        };

        return new TableSpec(
            name: "customtable_9ft",
            sourceBlendPath: "/home/justin/Desktop/customtable_9ft.blend",
            clothMin: clothMin,
            clothMax: clothMax,
            ballDiameterMeters: 0.05715f,
            cueBallSpawn: new Vector2(-0.733902f, 0.002383f),
            rackApexSpot: new Vector2(0.616941f, 0.0f),
            cushions: cushions,
            jawSegments: jaws,
            pockets: pockets);
    }

    private static CushionSegment CreateSegment(string sourceName, Vector2 start, Vector2 end, Vector2 tableCenter)
    {
        return new CushionSegment(sourceName, start, end, ResolveInwardNormal(start, end, tableCenter));
    }

    private static CushionSegment CreateCornerJaw(string sourceName, Vector2 railEndpoint, Vector2 pocketCenter, Vector2 tableCenter)
    {
        var apex = BuildJawApex(pocketCenter, tableCenter, 0.03f);
        return CreateSegment(sourceName, railEndpoint, apex, tableCenter);
    }

    private static CushionSegment CreateSideJaw(string sourceName, Vector2 railEndpoint, Vector2 pocketCenter, Vector2 tableCenter)
    {
        var apex = BuildJawApex(pocketCenter, tableCenter, 0.031f);
        return CreateSegment(sourceName, railEndpoint, apex, tableCenter);
    }

    private static PocketSpec CreatePocketFromJawStarts(
        string sourceName,
        PocketKind kind,
        Vector2 center,
        float captureRadiusMeters,
        CushionSegment firstJaw,
        CushionSegment secondJaw,
        float dropRadiusMeters,
        float maxEntrySpeedMetersPerSecond)
    {
        var mouthCenter = (firstJaw.Start + secondJaw.Start) * 0.5f;
        var mouthHalfWidthMeters = Vector2.Distance(firstJaw.Start, secondJaw.Start) * 0.5f;

        return new PocketSpec(
            sourceName,
            kind,
            center,
            captureRadiusMeters,
            mouthCenter,
            mouthHalfWidthMeters,
            dropRadiusMeters,
            maxEntrySpeedMetersPerSecond);
    }

    private static CushionSegment FindSegment(string sourceName, IReadOnlyList<CushionSegment> segments)
    {
        foreach (var segment in segments)
        {
            if (segment.SourceName == sourceName)
            {
                return segment;
            }
        }

        throw new InvalidOperationException($"Missing segment '{sourceName}' while building the hardcoded table spec.");
    }

    private static Vector2 BuildJawApex(Vector2 pocketCenter, Vector2 tableCenter, float offsetMeters)
    {
        var inwardDirection = Vector2.Normalize(tableCenter - pocketCenter);
        return pocketCenter + (inwardDirection * offsetMeters);
    }

    private static Vector2 ResolveInwardNormal(Vector2 start, Vector2 end, Vector2 tableCenter)
    {
        var direction = Vector2.Normalize(end - start);
        var leftNormal = new Vector2(-direction.Y, direction.X);
        var midpoint = (start + end) * 0.5f;

        return Vector2.Dot(leftNormal, tableCenter - midpoint) >= 0.0f
            ? leftNormal
            : -leftNormal;
    }
}
