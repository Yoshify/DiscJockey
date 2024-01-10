using Concentus.Structs;
using DiscJockey.Audio.Data;

namespace DiscJockey.Audio;

public class AudioDecoder
{
    private readonly OpusDecoder _opusDecoder;

    public AudioDecoder(AudioFormat audioFormat)
    {
        AudioFormat = audioFormat;
        DiscJockeyPlugin.LogInfo(audioFormat.ToString());
        _opusDecoder = new OpusDecoder(AudioFormat.SamplingRate, AudioFormat.Channels);
    }

    public AudioFormat AudioFormat { get; }

    public float[] Decode(byte[] compressedFrame)
    {
        var decodedFrame = new float[AudioFormat.FrameSize * AudioFormat.Channels];
        _opusDecoder.Decode(compressedFrame, 0, compressedFrame.Length, decodedFrame, 0, AudioFormat.FrameSize);
        return decodedFrame;
    }

    public void Reset()
    {
        _opusDecoder.ResetState();
    }
}