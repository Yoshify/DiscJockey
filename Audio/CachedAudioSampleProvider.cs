using System;
using NAudio.Wave;

namespace DiscJockey.Audio;

public class CachedAudioSampleProvider : ISampleProvider
{
    private readonly CachedAudio _cachedAudio;
    private long _position;

    public CachedAudioSampleProvider(CachedAudio cachedAudio)
    {
        _cachedAudio = cachedAudio;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = _cachedAudio.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(_cachedAudio.AudioData, offset, buffer, 0, samplesToCopy);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }

    public WaveFormat WaveFormat => _cachedAudio.Format;
}