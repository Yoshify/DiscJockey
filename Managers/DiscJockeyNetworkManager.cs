using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using DiscJockey.API;
using DiscJockey.Data;
using DiscJockey.Utils;

namespace DiscJockey.Managers
{
    public class DiscJockeyNetworkManager : NetworkBehaviour
    {

        public static float LocalTimeDelta;
        private static float lastLocalTime = 0;

        public static DiscJockeyNetworkManager Instance;
        
        public static Dictionary<ulong, BoomboxMetadata> BoomboxNetworkMetadata = new Dictionary<ulong, BoomboxMetadata>();
        public static Dictionary<ulong, BoomboxItem> BoomboxInstances = new Dictionary<ulong, BoomboxItem>();

        public static event Action<BoomboxItem, BoomboxMetadata> OnPlayTrackRequestReceived;
        public static event Action<BoomboxItem, BoomboxMetadata> OnStopTrackRequestReceived;
        public static event Action<BoomboxItem, BoomboxMetadata> OnScrubTrackRequestReceived;
        public static event Action<ulong, string> OnPlayerCompletedDownloadTask;
        public static event Action<string> OnPlayerReceivedDownloadTask;
        public static event Action<ulong, string> OnPlayerFailedDownloadTask;
        public static event Action<string> OnAllPlayersCompletedDownloadTask;
        public static event Action OnSpawned;

        private static Dictionary<string, List<ulong>> PendingDownloadTasks = new Dictionary<string, List<ulong>>();

        [ClientRpc]
        public void RegisterBoomboxClientRpc(ulong boomboxId)
        {
            if (!BoomboxNetworkMetadata.ContainsKey(boomboxId))
            {
                DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RegisterBoomboxClientRpc>: Boombox {boomboxId} registered on Client");
                BoomboxNetworkMetadata.Add(boomboxId, BoomboxMetadata.Empty());
                BoomboxInstances.Add(boomboxId, NetworkManager.Singleton.SpawnManager.SpawnedObjects[boomboxId].GetComponent<BoomboxItem>());
            }
        }

 
        [ServerRpc(RequireOwnership = false)]
        public void RegisterBoomboxServerRpc(ulong boomboxId)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RegisterBoomboxServerRpc>: SERVER RECEIVED BOOMBOX: {boomboxId} - NOTIFYING CLIENTS");
            RegisterBoomboxClientRpc(boomboxId);
        }

        [ClientRpc]
        public void UpdateMetadataForBoomboxClientRpc(ulong boomboxId, BoomboxMetadata metadata)
        {
            if (!BoomboxNetworkMetadata.ContainsKey(boomboxId))
            {
                DiscJockeyPlugin.LogError($"DiscJockeyNetworkManager<UpdateMetadataForBoomboxClientRpc>: Can't update metadata for Boombox that isn't registered or does not exist!");
                return;
            }

            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<UpdateMetadataForBoomboxClientRpc>: Boombox {boomboxId} updated with data {metadata}");
            BoomboxNetworkMetadata[boomboxId] = metadata;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateMetadataForBoomboxServerRpc(ulong boomboxId, BoomboxMetadata metadata)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<UpdateMetadataForBoomboxServerRpc>: SERVER RECEIVED METADATA: {metadata} - NOTIFYING CLIENTS");
            UpdateMetadataForBoomboxClientRpc(boomboxId, metadata);
        }

        [ClientRpc]
        public void RequestPlayForBoomboxClientRpc(ulong boomboxId, BoomboxMetadata metadata)
        {
            if (!BoomboxNetworkMetadata.ContainsKey(boomboxId))
            {
                return;
            }

            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RequestPlayForBoomboxClientRpc>: CLIENT BOOMBOX {boomboxId} PLAY REQUEST RECEIVED");
            BoomboxNetworkMetadata[boomboxId] = metadata;
            OnPlayTrackRequestReceived?.Invoke(BoomboxInstances[boomboxId], metadata);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayForBoomboxServerRpc(ulong boomboxId, BoomboxMetadata metadata)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RequestPlayForBoomboxServerRpc>: SERVER BOOMBOX {boomboxId} PLAY REQUEST RECEIVED");
            RequestPlayForBoomboxClientRpc(boomboxId, metadata);
        }

