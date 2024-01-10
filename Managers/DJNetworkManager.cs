using System.Collections.Generic;
using DiscJockey.Audio.Data;
using DiscJockey.Data;
using DiscJockey.Networking;
using DiscJockey.Networking.Audio;
using Unity.Netcode;

namespace DiscJockey.Managers;

public class DJNetworkManager : NetworkBehaviour
{
    public delegate void AudioStreamEventHandler(ulong senderClientId, ulong networkedBoomboxId);

    public delegate void AudioStreamStartEventHandler(ulong senderClientId, ulong networkedBoomboxId,
        TrackMetadata trackMetadata, AudioFormat audioFormat);

    public delegate void AudioStreamTransmitEventHandler(ulong networkedBoomboxId, NetworkedAudioPacket audioPacket);

    public delegate void BoomboxPlaybackModeChangedEventHandler(ulong networkedBoomboxId, BoomboxPlaybackMode mode);

    public delegate void BoomboxVolumeChangedEventHandler(ulong networkedBoomboxId, float volume);

    public static DJNetworkManager Instance;
    public static readonly Dictionary<ulong, NetworkedBoombox> Boomboxes = new();


    public override void OnDestroy()
    {
        Boomboxes.Clear();

        base.OnDestroy();
    }

    public static event AudioStreamStartEventHandler OnAudioStreamTransmitStarted;
    public static event AudioStreamEventHandler OnAudioStreamTransmitStopped;
    public static event AudioStreamStartEventHandler OnAudioStreamPlaybackStarted;
    public static event AudioStreamEventHandler OnAudioStreamPlaybackStopped;
    public static event AudioStreamTransmitEventHandler OnAudioStreamPacketReceived;

    public static event BoomboxVolumeChangedEventHandler OnBoomboxVolumeChanged;
    public static event BoomboxPlaybackModeChangedEventHandler OnBoomboxPlaybackModeChanged;

    [ClientRpc]
    public void RegisterBoomboxClientRpc(ulong boomboxId)
    {
        if (Boomboxes.ContainsKey(boomboxId))
        {
            DiscJockeyPlugin.LogWarning(
                $"DiscJockeyNetworkManager<RegisterBoomboxClientRpc>: Boombox {boomboxId} is already registered!");
            return;
        }

        var boomboxItem = NetworkManager.Singleton.SpawnManager.SpawnedObjects[boomboxId].GetComponent<BoomboxItem>();
        Boomboxes.Add(boomboxId, new NetworkedBoombox(boomboxItem));
        DiscJockeyPlugin.LogInfo($"DiscJockeyNetworkManager<RegisterBoomboxClientRpc>: Boombox {boomboxId} registered");
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
                "DiscJockeyNetworkManager<UnregisterBoomboxClientRpc>: Attempted to unregister a Boombox that was never registered!");
            return;
        }

        if (Boomboxes.Remove(boomboxId))
            DiscJockeyPlugin.LogInfo(
                $"DiscJockeyNetworkManager<UnregisterBoomboxClientRpc>: Boombox {boomboxId} unregistered");
        else
            DiscJockeyPlugin.LogError(
                $"DiscJockeyNetworkManager<UnregisterBoomboxClientRpc>: Failed to unregister Boombox {boomboxId}!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnregisterBoomboxServerRpc(ulong boomboxId)
    {
        UnregisterBoomboxClientRpc(boomboxId);
    }

    [ClientRpc]
    public void ReceiveAudioPacketClientRpc(ulong networkedBoomboxId, NetworkedAudioPacket data)
    {
        OnAudioStreamPacketReceived?.Invoke(networkedBoomboxId, data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendAudioPacketServerRpc(ulong networkedBoomboxId, NetworkedAudioPacket data)
    {
        ReceiveAudioPacketClientRpc(networkedBoomboxId, data);
    }

    [ClientRpc]
    public void ReceiveAudioTransmissionStreamStartedClientRpc(ulong senderClientId, ulong networkedBoomboxId,
        TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        OnAudioStreamTransmitStarted?.Invoke(senderClientId, networkedBoomboxId, trackMetadata, audioFormat);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendAudioStreamTransmissionStartedServerRpc(ulong senderClientId, ulong networkedBoomboxId,
        TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        ReceiveAudioTransmissionStreamStartedClientRpc(senderClientId, networkedBoomboxId, trackMetadata, audioFormat);
    }

    [ClientRpc]
    public void ReceiveAudioStreamTransmissionStoppedClientRpc(ulong senderClientId, ulong networkedBoomboxId)
    {
        OnAudioStreamTransmitStopped?.Invoke(senderClientId, networkedBoomboxId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendAudioStreamTransmissionStoppedServerRpc(ulong senderClientId, ulong networkedBoomboxId)
    {
        ReceiveAudioStreamTransmissionStoppedClientRpc(senderClientId, networkedBoomboxId);
    }

    [ClientRpc]
    public void ReceiveBoomboxVolumeChangeRequestClientRpc(ulong networkedBoomboxId, float volume)
    {
        OnBoomboxVolumeChanged?.Invoke(networkedBoomboxId, volume);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestBoomboxVolumeChangeServerRpc(ulong networkedBoomboxId, float volume)
    {
        ReceiveBoomboxVolumeChangeRequestClientRpc(networkedBoomboxId, volume);
    }

    [ClientRpc]
    public void ReceiveBoomboxPlaybackModeChangeRequestClientRpc(ulong networkedBoomboxId, BoomboxPlaybackMode mode)
    {
        OnBoomboxPlaybackModeChanged?.Invoke(networkedBoomboxId, mode);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestBoomboxPlaybackModeChangeServerRpc(ulong networkedBoomboxId, BoomboxPlaybackMode mode)
    {
        ReceiveBoomboxPlaybackModeChangeRequestClientRpc(networkedBoomboxId, mode);
    }


    [ClientRpc]
    public void ReceiveAudioStreamPlaybackStartedClientRpc(ulong senderClientId, ulong networkedBoomboxId,
        TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        OnAudioStreamPlaybackStarted?.Invoke(senderClientId, networkedBoomboxId, trackMetadata, audioFormat);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendAudioStreamPlaybackStartedServerRpc(ulong senderClientId, ulong networkedBoomboxId,
        TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        ReceiveAudioStreamPlaybackStartedClientRpc(senderClientId, networkedBoomboxId, trackMetadata, audioFormat);
    }

    [ClientRpc]
    public void ReceiveAudioStreamPlaybackStoppedClientRpc(ulong senderClientId, ulong networkedBoomboxId)
    {
        OnAudioStreamPlaybackStopped?.Invoke(senderClientId, networkedBoomboxId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendAudioStreamPlaybackStoppedServerRpc(ulong senderClientId, ulong networkedBoomboxId)
    {
        ReceiveAudioStreamPlaybackStoppedClientRpc(senderClientId, networkedBoomboxId);
    }

    public override void OnNetworkSpawn()
    {
        if (Instance == null) Instance = this;
    }
}