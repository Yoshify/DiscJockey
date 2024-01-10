using System;
using Unity.Netcode;

namespace DiscJockey.Audio.Data;

public struct AudioFormat : INetworkSerializable, IEquatable<AudioFormat>
{
    public int SamplingRate;
    public int MillisecondsPerFrame;
    public int AudioFramesPerSecond => 1000 / MillisecondsPerFrame;
    public int FrameSize => SamplingRate / AudioFramesPerSecond;
    public int Channels;

    public AudioFormat(int samplingRate = 48000, int frameSizeMs = 20, int channels = 2)
    {
        SamplingRate = samplingRate;
        Channels = channels;
        MillisecondsPerFrame = frameSizeMs;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SamplingRate);
        serializer.SerializeValue(ref MillisecondsPerFrame);
        serializer.SerializeValue(ref Channels);
    }

    public bool Equals(AudioFormat other)
    {
        return SamplingRate == other.SamplingRate && MillisecondsPerFrame == other.MillisecondsPerFrame &&
               Channels == other.Channels;
    }

    public override bool Equals(object obj)
    {
        return obj is AudioFormat other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SamplingRate, MillisecondsPerFrame, Channels);
    }

    public override string ToString()
    {
        return
            $"AudioFormat(SamplingRate<{SamplingRate}>, MsPerFrame<{MillisecondsPerFrame}>, AudioFPS<{AudioFramesPerSecond}>, FrameSize<{FrameSize}>, Channels<{Channels}>)";
    }
}