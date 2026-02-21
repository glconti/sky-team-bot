namespace SkyTeam.Domain;

sealed class BrakesModule : GameModule
{
    private static readonly int[] RequiredValues = [2, 4, 6];

    private int _nextRequiredIndex;

    internal int BrakesValue { get; private set; }

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _nextRequiredIndex < RequiredValues.Length;

    public override bool CanAcceptOrangeDie(Player player) => false;

    public override string GetModuleName() => "Brakes";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice,
        CoffeeTokenPool tokenPool)
    {
        if (currentPlayer != Player.Pilot) yield break;
        if (_nextRequiredIndex >= RequiredValues.Length) yield break;

        var availableTokens = tokenPool.Count;
        var requiredValue = RequiredValues[_nextRequiredIndex];
        var switchNumber = _nextRequiredIndex + 1;

        foreach (var rolledValue in unusedBlueDice.Select(die => (int)die).Distinct().Order())
        {
            var tokenCost = Math.Abs(requiredValue - rolledValue);
            if (tokenCost > availableTokens) continue;

            yield return new ActivateBrakesCommand(rolledValue, requiredValue, tokenCost, switchNumber);
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_nextRequiredIndex >= RequiredValues.Length)
            throw new InvalidOperationException("All brakes switches are already activated.");

        var requiredValue = RequiredValues[_nextRequiredIndex];
        if ((int)die != requiredValue)
            throw new InvalidOperationException($"Brakes requires die value {requiredValue} next.");

        _nextRequiredIndex++;
        BrakesValue = requiredValue;
    }

    private sealed record ActivateBrakesCommand(
        int RolledValue,
        int RequiredValue,
        int TokenCost,
        int SwitchNumber) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Brakes.AssignBlue:{RolledValue}"
            : $"Brakes.AssignBlue:{RolledValue}>{RequiredValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Brakes: activate switch {SwitchNumber} with {RolledValue}"
            : $"Brakes: activate switch {SwitchNumber} with {RolledValue} as {RequiredValue} (cost {TokenCost})";
    }
}
