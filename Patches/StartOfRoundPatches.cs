using HarmonyLib;
using Unity.Netcode;
using Object = UnityEngine.Object;

namespace DiscJockey.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatches
    {
        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        private static void Start()
        {
            if(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                GameNetworkManagerPatches.HostDJNetworkManager = Object.Instantiate(GameNetworkManagerPatches.DJNetworkManagerPrefab);
                GameNetworkManagerPatches.HostDJNetworkManager.GetComponent<NetworkObject>().Spawn(true);
                DiscJockeyPlugin.LogInfo($"StartOfRound_Start<Start>: Spawned DiscJockeyNetworkManager: {GameNetworkManagerPatches.HostDJNetworkManager != null}");
            }
        }
    }
}
