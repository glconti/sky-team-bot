namespace SkyTeam.Domain;

sealed class EnginesModule(Airport airport) : GameModule
{
    private readonly Airport _airport = airport ?? throw new ArgumentNullException(nameof(airport));

    private BlueDie? _blueDie;
    private OrangeDie? _orangeDie;

    internal int? LastSpeed { get; private set; }

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _blueDie is null;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _orangeDie is null;

    public override string GetModuleName() => "Engines";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice)
    {
        if (currentPlayer == Player.Pilot && _blueDie is null)
        {
            foreach (var value in unusedBlueDice.Select(die => (int)die).Distinct().Order())
                yield return CreateBlueCommand(value);
        }

        if (currentPlayer == Player.Copilot && _orangeDie is null)
        {
            foreach (var value in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
                yield return CreateOrangeCommand(value);
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_blueDie is not null)
            throw new InvalidOperationException("Blue die already assigned.");

        _blueDie = die;
        ResolveEnginesIfReady();
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_orangeDie is not null)
            throw new InvalidOperationException("Orange die already assigned.");

        _orangeDie = die;
        ResolveEnginesIfReady();
    }

    public override void ResetRound()
    {
        _blueDie = null;
        _orangeDie = null;
        LastSpeed = null;
    }

    private void ResolveEnginesIfReady()
    {
        if (_blueDie is null || _orangeDie is null) return;

        var speed = (int)_blueDie + (int)_orangeDie;
        LastSpeed = speed;

        var advanceBy = CalculateApproachAdvance(speed);

        if (!_airport.IsFinalRound)
            _airport.AdvanceApproach(advanceBy);
    }

    private int CalculateApproachAdvance(int speed)
    {
        if (_airport.IsFinalRound) return 0;
        if (speed < _airport.BlueAerodynamicsThreshold) return 0;
        if (speed <= _airport.OrangeAerodynamicsThreshold) return 1;

        return 2;
    }

    private GameCommand CreateBlueCommand(int value)
    {
        EnginesPlacementPreview? preview = null;

        if (_orangeDie is not null)
            preview = GetPreview(value + (int)_orangeDie);

        return new AssignEnginesBlueDieCommand(value, preview);
    }

    private GameCommand CreateOrangeCommand(int value)
    {
        EnginesPlacementPreview? preview = null;

        if (_blueDie is not null)
            preview = GetPreview((int)_blueDie + value);

        return new AssignEnginesOrangeDieCommand(value, preview);
    }

    private EnginesPlacementPreview GetPreview(int speed)
    {
        var advanceBy = CalculateApproachAdvance(speed);
        var resultingPosition = _airport.CurrentPositionIndex + advanceBy;

        return new EnginesPlacementPreview(speed, advanceBy, resultingPosition);
    }

    private sealed record EnginesPlacementPreview(int Speed, int AdvanceBy, int ResultingPosition);

    private sealed record AssignEnginesBlueDieCommand(int Value, EnginesPlacementPreview? Preview) : GameCommand
    {
        public override string CommandId => $"Engines.AssignBlue:{Value}";

        public override string DisplayName => Preview is null
            ? $"Engines: place blue {Value}"
            : $"Engines: place blue {Value} (speed {Preview.Speed}, advance {Preview.AdvanceBy})";
    }

    private sealed record AssignEnginesOrangeDieCommand(int Value, EnginesPlacementPreview? Preview) : GameCommand
    {
        public override string CommandId => $"Engines.AssignOrange:{Value}";

        public override string DisplayName => Preview is null
            ? $"Engines: place orange {Value}"
            : $"Engines: place orange {Value} (speed {Preview.Speed}, advance {Preview.AdvanceBy})";
    }
}