        [ClientRpc]
        public void RequestStopForBoomboxClientRpc(ulong boomboxId)
        {
            if (!BoomboxNetworkMetadata.ContainsKey(boomboxId))
            {
                return;
            }

            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RequestStopForBoomboxClientRpc>: CLIENT BOOMBOX {boomboxId} STOP REQUEST RECEIVED");
            BoomboxNetworkMetadata[boomboxId] = BoomboxMetadata.Empty();
            OnStopTrackRequestReceived?.Invoke(BoomboxInstances[boomboxId], BoomboxNetworkMetadata[boomboxId]);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestStopForBoomboxServerRpc(ulong boomboxId)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RequestStopForBoomboxServerRpc>: SERVER BOOMBOX {boomboxId} STOP REQUEST RECEIVED");
            RequestStopForBoomboxClientRpc(boomboxId);
        }
        
        [ClientRpc]
        public void ScrubTrackOnBoomboxClientRpc(ulong boomboxId, TrackMetadata trackMetadata)
        {
            if (!BoomboxNetworkMetadata.ContainsKey(boomboxId))
            {
                return;
            }

            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RequestStopForBoomboxClientRpc>: CLIENT BOOMBOX {boomboxId} SCRUB REQUEST RECEIVED");
            var metadata = BoomboxNetworkMetadata[boomboxId];
            metadata.UpdateTrackMetadata(trackMetadata);
            BoomboxNetworkMetadata[boomboxId] = metadata;
            OnScrubTrackRequestReceived?.Invoke(BoomboxInstances[boomboxId], BoomboxNetworkMetadata[boomboxId]);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ScrubTrackOnBoomboxServerRpc(ulong boomboxId, TrackMetadata trackMetadata)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<ScrubTrackOnBoomboxServerRpc>: SERVER BOOMBOX {boomboxId} SCRUB REQUEST RECEIVED");
            ScrubTrackOnBoomboxClientRpc(boomboxId, trackMetadata);
        }

