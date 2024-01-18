using System;
using DiscJockey.Networking;

namespace DiscJockey.Events;

public class StreamStoppedEventArgs : EventArgs
{
    public ulong SenderId { get; private set; }
    public ulong NetworkedBoomboxId { get; private set; }

    public StreamStoppedEventArgs(ulong senderId, ulong networkedBoomboxId)
    {
        SenderId = senderId;
        NetworkedBoomboxId = networkedBoomboxId;
    }
}