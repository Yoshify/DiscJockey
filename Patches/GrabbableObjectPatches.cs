using DiscJockey.Input;
using DiscJockey.Managers;
using HarmonyLib;

namespace DiscJockey.Patches;

[HarmonyPatch(typeof(GrabbableObject))]
internal class GrabbableObjectPatches
{
    [HarmonyPatch(typeof(GrabbableObject), "EquipItem")]
    [HarmonyPostfix]
    private static void EquipItemPatch(GrabbableObject __instance)
    {
        if (__instance is BoomboxItem { IsOwner: true } boombox)
        {
            BoomboxManager.OnHeldBoombox(boombox.NetworkObjectId);
            
        }
    }

    [HarmonyPatch(typeof(GrabbableObject), "DiscardItemOnClient")]
    [HarmonyPostfix]
    private static void DiscardItemOnClientPatch(GrabbableObject __instance)
    {
        if (__instance is BoomboxItem && __instance.IsOwner && BoomboxManager.IsLookingAtOrHoldingBoombox)
        {
            
            BoomboxManager.OnDroppedOrPocketedBoombox();
        }
    }
    
    [HarmonyPatch(typeof(GrabbableObject), "EnablePhysics")]
    [HarmonyPrefix]
    private static bool EnablePhysicsPatch(GrabbableObject __instance, bool enable)
    {
        if (__instance is BoomboxItem)
        {
            if (!enable)
            {
                if (!__instance.IsOwner)
                {
                    return false;
                }
            }
        }

        return true;
    }
}