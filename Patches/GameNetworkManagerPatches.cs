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
        DiscJockeyPlugin.LogInfo($"Loaded DiscJockeyNetworkManager Prefab? {DJNetworkManagerPrefab != null}");

        if (DJNetworkManagerPrefab != null)
        {
            NetworkManager.Singleton.AddNetworkPrefab(DJNetworkManagerPrefab);
            DiscJockeyPlugin.LogInfo("Registered DiscJockeyNetworkManager Prefab");
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
    [HarmonyPrefix]
    public static void StartDisconnectPatch()
    {
        foreach (var boombox in DJNetworkManager.Boomboxes.Values)
        {
            boombox.StopStreamAndNotify();
        }
        
        BoomboxManager.OnDroppedOrPocketedBoombox();
        BoomboxManager.OnLookedAwayFromBoombox();
        InputManager.EnablePlayerInteractions();
        DiscJockeyConfig.RevertSync();
        
        try
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                DiscJockeyPlugin.LogInfo("Destroying DiscJockeyNetworkManager");
                Object.Destroy(HostDJNetworkManager);
                HostDJNetworkManager = null;
            }
        }
        catch
        {
            DiscJockeyPlugin.LogError("Failed to destroy DiscJockeyNetworkManager");
        }

        OnDisconnect?.Invoke();
    }
}