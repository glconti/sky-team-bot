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

    internal GameStatus Status { get; private set; } = GameStatus.InProgress;

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

        if (_altitude.IsLanded)
        {
            _airport.EnterFinalRound();

            if (_airport.CurrentPositionIndex != _airport.SegmentCount - 1)
                Status = GameStatus.Lost;
        }
    }

    public void NextRound()
    {
        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Cannot proceed to next round after the game has ended.");

        if (_state.UnusedBlueDice.Count > 0 || _state.UnusedOrangeDice.Count > 0)
            throw new InvalidOperationException("Cannot proceed to next round with unused dice.");

        if (_altitude.IsLanded)
        {
            EvaluateLandingOutcome();
            return;
        }

        _altitude.Advance();
        _state.SetCurrentPlayer(_altitude.CurrentPlayer);

        if (_altitude.IsLanded)
        {
            _airport.EnterFinalRound();

            if (_airport.CurrentPositionIndex != _airport.SegmentCount - 1)
            {
                Status = GameStatus.Lost;
                return;
            }
        }

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
        if (Status != GameStatus.InProgress) yield break;

        if (_state.UnusedBlueDice.Count == 0 && _state.UnusedOrangeDice.Count == 0)
        {
            yield return _allCommands[NextRoundCommand.Instance.CommandId];
            yield break;
        }

        var tokenPool = _modules
            .OfType<ConcentrationModule>()
            .SingleOrDefault()?
            .TokenPool ?? new CoffeeTokenPool();

        foreach (var gameCommand in _modules.SelectMany(module =>
                     module.GetAvailableCommands(_state.CurrentPlayer, _state.UnusedBlueDice, _state.UnusedOrangeDice, tokenPool)))
            yield return gameCommand;
    }

    /// <summary>
    ///     Executes a command by placing a die on the specified module.
    /// </summary>
    public void ExecuteCommand(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Cannot execute commands after the game has ended.");

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

        var valuePart = parts[1];
        var valueParts = valuePart.Split('>', 2);

        if (!int.TryParse(valueParts[0], out var rolledValue))
            throw new InvalidOperationException($"Invalid command id '{commandId}'.");

        var effectiveValue = rolledValue;

        if (valueParts.Length == 2)
        {
            if (!int.TryParse(valueParts[1], out effectiveValue))
                throw new InvalidOperationException($"Invalid command id '{commandId}'.");
        }

        var tokenCost = Math.Abs(effectiveValue - rolledValue);

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

        void SpendTokensIfNeeded()
        {
            if (tokenCost == 0) return;

            Concentration().SpendTokens(tokenCost);
        }

        BlueDie GetBlueDieForAssignment(BlueDie rolledDie) => tokenCost == 0
            ? rolledDie
            : BlueDie.FromValue(effectiveValue);

        OrangeDie GetOrangeDieForAssignment(OrangeDie rolledDie) => tokenCost == 0
            ? rolledDie
            : OrangeDie.FromValue(effectiveValue);

        try
        {
            switch (prefix)
            {
            case "Axis.AssignBlue":
            {
                var rolledDie = GetUnusedBlueDie(rolledValue);
                SpendTokensIfNeeded();
                Axis().AssignBlueDie(GetBlueDieForAssignment(rolledDie));
                _state.RemoveBlueDie(rolledDie);
                break;
            }
            case "Axis.AssignOrange":
            {
                var rolledDie = GetUnusedOrangeDie(rolledValue);
                SpendTokensIfNeeded();
                Axis().AssignOrangeDie(GetOrangeDieForAssignment(rolledDie));
                _state.RemoveOrangeDie(rolledDie);
                break;
            }
            case "Engines.AssignBlue":
            {
                var rolledDie = GetUnusedBlueDie(rolledValue);
                SpendTokensIfNeeded();
                Engines().AssignBlueDie(GetBlueDieForAssignment(rolledDie));
                _state.RemoveBlueDie(rolledDie);
                break;
            }
            case "Engines.AssignOrange":
            {
                var rolledDie = GetUnusedOrangeDie(rolledValue);
                SpendTokensIfNeeded();
                Engines().AssignOrangeDie(GetOrangeDieForAssignment(rolledDie));
                _state.RemoveOrangeDie(rolledDie);
                break;
            }
            case "Brakes.AssignBlue":
            {
                var rolledDie = GetUnusedBlueDie(rolledValue);
                SpendTokensIfNeeded();
                Brakes().AssignBlueDie(GetBlueDieForAssignment(rolledDie));
                _state.RemoveBlueDie(rolledDie);
                break;
            }
            case "Flaps.AssignOrange":
            {
                var rolledDie = GetUnusedOrangeDie(rolledValue);
                SpendTokensIfNeeded();
                Flaps().AssignOrangeDie(GetOrangeDieForAssignment(rolledDie));
                _state.RemoveOrangeDie(rolledDie);
                break;
            }
            case "LandingGear.AssignBlue":
            {
                var rolledDie = GetUnusedBlueDie(rolledValue);
                SpendTokensIfNeeded();
                LandingGear().AssignBlueDie(GetBlueDieForAssignment(rolledDie));
                _state.RemoveBlueDie(rolledDie);
                break;
            }
            case "Radio.AssignBlue":
            {
                var rolledDie = GetUnusedBlueDie(rolledValue);
                SpendTokensIfNeeded();
                Radio().AssignBlueDie(GetBlueDieForAssignment(rolledDie));
                _state.RemoveBlueDie(rolledDie);
                break;
            }
            case "Radio.AssignOrange":
            {
                var rolledDie = GetUnusedOrangeDie(rolledValue);
                SpendTokensIfNeeded();
                Radio().AssignOrangeDie(GetOrangeDieForAssignment(rolledDie));
                _state.RemoveOrangeDie(rolledDie);
                break;
            }
            case "Concentration.AssignBlue":
            {
                var rolledDie = GetUnusedBlueDie(rolledValue);
                SpendTokensIfNeeded();
                Concentration().AssignBlueDie(GetBlueDieForAssignment(rolledDie));
                _state.RemoveBlueDie(rolledDie);
                break;
            }
            case "Concentration.AssignOrange":
            {
                var rolledDie = GetUnusedOrangeDie(rolledValue);
                SpendTokensIfNeeded();
                Concentration().AssignOrangeDie(GetOrangeDieForAssignment(rolledDie));
                _state.RemoveOrangeDie(rolledDie);
                break;
            }
            default:
                throw new InvalidOperationException($"Invalid command id '{commandId}'.");
            }

            _state.SwitchPlayer();
        }
        catch
        {
            Status = GameStatus.Lost;
            throw;
        }

        TModule GetRequiredModule<TModule>(string name) where TModule : GameModule
        {
            var module = _modules.OfType<TModule>().SingleOrDefault();
            return module ?? throw new InvalidOperationException($"{name} module is not present.");
        }
    }

    private void EvaluateLandingOutcome()
    {
        var allPlaneTokensCleared = _airport.PathSegments.All(segment => segment.PlaneTokens == 0);

        var axisModule = _modules.OfType<AxisPositionModule>().SingleOrDefault();
        var enginesModule = _modules.OfType<EnginesModule>().SingleOrDefault();
        var brakesModule = _modules.OfType<BrakesModule>().SingleOrDefault();
        var flapsModule = _modules.OfType<FlapsModule>().SingleOrDefault();
        var landingGearModule = _modules.OfType<LandingGearModule>().SingleOrDefault();

        var axisOk = axisModule?.AxisPosition is >= -2 and <= 2;
        var enginesOk = enginesModule?.LastSpeed is >= 9;
        var brakesOk = brakesModule?.BrakesValue is >= 6;
        var flapsOk = flapsModule?.FlapsValue is >= 4;
        var landingGearOk = landingGearModule?.LandingGearValue is >= 3;

        var isWin = allPlaneTokensCleared
                    && axisOk == true
                    && enginesOk == true
                    && brakesOk == true
                    && flapsOk == true
                    && landingGearOk == true;

        Status = isWin ? GameStatus.Won : GameStatus.Lost;
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
