using DiscJockey.Audio;
using DiscJockey.Input;
using DiscJockey.Managers;
using DiscJockey.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiscJockey.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerBPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static void ConnectClientToPlayerObjectPatch(PlayerControllerB __instance)
    {
        AudioManager.TrackList.TakeOwnershipOfTracklist(
            __instance.playerClientId,
            GameUtils.GetPlayerName(__instance.playerClientId)
        );

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("DiscJockey_OnRequestConfigSync", DiscJockeyConfig.OnRequestSync);
            DiscJockeyConfig.Synced = true;
            return;
        }

        DiscJockeyConfig.Synced = false;
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("DiscJockey_OnReceiveConfigSync", DiscJockeyConfig.OnReceiveSync);
        DiscJockeyConfig.RequestSync();
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
    [HarmonyPrefix]
    private static void HoverTipPrefixPatch(PlayerControllerB __instance)
    {
        if (!__instance.isGrabbingObjectAnimation && !__instance.inSpecialInteractAnimation && !__instance.inTerminalMenu)
        {
            __instance.interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            if (Physics.Raycast(__instance.interactRay, out __instance.hit, __instance.grabDistance, __instance.interactableObjectsMask) && __instance.hit.collider.gameObject.layer != 8)
            {
                if (__instance.hit.collider.gameObject.name.Contains("Boombox"))
                {
                    if (__instance.hit.collider.gameObject.TryGetComponent<BoomboxItem>(out var boombox))
                    {
                        boombox.customGrabTooltip = boombox.isHeld ? InputManager.OpenDiscJockeyTooltip : $"Grab Boombox:  [E]\n{InputManager.OpenDiscJockeyTooltip}";
                        BoomboxManager.OnLookedAtBoombox(boombox);
                        return;
                    }
                }
            }

            if (!UIManager.Instance.UIPanelActive && BoomboxManager.IsLookingAtBoombox)
            {
                BoomboxManager.OnLookedAwayFromBoombox();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Interact_performed")]
    [HarmonyPrefix]
    private static bool InteractPerformedPrefixPatch(PlayerControllerB __instance)
    {
        return !UIManager.Instance.UIPanelActive;
    }
}