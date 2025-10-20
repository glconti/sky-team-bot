namespace SkyTeam.Domain;

abstract class Die
{
    private static readonly Random Random = new();

    protected Die(int value)
    {
        if (value is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(value),
                "Die value must be between 1 and 6.");

        Value = value;
    }

    private int Value { get; }

    protected static int Roll() => Random.Next(1, 7); // Returns 1-6

    public static implicit operator int(Die die) => die.Value;
}