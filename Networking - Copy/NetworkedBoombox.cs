using System;
using DiscJockey.Networking.Data;
using DiscJockey.Utils;
using UnityEngine;

namespace DiscJockey.Data;

public class NetworkedBoombox
{
    public BoomboxItem Boombox;
    public ulong NetworkId => Boombox.NetworkObjectId;
    private readonly NetworkedAudioSource _networkedAudioSource;

    public AudioClipMetadata CurrentAudioClipMetadata => _networkedAudioSource.CurrentAudioClipMetadata;
    public TrackMetadata CurrentTrackMetadata => _networkedAudioSource.CurrentTrackMetadata;
    public float TrackProgress => _networkedAudioSource.Time;
    public bool IsPlaying => _networkedAudioSource.IsPlaying;
    public bool IsStreaming => _networkedAudioSource.StreamingState == NetworkedAudioSource.AudioStreamingStates.Streaming;
    public TrackPlaybackModes TrackPlaybackMode = TrackPlaybackModes.Sequential;
    
    public NetworkedBoombox(BoomboxItem boombox)
    {
        Boombox = boombox;
        _networkedAudioSource = new NetworkedAudioSource(boombox.boomboxAudio);
        _networkedAudioSource.OnPlaybackStopped += () =>
        {
            _networkedAudioSource.PlayOneShotLocally(boombox.GetStopAudio());
        };
    }

    public void StartStreamingTrack(Track track)
    {
        if (IsStreaming)
        {
            _networkedAudioSource.StopStreaming();
        }
        
        _networkedAudioSource.StartStreamingTrack(track);
    }
    public void StopStreaming() => _networkedAudioSource.StopStreaming();
    public void SetVolume(float volume) => _networkedAudioSource.SetVolume(volume);
}