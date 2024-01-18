using System;
using DiscJockey.Networking;
using DiscJockey.Networking.Audio;

namespace DiscJockey.Events;

public class StreamTransmitEventArgs : EventArgs
{
    public ulong SenderId { get; private set; }
    public ulong NetworkedBoomboxId { get; private set; }

    public NetworkedAudioPacket AudioPacket { get; private set; }

    public StreamTransmitEventArgs(ulong senderId, ulong networkedBoomboxId, NetworkedAudioPacket audioPacket)
    {
        SenderId = senderId;
        NetworkedBoomboxId = networkedBoomboxId;
        AudioPacket = audioPacket;
    }
}