using System;
using DiscJockey.Input;
using DiscJockey.Managers;
using DiscJockey.Utils;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiscJockey.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
internal class GameNetworkManagerPatches
{
    public static GameObject DJNetworkManagerPrefab;
    public static GameObject HostDJNetworkManager;

    public static event Action OnDisconnect;

    [HarmonyPatch(typeof(GameNetworkManager), "Start")]
    [HarmonyPrefix]
    public static void StartPatch()
    {
        DJNetworkManagerPrefab = AssetLoader.NetworkManagerPrefab;
        DiscJockeyPlugin.LogInfo(
            $"GameNetworkManagerPatches<StartPatch>: Loaded DiscJockeyNetworkManager Prefab? {DJNetworkManagerPrefab != null}");

        if (DJNetworkManagerPrefab != null)
        {
            NetworkManager.Singleton.AddNetworkPrefab(DJNetworkManagerPrefab);
            DiscJockeyPlugin.LogInfo(
                "GameNetworkManagerPatches<StartPatch>: Registered DiscJockeyNetworkManager Prefab");
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
    [HarmonyPrefix]
    public static void StartDisconnectPatch()
    {
        try
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                DiscJockeyPlugin.LogInfo("GameNetworkManagerPatches<StartDisconnectPatch>: Destroying NetworkManager");
                Object.Destroy(HostDJNetworkManager);
                HostDJNetworkManager = null;
            }
        }
        catch
        {
            DiscJockeyPlugin.LogError(
                "GameNetworkManagerPatches<StartDisconnectPatch>: Failed to destroy NetworkManager");
        }

        if (BoomboxManager.IsLookingAtOrHoldingBoombox)
        {
            if (BoomboxManager.LookedAtOrHeldBoombox.IsStreaming)
            {
                BoomboxManager.LookedAtOrHeldBoombox.StopStreamAndPlaybackAndNotify();
            }
        }
        
        BoomboxManager.OnDroppedBoombox();
        BoomboxManager.OnLookedAwayFromBoombox();
        InputManager.EnablePlayerInteractions();
        DiscJockeyConfig.RevertSync();

        OnDisconnect?.Invoke();
    }
}