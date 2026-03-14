using System.Numerics;

namespace CodexBuilding.Billiards.Core.Simulation;

public readonly record struct ResolvedCueStrike(
    Vector2 AimDirection,
    float StrikeSpeedMetersPerSecond,
    Vector2 TipOffsetNormalized,
    Vector2 InitialVelocity,
    SpinState InitialSpin);
