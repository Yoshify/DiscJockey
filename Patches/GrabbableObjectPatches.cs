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
            
            BoomboxManager.OnDroppedBoombox();
        }
    }
    
    [HarmonyPatch(typeof(GrabbableObject), "EnablePhysics")]
    [HarmonyPrefix]
    private static bool EnablePhysicsPatch(GrabbableObject __instance, bool enable)
    {
        if (__instance is BoomboxItem)
        {
            if (enable)
            {
                __instance.customGrabTooltip = $"Grab Boombox:  [E]\n{InputManager.OpenDiscJockeyTooltip}";
            }
            else
            {
                __instance.customGrabTooltip = InputManager.OpenDiscJockeyTooltip;
                return false;
            }
        }

        return true;
    }
}