        [ClientRpc]
        public void DownloadYouTubeAudioClientRpc(string taskId, string url)
        {
            CreatePendingDownloadTask(taskId);
            OnPlayerReceivedDownloadTask?.Invoke(taskId);
            AudioLoaderAPI.DownloadAudioFromYouTubeUrl(taskId, url);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DownloadYouTubeAudioServerRpc(ulong requestingPlayerId, string taskId, string url)
        {
            var formattedPlayerNameText =
                GameUtils.GetColourFormattedText(GameUtils.GetPlayerName(requestingPlayerId), GameUtils.PlayerNameColour);
            GameUtils.LogDiscJockeyMessageToServer($"{formattedPlayerNameText} has requested a download for {url}");
            DownloadYouTubeAudioClientRpc(taskId, url);
        }
        
        [ClientRpc]
        public void NotifyDownloadTaskCompleteClientRpc(ulong playerId, string taskId)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<NotifyDownloadTaskCompleteClientRpc>: Player {playerId} has finished downloading task {taskId}");
            UpdatePendingDownloadTask(taskId, playerId);
            OnPlayerCompletedDownloadTask?.Invoke(playerId, taskId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void NotifyDownloadTaskCompleteServerRpc(ulong playerId, string taskId)
        {
            NotifyDownloadTaskCompleteClientRpc(playerId, taskId);
        }
        
        [ClientRpc]
        public void NotifyPlayerFailedDownloadTaskClientRpc(ulong playerId, string taskId)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<NotifyPlayerFailedDownloadTaskClientRpc>: Player {playerId} has failed downloading task {taskId}");
            CancelPendingTask(taskId);
            OnPlayerFailedDownloadTask?.Invoke(playerId, taskId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void NotifyPlayerFailedDownloadTaskServerRpc(ulong playerId, string taskId)
        {
            var formattedPlayerNameText =
                GameUtils.GetColourFormattedText(GameUtils.GetPlayerName(playerId), GameUtils.PlayerNameColour);
            GameUtils.LogDiscJockeyMessageToServer($"{formattedPlayerNameText} failed downloading a track. Cancelling the download.");
            NotifyPlayerFailedDownloadTaskClientRpc(playerId, taskId);
        }

        public override void OnNetworkSpawn()
        {
            if (Instance == null) Instance = this;
        }

        public bool PlayerHasPendingDownloadTask(ulong playerId, out List<string> pendingTasks)
        {
            var allPendingTasks = PendingDownloadTasks.Keys.ToList().Where(task => PendingDownloadTasks[task].Contains(playerId)).ToList();

            pendingTasks = allPendingTasks;
            
            return allPendingTasks.Count > 0;
        }

        private void CancelPendingTask(string taskId)
        {
            PendingDownloadTasks.Remove(taskId);
        }

        private void CreatePendingDownloadTask(string taskId)
        {
            var clients = StartOfRound.Instance.ClientPlayerList.Keys.ToList();
            PendingDownloadTasks.Add(taskId, clients);
            DiscJockeyPlugin.LogInfo(
                $"DiscJockeyNetworkManager<CreatePendingDownloadTask>: Created pending task {taskId} with client IDs {string.Join(", ", clients)}");
        }

        private void UpdatePendingDownloadTask(string taskId, ulong playerId)
        {
            if (!PendingDownloadTasks.ContainsKey(taskId))
            {
                DiscJockeyPlugin.LogError($"DiscJockeyNetworkManager<UpdatePendingDownloadTask>: Cannot update pending task {taskId} as it was never added as a pending task!");
                return;
            }

            if (PendingDownloadTasks[taskId] == null)
            {
                DiscJockeyPlugin.LogError($"DiscJockeyNetworkManager<UpdatePendingDownloadTask>: Cannot update pending task {taskId} as it has no pending players!");
                return;
            }
            
            if (PendingDownloadTasks[taskId].Count == 0)
            {
                DiscJockeyPlugin.LogWarning($"DiscJockeyNetworkManager<UpdatePendingDownloadTask>: Download task {taskId} should already be complete!");
                return;
            }
            
            if (!PendingDownloadTasks[taskId].Contains(playerId))
            {
                DiscJockeyPlugin.LogWarning($"DiscJockeyNetworkManager<UpdatePendingDownloadTask>: Player ID {playerId} has already completed download task {taskId} - why did this notify twice?");
                return;
            }

            PendingDownloadTasks[taskId].Remove(playerId);

            if (PendingDownloadTasks[taskId].Count == 0)
            {
                DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<UpdatePendingDownloadTask>: Task {taskId} complete. Clearing it out.");
                PendingDownloadTasks.Remove(taskId);
                OnAllPlayersCompletedDownloadTask?.Invoke(taskId);
            }
        }

        public override void OnDestroy()
        {
            PendingDownloadTasks.Clear();
            BoomboxInstances.Clear();
            BoomboxNetworkMetadata.Clear();
            
            base.OnDestroy();
        }

        private void UpdateActiveTrackProgresses()
        {
            foreach(var boomboxNetworkId in BoomboxNetworkMetadata.Keys.ToList())
            {
                var metadata = BoomboxNetworkMetadata[boomboxNetworkId];

                if(metadata.CurrentTrackMetadata.TrackSelected)
                {
                    metadata.CurrentTrackMetadata.Progress = BoomboxInstances[boomboxNetworkId].boomboxAudio.time;
                    BoomboxNetworkMetadata[boomboxNetworkId] = metadata;

                    if(metadata.CurrentTrackMetadata.Progress > metadata.CurrentTrackMetadata.Length)
                    {
                        DiscJockeyAudioManager.RequestPlayTrack(
                            DiscJockeyAudioManager.TrackList.GetNextTrack(
                                metadata.CurrentTrackMetadata.Index,
                                metadata.TrackMode));
                    }
                }
            }
        }

        private void Update()
        {
            var localTime = NetworkManager.Singleton.LocalTime.TimeAsFloat;
            LocalTimeDelta = localTime - lastLocalTime;
            lastLocalTime = localTime;

            UpdateActiveTrackProgresses();
        }
    }
}
