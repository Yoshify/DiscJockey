using Unity.Netcode;

namespace DiscJockey.Networking.Data;

public struct NetworkedAudioPacket : INetworkSerializable
{
    public ulong TargetId;
    public byte[] Fragment;
    public bool EndOfStream;
    public int FragmentLength => Fragment.Length;

    public AudioClipMetadata AudioClipMetadata;
    public TrackMetadata TrackMetadata;


    public NetworkedAudioPacket(byte[] fragment, bool endOfStream, AudioClipMetadata audioClipMetadata, TrackMetadata trackMetadata)
    {
        Fragment = fragment;
        EndOfStream = endOfStream;
        AudioClipMetadata = audioClipMetadata;
        TrackMetadata = trackMetadata;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Fragment);
        serializer.SerializeValue(ref EndOfStream);
        AudioClipMetadata.NetworkSerialize(serializer);
        TrackMetadata.NetworkSerialize(serializer);
    }
}