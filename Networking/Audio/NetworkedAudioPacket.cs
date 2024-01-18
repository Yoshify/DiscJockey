using DiscJockey.Audio.Data;
using Unity.Netcode;

namespace DiscJockey.Networking.Audio;

public struct NetworkedAudioPacket : INetworkSerializable
{
    public byte[] Frame;
    public StreamInformation StreamInformation;


    public NetworkedAudioPacket(byte[] frame, StreamInformation streamInformation)
    {
        Frame = frame;
        StreamInformation = streamInformation;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Frame);
        StreamInformation.NetworkSerialize(serializer);
    }
}