namespace SkyTeam.Domain;

record Die
{
    private static readonly Random Random = new();

    private Die(int value) => Value = value;

    private int Value { get; }

    public static Die Roll() => new(Random.Next(1, 7)); // Returns 1-6

    internal static Die FromValue(int value)
    {
        if (value is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(value), "Die values must be between 1 and 6.");

        return new Die(value);
    }

    public static implicit operator int(Die die) => die.Value;
}

record BlueDie
{
    private readonly Die _die;

    private BlueDie(Die die) => _die = die;
    private BlueDie() => _die = Die.Roll();

    public static BlueDie Roll() => new();

    internal static BlueDie FromValue(int value) => new(Die.FromValue(value));

    public static implicit operator int(BlueDie die) => die._die;
}

record OrangeDie
{
    private readonly Die _die;

    private OrangeDie(Die die) => _die = die;
    private OrangeDie() => _die = Die.Roll();

    public static OrangeDie Roll() => new();

    internal static OrangeDie FromValue(int value) => new(Die.FromValue(value));

    public static implicit operator int(OrangeDie die) => die._die;
}