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

        try
        {
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
        catch (GameRuleLossException)
        {
            Status = GameStatus.Lost;
            throw;
        }
    }

    public void SwitchPlayer() => _state.SwitchPlayer();

    private ConcentrationModule? Concentration => _modules.OfType<ConcentrationModule>().SingleOrDefault();

    internal CoffeeTokenPool TokenPool => Concentration?.TokenPool ?? new CoffeeTokenPool();

    internal void SpendCoffeeTokens(int amount)
    {
        var concentration = Concentration
            ?? throw new InvalidOperationException("Cannot spend coffee tokens without a Concentration module.");

        concentration.SpendCoffeeTokens(amount);
    }

    internal BlueDie GetUnusedBlueDie(int targetValue)
    {
        var die = _state.UnusedBlueDice.FirstOrDefault(d => (int)d == targetValue);
        return die ?? throw new InvalidOperationException($"No unused blue die found with value {targetValue}.");
    }

    internal OrangeDie GetUnusedOrangeDie(int targetValue)
    {
        var die = _state.UnusedOrangeDice.FirstOrDefault(d => (int)d == targetValue);
        return die ?? throw new InvalidOperationException($"No unused orange die found with value {targetValue}.");
    }

    internal void RemoveUnusedDie(BlueDie die) => _state.RemoveBlueDie(die);
    internal void RemoveUnusedDie(OrangeDie die) => _state.RemoveOrangeDie(die);


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

        var tokenPool = TokenPool;

        foreach (var gameCommand in _modules.SelectMany(module =>
                     module.GetAvailableCommands(_state.CurrentPlayer, _state.UnusedBlueDice, _state.UnusedOrangeDice, tokenPool)))
            yield return gameCommand;
    }

    /// <summary>
    ///     Executes a command by resolving it from the available command set and executing it.
    /// </summary>
    public void ExecuteCommand(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Cannot execute commands after the game has ended.");

        var command = GetAvailableCommands().SingleOrDefault(c => c.CommandId == commandId);

        if (command is null)
            throw new InvalidOperationException($"Command '{commandId}' is not currently available.");

        try
        {
            command.Execute(this);
        }
        catch (GameRuleLossException)
        {
            Status = GameStatus.Lost;
            throw;
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

    internal override void Execute(Game game) => game.NextRound();
}
