using System;
using Concentus.Enums;
using Concentus.Structs;
using DiscJockey.Audio.Data;

namespace DiscJockey.Audio;

public class AudioEncoder
{
    private readonly OpusEncoder _opusEncoder;

    public AudioEncoder(AudioFormat audioFormat, int bitrate, int complexity)
    {
        AudioFormat = audioFormat;
        Bitrate = bitrate;
        Complexity = complexity;

        _opusEncoder = new OpusEncoder(AudioFormat.SamplingRate, AudioFormat.Channels,
            OpusApplication.OPUS_APPLICATION_AUDIO)
        {
            Bitrate = bitrate,
            SignalType = OpusSignal.OPUS_SIGNAL_MUSIC,
            UseVBR = true,
            Complexity = complexity
        };
    }

    public AudioFormat AudioFormat { get; }
    public int Bitrate { get; }
    public int Complexity { get; }

    public byte[] Encode(float[] frame)
    {
        var compressedFrame = new byte[AudioFormat.FrameSize * AudioFormat.Channels];
        var len = _opusEncoder.Encode(frame, 0, AudioFormat.FrameSize, compressedFrame, 0, compressedFrame.Length);
        Array.Resize(ref compressedFrame, len);
        return compressedFrame;
    }

    public void Reset()
    {
        _opusEncoder.ResetState();
    }
}