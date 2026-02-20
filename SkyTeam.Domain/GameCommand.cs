namespace SkyTeam.Domain;

/// <summary>
/// Represents an executable command available to a player based on the current game state.
/// </summary>
abstract record GameCommand
{
    public abstract string CommandId { get; }
    public abstract string DisplayName { get; }
}

enum DieType
{
    Blue,
    Orange
}

