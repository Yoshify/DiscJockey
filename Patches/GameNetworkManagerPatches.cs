using DiscJockey.Managers;
using HarmonyLib;
using System;
using DiscJockey.Utils;
using Unity.Netcode;
using UnityEngine;

namespace DiscJockey.Patches
{
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
            DJNetworkManagerPrefab = AssetUtils.NetworkManagerPrefab;
            DiscJockeyPlugin.LogInfo($"GameNetworkManagerPatches<StartPatch>: Loaded DiscJockeyNetworkManager Prefab? {DJNetworkManagerPrefab != null}");

            if (DJNetworkManagerPrefab != null )
            {
                NetworkManager.Singleton.AddNetworkPrefab(DJNetworkManagerPrefab);
                DiscJockeyPlugin.LogInfo("GameNetworkManagerPatches<StartPatch>: Registered DiscJockeyNetworkManager Prefab");
            }
        }
        
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        [HarmonyPrefix]
        public static void StartDisconnectPatch()
        {
            if (DiscJockeyNetworkManager.Instance.PlayerHasPendingDownloadTask(LocalPlayerHelper.Player.playerClientId,
                    out var tasks))
            {
                foreach (var task in tasks)
                {
                    DiscJockeyNetworkManager.Instance.NotifyPlayerFailedDownloadTaskServerRpc(LocalPlayerHelper.Player.playerClientId, task);
                }
            }
            
            try
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    DiscJockeyPlugin.LogInfo("GameNetworkManagerPatches<StartDisconnectPatch>: Destroying NetworkManager");
                    UnityEngine.Object.Destroy(HostDJNetworkManager);
                    HostDJNetworkManager = null;
                }
            }
            catch
            {
                DiscJockeyPlugin.LogError("GameNetworkManagerPatches<StartDisconnectPatch>: Failed to destroy NetworkManager");
            }

            DiscJockeyBoomboxManager.DisableInteraction();
            DiscJockeyInputManager.EnablePlayerInteractions();
            
            OnDisconnect?.Invoke();
        }
    }
}
