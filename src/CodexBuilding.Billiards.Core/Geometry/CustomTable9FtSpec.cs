using System.Numerics;

namespace CodexBuilding.Billiards.Core.Geometry;

public static class CustomTable9FtSpec
{
    public static TableSpec Create()
    {
        var cushions = new[]
        {
            new CushionSegment(
                "rail_head",
                new Vector2(-1.2249266f, -0.56977034f),
                new Vector2(-1.2249266f, 0.57038474f)),
            new CushionSegment(
                "rail_foot",
                new Vector2(1.2258036f, -0.5681504f),
                new Vector2(1.2258036f, 0.5702276f)),
            new CushionSegment(
                "rail_upper_left",
                new Vector2(-1.2068906f, -0.59292513f),
                new Vector2(-0.050392687f, -0.59292513f)),
            new CushionSegment(
                "rail_upper_right",
                new Vector2(0.050399005f, -0.59292495f),
                new Vector2(1.2069001f, -0.59292495f)),
            new CushionSegment(
                "rail_bottom_left",
                new Vector2(-1.2068906f, 0.5953581f),
                new Vector2(-0.050392687f, 0.5953581f)),
            new CushionSegment(
                "rail_bottom_right",
                new Vector2(0.050398767f, 0.5953247f),
                new Vector2(1.2068999f, 0.5953247f))
        };

        var pockets = new[]
        {
            new PocketSpec("pockett_TL1", PocketKind.Corner, new Vector2(1.2640905f, 0.49089444f), 0.0584f),
            new PocketSpec("pocket_BL2", PocketKind.Corner, new Vector2(1.2508553f, -0.6167401f), 0.0584f),
            new PocketSpec("pocket_BM3", PocketKind.Side, new Vector2(0.0000029f, -0.66602194f), 0.0614f),
            new PocketSpec("pocket_BR4", PocketKind.Corner, new Vector2(-1.2508456f, -0.6167401f), 0.0584f),
            new PocketSpec("Pocket_TR5", PocketKind.Corner, new Vector2(-1.2508456f, 0.61673915f), 0.0584f),
            new PocketSpec("Pocket_TM6", PocketKind.Side, new Vector2(0.0000029f, 0.66556805f), 0.0614f)
        };

        return new TableSpec(
            name: "customtable_9ft",
            sourceBlendPath: "/home/justin/Desktop/customtable_9ft.blend",
            clothMin: new Vector2(-1.2699999f, -0.63499993f),
            clothMax: new Vector2(1.2699999f, 0.63499993f),
            ballDiameterMeters: 0.05715f,
            cueBallSpawn: new Vector2(-0.733902f, 0.002383f),
            rackApexSpot: new Vector2(0.616941f, 0.0f),
            cushions: cushions,
            pockets: pockets);
    }
}
