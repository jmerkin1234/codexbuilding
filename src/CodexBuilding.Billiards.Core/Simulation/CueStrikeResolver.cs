using System.Numerics;

namespace CodexBuilding.Billiards.Core.Simulation;

public static class CueStrikeResolver
{
    public static ResolvedCueStrike Resolve(ShotInput shotInput, SimulationConfig config)
    {
        if (shotInput.AimDirection.LengthSquared() <= float.Epsilon)
        {
            throw new ArgumentException("Aim direction must be non-zero.", nameof(shotInput));
        }

        if (shotInput.StrikeSpeedMetersPerSecond <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shotInput),
                "Strike speed must be greater than zero.");
        }

        var aimDirection = Vector2.Normalize(shotInput.AimDirection);
        var tipOffset = ClampToUnitCircle(shotInput.TipOffsetNormalized);
        var initialVelocity = aimDirection * shotInput.StrikeSpeedMetersPerSecond;

        var initialSpin = new SpinState(
            SideSpinRps: tipOffset.X * config.MaxSideSpinRps,
            ForwardSpinRps: tipOffset.Y >= 0.0f
                ? tipOffset.Y * config.MaxFollowSpinRps
                : tipOffset.Y * config.MaxDrawSpinRps,
            VerticalSpinRps: 0.0f);

        return new ResolvedCueStrike(
            AimDirection: aimDirection,
            StrikeSpeedMetersPerSecond: shotInput.StrikeSpeedMetersPerSecond,
            TipOffsetNormalized: tipOffset,
            InitialVelocity: initialVelocity,
            InitialSpin: initialSpin);
    }

    private static Vector2 ClampToUnitCircle(Vector2 tipOffset)
    {
        var lengthSquared = tipOffset.LengthSquared();
        if (lengthSquared <= 1.0f || lengthSquared <= float.Epsilon)
        {
            return tipOffset;
        }

        return Vector2.Normalize(tipOffset);
    }
}
