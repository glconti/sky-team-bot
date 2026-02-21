using System.Collections.Immutable;

namespace SkyTeam.Application.Round;

public sealed record SecretHandDie(int Index, DieValue Value, bool IsUsed);

public sealed class SecretDiceHand
{
    public const int DicePerHand = 4;

    private readonly ImmutableArray<SecretHandDie> _dice;

    private SecretDiceHand(ImmutableArray<SecretHandDie> dice) => _dice = dice;

    public IReadOnlyList<SecretHandDie> Dice => _dice;

    public static SecretDiceHand Create(IEnumerable<int> rolledValues)
    {
        ArgumentNullException.ThrowIfNull(rolledValues);

        var values = rolledValues.ToArray();
        if (values.Length != DicePerHand)
            throw new ArgumentException($"A hand must contain exactly {DicePerHand} dice.", nameof(rolledValues));

        var dice = values
            .Select((value, index) => new SecretHandDie(index, new DieValue(value), IsUsed: false))
            .ToImmutableArray();

        return new SecretDiceHand(dice);
    }

    public bool CanUse(int dieIndex) => dieIndex is >= 0 and < DicePerHand && !_dice[dieIndex].IsUsed;

    public SecretDiceHand UseDie(int dieIndex, out DieValue value)
    {
        if (dieIndex is < 0 or >= DicePerHand)
            throw new ArgumentOutOfRangeException(nameof(dieIndex));

        var die = _dice[dieIndex];
        if (die.IsUsed)
            throw new InvalidOperationException("This die has already been used.");

        value = die.Value;
        return new SecretDiceHand(_dice.SetItem(dieIndex, die with { IsUsed = true }));
    }

    public SecretDiceHand UnuseDie(int dieIndex)
    {
        if (dieIndex is < 0 or >= DicePerHand)
            throw new ArgumentOutOfRangeException(nameof(dieIndex));

        var die = _dice[dieIndex];
        if (!die.IsUsed)
            throw new InvalidOperationException("This die is not used.");

        return new SecretDiceHand(_dice.SetItem(dieIndex, die with { IsUsed = false }));
    }
}
