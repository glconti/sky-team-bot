namespace SkyTeam.Domain;

sealed class AxisPositionModule : GameModule
{
    private const int LossLimit = 3;

    private int _axisPosition;
    private BlueDie? _blueDie;
    private OrangeDie? _orangeDie;

    internal int AxisPosition => _axisPosition;

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _blueDie is null;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _orangeDie is null;

    public override string GetModuleName() => "Axis Position";

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
        ResolveAxisIfReady();
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_orangeDie is not null)
            throw new InvalidOperationException("Orange die already assigned.");

        _orangeDie = die;
        ResolveAxisIfReady();
    }

    public override void ResetRound()
    {
        _blueDie = null;
        _orangeDie = null;
    }

    private void ResolveAxisIfReady()
    {
        if (_blueDie is null || _orangeDie is null) return;

        var blueValue = (int)_blueDie;
        var orangeValue = (int)_orangeDie;
        var newAxisPosition = CalculateAxisPositionAfterResolution(blueValue, orangeValue);

        EnsureInBounds(newAxisPosition);
        _axisPosition = newAxisPosition;
    }

    private int CalculateAxisPositionAfterResolution(int blueValue, int orangeValue)
    {
        if (blueValue == orangeValue) return _axisPosition;

        var difference = Math.Abs(blueValue - orangeValue);
        return blueValue > orangeValue ? _axisPosition + difference : _axisPosition - difference;
    }

    private static void EnsureInBounds(int axisPosition)
    {
        if (axisPosition is > -LossLimit and < LossLimit) return;

        throw new InvalidOperationException("Axis position out of bounds.");
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
        int? resulting = _orangeDie is null
            ? null
            : CalculateAxisPositionAfterResolution(effectiveValue, (int)_orangeDie);

        return new AssignAxisBlueDieCommand(rolledValue, effectiveValue, tokenCost, resulting);
    }

    private GameCommand CreateOrangeCommand(int rolledValue, int effectiveValue, int tokenCost)
    {
        int? resulting = _blueDie is null
            ? null
            : CalculateAxisPositionAfterResolution((int)_blueDie, effectiveValue);

        return new AssignAxisOrangeDieCommand(rolledValue, effectiveValue, tokenCost, resulting);
    }

    private sealed record AssignAxisBlueDieCommand(
        int RolledValue,
        int EffectiveValue,
        int TokenCost,
        int? ResultingAxisPosition) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Axis.AssignBlue:{RolledValue}"
            : $"Axis.AssignBlue:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => CreateDisplayName("blue", RolledValue, EffectiveValue, TokenCost, ResultingAxisPosition);
    }

    private sealed record AssignAxisOrangeDieCommand(
        int RolledValue,
        int EffectiveValue,
        int TokenCost,
        int? ResultingAxisPosition) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Axis.AssignOrange:{RolledValue}"
            : $"Axis.AssignOrange:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => CreateDisplayName("orange", RolledValue, EffectiveValue, TokenCost, ResultingAxisPosition);
    }

    private static string CreateDisplayName(
        string color,
        int rolledValue,
        int effectiveValue,
        int tokenCost,
        int? resultingAxisPosition)
    {
        var label = tokenCost == 0
            ? $"Axis: place {color} {rolledValue}"
            : $"Axis: place {color} {rolledValue} as {effectiveValue} (cost {tokenCost})";

        return resultingAxisPosition is null
            ? label
            : $"{label} (axis -> {resultingAxisPosition})";
    }
}
