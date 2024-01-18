using System;
using DiscJockey.Audio;

namespace DiscJockey.Data;

public class Track
{
    public Track(CachedAudio audio)
    {
        Id = Guid.NewGuid().ToString();
        Audio = audio;
    }

    public string Id { get; private set; }
    public int IndexInTracklist { get; set; }
    public CachedAudio Audio { get; }
    public ulong OwnerId { get; private set; }
    public string OwnerName { get; private set; }

    public void TakeOwnership(ulong ownerId, string ownerName)
    {
        OwnerId = ownerId;
        OwnerName = ownerName;
    }

    public override string ToString()
    {
        return $"Track<Name({Audio.Name}), OwnerId({OwnerId}), OwnerName({OwnerName})>";
    }
}