using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Tests;

public sealed class TableSpecTests
{
    [Fact]
    public void CustomTable9FtSpec_UsesSixRailSegments()
    {
        var table = CustomTable9FtSpec.Create();

        Assert.Equal(6, table.Cushions.Count);
    }

    [Fact]
    public void CustomTable9FtSpec_UsesSixPockets()
    {
        var table = CustomTable9FtSpec.Create();

        Assert.Equal(6, table.Pockets.Count);
    }

    [Fact]
    public void CustomTable9FtSpec_UsesTwelveJawSegments()
    {
        var table = CustomTable9FtSpec.Create();

        Assert.Equal(12, table.JawSegments.Count);
    }

    [Fact]
    public void CustomTable9FtSpec_UsesStandardPoolBallDiameter()
    {
        var table = CustomTable9FtSpec.Create();

        Assert.Equal(0.05715f, table.BallDiameterMeters, precision: 5);
    }
}
