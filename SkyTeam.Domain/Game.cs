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
    private readonly GameState _state = new();

    internal Player CurrentPlayer => _state.CurrentPlayer;

    internal IReadOnlyList<BlueDie> UnusedBlueDice => _state.UnusedBlueDice;
    internal IReadOnlyList<OrangeDie> UnusedOrangeDice => _state.UnusedOrangeDice;

    public Game(Airport airport, Altitude altitude, GameModule[] modules)
    {
        ArgumentNullException.ThrowIfNull(airport);
        ArgumentNullException.ThrowIfNull(altitude);
        ArgumentNullException.ThrowIfNull(modules);

        _airport = airport;
        _altitude = altitude;
        _modules = modules.ToArray();
        _state.SetCurrentPlayer(altitude.CurrentPlayer);
        RollDice();
    }

    public void NextRound()
    {
        if (_state.UnusedBlueDice.Count > 0 || _state.UnusedOrangeDice.Count > 0)
            throw new InvalidOperationException("Cannot proceed to next round with unused dice.");

        _altitude.Advance();
        _state.SetCurrentPlayer(_altitude.CurrentPlayer);
        RollDice();
    }

    public void SwitchPlayer() => _state.SwitchPlayer();


    /// <summary>
    ///     Gets all available commands for the current player based on game state.
    /// </summary>
    public IEnumerable<GameCommand> GetAvailableCommands()
    {
        if (_state.UnusedBlueDice.Count == 0 && _state.UnusedOrangeDice.Count == 0)
        {
            yield return _allCommands[NextRoundCommand.Instance.CommandId];
            yield break;
        }

        foreach (var gameCommand in _modules.SelectMany(module =>
                     module.GetAvailableCommands(_state.CurrentPlayer)))
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
        _state.ClearUnusedDice();

        for (var i = 0; i < 4; i++)
        {
            _state.AddBlueDie(BlueDie.Roll());
            _state.AddOrangeDie(OrangeDie.Roll());
        }
    }
}

sealed record NextRoundCommand : GameCommand
{
    private NextRoundCommand()
    {
    }

    public override string CommandId => "NextRound";
    public override string DisplayName => "Proceed to Next Round";
    public static NextRoundCommand Instance { get; } = new();

    public static void Execute(Game game) => game.NextRound();
}
