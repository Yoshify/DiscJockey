using DiscJockey.Managers;
using HarmonyLib;

namespace DiscJockey.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatches
    {
        [HarmonyPatch(typeof(GrabbableObject), "EquipItem")]
        [HarmonyPostfix]
        static void EquipItemPatch(GrabbableObject __instance)
        {
            if(__instance is BoomboxItem boombox && boombox.IsOwner)
            {
                DiscJockeyBoomboxManager.EnableInteractionWithBoombox(boombox);
            }
        }
        
        [HarmonyPatch(typeof(GrabbableObject), "DiscardItemOnClient")]
        [HarmonyPostfix]
        static void DiscardItemOnClientPatch(GrabbableObject __instance)
        {
            if(__instance is BoomboxItem && __instance.IsOwner && DiscJockeyBoomboxManager.InteractionsActive)
            {
                DiscJockeyBoomboxManager.DisableInteraction();
            }
        }
    }
}
