using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

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
    
    public static async Task<CachedAudio> FromFilePath(string fileName, string name)
    {
        var cachedAudio = new CachedAudio();
        await using var audioFileReader = new AudioFileReader(fileName);
        cachedAudio.Length = (float)audioFileReader.TotalTime.TotalSeconds;
        cachedAudio.Format = audioFileReader.WaveFormat;
        cachedAudio.LengthInSamples = (int)(audioFileReader.Length / 4);
        cachedAudio.AudioData = new float[cachedAudio.LengthInSamples];
        cachedAudio.Name = name;

        await Task.Run(() => audioFileReader.Read(cachedAudio.AudioData, 0, cachedAudio.LengthInSamples));
        return cachedAudio;
    }
}