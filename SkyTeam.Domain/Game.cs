namespace SkyTeam.Domain;

class Game
{
    private readonly Airport _airport;

    private readonly Dictionary<string, GameCommand> _allCommands = new()
    {
        { NextRoundCommand.Instance.CommandId, NextRoundCommand.Instance }
    };

    private readonly Altitude _altitude;
    private readonly GameModule[] _modules;

    private readonly List<BlueDie> _unusedBlueDice = [];
    private readonly List<OrangeDie> _unusedOrangeDice = [];

    private Player _currentPlayer;

    internal Player CurrentPlayer => _currentPlayer;

    internal IReadOnlyList<BlueDie> UnusedBlueDice => _unusedBlueDice.AsReadOnly();
    internal IReadOnlyList<OrangeDie> UnusedOrangeDice => _unusedOrangeDice.AsReadOnly();

    public Game(Airport airport, Altitude altitude, GameModule[] modules)
    {
        ArgumentNullException.ThrowIfNull(airport);
        ArgumentNullException.ThrowIfNull(altitude);
        ArgumentNullException.ThrowIfNull(modules);

        _airport = airport;
        _altitude = altitude;
        _modules = modules.ToArray();
        _currentPlayer = altitude.CurrentPlayer;
        RollDice();
    }

    public void NextRound()
    {
        if (_unusedBlueDice.Count > 0 || _unusedOrangeDice.Count > 0)
            throw new InvalidOperationException("Cannot proceed to next round with unused dice.");

        _altitude.Advance();
        _currentPlayer = _altitude.CurrentPlayer;
        RollDice();
    }

    public void SwitchPlayer() =>
        _currentPlayer = _currentPlayer == Player.Pilot ? Player.Copilot : Player.Pilot;

    public Game New() =>
        new(
            new MontrealAirport(),
            new(),
            [
                new AxisPositionModule()
            ]);

    /// <summary>
    ///     Gets all available commands for the current player based on game state.
    /// </summary>
    public IEnumerable<GameCommand> GetAvailableCommands()
    {
        if (_unusedBlueDice.Count == 0 && _unusedOrangeDice.Count == 0)
        {
            yield return _allCommands[NextRoundCommand.Instance.CommandId];
            yield break;
        }

        foreach (var gameCommand in _modules.SelectMany(module =>
                     module.GetAvailableCommands(_currentPlayer)))
            yield return gameCommand;
    }

    /// <summary>
    ///     Executes a command by placing a die on the specified module.
    /// </summary>
    public void ExecuteCommand(string command)
    {
    }

    /// <summary>
    ///     Rolls dice for the current round and adds them to the game state.
    /// </summary>
    private void RollDice()
    {
        _unusedBlueDice.Clear();
        _unusedOrangeDice.Clear();

        for (var i = 0; i < 4; i++)
        {
            _unusedBlueDice.Add(BlueDie.Roll());
            _unusedOrangeDice.Add(OrangeDie.Roll());
        }
    }
}

record NextRoundCommand : GameCommand
{
    public override string CommandId => "NextRound";
    public override string DisplayName => "Proceed to Next Round";
    public static GameCommand Instance { get; set; } = new NextRoundCommand();

    public static void Execute(Game game) => game.NextRound();
}