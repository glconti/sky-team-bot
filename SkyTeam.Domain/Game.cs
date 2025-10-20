namespace SkyTeam.Domain;

class Game
{
    private readonly GameModule[] _modules;

    public Game(Airport airport, Altitude altitude, IEnumerable<GameModule> modules) =>
        _modules = modules.ToArray();

    public Game New() =>
        new(
            new MontrealAirport(),
            new(),
            [
                new AxisPositionModule()
            ]);
}