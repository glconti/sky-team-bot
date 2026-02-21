namespace SkyTeam.Domain;

sealed class ConcentrationModule : GameModule
{
    private const int SlotsPerRound = 2;

    private int _slotsUsed;
    private CoffeeTokenPool _tokenPool;

    internal CoffeeTokenPool TokenPool => _tokenPool;

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _slotsUsed < SlotsPerRound;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _slotsUsed < SlotsPerRound;

    public override string GetModuleName() => "Concentration";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice,
        CoffeeTokenPool tokenPool)
    {
        if (_slotsUsed >= SlotsPerRound) yield break;

        var availableTokens = tokenPool.Count;

        if (currentPlayer == Player.Pilot)
        {
            foreach (var rolledValue in unusedBlueDice.Select(die => (int)die).Distinct().Order())
            {
                yield return new AssignConcentrationBlueDieCommand(rolledValue, rolledValue, TokenCost: 0);

                foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
                    yield return new AssignConcentrationBlueDieCommand(rolledValue, effectiveValue, TokenCost: Math.Abs(effectiveValue - rolledValue));
            }

            yield break;
        }

        if (currentPlayer != Player.Copilot) yield break;

        foreach (var rolledValue in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
        {
            yield return new AssignConcentrationOrangeDieCommand(rolledValue, rolledValue, TokenCost: 0);

            foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
                yield return new AssignConcentrationOrangeDieCommand(rolledValue, effectiveValue, TokenCost: Math.Abs(effectiveValue - rolledValue));
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_slotsUsed >= SlotsPerRound)
            throw new InvalidOperationException("No concentration spaces available this round.");

        _slotsUsed++;
        _tokenPool = _tokenPool.Earn();
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_slotsUsed >= SlotsPerRound)
            throw new InvalidOperationException("No concentration spaces available this round.");

        _slotsUsed++;
        _tokenPool = _tokenPool.Earn();
    }

    internal void SpendTokens(int amount) => _tokenPool = _tokenPool.Spend(amount);

    public override void ResetRound() => _slotsUsed = 0;

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

    private sealed record AssignConcentrationBlueDieCommand(int RolledValue, int EffectiveValue, int TokenCost) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Concentration.AssignBlue:{RolledValue}"
            : $"Concentration.AssignBlue:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Concentration: place blue {RolledValue}"
            : $"Concentration: place blue {RolledValue} as {EffectiveValue} (cost {TokenCost})";
    }

    private sealed record AssignConcentrationOrangeDieCommand(int RolledValue, int EffectiveValue, int TokenCost) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Concentration.AssignOrange:{RolledValue}"
            : $"Concentration.AssignOrange:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Concentration: place orange {RolledValue}"
            : $"Concentration: place orange {RolledValue} as {EffectiveValue} (cost {TokenCost})";
    }
}
