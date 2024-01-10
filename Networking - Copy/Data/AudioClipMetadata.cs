using Unity.Netcode;

namespace DiscJockey.Networking.Data;

public struct AudioClipMetadata : INetworkSerializable
{
    public string Name;
    public int Frequency;
    public int Channels;
    public float Length;

    public AudioClipMetadata(string name, int frequency, int channels, float length)
    {
        Name = name;
        Frequency = frequency;
        Channels = channels;
        Length = length;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Frequency);
        serializer.SerializeValue(ref Channels);
        serializer.SerializeValue(ref Length);
    }
}