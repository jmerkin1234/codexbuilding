namespace CodexBuilding.Billiards.Core.Simulation;

public readonly record struct SpinState(
    float SideSpinRps,
    float ForwardSpinRps,
    float VerticalSpinRps);
