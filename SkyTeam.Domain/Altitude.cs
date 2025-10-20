namespace SkyTeam.Domain;

class Altitude
{
    private readonly Queue<AltitudeSegment> _altitudeSegments = new(
    [
        new(true), // 6000
        new(), // 5000
        new(), // 4000
        new(), // 3000
        new(true), // 2000
        new(), // 1000
        new() // 0 Landed
    ]);

    public AltitudeSegment CurrentAltitude => _altitudeSegments.Peek();

    public Player CurrentPlayer { get; private set; } = Player.Pilot;

    public void Advance()
    {
        _altitudeSegments.Dequeue();

        CurrentPlayer = CurrentPlayer == Player.Pilot ? Player.Copilot : Player.Pilot;
    }
}

class AltitudeSegment(bool hasReroll = false)
{
    public bool HasReroll { get; } = hasReroll;
}

enum Player
{
    Pilot,
    Copilot
}