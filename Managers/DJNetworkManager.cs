using System;
using System.Collections.Generic;
using DiscJockey.Audio.Data;
using DiscJockey.Data;
using DiscJockey.Events;
using DiscJockey.Networking;
using DiscJockey.Networking.Audio;
using Unity.Netcode;

namespace DiscJockey.Managers;

public class DJNetworkManager : NetworkBehaviour
{
    public static DJNetworkManager Instance;
    public static readonly Dictionary<ulong, NetworkedBoombox> Boomboxes = new();
    
    public override void OnDestroy()
    {
        Boomboxes.Clear();
        base.OnDestroy();
    }
    
    public static event Action<StreamStartedEventArgs> OnStreamStarted;
    public static event Action<StreamStoppedEventArgs> OnStreamStopped;

    [ClientRpc]
    public void RegisterBoomboxClientRpc(ulong boomboxId)
    {
        if (Boomboxes.ContainsKey(boomboxId))
        {
            DiscJockeyPlugin.LogWarning(
                $"Boombox {boomboxId} is already registered!");
            return;
        }

        var boomboxItem = NetworkManager.Singleton.SpawnManager.SpawnedObjects[boomboxId].GetComponent<BoomboxItem>();
        Boomboxes.Add(boomboxId, new NetworkedBoombox(boomboxItem));
        DiscJockeyPlugin.LogInfo($"Boombox {boomboxId} registered");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterBoomboxServerRpc(ulong boomboxId)
    {
        RegisterBoomboxClientRpc(boomboxId);
    }

    [ClientRpc]
    public void UnregisterBoomboxClientRpc(ulong boomboxId)
    {
        if (!Boomboxes.ContainsKey(boomboxId))
        {
            DiscJockeyPlugin.LogWarning(
                "Attempted to unregister a Boombox that was never registered!");
            return;
        }

        if (Boomboxes.Remove(boomboxId))
            DiscJockeyPlugin.LogInfo(
                $"Boombox {boomboxId} unregistered");
        else
            DiscJockeyPlugin.LogError(
                $"Failed to unregister Boombox {boomboxId}!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnregisterBoomboxServerRpc(ulong boomboxId)
    {
        UnregisterBoomboxClientRpc(boomboxId);
    }

    [ClientRpc]
    public void ReceiveAudioPacketClientRpc(ulong networkedBoomboxId, NetworkedAudioPacket data)
    {
        if (Boomboxes.TryGetValue(networkedBoomboxId, out var boombox))
        {
            boombox.ReceiveStreamPacket(data);
        }
        else
        {
            DiscJockeyPlugin.LogError($"Can't receive stream packet as {networkedBoomboxId} is not a registered boombox!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendAudioPacketServerRpc(ulong networkedBoomboxId, NetworkedAudioPacket data)
    {
        ReceiveAudioPacketClientRpc(networkedBoomboxId, data);
    }

    [ClientRpc]
    public void ReceiveBoomboxVolumeChangeRequestClientRpc(ulong networkedBoomboxId, float volume)
    {
        if (Boomboxes.TryGetValue(networkedBoomboxId, out var boombox))
        {
            boombox.SetVolume(volume);
        }
        else
        {
            DiscJockeyPlugin.LogError($"Can't change volume as {networkedBoomboxId} is not a registered boombox!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestBoomboxVolumeChangeServerRpc(ulong networkedBoomboxId, float volume)
    {
        ReceiveBoomboxVolumeChangeRequestClientRpc(networkedBoomboxId, volume);
    }

    [ClientRpc]
    public void ReceiveBoomboxPlaybackModeChangeRequestClientRpc(ulong networkedBoomboxId, BoomboxPlaybackMode mode)
    {
        if (Boomboxes.TryGetValue(networkedBoomboxId, out var boombox))
        {
            boombox.SetPlaybackMode(mode);
        }
        else
        {
            DiscJockeyPlugin.LogError($"Can't change playback mode as {networkedBoomboxId} is not a registered boombox!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestBoomboxPlaybackModeChangeServerRpc(ulong networkedBoomboxId, BoomboxPlaybackMode mode)
    {
        ReceiveBoomboxPlaybackModeChangeRequestClientRpc(networkedBoomboxId, mode);
    }
    
    [ClientRpc]
    public void NotifyStreamStartedClientRpc(ulong senderId, ulong networkedBoomboxId, StreamInformation streamInformation)
    {
        if (Boomboxes.TryGetValue(networkedBoomboxId, out var boombox))
        {
            boombox.ListenToStream(senderId, streamInformation);
            OnStreamStarted?.Invoke(new StreamStartedEventArgs(senderId, networkedBoomboxId, streamInformation));
        }
        else
        {
            DiscJockeyPlugin.LogError($"Can't listen to stream as {networkedBoomboxId} is not a registered boombox!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyStreamStartedServerRpc(ulong senderId, ulong networkedBoomboxId, StreamInformation streamInformation)
    {
        NotifyStreamStartedClientRpc(senderId, networkedBoomboxId, streamInformation);
    }

    [ClientRpc]
    public void NotifyStreamStoppedClientRpc(ulong senderId, ulong networkedBoomboxId)
    {
        if (Boomboxes.TryGetValue(networkedBoomboxId, out var boombox))
        {
            boombox.StopListeningToStream();
            OnStreamStopped?.Invoke(new StreamStoppedEventArgs(senderId, networkedBoomboxId));
        }
        else
        {
            DiscJockeyPlugin.LogError($"Can't stop {networkedBoomboxId} is not a registered boombox!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyStreamStoppedServerRpc(ulong senderId, ulong networkedBoomboxId)
    {
        NotifyStreamStoppedClientRpc(senderId, networkedBoomboxId);
    }
    
    public override void OnNetworkSpawn()
    {
        if (Instance == null) Instance = this;
    }
}