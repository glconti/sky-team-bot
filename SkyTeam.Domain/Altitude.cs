namespace SkyTeam.Domain;

class Altitude
{
    private readonly Queue<AltituteSegment> _altituteSegments = new(
    [
        new(), // 6000
        new(), // 5000
        new(), // 4000
        new(), // 3000
        new(), // 2000
        new(), // 1000
        new() // 0 Landed
    ]);

    public int CurrentAltitude => _altituteSegments.Count;

    public void Advance() => _altituteSegments.Dequeue();
}

class AltituteSegment
{
}