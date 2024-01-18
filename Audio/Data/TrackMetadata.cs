using System;
using Unity.Netcode;

namespace DiscJockey.Audio.Data;

public struct TrackMetadata : INetworkSerializable, IEquatable<TrackMetadata>
{
    public string Id;
    public int IndexInOwnersTracklist;
    public ulong OwnerId;
    public string OwnerName;
    public string Name;
    public float LengthInSeconds;
    public int LengthInSamples;

    public TrackMetadata(string id, int indexInOwnersTracklist, ulong ownerId, string ownerName, string name,
        float lengthInSeconds, int lengthInSamples)
    {
        Id = id;
        IndexInOwnersTracklist = indexInOwnersTracklist;
        OwnerId = ownerId;
        OwnerName = ownerName;
        Name = name;
        LengthInSeconds = lengthInSeconds;
        LengthInSamples = lengthInSamples;
    }

    public override string ToString()
    {
        return $"TrackMetadata<Id({Id}), Name({Name}), Index({IndexInOwnersTracklist}), Owner(#{OwnerId}: {OwnerName}, Length({LengthInSeconds}), LengthInSamples({LengthInSamples})>";
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref IndexInOwnersTracklist);
        serializer.SerializeValue(ref OwnerId);
        serializer.SerializeValue(ref OwnerName);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref LengthInSeconds);
        serializer.SerializeValue(ref LengthInSamples);
    }

    public bool Equals(TrackMetadata other)
    {
        return Id == other.Id && IndexInOwnersTracklist == other.IndexInOwnersTracklist && OwnerId == other.OwnerId &&
               OwnerName == other.OwnerName && Name == other.Name && LengthInSeconds.Equals(other.LengthInSeconds) &&
               LengthInSamples == other.LengthInSamples;
    }

    public override bool Equals(object obj)
    {
        return obj is TrackMetadata other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, IndexInOwnersTracklist, OwnerId, OwnerName, Name, LengthInSeconds, LengthInSamples);
    }
}