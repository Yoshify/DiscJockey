using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using DiscJockey.Audio;
using DiscJockey.Input;
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
        __instance.itemProperties.canBeGrabbedBeforeGameStart = true;

        if (!__instance.itemProperties.toolTips.Contains(InputManager.OpenDiscJockeyTooltip))
            __instance.itemProperties.toolTips = __instance.itemProperties.toolTips.AddItem(InputManager.OpenDiscJockeyTooltip).ToArray();

        

        if (DiscJockeyConfig.LocalConfig.AddVanillaSongsToTracklist)
        {
            AudioManager.LoadVanillaMusicFrom(__instance);
        }

        if (DiscJockeyConfig.SyncedConfig.DisableBatteryDrain) __instance.itemProperties.batteryUsage = int.MaxValue;
    }

    [HarmonyPatch(typeof(BoomboxItem), "StartMusic")]
    [HarmonyPrefix]
    public static bool StartMusicPatch(bool startMusic, BoomboxItem __instance)
    {
        if (!AudioManager.IsReady() || !__instance.IsOwner || !DJNetworkManager.Boomboxes.TryGetValue(__instance.NetworkObjectId, out var networkedBoombox)) return false;
        
        if (startMusic)
        {
            if (networkedBoombox.IsPlaying || networkedBoombox.IsStreaming)
            {
                DiscJockeyPlugin.LogInfo("BoomboxItemPatches<StartMusicPatch>: Stop music requested");
                networkedBoombox.StopStreamAndPlaybackAndNotify();
            }
            else
            {
                if (AudioManager.TrackList.HasAnyTracks)
                {
                    if (networkedBoombox.ActiveTrackMetadata.HasValue && networkedBoombox.LocalClientOwnsCurrentTrack)
                    {
                        DiscJockeyPlugin.LogInfo(
                            "BoomboxItemPatches<StartMusicPatch>: Start music requested, we own the current or previous track, requesting next track");
                        networkedBoombox.StartStreamingTrack(AudioManager.TrackList.GetNextTrack(networkedBoombox.CurrentTrackIndexInOwnersTracklist, networkedBoombox.BoomboxPlaybackMode));
                    }
                    else
                    {
                        DiscJockeyPlugin.LogInfo("BoomboxItemPatches<StartMusicPatch>: Start music requested, we don't own the current or previous track, requesting first track in our list");
                        var firstTrack = AudioManager.TrackList.GetTrackAtIndex(0);
                        networkedBoombox.StartStreamingTrack(firstTrack);
                    }
                }
            }
        }
        else
        {
            DiscJockeyPlugin.LogInfo("BoomboxItem<StartMusicPatch>: Stop music requested");
            networkedBoombox.StopStreamAndPlaybackAndNotify();
        }

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
        if (__instance.IsOwner && BoomboxManager.IsHoldingBoombox)
            BoomboxManager.OnDroppedOrPocketedBoombox();
    }
}