using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Simulation;

namespace CodexBuilding.Billiards.Core.Training;

public static class TrainingScenarioLibrary
{
    public static IReadOnlyList<TrainingScenarioDefinition> CreateDefaults(TableSpec tableSpec)
    {
        return new[]
        {
            CreateStraightStopScenario(tableSpec),
            CreateFollowThroughScenario(tableSpec),
            CreateDrawBackScenario(tableSpec),
            CreateCutToCornerScenario(tableSpec),
            CreateOneRailBankScenario(tableSpec),
            CreateEightBallFinishScenario(tableSpec)
        };
    }

    private static TrainingScenarioDefinition CreateStraightStopScenario(TableSpec tableSpec)
    {
        var cuePosition = ProjectPosition(tableSpec, -0.62f, 0.0f);
        var objectPosition = ProjectPosition(tableSpec, -0.08f, 0.0f);

        return CreateScenario(
            tableSpec,
            id: "straight_stop",
            displayName: "Straight Stop",
            description: "Cue and object ball on the center line for a plain stop-shot drill.",
            selectedBallNumber: 1,
            suggestedShot: CreateSuggestedShot(cuePosition, objectPosition, speedMetersPerSecond: 1.75f, new Vector2(0.0f, 0.0f)),
            new ScenarioBallPlacement(0, cuePosition),
            new ScenarioBallPlacement(1, objectPosition));
    }

    private static TrainingScenarioDefinition CreateFollowThroughScenario(TableSpec tableSpec)
    {
        var cuePosition = ProjectPosition(tableSpec, -0.58f, -0.2f);
        var objectPosition = ProjectPosition(tableSpec, -0.04f, -0.2f);

        return CreateScenario(
            tableSpec,
            id: "follow_through",
            displayName: "Follow Through",
            description: "A straight drill seeded with topspin so the cue ball keeps running after contact.",
            selectedBallNumber: 2,
            suggestedShot: CreateSuggestedShot(cuePosition, objectPosition, speedMetersPerSecond: 1.95f, new Vector2(0.0f, 0.56f)),
            new ScenarioBallPlacement(0, cuePosition),
            new ScenarioBallPlacement(2, objectPosition));
    }

    private static TrainingScenarioDefinition CreateDrawBackScenario(TableSpec tableSpec)
    {
        var cuePosition = ProjectPosition(tableSpec, -0.58f, 0.2f);
        var objectPosition = ProjectPosition(tableSpec, -0.02f, 0.2f);

        return CreateScenario(
            tableSpec,
            id: "draw_back",
            displayName: "Draw Back",
            description: "A straight drill seeded with backspin so the cue ball can pull off the object ball.",
            selectedBallNumber: 3,
            suggestedShot: CreateSuggestedShot(cuePosition, objectPosition, speedMetersPerSecond: 2.05f, new Vector2(0.0f, -0.6f)),
            new ScenarioBallPlacement(0, cuePosition),
            new ScenarioBallPlacement(3, objectPosition));
    }

    private static TrainingScenarioDefinition CreateCutToCornerScenario(TableSpec tableSpec)
    {
        var cuePosition = ProjectPosition(tableSpec, -0.34f, -0.24f);
        var objectPosition = ProjectPosition(tableSpec, 0.16f, -0.02f);

        return CreateScenario(
            tableSpec,
            id: "cut_to_corner",
            displayName: "Cut To Corner",
            description: "Cue ball starts off-line for a medium cut-angle drill toward a corner pocket lane.",
            selectedBallNumber: 4,
            suggestedShot: CreateSuggestedShot(cuePosition, objectPosition, speedMetersPerSecond: 1.85f, new Vector2(0.08f, 0.0f)),
            new ScenarioBallPlacement(0, cuePosition),
            new ScenarioBallPlacement(4, objectPosition));
    }

    private static TrainingScenarioDefinition CreateOneRailBankScenario(TableSpec tableSpec)
    {
        var cuePosition = ProjectPosition(tableSpec, -0.3f, 0.14f);
        var objectPosition = ProjectPosition(tableSpec, 0.42f, 0.62f);

        return CreateScenario(
            tableSpec,
            id: "one_rail_bank",
            displayName: "One-Rail Bank",
            description: "Object ball starts near the upper rail for a one-cushion bank-shot setup.",
            selectedBallNumber: 12,
            suggestedShot: CreateSuggestedShot(cuePosition, objectPosition, speedMetersPerSecond: 2.2f, new Vector2(0.03f, 0.12f)),
            new ScenarioBallPlacement(0, cuePosition),
            new ScenarioBallPlacement(12, objectPosition));
    }

