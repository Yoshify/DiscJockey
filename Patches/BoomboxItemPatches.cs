using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using DiscJockey.Managers;
using DiscJockey.Utils;
using HarmonyLib;

namespace DiscJockey.Patches
{
    [HarmonyPatch(typeof(BoomboxItem))]
    internal class BoomboxItemPatches
    {
        [HarmonyPatch(typeof(BoomboxItem), "Start")]
        [HarmonyPostfix]
        private static void StartPatch(BoomboxItem __instance)
        {
            DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartPatch>: Registering Boombox");
            DiscJockeyNetworkManager.Instance.RegisterBoomboxServerRpc(__instance.NetworkObjectId);
            DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartPatch>: Assigning tooltip");
            __instance.itemProperties.canBeGrabbedBeforeGameStart = true;

            var panelHotkeyRaw = DiscJockeyConfig.DiscJockeyPanelHotkey.Value;
            var panelHotkeySplit = panelHotkeyRaw.Split('/');
            var panelHotkeyCleaned = string.Empty;
            
            // Input Actions should have a /, but lets check to be sure.
            panelHotkeyCleaned = panelHotkeySplit.Length == 1 ? panelHotkeySplit[0] : panelHotkeySplit[1];
            
            __instance.itemProperties.toolTips = __instance.itemProperties.toolTips.AddItem($"Open DiscJockey:    [{panelHotkeyCleaned}]").ToArray();
            __instance.boomboxAudio.loop = false;

            DiscJockeyNetworkManager.OnPlayTrackRequestReceived += (boombox, metadata) =>
            {
                if (__instance.NetworkObjectId == boombox.NetworkObjectId)
                {
                    __instance.PlayTrack(metadata.CurrentTrackMetadata.Index);
                }
            };
            
            DiscJockeyNetworkManager.OnStopTrackRequestReceived += (boombox, metadata) =>
            {
                if (__instance.NetworkObjectId == boombox.NetworkObjectId)
                {
                    __instance.StopTrack();
                }
            };

            DiscJockeyNetworkManager.OnScrubTrackRequestReceived += (boombox, metadata) =>
            {
                if (__instance.NetworkObjectId == boombox.NetworkObjectId)
                {
                    __instance.ScrubTrack(metadata.CurrentTrackMetadata.Progress);
                }
            };
        }
        
        [HarmonyPatch(typeof(BoomboxItem), "ItemActivate")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ItemActivatePatch(IEnumerable<CodeInstruction> codeInstructions)
        {
            var code = new List<CodeInstruction>(codeInstructions);
            var replacementIndex = -1;
            for(var i = 0; i < code.Count(); i++)
            {
                var currentInstruction = code[i];
                var nextInstruction = code[i + 1];

                if(currentInstruction.opcode == OpCodes.Ldarg_1 && nextInstruction.opcode == OpCodes.Ldc_I4_0)
                {
                    replacementIndex = i;
                    break;
                }
            }
            code.RemoveAt(replacementIndex);
            code.Insert(replacementIndex, new CodeInstruction(OpCodes.Ldarg_2));
            return code;
        }
        
        [HarmonyPatch(typeof(BoomboxItem), "StartMusic")]
        [HarmonyPrefix]
        public static bool StartMusicPatch(bool startMusic, BoomboxItem __instance)
        {
            DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Called, startMusic is {startMusic}");

            if (DiscJockeyAudioManager.IsReady())
            {
                DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: DiscJockeyAudioManager ready");
                if (startMusic)
                {
                    if (__instance.isPlayingMusic)
                    {
                        DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Stop Music requested");
                        __instance.isBeingUsed = false;
                        __instance.isPlayingMusic = false;
                        __instance.boomboxAudio.Stop();
                        __instance.boomboxAudio.PlayOneShot(__instance.stopAudios[UnityEngine.Random.Range(0, __instance.stopAudios.Length)]);
                    }
                    else
                    {
                        DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Start Music requested");
                        if (DiscJockeyBoomboxManager.InteractionsActive)
                        {
                            if (DiscJockeyBoomboxManager.ActiveBoomboxMetadata.CurrentTrackMetadata.TrackSelected)
                            {
                                DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Playing track from current selection: {DiscJockeyBoomboxManager.ActiveBoomboxMetadata}");
                                __instance.boomboxAudio.clip = DiscJockeyAudioManager.TrackList.GetTrackAtIndex(DiscJockeyBoomboxManager.ActiveBoomboxMetadata.CurrentTrackMetadata.Index).AudioClip;
                            }
                            else
                            {
                                DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Requesting random track");
                                var randomTrack = DiscJockeyAudioManager.TrackList.GetRandomTrack();
                                __instance.boomboxAudio.clip = randomTrack.AudioClip;
                                DiscJockeyAudioManager.RequestPlayTrack(randomTrack);
                            }
                        }
                        else
                        {
                            DiscJockeyPlugin.LogInfo($"BoomboxItemPatches<StartMusicPatch>: Requesting random track");
                            var randomTrack = DiscJockeyAudioManager.TrackList.GetRandomTrack();
                            __instance.boomboxAudio.clip = randomTrack.AudioClip;
                            DiscJockeyAudioManager.RequestPlayTrack(randomTrack);
                        }

                        __instance.boomboxAudio.pitch = 1f;
                        __instance.boomboxAudio.Play();
                        __instance.isBeingUsed = true;
                        __instance.isPlayingMusic = true;
                    }
                }
                else
                {
                    DiscJockeyPlugin.LogInfo($"BoomboxItem<StartMusic>: Stop Music requested");
                    __instance.isBeingUsed = false;
                    __instance.isPlayingMusic = false;
                    __instance.boomboxAudio.Stop();
                    __instance.boomboxAudio.PlayOneShot(__instance.stopAudios[UnityEngine.Random.Range(0, __instance.stopAudios.Length)]);
                }
                
            }
            else
            {
                __instance.isBeingUsed = startMusic;
                __instance.isPlayingMusic = startMusic;
            }
            return false;
        }
        
        [HarmonyPatch(typeof(BoomboxItem), "PocketItem")]
        [HarmonyPostfix]
        private static void PocketItemPatch(BoomboxItem __instance)
        {
            if (__instance.IsOwner && DiscJockeyBoomboxManager.InteractionsActive)
            {
                DiscJockeyBoomboxManager.DisableInteraction();
            }
        }
    }
}
