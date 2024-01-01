using DiscJockey.Managers;

namespace DiscJockey.Utils
{
    public static class BoomboxExtensions
    {
        public static void StopTrack(this BoomboxItem instance)
        {
            instance.boomboxAudio.time = 0;
            instance.isBeingUsed = false;
            instance.isPlayingMusic = false;
            instance.boomboxAudio.Stop();
            instance.PlayStopAudio();
        }

        public static void PlayStopAudio(this BoomboxItem instance)
        {
            instance.boomboxAudio.PlayOneShot(instance.stopAudios[UnityEngine.Random.Range(0, instance.stopAudios.Length)]);
        }

        public static void PlayTrack(this BoomboxItem instance, int trackIndex)
        {
            if(instance.isPlayingMusic)
            {
                instance.StopTrack();
            }

            instance.boomboxAudio.time = 0;
            instance.boomboxAudio.clip = DiscJockeyAudioManager.TrackList.GetTrackAtIndex(trackIndex).AudioClip;
            instance.boomboxAudio.pitch = 1f;
            instance.boomboxAudio.Play();
            instance.isBeingUsed = true;
            instance.isPlayingMusic = true;
        }

        public static void ScrubTrack(this BoomboxItem instance, float time)
        {
            instance.boomboxAudio.time = time;
        }
    }
}
