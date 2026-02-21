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

        foreach (var module in _modules)
            module.ResetRound();

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
                     module.GetAvailableCommands(_state.CurrentPlayer, _state.UnusedBlueDice, _state.UnusedOrangeDice)))
            yield return gameCommand;
    }

    /// <summary>
    ///     Executes a command by placing a die on the specified module.
    /// </summary>
    public void ExecuteCommand(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        var availableCommandIds = GetAvailableCommands()
            .Select(command => command.CommandId)
            .ToHashSet(StringComparer.Ordinal);

        if (!availableCommandIds.Contains(commandId))
            throw new InvalidOperationException($"Command '{commandId}' is not currently available.");

        if (commandId == NextRoundCommand.Instance.CommandId)
        {
            NextRound();
            return;
        }

        var parts = commandId.Split(':', 2);
        if (parts.Length != 2)
            throw new InvalidOperationException($"Invalid command id '{commandId}'.");

        var prefix = parts[0];

        if (!int.TryParse(parts[1], out var value))
            throw new InvalidOperationException($"Invalid command id '{commandId}'.");

        AxisPositionModule Axis() => GetRequiredModule<AxisPositionModule>("Axis");
        EnginesModule Engines() => GetRequiredModule<EnginesModule>("Engines");
        BrakesModule Brakes() => GetRequiredModule<BrakesModule>("Brakes");
        FlapsModule Flaps() => GetRequiredModule<FlapsModule>("Flaps");
        LandingGearModule LandingGear() => GetRequiredModule<LandingGearModule>("LandingGear");
        RadioModule Radio() => GetRequiredModule<RadioModule>("Radio");
        ConcentrationModule Concentration() => GetRequiredModule<ConcentrationModule>("Concentration");

        BlueDie GetUnusedBlueDie(int targetValue)
        {
            var die = _state.UnusedBlueDice.FirstOrDefault(d => (int)d == targetValue);
            return die ?? throw new InvalidOperationException($"No unused blue die found with value {targetValue}.");
        }

        OrangeDie GetUnusedOrangeDie(int targetValue)
        {
            var die = _state.UnusedOrangeDice.FirstOrDefault(d => (int)d == targetValue);
            return die ?? throw new InvalidOperationException($"No unused orange die found with value {targetValue}.");
        }

        switch (prefix)
        {
            case "Axis.AssignBlue":
            {
                var die = GetUnusedBlueDie(value);
                Axis().AssignBlueDie(die);
                _state.RemoveBlueDie(die);
                break;
            }
            case "Axis.AssignOrange":
            {
                var die = GetUnusedOrangeDie(value);
                Axis().AssignOrangeDie(die);
                _state.RemoveOrangeDie(die);
                break;
            }
            case "Engines.AssignBlue":
            {
                var die = GetUnusedBlueDie(value);
                Engines().AssignBlueDie(die);
                _state.RemoveBlueDie(die);
                break;
            }
            case "Engines.AssignOrange":
            {
                var die = GetUnusedOrangeDie(value);
                Engines().AssignOrangeDie(die);
                _state.RemoveOrangeDie(die);
                break;
            }
            case "Brakes.AssignBlue":
            {
                var die = GetUnusedBlueDie(value);
                Brakes().AssignBlueDie(die);
                _state.RemoveBlueDie(die);
                break;
            }
            case "Flaps.AssignOrange":
            {
                var die = GetUnusedOrangeDie(value);
                Flaps().AssignOrangeDie(die);
                _state.RemoveOrangeDie(die);
                break;
            }
            case "LandingGear.AssignBlue":
            {
                var die = GetUnusedBlueDie(value);
                LandingGear().AssignBlueDie(die);
                _state.RemoveBlueDie(die);
                break;
            }
            case "Radio.AssignBlue":
            {
                var die = GetUnusedBlueDie(value);
                Radio().AssignBlueDie(die);
                _state.RemoveBlueDie(die);
                break;
            }
            case "Radio.AssignOrange":
            {
                var die = GetUnusedOrangeDie(value);
                Radio().AssignOrangeDie(die);
                _state.RemoveOrangeDie(die);
                break;
            }
            case "Concentration.AssignBlue":
            {
                var die = GetUnusedBlueDie(value);
                Concentration().AssignBlueDie(die);
                _state.RemoveBlueDie(die);
                break;
            }
            case "Concentration.AssignOrange":
            {
                var die = GetUnusedOrangeDie(value);
                Concentration().AssignOrangeDie(die);
                _state.RemoveOrangeDie(die);
                break;
            }
            default:
                throw new InvalidOperationException($"Invalid command id '{commandId}'.");
        }

        _state.SwitchPlayer();

        TModule GetRequiredModule<TModule>(string name) where TModule : GameModule
        {
            var module = _modules.OfType<TModule>().SingleOrDefault();
            return module ?? throw new InvalidOperationException($"{name} module is not present.");
        }
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