    private static TrainingScenarioDefinition CreateEightBallFinishScenario(TableSpec tableSpec)
    {
        var cuePosition = ProjectPosition(tableSpec, -0.24f, 0.1f);
        var eightBallPosition = ProjectPosition(tableSpec, 0.3f, 0.18f);

        return CreateScenario(
            tableSpec,
            id: "eight_ball_finish",
            displayName: "Eight Ball Finish",
            description: "A short endgame layout with only the cue ball and 8-ball left on the table.",
            selectedBallNumber: 8,
            suggestedShot: CreateSuggestedShot(cuePosition, eightBallPosition, speedMetersPerSecond: 1.65f, new Vector2(0.0f, 0.0f)),
            new ScenarioBallPlacement(0, cuePosition),
            new ScenarioBallPlacement(8, eightBallPosition));
    }

    private static TrainingScenarioDefinition CreateScenario(
        TableSpec tableSpec,
        string id,
        string displayName,
        string description,
        int selectedBallNumber,
        ShotInput suggestedShot,
        params ScenarioBallPlacement[] visibleBalls)
    {
        return new TrainingScenarioDefinition(
            id,
            displayName,
            description,
            selectedBallNumber,
            suggestedShot,
            BuildLayout(tableSpec, visibleBalls));
    }

    private static BallState[] BuildLayout(TableSpec tableSpec, IReadOnlyList<ScenarioBallPlacement> visibleBalls)
    {
        var visibleLookup = visibleBalls.ToDictionary(ball => ball.BallNumber);
        var rackSpot = tableSpec.RackApexSpot;
        var balls = new BallState[16];

        for (var ballNumber = 0; ballNumber <= 15; ballNumber++)
        {
            if (visibleLookup.TryGetValue(ballNumber, out var visibleBall))
            {
                balls[ballNumber] = new BallState(
                    BallNumber: ballNumber,
                    Kind: ResolveBallKind(ballNumber),
                    Position: visibleBall.Position,
                    Velocity: Vector2.Zero,
                    Spin: new SpinState(0.0f, 0.0f, 0.0f),
                    IsPocketed: false);
                continue;
            }

            balls[ballNumber] = new BallState(
                BallNumber: ballNumber,
                Kind: ResolveBallKind(ballNumber),
                Position: rackSpot,
                Velocity: Vector2.Zero,
                Spin: new SpinState(0.0f, 0.0f, 0.0f),
                IsPocketed: true);
        }

        return balls;
    }

    private static BallKind ResolveBallKind(int ballNumber)
    {
        return ballNumber switch
        {
            0 => BallKind.Cue,
            8 => BallKind.Eight,
            <= 7 => BallKind.Solid,
            _ => BallKind.Stripe
        };
    }

    private static ShotInput CreateSuggestedShot(
        Vector2 cuePosition,
        Vector2 objectPosition,
        float speedMetersPerSecond,
        Vector2 tipOffsetNormalized)
    {
        return new ShotInput(
            AimDirection: Vector2.Normalize(objectPosition - cuePosition),
            StrikeSpeedMetersPerSecond: speedMetersPerSecond,
            TipOffsetNormalized: tipOffsetNormalized);
    }

    private static Vector2 ProjectPosition(TableSpec tableSpec, float normalizedX, float normalizedY)
    {
        var ballRadius = tableSpec.BallDiameterMeters * 0.5f;
        var margin = ballRadius * 2.2f;
        var min = tableSpec.ClothMin + new Vector2(margin, margin);
        var max = tableSpec.ClothMax - new Vector2(margin, margin);

        var x = min.X + (((normalizedX + 1.0f) * 0.5f) * (max.X - min.X));
        var y = min.Y + (((normalizedY + 1.0f) * 0.5f) * (max.Y - min.Y));
        return new Vector2(x, y);
    }

    private readonly struct ScenarioBallPlacement
    {
        public ScenarioBallPlacement(int ballNumber, Vector2 position)
        {
            BallNumber = ballNumber;
            Position = position;
        }

        public int BallNumber { get; }

        public Vector2 Position { get; }
    }
}
