namespace SkyTeam.Domain;

sealed class Airport
{
    private readonly PathSegment[] _segments;
    private int _currentPositionIndex;

    internal double BlueAerodynamicsThreshold { get; private set; } = 4.5;
    internal double OrangeAerodynamicsThreshold { get; private set; } = 8.5;

    public Airport(IEnumerable<PathSegment> pathSegments)
    {
        ArgumentNullException.ThrowIfNull(pathSegments);

        _segments = pathSegments.ToArray();
    }

    // Compatibility surface for existing tests.
    internal IReadOnlyList<PathSegment> PathSegments => _segments;

    internal int CurrentPositionIndex => _currentPositionIndex;
    internal int SegmentCount => _segments.Length;
    internal PathSegment CurrentSegment => _segments[_currentPositionIndex];

    internal void AdvanceApproach(int steps)
    {
        if (steps < 0)
            throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be non-negative.");

        if (steps == 0) return;

        if (_segments.Length == 0)
            throw new InvalidOperationException("Cannot advance approach when no path segments exist.");

        if (CurrentSegment.PlaneTokens > 0)
            throw new InvalidOperationException("Cannot advance approach with airplanes at current position.");

        if (_currentPositionIndex + steps >= _segments.Length)
            throw new InvalidOperationException("Approach overshoot.");

        _currentPositionIndex += steps;
    }

    internal bool TryRemovePlaneTokenAtOffset(int offsetFromCurrent)
    {
        if (offsetFromCurrent < 0)
            throw new ArgumentOutOfRangeException(nameof(offsetFromCurrent));

        var index = _currentPositionIndex + offsetFromCurrent;
        if (index >= _segments.Length) return false;

        return _segments[index].TryRemovePlaneToken();
    }

    internal void MoveBlueAerodynamicsRight() => BlueAerodynamicsThreshold += 1;
    internal void MoveOrangeAerodynamicsRight() => OrangeAerodynamicsThreshold += 1;
}

sealed class MontrealAirport
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

sealed class PathSegment
{
    internal int PlaneTokens { get; private set; }

    // Compatibility surface for existing tests.
    internal int NumberOfPlanes => PlaneTokens;

    public PathSegment(int planeTokens)
    {
        if (planeTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(planeTokens), "Plane token count must be non-negative.");

        PlaneTokens = planeTokens;
    }

    internal bool TryRemovePlaneToken()
    {
        if (PlaneTokens == 0) return false;

        PlaneTokens--;
        return true;
    }
}