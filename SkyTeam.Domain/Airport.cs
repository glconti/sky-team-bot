namespace SkyTeam.Domain;

abstract class Airport
{
    protected List<PathSegment> _pathSegments = new();
}

class MontrealAirport : Airport
{
    public MontrealAirport()
    {
        _pathSegments.Add(new(0));
        _pathSegments.Add(new(0));
        _pathSegments.Add(new(1));
        _pathSegments.Add(new(0));
        _pathSegments.Add(new(3));
    }
}

class PathSegment(int NumberOfPlanes)
{
}