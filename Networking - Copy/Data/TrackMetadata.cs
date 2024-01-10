using Unity.Netcode;

namespace DiscJockey.Networking.Data;

public struct TrackMetadata : INetworkSerializable
{
    public ulong OwnerId;
    public string OwnerName;

    public TrackMetadata(ulong ownerId, string ownerName)
    {
        OwnerId = ownerId;
        OwnerName = ownerName;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref OwnerId);
        serializer.SerializeValue(ref OwnerName);
    }
}