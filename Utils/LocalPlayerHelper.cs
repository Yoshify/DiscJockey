using GameNetcodeStuff;
using Unity.Netcode;

namespace DiscJockey.Utils;

public class LocalPlayerHelper
{
    public static PlayerControllerB Player =>
        GameNetworkManager.Instance != null ? GameNetworkManager.Instance?.localPlayerController : null;
}