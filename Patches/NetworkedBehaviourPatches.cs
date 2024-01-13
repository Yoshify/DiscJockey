using DiscJockey.Managers;
using HarmonyLib;
using Unity.Netcode;

namespace DiscJockey.Patches;

[HarmonyPatch(typeof(NetworkBehaviour))]
public class NetworkedBehaviourPatches
{
    // I don't want to have to patch NetworkBehaviour but I also don't know how else to hook object despawns when neither
    // GrabbableObject or BoomboxItem implement OnNetworkDespawn or OnDestroy...
    [HarmonyPatch(typeof(NetworkBehaviour), "OnDestroy")]
    [HarmonyPostfix]
    private static void OnDestroyPatch(NetworkBehaviour __instance)
    {
        if (__instance is BoomboxItem boombox)
        {
            if (BoomboxManager.IsLookingAtOrHoldingBoombox &&
                BoomboxManager.LookedAtOrHeldBoombox.NetworkedBoomboxId == __instance.NetworkObjectId)
            {
                if (BoomboxManager.LookedAtOrHeldBoombox.IsStreaming)
                {
                    BoomboxManager.LookedAtOrHeldBoombox.StopStreamAndPlaybackAndNotify();    
                }
                
                BoomboxManager.OnDroppedBoombox();
                BoomboxManager.OnLookedAwayFromBoombox();
            }
            
            DJNetworkManager.Instance.UnregisterBoomboxServerRpc(boombox.NetworkObjectId);
        }
    }
}