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
            BoomboxManager.EnableInteractionWithBoombox(boombox.NetworkObjectId);
    }

    [HarmonyPatch(typeof(GrabbableObject), "DiscardItemOnClient")]
    [HarmonyPostfix]
    private static void DiscardItemOnClientPatch(GrabbableObject __instance)
    {
        if (__instance is BoomboxItem && __instance.IsOwner && BoomboxManager.IsLookingAtOrHoldingBoombox)
            BoomboxManager.DisableInteraction();
    }
}