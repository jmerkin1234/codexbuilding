using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class StandardEightBallRack
{
    private static readonly int[] RackOrder =
    [
        1,
        9, 2,
        3, 8, 10,
        11, 4, 12, 5,
        6, 13, 7, 14, 15
    ];

    public static BallState[] Create(TableSpec tableSpec, float rackGapMeters = 0.0005f)
    {
        if (rackGapMeters < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(rackGapMeters), "Rack gap cannot be negative.");
        }

        var balls = new List<BallState>(capacity: 16)
        {
            new(
                BallNumber: 0,
                Kind: BallKind.Cue,
                Position: tableSpec.CueBallSpawn,
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: false)
        };

        var centerSpacing = tableSpec.BallDiameterMeters + rackGapMeters;
        var rowAdvance = centerSpacing * 0.8660254f;
        var rackIndex = 0;

        for (var row = 0; row < 5; row++)
        {
            var rowX = tableSpec.RackApexSpot.X + (row * rowAdvance);
            var rowOffset = row * 0.5f;

            for (var slot = 0; slot <= row; slot++)
            {
                var ballNumber = RackOrder[rackIndex++];
                var rowY = tableSpec.RackApexSpot.Y + ((slot - rowOffset) * centerSpacing);

                balls.Add(new BallState(
                    BallNumber: ballNumber,
                    Kind: GetBallKind(ballNumber),
                    Position: new Vector2(rowX, rowY),
                    Velocity: Vector2.Zero,
                    Spin: new SpinState(0.0f, 0.0f, 0.0f),
                    IsPocketed: false));
            }
        }

        return balls.ToArray();
    }

    private static BallKind GetBallKind(int ballNumber)
    {
        if (ballNumber == 8)
        {
            return BallKind.Eight;
        }

        return ballNumber < 8 ? BallKind.Solid : BallKind.Stripe;
    }
}
