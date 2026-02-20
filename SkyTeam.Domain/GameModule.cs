namespace SkyTeam.Domain;

abstract class GameModule
{
    /// <summary>
    /// Determines if this module can accept a blue die from the specified player.
    /// </summary>
    public abstract bool CanAcceptBlueDie(Player player);

    /// <summary>
    /// Determines if this module can accept an orange die from the specified player.
    /// </summary>
    public abstract bool CanAcceptOrangeDie(Player player);

    /// <summary>
    /// Gets a display name for this module.
    /// </summary>
    public abstract string GetModuleName();

    public abstract IEnumerable<GameCommand> GetAvailableCommands(Player currentPlayer);
}