using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;
using CodexBuilding.Billiards.Core.Training;

namespace CodexBuilding.Billiards.Tests;

public sealed class TrainingScenarioLibraryTests
{
    [Fact]
    public void CreateDefaults_ReturnsSixDistinctScenarios()
    {
        var table = CustomTable9FtSpec.Create();

        var scenarios = TrainingScenarioLibrary.CreateDefaults(table);

        Assert.Equal(6, scenarios.Count);
        Assert.Equal(6, scenarios.Select(scenario => scenario.Id).Distinct().Count());
        Assert.All(scenarios, scenario => Assert.False(string.IsNullOrWhiteSpace(scenario.DisplayName)));
    }

    [Fact]
    public void CreateDefaults_UsesLegalVisibleBallPlacements()
    {
        var table = CustomTable9FtSpec.Create();
        var ballRadius = table.BallDiameterMeters * 0.5f;
        var min = table.ClothMin + new Vector2(ballRadius, ballRadius);
        var max = table.ClothMax - new Vector2(ballRadius, ballRadius);

        var scenarios = TrainingScenarioLibrary.CreateDefaults(table);

        foreach (var scenario in scenarios)
        {
            var visibleBalls = scenario.Balls.Where(ball => !ball.IsPocketed).ToArray();
            Assert.True(visibleBalls.Length >= 2, $"Scenario {scenario.Id} does not expose enough visible balls.");
            Assert.Contains(visibleBalls, ball => ball.BallNumber == 0);
            Assert.Contains(visibleBalls, ball => ball.BallNumber == scenario.SelectedBallNumber);

            foreach (var ball in visibleBalls)
            {
                Assert.InRange(ball.Position.X, min.X, max.X);
                Assert.InRange(ball.Position.Y, min.Y, max.Y);
            }

            for (var firstIndex = 0; firstIndex < visibleBalls.Length; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < visibleBalls.Length; secondIndex++)
                {
                    var distance = Vector2.Distance(visibleBalls[firstIndex].Position, visibleBalls[secondIndex].Position);
                    Assert.True(
                        distance >= table.BallDiameterMeters,
                        $"Scenario {scenario.Id} overlaps balls {visibleBalls[firstIndex].BallNumber} and {visibleBalls[secondIndex].BallNumber}.");
                }
            }
        }
    }

    [Fact]
    public void CreateDefaults_ProvidesNormalizedSuggestedShots()
    {
        var table = CustomTable9FtSpec.Create();

        var scenarios = TrainingScenarioLibrary.CreateDefaults(table);

        foreach (var scenario in scenarios)
        {
            Assert.InRange(scenario.SuggestedShot.AimDirection.Length(), 0.999f, 1.001f);
            Assert.InRange(scenario.SuggestedShot.StrikeSpeedMetersPerSecond, 0.3f, 4.0f);
            Assert.True(
                scenario.SuggestedShot.TipOffsetNormalized.Length() <= 1.0f,
                $"Scenario {scenario.Id} has an invalid tip offset.");
        }
    }
}
