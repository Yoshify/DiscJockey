using UnityEngine;

namespace DiscJockey.Data
{
    public class Track
    {
        public readonly AudioClip AudioClip;
        public TrackMetadata Metadata;

        public Track(AudioClip audioClip, TrackMetadata metadata)
        {
            AudioClip = audioClip;
            Metadata = metadata;
        }

        public override string ToString()
        {
            return Metadata.ToString();
        }
    }
}