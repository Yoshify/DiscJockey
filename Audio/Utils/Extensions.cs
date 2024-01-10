using DiscJockey.Audio.Data;
using DiscJockey.Data;

namespace DiscJockey.Networking.Audio.Utils;

public static class Extensions
{
    public static TrackMetadata ExtractMetadata(this Track track)
    {
        return new TrackMetadata(track.Id, track.IndexInTracklist, track.OwnerId, track.OwnerName,
            track.Audio.Name, track.Audio.Length, track.Audio.LengthInSamples);
    }
}