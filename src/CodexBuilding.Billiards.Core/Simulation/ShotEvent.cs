namespace CodexBuilding.Billiards.Core.Simulation;

public sealed class ShotEvent
{
    public ShotEvent(ShotEventType eventType, int? ballNumber, string detail)
    {
        EventType = eventType;
        BallNumber = ballNumber;
        Detail = detail;
    }

    public ShotEventType EventType { get; }

    public int? BallNumber { get; }

    public string Detail { get; }
}
