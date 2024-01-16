using System.Linq;
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
            
            // TODO: This is a workaround to update controlTips on the Boombox with the latest keybinds
            // until we get a rebind event/callback out of InputUtils
            
            // Our tooltip should always be the last on the stack, so pop it off...
            var toolTips = boombox.itemProperties.toolTips.ToList();
            toolTips.RemoveAt(toolTips.Count - 1);
            
            // ...and add the latest
            toolTips.Add(InputManager.OpenDiscJockeyTooltip);
            boombox.itemProperties.toolTips = toolTips.ToArray();
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