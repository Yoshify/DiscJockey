namespace DiscJockey.Utils;

public class GameUtils
{
    public static string GetPlayerName(ulong playerId)
    {
        return StartOfRound.Instance.ClientPlayerList.TryGetValue(playerId, out var playerIndex)
            ? StartOfRound.Instance.allPlayerScripts[playerIndex].playerUsername 
            : "UNKNOWN";
    }
}