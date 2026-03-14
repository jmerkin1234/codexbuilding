namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationConfig
{
    public static SimulationConfig Default { get; } = new(
        fixedStepSeconds: 1.0f / 240.0f,
        settleSpeedThresholdMetersPerSecond: 0.01f,
        maxFixedStepsPerAdvance: 512,
        maxSideSpinRps: 12.0f,
        maxFollowSpinRps: 10.0f,
        maxDrawSpinRps: 11.0f,
        slidingFrictionAccelerationMetersPerSecondSquared: 1.8f,
        rollingFrictionAccelerationMetersPerSecondSquared: 0.22f,
        spinDecayRpsPerSecond: 1.2f,
        sideSpinCurveAccelerationMetersPerSecondSquaredPerRps: 0.015f,
        movingSideSpinDecayRpsPerSecondPerMetersPerSecond: 0.35f,
        rollingMatchToleranceMetersPerSecond: 0.01f,
        spinSettleThresholdRps: 0.05f,
        ballCollisionRestitution: 0.96f,
        ballCollisionTangentialTransferFactor: 0.35f,
        ballCollisionSpinTransferFactor: 0.12f,
        maxCollisionIterationsPerStep: 4,
        boundaryRestitution: 0.9f,
        boundaryTangentialFrictionFactor: 0.25f,
        boundarySpinTransferFactor: 0.45f,
        maxBoundaryIterationsPerStep: 4,
        boundaryGlancingRestitution: 0.78f,
        boundaryTangentialVelocityRetention: 0.97f);

    public SimulationConfig(
        float fixedStepSeconds,
        float settleSpeedThresholdMetersPerSecond,
        int maxFixedStepsPerAdvance,
        float maxSideSpinRps,
        float maxFollowSpinRps,
        float maxDrawSpinRps,
        float slidingFrictionAccelerationMetersPerSecondSquared = 1.8f,
        float rollingFrictionAccelerationMetersPerSecondSquared = 0.22f,
        float spinDecayRpsPerSecond = 1.2f,
        float sideSpinCurveAccelerationMetersPerSecondSquaredPerRps = 0.015f,
        float movingSideSpinDecayRpsPerSecondPerMetersPerSecond = 0.35f,
        float rollingMatchToleranceMetersPerSecond = 0.01f,
        float spinSettleThresholdRps = 0.05f,
        float ballCollisionRestitution = 0.96f,
        float ballCollisionTangentialTransferFactor = 0.35f,
        float ballCollisionSpinTransferFactor = 0.12f,
        int maxCollisionIterationsPerStep = 4,
        float boundaryRestitution = 0.9f,
        float boundaryTangentialFrictionFactor = 0.25f,
        float boundarySpinTransferFactor = 0.45f,
        int maxBoundaryIterationsPerStep = 4,
        float boundaryGlancingRestitution = 0.78f,
        float boundaryTangentialVelocityRetention = 0.97f)
    {
        if (fixedStepSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedStepSeconds), "Fixed step must be positive.");
        }

        if (settleSpeedThresholdMetersPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settleSpeedThresholdMetersPerSecond),
                "Settle threshold cannot be negative.");
        }

        if (maxFixedStepsPerAdvance <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFixedStepsPerAdvance),
                "Max fixed steps per advance must be positive.");
        }

        if (maxSideSpinRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSideSpinRps), "Max side spin cannot be negative.");
        }

        if (maxFollowSpinRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFollowSpinRps), "Max follow spin cannot be negative.");
        }

        if (maxDrawSpinRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDrawSpinRps), "Max draw spin cannot be negative.");
        }

        if (slidingFrictionAccelerationMetersPerSecondSquared < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slidingFrictionAccelerationMetersPerSecondSquared),
                "Sliding friction acceleration cannot be negative.");
        }

        if (rollingFrictionAccelerationMetersPerSecondSquared < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollingFrictionAccelerationMetersPerSecondSquared),
                "Rolling friction acceleration cannot be negative.");
        }

        if (spinDecayRpsPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(spinDecayRpsPerSecond), "Spin decay cannot be negative.");
        }

        if (sideSpinCurveAccelerationMetersPerSecondSquaredPerRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sideSpinCurveAccelerationMetersPerSecondSquaredPerRps),
                "Side-spin curve acceleration cannot be negative.");
        }

        if (movingSideSpinDecayRpsPerSecondPerMetersPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movingSideSpinDecayRpsPerSecondPerMetersPerSecond),
                "Moving side-spin decay cannot be negative.");
        }

        if (rollingMatchToleranceMetersPerSecond < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rollingMatchToleranceMetersPerSecond),
                "Rolling-match tolerance cannot be negative.");
        }

        if (spinSettleThresholdRps < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spinSettleThresholdRps),
                "Spin-settle threshold cannot be negative.");
        }

        if (ballCollisionRestitution < 0.0f || ballCollisionRestitution > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ballCollisionRestitution),
                "Ball collision restitution must be between zero and one.");
        }

        if (ballCollisionTangentialTransferFactor < 0.0f || ballCollisionTangentialTransferFactor > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ballCollisionTangentialTransferFactor),
                "Ball collision tangential transfer factor must be between zero and one.");
        }

        if (ballCollisionSpinTransferFactor < 0.0f || ballCollisionSpinTransferFactor > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ballCollisionSpinTransferFactor),
                "Ball collision spin transfer factor must be between zero and one.");
        }

        if (maxCollisionIterationsPerStep <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCollisionIterationsPerStep),
                "Max collision iterations per step must be positive.");
        }

        if (boundaryRestitution < 0.0f || boundaryRestitution > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boundaryRestitution),
                "Boundary restitution must be between zero and one.");
        }

        if (boundaryTangentialFrictionFactor < 0.0f || boundaryTangentialFrictionFactor > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boundaryTangentialFrictionFactor),
                "Boundary tangential friction factor must be between zero and one.");
        }

        if (boundarySpinTransferFactor < 0.0f || boundarySpinTransferFactor > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boundarySpinTransferFactor),
                "Boundary spin transfer factor must be between zero and one.");
        }

        if (boundaryGlancingRestitution < 0.0f || boundaryGlancingRestitution > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boundaryGlancingRestitution),
                "Boundary glancing restitution must be between zero and one.");
        }

        if (boundaryTangentialVelocityRetention < 0.0f || boundaryTangentialVelocityRetention > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boundaryTangentialVelocityRetention),
                "Boundary tangential velocity retention must be between zero and one.");
        }

        if (maxBoundaryIterationsPerStep <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxBoundaryIterationsPerStep),
                "Max boundary iterations per step must be positive.");
        }

        FixedStepSeconds = fixedStepSeconds;
        SettleSpeedThresholdMetersPerSecond = settleSpeedThresholdMetersPerSecond;
        MaxFixedStepsPerAdvance = maxFixedStepsPerAdvance;
        MaxSideSpinRps = maxSideSpinRps;
        MaxFollowSpinRps = maxFollowSpinRps;
        MaxDrawSpinRps = maxDrawSpinRps;
        SlidingFrictionAccelerationMetersPerSecondSquared = slidingFrictionAccelerationMetersPerSecondSquared;
        RollingFrictionAccelerationMetersPerSecondSquared = rollingFrictionAccelerationMetersPerSecondSquared;
        SpinDecayRpsPerSecond = spinDecayRpsPerSecond;
        SideSpinCurveAccelerationMetersPerSecondSquaredPerRps = sideSpinCurveAccelerationMetersPerSecondSquaredPerRps;
        MovingSideSpinDecayRpsPerSecondPerMetersPerSecond = movingSideSpinDecayRpsPerSecondPerMetersPerSecond;
        RollingMatchToleranceMetersPerSecond = rollingMatchToleranceMetersPerSecond;
        SpinSettleThresholdRps = spinSettleThresholdRps;
        BallCollisionRestitution = ballCollisionRestitution;
        BallCollisionTangentialTransferFactor = ballCollisionTangentialTransferFactor;
        BallCollisionSpinTransferFactor = ballCollisionSpinTransferFactor;
        MaxCollisionIterationsPerStep = maxCollisionIterationsPerStep;
        BoundaryRestitution = boundaryRestitution;
        BoundaryTangentialFrictionFactor = boundaryTangentialFrictionFactor;
        BoundarySpinTransferFactor = boundarySpinTransferFactor;
        MaxBoundaryIterationsPerStep = maxBoundaryIterationsPerStep;
        BoundaryGlancingRestitution = boundaryGlancingRestitution;
        BoundaryTangentialVelocityRetention = boundaryTangentialVelocityRetention;
    }

    public float FixedStepSeconds { get; }

    public float SettleSpeedThresholdMetersPerSecond { get; }

    public int MaxFixedStepsPerAdvance { get; }

    public float MaxSideSpinRps { get; }

    public float MaxFollowSpinRps { get; }

    public float MaxDrawSpinRps { get; }

    public float SlidingFrictionAccelerationMetersPerSecondSquared { get; }

    public float RollingFrictionAccelerationMetersPerSecondSquared { get; }

    public float SpinDecayRpsPerSecond { get; }

    public float SideSpinCurveAccelerationMetersPerSecondSquaredPerRps { get; }

    public float MovingSideSpinDecayRpsPerSecondPerMetersPerSecond { get; }

    public float RollingMatchToleranceMetersPerSecond { get; }

    public float SpinSettleThresholdRps { get; }

    public float BallCollisionRestitution { get; }

    public float BallCollisionTangentialTransferFactor { get; }

    public float BallCollisionSpinTransferFactor { get; }

    public int MaxCollisionIterationsPerStep { get; }

    public float BoundaryRestitution { get; }

    public float BoundaryTangentialFrictionFactor { get; }

    public float BoundarySpinTransferFactor { get; }

    public int MaxBoundaryIterationsPerStep { get; }

    public float BoundaryGlancingRestitution { get; }

    public float BoundaryTangentialVelocityRetention { get; }
}
