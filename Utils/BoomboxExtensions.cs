using UnityEngine;

namespace DiscJockey.Utils;

public static class BoomboxExtensions
{
    public static AudioClip GetStopAudio(this BoomboxItem instance) => instance.stopAudios[Random.Range(0, instance.stopAudios.Length)];
}