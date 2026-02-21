namespace SkyTeam.Domain;

sealed class RadioModule(Airport airport) : GameModule
{
    private const int MaxOrangeDicePerRound = 2;

    private readonly Airport _airport = airport ?? throw new ArgumentNullException(nameof(airport));

    private BlueDie? _blueDie;
    private readonly List<OrangeDie> _orangeDice = [];

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _blueDie is null;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _orangeDice.Count < MaxOrangeDicePerRound;

    public override string GetModuleName() => "Radio";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice,
        CoffeeTokenPool tokenPool)
    {
        var availableTokens = tokenPool.Count;

        if (currentPlayer == Player.Pilot)
        {
            if (_blueDie is not null) yield break;

            foreach (var rolledValue in unusedBlueDice.Select(die => (int)die).Distinct().Order())
            {
                yield return new AssignRadioBlueDieCommand(rolledValue, rolledValue, TokenCost: 0);

                foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
                    yield return new AssignRadioBlueDieCommand(rolledValue, effectiveValue, TokenCost: Math.Abs(effectiveValue - rolledValue));
            }

            yield break;
        }

        if (currentPlayer != Player.Copilot) yield break;
        if (_orangeDice.Count >= MaxOrangeDicePerRound) yield break;

        foreach (var rolledValue in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
        {
            yield return new AssignRadioOrangeDieCommand(rolledValue, rolledValue, TokenCost: 0);

            foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
                yield return new AssignRadioOrangeDieCommand(rolledValue, effectiveValue, TokenCost: Math.Abs(effectiveValue - rolledValue));
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_blueDie is not null)
            throw new InvalidOperationException("Blue die already assigned.");

        _blueDie = die;
        _airport.TryRemovePlaneTokenAtOffset((int)die - 1);
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_orangeDice.Count >= MaxOrangeDicePerRound)
            throw new InvalidOperationException("All orange dice already assigned.");

        _orangeDice.Add(die);
        _airport.TryRemovePlaneTokenAtOffset((int)die - 1);
    }

    public override void ResetRound()
    {
        _blueDie = null;
        _orangeDice.Clear();
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

    private sealed record AssignRadioBlueDieCommand(int RolledValue, int EffectiveValue, int TokenCost) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Radio.AssignBlue:{RolledValue}"
            : $"Radio.AssignBlue:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Radio: place blue {RolledValue}"
            : $"Radio: place blue {RolledValue} as {EffectiveValue} (cost {TokenCost})";
    }

    private sealed record AssignRadioOrangeDieCommand(int RolledValue, int EffectiveValue, int TokenCost) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Radio.AssignOrange:{RolledValue}"
            : $"Radio.AssignOrange:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Radio: place orange {RolledValue}"
            : $"Radio: place orange {RolledValue} as {EffectiveValue} (cost {TokenCost})";
    }
}
