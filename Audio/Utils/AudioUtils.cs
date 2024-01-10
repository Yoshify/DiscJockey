using System.IO;
using UnityEngine;

namespace DiscJockey.Audio.Utils;

public static class AudioUtils
{
    public static bool IsValidAudioType(string filePath)
    {
        return GetAudioType(filePath) != AudioType.UNKNOWN;
    }

    public static AudioType GetAudioType(string filePath)
    {
        return Path.GetExtension(filePath).ToLower() switch
        {
            ".mp3" => AudioType.MPEG,
            ".wav" => AudioType.WAV,
            _ => AudioType.UNKNOWN
        };
    }
}