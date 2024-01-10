using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using DiscJockey.Audio;
using DiscJockey.Input;
using DiscJockey.Input.Utils;
using DiscJockey.Managers;
using HarmonyLib;
using UnityEngine;

namespace DiscJockey.Patches;

[HarmonyPatch(typeof(BoomboxItem))]
internal class BoomboxItemPatches
{
    [HarmonyPatch(typeof(BoomboxItem), "Start")]
    [HarmonyPostfix]
    private static void StartPatch(BoomboxItem __instance)
    {
        DiscJockeyPlugin.LogInfo("BoomboxItemPatches<StartPatch>: Registering Boombox");
        DJNetworkManager.Instance.RegisterBoomboxServerRpc(__instance.NetworkObjectId);
        DiscJockeyPlugin.LogInfo("BoomboxItemPatches<StartPatch>: Assigning tooltip");
        __instance.itemProperties.canBeGrabbedBeforeGameStart = true;

        if (!__instance.itemProperties.toolTips.Contains(InputManager.OpenDiscJockeyTooltip))
            __instance.itemProperties.toolTips = __instance.itemProperties.toolTips.AddItem(InputManager.OpenDiscJockeyTooltip).ToArray();

        if (DiscJockeyConfig.SyncedConfig.DisableBatteryDrain) __instance.itemProperties.batteryUsage = int.MaxValue;
    }

    [HarmonyPatch(typeof(BoomboxItem), "StartMusic")]
    [HarmonyPrefix]
    public static bool StartMusicPatch(bool startMusic, BoomboxItem __instance)
    {
        DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Called, startMusic is {startMusic}");

        if (!AudioManager.IsReady() || !BoomboxManager.IsLookingAtOrHoldingBoombox) return false;

        DiscJockeyPlugin.LogInfo("BoomboxItemPatches<StartMusicPatch>: DiscJockeyAudioManager ready");
        if (startMusic)
        {
            if (BoomboxManager.LookedAtOrHeldBoombox.IsPlaying ||
                BoomboxManager.LookedAtOrHeldBoombox.IsStreaming)
            {
                DiscJockeyPlugin.LogInfo("BoomboxItemPatches<StartMusicPatch>: Stop Music requested");
                AudioManager.RequestStopTrack();
            }
            else
            {
                if (AudioManager.TrackList.HasAnyTracks)
                {
                    DiscJockeyPlugin.LogInfo(
                        "BoomboxItemPatches<StartMusicPatch>: Start Music requested, requesting random track");
                    var randomTrack = AudioManager.TrackList.GetRandomTrack();
                    AudioManager.RequestPlayTrack(randomTrack);
                }
            }
        }
        else
        {
            DiscJockeyPlugin.LogInfo("BoomboxItem<StartMusic>: Stop Music requested");
            AudioManager.RequestStopTrack();
        }

        __instance.isBeingUsed = startMusic;
        __instance.isPlayingMusic = startMusic;

        return false;
    }

    [HarmonyPatch(typeof(BoomboxItem), "PocketItem")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PocketItemTranspilerPatch(IEnumerable<CodeInstruction> codeInstructions)
    {
        return new CodeMatcher(codeInstructions)
            .SearchForward(i => i.Calls(AccessTools.Method(typeof(GrabbableObject), "PocketItem")))
            .Advance(1)
            .Set(OpCodes.Ret, null)
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(BoomboxItem), "PocketItem")]
    [HarmonyPostfix]
    private static void PocketItemPatch(BoomboxItem __instance)
    {
        if (__instance.IsOwner && BoomboxManager.IsLookingAtOrHoldingBoombox)
            BoomboxManager.DisableInteraction();
    }
}