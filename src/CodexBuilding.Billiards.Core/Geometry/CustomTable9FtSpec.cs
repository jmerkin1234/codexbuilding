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
                new Vector2(-1.2249266f, -0.51401377f),
                new Vector2(-1.2249266f, 0.51462537f),
                tableCenter),
            CreateSegment(
                "rail_foot",
                new Vector2(1.2258036f, -0.5123930f),
                new Vector2(1.2258036f, 0.51447380f),
                tableCenter),
            CreateSegment(
                "rail_upper_left",
                new Vector2(-1.1487218f, -0.5934566f),
                new Vector2(-0.062250376f, -0.5934566f),
                tableCenter),
            CreateSegment(
                "rail_upper_right",
                new Vector2(0.062256694f, -0.5929293f),
                new Vector2(1.1487315f, -0.5929293f),
                tableCenter),
            CreateSegment(
                "rail_bottom_left",
                new Vector2(-1.1487218f, 0.5953581f),
                new Vector2(-0.062250376f, 0.5953581f),
                tableCenter),
            CreateSegment(
                "rail_bottom_right",
                new Vector2(0.062256455f, 0.5953247f),
                new Vector2(1.1487312f, 0.5953247f),
                tableCenter)
        };

        var jaws = new[]
        {
            CreateCornerJaw("pocket_BR4_jaw_vertical", new Vector2(-1.2249266f, -0.51401377f), new Vector2(-1.2508408f, -0.6166018f), tableCenter),
            CreateCornerJaw("pocket_BR4_jaw_horizontal", new Vector2(-1.1487218f, -0.5934566f), new Vector2(-1.2508408f, -0.6166018f), tableCenter),
            CreateCornerJaw("pocket_BL2_jaw_vertical", new Vector2(1.2258036f, -0.5123930f), new Vector2(1.2508457f, -0.6166018f), tableCenter),
            CreateCornerJaw("pocket_BL2_jaw_horizontal", new Vector2(1.1487315f, -0.5929293f), new Vector2(1.2508457f, -0.6166018f), tableCenter),
            CreateCornerJaw("Pocket_TR5_jaw_vertical", new Vector2(-1.2249266f, 0.51462537f), new Vector2(-1.2508408f, 0.6166031f), tableCenter),
            CreateCornerJaw("Pocket_TR5_jaw_horizontal", new Vector2(-1.1487218f, 0.5953581f), new Vector2(-1.2508408f, 0.6166031f), tableCenter),
            CreateCornerJaw("pocket_TL1_jaw_vertical", new Vector2(1.2258036f, 0.5144738f), new Vector2(1.2491074f, 0.6177103f), tableCenter),
            CreateCornerJaw("pocket_TL1_jaw_horizontal", new Vector2(1.1487312f, 0.5953247f), new Vector2(1.2491074f, 0.6177103f), tableCenter),
            CreateSideJaw("pocket_BM3_jaw_left", new Vector2(-0.062250376f, -0.5934566f), new Vector2(0.0000029057f, -0.6650000f), tableCenter),
            CreateSideJaw("pocket_BM3_jaw_right", new Vector2(0.062256694f, -0.5929293f), new Vector2(0.0000029057f, -0.6650000f), tableCenter),
            CreateSideJaw("Pocket_TM6_jaw_left", new Vector2(-0.062250376f, 0.5953581f), new Vector2(0.0000029057f, 0.6650000f), tableCenter),
            CreateSideJaw("Pocket_TM6_jaw_right", new Vector2(0.062256455f, 0.5953247f), new Vector2(0.0000029057f, 0.6650000f), tableCenter)
        };

        var pockets = new[]
        {
            CreatePocketFromJawStarts(
                "pocket_TL1",
                PocketKind.Corner,
                new Vector2(1.2491074f, 0.6177103f),
                0.0584f,
                FindSegment("pocket_TL1_jaw_vertical", jaws),
                FindSegment("pocket_TL1_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "pocket_BL2",
                PocketKind.Corner,
                new Vector2(1.2508457f, -0.6166018f),
                0.0584f,
                FindSegment("pocket_BL2_jaw_vertical", jaws),
                FindSegment("pocket_BL2_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "pocket_BM3",
                PocketKind.Side,
                new Vector2(0.0000029057f, -0.6650000f),
                0.0614f,
                FindSegment("pocket_BM3_jaw_left", jaws),
                FindSegment("pocket_BM3_jaw_right", jaws),
                dropRadiusMeters: 0.044f,
                maxEntrySpeedMetersPerSecond: 1.0f),
            CreatePocketFromJawStarts(
                "pocket_BR4",
                PocketKind.Corner,
                new Vector2(-1.2508408f, -0.6166018f),
                0.0584f,
                FindSegment("pocket_BR4_jaw_vertical", jaws),
                FindSegment("pocket_BR4_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "Pocket_TR5",
                PocketKind.Corner,
                new Vector2(-1.2508408f, 0.6166031f),
                0.0584f,
                FindSegment("Pocket_TR5_jaw_vertical", jaws),
                FindSegment("Pocket_TR5_jaw_horizontal", jaws),
                dropRadiusMeters: 0.0405f,
                maxEntrySpeedMetersPerSecond: 1.15f),
            CreatePocketFromJawStarts(
                "Pocket_TM6",
                PocketKind.Side,
                new Vector2(0.0000029057f, 0.6650000f),
                0.0614f,
                FindSegment("Pocket_TM6_jaw_left", jaws),
                FindSegment("Pocket_TM6_jaw_right", jaws),
                dropRadiusMeters: 0.044f,
                maxEntrySpeedMetersPerSecond: 1.0f)
        };

        return new TableSpec(
            name: "MASTERtable_9ft",
            sourceBlendPath: "/home/justin/Desktop/MASTERtable_9ft.blend",
            clothMin: clothMin,
            clothMax: clothMax,
            ballDiameterMeters: 0.05715f,
            cueBallSpawn: new Vector2(-0.733902f, 0.002383f),
            rackApexSpot: new Vector2(0.63499995f, 0.0f),
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
