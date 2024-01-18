using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using UnityEngine;

namespace DiscJockey.Audio;

public class CachedAudio
{
    public CachedAudio()
    {
    }

    public float[] AudioData { get; private set; }
    public WaveFormat Format { get; private set; }
    public string Name { get; private set; }
    public float Length { get; private set; }
    public int LengthInSamples { get; private set; }

    public static CachedAudio FromAudioClip(AudioClip audioClip)
    {
        var cachedAudio = new CachedAudio();
        cachedAudio.Length = audioClip.length;
        cachedAudio.LengthInSamples = (int)(audioClip.frequency * audioClip.channels * audioClip.length);
        cachedAudio.Format = new WaveFormat(audioClip.frequency, audioClip.channels);
        cachedAudio.Name = audioClip.name;
        cachedAudio.AudioData = new float[cachedAudio.LengthInSamples];
        audioClip.GetData(cachedAudio.AudioData, 0);
        return cachedAudio;
    }

    private static void NormalizeAudio(float[] data)
    {
        var max = data.Select(Mathf.Abs).Prepend(float.MinValue).Max();
        for (var i = 0; i < data.Length; i++) data[i] /= max;
    }

    public static async Task<CachedAudio> FromFilePath(string filePath, string name)
    {
        var cachedAudio = new CachedAudio();
        await using var audioFileReader = new AudioFileReader(filePath);
        cachedAudio.Length = (float)audioFileReader.TotalTime.TotalSeconds;
        cachedAudio.Format = audioFileReader.WaveFormat;
        cachedAudio.LengthInSamples = (int)(audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels * cachedAudio.Length);
        cachedAudio.AudioData = new float[cachedAudio.LengthInSamples];
        cachedAudio.Name = name;
        
        if (audioFileReader.WaveFormat.SampleRate != 48000)
        {
            DiscJockeyPlugin.LogInfo($"{name} will be resampled to 48khz so that it's Opus compatible");
            var conversionFormat = new WaveFormat(48000, audioFileReader.WaveFormat.Channels);
            using var resampler = new MediaFoundationResampler(audioFileReader, conversionFormat);
            cachedAudio.Format = conversionFormat;
            cachedAudio.LengthInSamples = (int)(resampler.WaveFormat.SampleRate * resampler.WaveFormat.Channels * cachedAudio.Length);
            cachedAudio.AudioData = new float[cachedAudio.LengthInSamples];
            await Task.Run(() => resampler.ToSampleProvider().Read(cachedAudio.AudioData, 0, cachedAudio.LengthInSamples));
            return cachedAudio;
        }
        
        await Task.Run(() => audioFileReader.Read(cachedAudio.AudioData, 0, cachedAudio.LengthInSamples));
        return cachedAudio;
    }
}