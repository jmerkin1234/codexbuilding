using System.Numerics;
using CodexBuilding.Billiards.Core.Geometry;

namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class SimulationWorld
{
    private readonly List<BallState> _balls;
    private readonly List<ShotEvent> _eventLog = new();
    private readonly List<ShotEvent> _pendingEvents = new();

    public SimulationWorld(TableSpec tableSpec, SimulationConfig config, IEnumerable<BallState> initialBalls)
    {
        TableSpec = tableSpec;
        Config = config;
        _balls = new List<BallState>(initialBalls);
        Phase = HasActiveMotion() ? SimulationPhase.Running : SimulationPhase.Idle;
    }

    public TableSpec TableSpec { get; }

    public SimulationConfig Config { get; }

    public SimulationPhase Phase { get; private set; }

    public float SimulationTimeSeconds { get; private set; }

    public float AccumulatorSeconds { get; private set; }

    public int TotalFixedStepsExecuted { get; private set; }

    public IReadOnlyList<BallState> Balls => _balls;

    public IReadOnlyList<ShotEvent> Events => _eventLog;

    public void Reset(IEnumerable<BallState> balls)
    {
        _balls.Clear();
        _balls.AddRange(balls);
        _eventLog.Clear();
        _pendingEvents.Clear();
        Phase = HasActiveMotion() ? SimulationPhase.Running : SimulationPhase.Idle;
        SimulationTimeSeconds = 0.0f;
        AccumulatorSeconds = 0.0f;
        TotalFixedStepsExecuted = 0;
    }

    public ResolvedCueStrike ApplyCueStrike(ShotInput shotInput)
    {
        if (Phase == SimulationPhase.Running && HasActiveMotion())
        {
            throw new InvalidOperationException("Cannot apply a cue strike while the simulation is already running.");
        }

        var cueBallIndex = FindCueBallIndex();
        if (cueBallIndex < 0)
        {
            throw new InvalidOperationException("A cue strike requires an active cue ball.");
        }

        var resolvedCueStrike = CueStrikeResolver.Resolve(shotInput, Config);
        var cueBall = _balls[cueBallIndex];
        _balls[cueBallIndex] = cueBall with
        {
            Velocity = resolvedCueStrike.InitialVelocity,
            Spin = resolvedCueStrike.InitialSpin
        };

        AccumulatorSeconds = 0.0f;
        PublishEvent(new ShotEvent(
            ShotEventType.CueStrike,
            ballNumber: cueBall.BallNumber,
            detail:
            $"aim=({resolvedCueStrike.AimDirection.X:0.###},{resolvedCueStrike.AimDirection.Y:0.###}) " +
            $"speed={resolvedCueStrike.StrikeSpeedMetersPerSecond:0.###} " +
            $"tip=({resolvedCueStrike.TipOffsetNormalized.X:0.###},{resolvedCueStrike.TipOffsetNormalized.Y:0.###}) " +
            $"spin=({resolvedCueStrike.InitialSpin.SideSpinRps:0.###},{resolvedCueStrike.InitialSpin.ForwardSpinRps:0.###},{resolvedCueStrike.InitialSpin.VerticalSpinRps:0.###})"));
        Phase = SimulationPhase.Running;

        return resolvedCueStrike;
    }

    public ShotResult Advance(float deltaTimeSeconds)
    {
        if (deltaTimeSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds), "Delta time cannot be negative.");
        }

        var eventsThisAdvance = new List<ShotEvent>();
        FlushPendingEvents(eventsThisAdvance);

        if (HasActiveMotion() && Phase != SimulationPhase.Running)
        {
            Phase = SimulationPhase.Running;
        }

        if (Phase != SimulationPhase.Running)
        {
            return CreateResult(eventsThisAdvance, fixedStepsExecuted: 0);
        }

        AccumulatorSeconds += deltaTimeSeconds;

        var fixedStepsExecuted = 0;
        while (AccumulatorSeconds >= Config.FixedStepSeconds && fixedStepsExecuted < Config.MaxFixedStepsPerAdvance)
        {
            ExecuteFixedStep(Config.FixedStepSeconds);
            AccumulatorSeconds -= Config.FixedStepSeconds;
            fixedStepsExecuted++;
            TotalFixedStepsExecuted++;
        }

        if (AccumulatorSeconds < 0.0f)
        {
            AccumulatorSeconds = 0.0f;
        }

        if (fixedStepsExecuted > 0 && !HasActiveMotion())
        {
            Phase = SimulationPhase.Settled;
            PublishEvent(new ShotEvent(
                ShotEventType.Settled,
                ballNumber: null,
                detail: $"time={SimulationTimeSeconds:0.###}"));
        }

        FlushPendingEvents(eventsThisAdvance);

        return CreateResult(eventsThisAdvance, fixedStepsExecuted);
    }

    public ShotResult Step(float deltaTimeSeconds)
    {
        return Advance(deltaTimeSeconds);
    }

    private int FindCueBallIndex()
    {
        for (var index = 0; index < _balls.Count; index++)
        {
            var ball = _balls[index];
            if (ball.Kind == BallKind.Cue && !ball.IsPocketed)
            {
                return index;
            }
        }

        return -1;
    }

    private ShotResult CreateResult(IReadOnlyList<ShotEvent> eventsThisAdvance, int fixedStepsExecuted)
    {
        return new ShotResult(
            balls: _balls.ToArray(),
            events: eventsThisAdvance,
            phase: Phase,
            fixedStepsExecuted: fixedStepsExecuted,
            simulationTimeSeconds: SimulationTimeSeconds,
            accumulatorSeconds: AccumulatorSeconds);
    }

    private void ExecuteFixedStep(float fixedStepSeconds)
    {
        var settleThresholdSquared = Config.SettleSpeedThresholdMetersPerSecond * Config.SettleSpeedThresholdMetersPerSecond;

        for (var index = 0; index < _balls.Count; index++)
        {
            var ball = _balls[index];
            if (ball.IsPocketed)
            {
                continue;
            }

            var velocity = ball.Velocity;
            if (velocity.LengthSquared() <= settleThresholdSquared)
            {
                velocity = Vector2.Zero;
            }

            var position = ball.Position + (velocity * fixedStepSeconds);
            _balls[index] = ball with
            {
                Position = position,
                Velocity = velocity
            };
        }

        SimulationTimeSeconds += fixedStepSeconds;
    }

    private bool HasActiveMotion()
    {
        var settleThresholdSquared = Config.SettleSpeedThresholdMetersPerSecond * Config.SettleSpeedThresholdMetersPerSecond;

        foreach (var ball in _balls)
        {
            if (ball.IsPocketed)
            {
                continue;
            }

            if (ball.Velocity.LengthSquared() > settleThresholdSquared)
            {
                return true;
            }
        }

        return false;
    }

    private void PublishEvent(ShotEvent shotEvent)
    {
        _eventLog.Add(shotEvent);
        _pendingEvents.Add(shotEvent);
    }

    private void FlushPendingEvents(List<ShotEvent> destination)
    {
        if (_pendingEvents.Count == 0)
        {
            return;
        }

        destination.AddRange(_pendingEvents);
        _pendingEvents.Clear();
    }
}
