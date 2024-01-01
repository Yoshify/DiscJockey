using System;
using System.IO;
using UnityEngine;

namespace DiscJockey.Utils
{
    internal class AudioUtils
    {
        public static string CleanAudioFilename(string fileName) => Path.GetFileNameWithoutExtension(fileName);

        public static bool IsValidAudioType(string filePath) => GetAudioType(filePath) != AudioType.UNKNOWN;

        public static AudioType GetAudioType(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLower())
            {
                case ".mp3":
                    return AudioType.MPEG;
                case ".wav":
                    return AudioType.WAV;
                case ".ogg":
                    return AudioType.OGGVORBIS;
                default:
                    DiscJockeyPlugin.LogWarning($"AudioUtils<GetAudioType>: Cannot load unsupported file type at {filePath}");
                    return AudioType.UNKNOWN;
            }
        }
    }
}
