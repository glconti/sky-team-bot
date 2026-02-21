namespace SkyTeam.Domain;

sealed class LandingGearModule(Airport airport) : GameModule
{
    private static readonly int[][] AllowedValuesBySwitch =
    [
        [1, 2],
        [3, 4],
        [5, 6]
    ];

    private readonly Airport _airport = airport ?? throw new ArgumentNullException(nameof(airport));
    private readonly bool[] _isSwitchActivated = new bool[AllowedValuesBySwitch.Length];

    internal int LandingGearValue { get; private set; }

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _isSwitchActivated.Any(activated => !activated);

    public override bool CanAcceptOrangeDie(Player player) => false;

    public override string GetModuleName() => "Landing Gear";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice,
        CoffeeTokenPool tokenPool)
    {
        if (currentPlayer != Player.Pilot) yield break;
        if (_isSwitchActivated.All(activated => activated)) yield break;

        var availableTokens = tokenPool.Count;

        foreach (var rolledValue in unusedBlueDice.Select(die => (int)die).Distinct().Order())
        {
            var unadjusted = CreateCommandIfPossible(rolledValue, rolledValue, tokenCost: 0);
            if (unadjusted is not null) yield return unadjusted;

            foreach (var effectiveValue in GetAdjustedValues(rolledValue, availableTokens))
            {
                var adjusted = CreateCommandIfPossible(rolledValue, effectiveValue, tokenCost: Math.Abs(effectiveValue - rolledValue));
                if (adjusted is not null) yield return adjusted;
            }
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_isSwitchActivated.All(activated => activated))
            throw new InvalidOperationException("All landing gear switches are already activated.");

        var value = (int)die;
        var switchIndex = GetSwitchIndexForValue(value);

        if (_isSwitchActivated[switchIndex]) return;

        _isSwitchActivated[switchIndex] = true;
        LandingGearValue++;
        _airport.MoveBlueAerodynamicsRight();
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

    private GameCommand? CreateCommandIfPossible(int rolledValue, int effectiveValue, int tokenCost)
    {
        var switchIndex = GetSwitchIndexForValue(effectiveValue);
        if (_isSwitchActivated[switchIndex]) return null;

        return new ActivateLandingGearCommand(rolledValue, effectiveValue, tokenCost, switchIndex + 1);
    }

    private static int GetSwitchIndexForValue(int value) => value switch
    {
        1 or 2 => 0,
        3 or 4 => 1,
        5 or 6 => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Die values must be between 1 and 6.")
    };

    private sealed record ActivateLandingGearCommand(
        int RolledValue,
        int EffectiveValue,
        int TokenCost,
        int SwitchNumber) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"LandingGear.AssignBlue:{RolledValue}"
            : $"LandingGear.AssignBlue:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Landing gear: deploy switch {SwitchNumber} with {RolledValue}"
            : $"Landing gear: deploy switch {SwitchNumber} with {RolledValue} as {EffectiveValue} (cost {TokenCost})";
    }
}
