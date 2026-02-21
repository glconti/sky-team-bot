namespace SkyTeam.Application.Round;

public static class SecretDiceRoller
{
    public static SecretDiceRoll Roll(Func<int> rollDie, int dicePerSeat = 4)
    {
        ArgumentNullException.ThrowIfNull(rollDie);
        if (dicePerSeat < 1)
            throw new ArgumentOutOfRangeException(nameof(dicePerSeat), dicePerSeat, "Must roll at least one die per seat.");

        return new SecretDiceRoll(
            PilotDice: RollMany(rollDie, dicePerSeat),
            CopilotDice: RollMany(rollDie, dicePerSeat));
    }

    private static int[] RollMany(Func<int> rollDie, int count)
    {
        var dice = new int[count];

        for (var i = 0; i < count; i++)
        {
            var value = rollDie();
            if (value is < 1 or > 6)
                throw new InvalidOperationException("Die roller produced an invalid value.");

            dice[i] = value;
        }

        return dice;
    }
}
