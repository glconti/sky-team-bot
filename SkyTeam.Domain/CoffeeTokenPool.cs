namespace SkyTeam.Domain;

readonly record struct CoffeeTokenPool
{
    private const int MaxCapacity = 3;

    public int Count { get; }

    public CoffeeTokenPool(int count = 0)
    {
        if (count is < 0 or > MaxCapacity)
            throw new ArgumentOutOfRangeException(nameof(count), $"Coffee token count must be between 0 and {MaxCapacity}.");

        Count = count;
    }

    public CoffeeTokenPool Earn(int amount = 1)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        return new CoffeeTokenPool(Math.Min(Count + amount, MaxCapacity));
    }

    public CoffeeTokenPool Spend(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        if (Count < amount)
            throw new InvalidOperationException("Not enough coffee tokens to spend.");

        return new CoffeeTokenPool(Count - amount);
    }
}
