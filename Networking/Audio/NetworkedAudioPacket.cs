using DiscJockey.Audio.Data;
using Unity.Netcode;

namespace DiscJockey.Networking.Audio;

public struct NetworkedAudioPacket : INetworkSerializable
{
    public byte[] Frame;
    public TrackMetadata TrackMetadata;
    public AudioFormat AudioFormat;


    public NetworkedAudioPacket(byte[] frame, TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        Frame = frame;
        TrackMetadata = trackMetadata;
        AudioFormat = audioFormat;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Frame);
        TrackMetadata.NetworkSerialize(serializer);
        AudioFormat.NetworkSerialize(serializer);
    }
}