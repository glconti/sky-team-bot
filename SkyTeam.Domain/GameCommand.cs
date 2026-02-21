namespace SkyTeam.Domain;

/// <summary>
/// Represents an executable command available to a player based on the current game state.
/// </summary>
abstract record GameCommand
{
    public abstract string CommandId { get; }
    public abstract string DisplayName { get; }

    internal abstract void Execute(Game game);
}

enum DieType
{
    Blue,
    Orange
}

