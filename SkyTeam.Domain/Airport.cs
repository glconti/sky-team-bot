namespace SkyTeam.Domain;

class Airport(IEnumerable<PathSegment> pathSegments)
{
    public Queue<PathSegment> PathSegments { get; } = new(pathSegments);
}

class MontrealAirport
{
    private readonly Airport _airport = new([
        new(0),
        new(0),
        new(1),
        new(2),
        new(1),
        new(3),
        new(2) // Landed
    ]);

    public static implicit operator Airport(MontrealAirport airport) => airport._airport;
}

record PathSegment(int NumberOfPlanes);