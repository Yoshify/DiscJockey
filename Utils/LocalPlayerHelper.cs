using GameNetcodeStuff;
using Unity.Netcode;

namespace DiscJockey.Utils
{
    public class LocalPlayerHelper
    {
        public static PlayerControllerB Player;

        public static void TrySetLocalPlayer(PlayerControllerB __instance)
        {
            if (NetworkManager.Singleton.LocalClientId != __instance.playerClientId)
                return;
            Player = __instance;
        }
    }
}