using System.Collections.Generic;
using System.Linq;

namespace DiscJockey.Utils
{
    public class GameUtils
    {
        public static List<ulong> ConnectedPlayerIDs => StartOfRound.Instance.ClientPlayerList.Keys.ToList();

        public static string GetPlayerName(ulong playerId) =>
            !StartOfRound.Instance.ClientPlayerList.TryGetValue(playerId, out var playerIndex) ? "UNKNOWN" : StartOfRound.Instance.allPlayerScripts[playerIndex].playerUsername;
        
        public const string DiscJockeyNameColour = "#1565C0";
        public const string StandardMessageColour = "#FFFF00";
        public const string PlayerNameColour = "#7069ff";
        
        public static void LogDiscJockeyMessageToServer(string message)
        {
            var formattedMessage = $"{DiscJockeyFormattedName}: {GetColourFormattedText(message, StandardMessageColour)}";
            HUDManager.Instance.AddTextToChatOnServer(formattedMessage);
        }

        private static string DiscJockeyFormattedName => GetColourFormattedText("DiscJockey", DiscJockeyNameColour);
        
        public static string GetColourFormattedText(string text, string hexColour) => $"<color={hexColour}>{text}</color>";
    }
}