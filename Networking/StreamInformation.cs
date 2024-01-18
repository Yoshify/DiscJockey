using DiscJockey.Audio.Data;
using Unity.Netcode;

namespace DiscJockey.Networking;

public struct StreamInformation : INetworkSerializable
{
    public TrackMetadata TrackMetadata;
    public AudioFormat AudioFormat;

    public StreamInformation(TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        TrackMetadata = trackMetadata;
        AudioFormat = audioFormat;
    }


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        TrackMetadata.NetworkSerialize(serializer);
        AudioFormat.NetworkSerialize(serializer);
    }
}