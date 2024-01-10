namespace DiscJockey.Utils;

public class GameUtils
{
    public const string DiscJockeyNameColour = "#1565C0";
    public const string StandardMessageColour = "#FFFFFF";
    public const string PlayerNameColour = "#388E3C";
    private static string DiscJockeyFormattedName => GetColourFormattedText("DiscJockey", DiscJockeyNameColour);

    public static string GetPlayerName(ulong playerId)
    {
        return !StartOfRound.Instance.ClientPlayerList.TryGetValue(playerId, out var playerIndex)
            ? "UNKNOWN"
            : StartOfRound.Instance.allPlayerScripts[playerIndex].playerUsername;
    }

    public static void LogDiscJockeyMessageToLocalChat(string message)
    {
        var formattedMessage =
            $"{DiscJockeyFormattedName}{GetColourFormattedText($"$: {message}", StandardMessageColour)}";
        HUDManager.Instance.AddChatMessage(formattedMessage);
    }

    public static string GetColourFormattedText(string text, string hexColour)
    {
        return $"<color={hexColour}>{text}</color>";
    }
}