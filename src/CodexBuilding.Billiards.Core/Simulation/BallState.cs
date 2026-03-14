using System.Numerics;

namespace CodexBuilding.Billiards.Core.Simulation;

public readonly record struct BallState(
    int BallNumber,
    BallKind Kind,
    Vector2 Position,
    Vector2 Velocity,
    SpinState Spin,
    bool IsPocketed);
