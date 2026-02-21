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
        IReadOnlyList<OrangeDie> unusedOrangeDice,
        CoffeeTokenPool tokenPool)
    {
        var availableTokens = tokenPool.Count;

        if (currentPlayer == Player.Pilot && _blueDie is null)
        {
            foreach (var rolledValue in unusedBlueDice.Select(die => (int)die).Distinct().Order())
            {
                yield return CreateBlueCommand(rolledValue, rolledValue, tokenCost: 0);

                foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
                    yield return CreateBlueCommand(rolledValue, effectiveValue, tokenCost: Math.Abs(effectiveValue - rolledValue));
            }
        }

        if (currentPlayer == Player.Copilot && _orangeDie is null)
        {
            foreach (var rolledValue in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
            {
                yield return CreateOrangeCommand(rolledValue, rolledValue, tokenCost: 0);

                foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
                    yield return CreateOrangeCommand(rolledValue, effectiveValue, tokenCost: Math.Abs(effectiveValue - rolledValue));
            }
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

    private static IEnumerable<int> GetAdjustedValues(int rolledValue, int availableTokens)
    {
        if (availableTokens <= 0) yield break;

        var min = Math.Max(1, rolledValue - availableTokens);
        var max = Math.Min(6, rolledValue + availableTokens);

        for (var value = min; value <= max; value++)
        {
            if (value == rolledValue) continue;

            yield return value;
        }
    }

    private GameCommand CreateBlueCommand(int rolledValue, int effectiveValue, int tokenCost)
    {
        EnginesPlacementPreview? preview = null;

        if (_orangeDie is not null)
            preview = GetPreview(effectiveValue + (int)_orangeDie);

        return new AssignEnginesBlueDieCommand(rolledValue, effectiveValue, tokenCost, preview);
    }

    private GameCommand CreateOrangeCommand(int rolledValue, int effectiveValue, int tokenCost)
    {
        EnginesPlacementPreview? preview = null;

        if (_blueDie is not null)
            preview = GetPreview((int)_blueDie + effectiveValue);

        return new AssignEnginesOrangeDieCommand(rolledValue, effectiveValue, tokenCost, preview);
    }

    private EnginesPlacementPreview GetPreview(int speed)
    {
        var advanceBy = CalculateApproachAdvance(speed);
        var resultingPosition = _airport.CurrentPositionIndex + advanceBy;

        return new EnginesPlacementPreview(speed, advanceBy, resultingPosition);
    }

    private sealed record EnginesPlacementPreview(int Speed, int AdvanceBy, int ResultingPosition);

    private sealed record AssignEnginesBlueDieCommand(
        int RolledValue,
        int EffectiveValue,
        int TokenCost,
        EnginesPlacementPreview? Preview) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Engines.AssignBlue:{RolledValue}"
            : $"Engines.AssignBlue:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => CreateDisplayName("blue", RolledValue, EffectiveValue, TokenCost, Preview);
    }

    private sealed record AssignEnginesOrangeDieCommand(
        int RolledValue,
        int EffectiveValue,
        int TokenCost,
        EnginesPlacementPreview? Preview) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Engines.AssignOrange:{RolledValue}"
            : $"Engines.AssignOrange:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => CreateDisplayName("orange", RolledValue, EffectiveValue, TokenCost, Preview);
    }

    private static string CreateDisplayName(
        string color,
        int rolledValue,
        int effectiveValue,
        int tokenCost,
        EnginesPlacementPreview? preview)
    {
        var label = tokenCost == 0
            ? $"Engines: place {color} {rolledValue}"
            : $"Engines: place {color} {rolledValue} as {effectiveValue} (cost {tokenCost})";

        return preview is null
            ? label
            : $"{label} (speed {preview.Speed}, advance {preview.AdvanceBy})";
    }
}
