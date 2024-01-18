using DiscJockey.Audio;
using DiscJockey.Audio.Data;
using DiscJockey.Data;
using DiscJockey.Managers;
using DiscJockey.Networking.Audio;
using DiscJockey.Networking.Audio.Utils;
using DiscJockey.Utils;

namespace DiscJockey.Networking;

public class NetworkedBoombox
{
    private const int SampleRate = 48000;
    private const int FrameSizeMs = 20;
    private readonly AudioStreamListener _audioStreamListener;
    private readonly AudioStreamer _audioStreamer;
    
    public StreamInformation? ActiveStreamInformation { get; private set; }
    public BoomboxItem Boombox;
    public BoomboxPlaybackMode BoomboxPlaybackMode = BoomboxPlaybackMode.Sequential;
    public float Volume => _audioStreamListener.Volume;

    public NetworkedBoombox(BoomboxItem boombox)
    {
        var format = new AudioFormat(SampleRate);
        Boombox = boombox;
        _audioStreamListener = new AudioStreamListener(boombox.boomboxAudio, format);
        _audioStreamer = new AudioStreamer(format);
        
        _audioStreamer.OnFrameReadyToSend += SendFrame;
        _audioStreamListener.OnPlaybackCompleted += OnPlaybackCompleted;
    }

    public ulong NetworkedBoomboxId => Boombox.NetworkObjectId;
    public float CurrentTrackLength => ActiveStreamInformation.HasValue ? ActiveStreamInformation.Value.TrackMetadata.LengthInSeconds : 0;
    public float CurrentTrackProgress => _audioStreamListener.Time;
    public int CurrentTrackIndexInOwnersTracklist => ActiveStreamInformation.HasValue ? ActiveStreamInformation.Value.TrackMetadata.IndexInOwnersTracklist : 0;
    public bool LocalClientOwnsCurrentTrack => ActiveStreamInformation.HasValue && ActiveStreamInformation.Value.TrackMetadata.OwnerId == LocalPlayerHelper.Player.playerClientId;

    public bool IsPlaying => _audioStreamListener.IsPlaying;
    public bool IsStreaming => _audioStreamer.IsStreaming;

    private void OnPlaybackCompleted()
    {
        if (LocalClientOwnsCurrentTrack)
        {
            DiscJockeyPlugin.LogInfo($"Playback of {ActiveStreamInformation?.TrackMetadata.Name} complete - moving on to the next track");
            StartStreamingTrack(
                AudioManager.TrackList.GetNextTrack(
                    CurrentTrackIndexInOwnersTracklist,
                    BoomboxPlaybackMode));
        }
    }

    public void ListenToStream(ulong senderId, StreamInformation streamInformation)
    {
        if (senderId != LocalPlayerHelper.Player.playerClientId)
        {
            if (_audioStreamer.IsStreaming)
            {
                DiscJockeyPlugin.LogInfo("We were streaming, but someone else is now. Stopping our stream.");
                StopSendingStream();
            }

            ActiveStreamInformation = streamInformation;
        }

        UpdatePlaybackStateForEntities(true);
        _audioStreamListener.StartListening(streamInformation);
    }
    
    public void SetPlaybackMode(BoomboxPlaybackMode mode) => BoomboxPlaybackMode = mode;

    public void ReceiveStreamPacket(NetworkedAudioPacket packet) => _audioStreamListener.AddFrameToBuffer(packet.Frame);

    private void SendFrame(byte[] frame)
    {
        if (!ActiveStreamInformation.HasValue)
        {
            DiscJockeyPlugin.LogWarning("Can't send a frame when there's no ActiveStreamInformation!");
            return;
        }
        
        DJNetworkManager.Instance.SendAudioPacketServerRpc(NetworkedBoomboxId, new NetworkedAudioPacket(
            frame,
            ActiveStreamInformation.Value
        ));
    }

    public void StartStreamingTrack(Track track)
    {
        DiscJockeyPlugin.LogInfo($"Player {LocalPlayerHelper.Player.playerClientId} is beginning to stream {track.Audio.Name}");
        ActiveStreamInformation = new StreamInformation(
            track.ExtractMetadata(),
            new AudioFormat(SampleRate, FrameSizeMs, track.Audio.Format.Channels)
        );
        _audioStreamer.StartStreaming(track.Audio, ActiveStreamInformation.Value.AudioFormat);
        DJNetworkManager.Instance.NotifyStreamStartedServerRpc(LocalPlayerHelper.Player.playerClientId, NetworkedBoomboxId, ActiveStreamInformation.Value);
    }

    private void UpdatePlaybackStateForEntities(bool value)
    {
        if (!DiscJockeyConfig.SyncedConfig.EntitiesHearMusic) return;
        Boombox.isPlayingMusic = value;
    }

    public void StopListeningToStream()
    {
        UpdatePlaybackStateForEntities(false);
        _audioStreamListener.StopPlayback();
        Boombox.boomboxAudio.PlayOneShot(Boombox.GetStopAudio());
    }

    private void StopSendingStream()
    {
        if (_audioStreamer.IsStreaming)
        {
            _audioStreamer.StopStreaming();
        }
    }

    public void StopStreamAndNotify()
    {
        StopSendingStream();
        StopListeningToStream();
        DJNetworkManager.Instance.NotifyStreamStoppedServerRpc(LocalPlayerHelper.Player.playerClientId, NetworkedBoomboxId);
    }

    public void SetVolume(float volume)
    {
        _audioStreamListener.SetVolume(volume);
    }
}