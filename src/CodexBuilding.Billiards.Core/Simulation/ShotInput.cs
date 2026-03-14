using System.Numerics;

namespace CodexBuilding.Billiards.Core.Simulation;

public readonly record struct ShotInput(
    Vector2 AimDirection,
    float StrikeSpeedMetersPerSecond,
    Vector2 TipOffsetNormalized);
