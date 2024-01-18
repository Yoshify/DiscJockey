using System;
using DiscJockey.Networking;

namespace DiscJockey.Events;

public class StreamStartedEventArgs : EventArgs
{
    public ulong SenderId { get; private set; }
    public ulong NetworkedBoomboxId { get; private set; }
    public StreamInformation StreamInformation { get; private set; }

    public StreamStartedEventArgs(ulong senderId, ulong networkedBoomboxId, StreamInformation streamInformation)
    {
        SenderId = senderId;
        NetworkedBoomboxId = networkedBoomboxId;
        StreamInformation = streamInformation;
    }
}