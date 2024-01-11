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
    private readonly NetworkedAudioReceiver _networkedAudioReceiver;
    private readonly NetworkedAudioSender _networkedAudioSender;
    private AudioFormat? _streamedAudioFormat;
    private TrackMetadata? _streamedTrackMetadata;

    public BoomboxItem Boombox;
    public BoomboxPlaybackMode BoomboxPlaybackMode = BoomboxPlaybackMode.Sequential;
    public float Volume => _networkedAudioReceiver.Volume;

    public NetworkedBoombox(BoomboxItem boombox)
    {
        var format = new AudioFormat(SampleRate);
        Boombox = boombox;
        _networkedAudioReceiver = new NetworkedAudioReceiver(boombox.boomboxAudio, format);
        _networkedAudioSender = new NetworkedAudioSender(format);

        DJNetworkManager.OnAudioStreamTransmitStarted += OnAudioStreamTransmitStarted;
        DJNetworkManager.OnAudioStreamPlaybackStopped += OnPlaybackStopped;
        DJNetworkManager.OnAudioStreamPacketReceived += OnAudioPacketPacketReceived;
        DJNetworkManager.OnBoomboxVolumeChanged += OnBoomboxVolumeChanged;
        DJNetworkManager.OnBoomboxPlaybackModeChanged += OnPlaybackModeChanged;
        _networkedAudioSender.OnFrameReadyToSend += SendFrame;
        _networkedAudioReceiver.OnPlaybackCompleted += OnPlaybackCompleted;
    }

    public ulong NetworkedBoomboxId => Boombox.NetworkObjectId;

    public float CurrentTrackLength => _streamedTrackMetadata?.LengthInSeconds ?? 0;
    public float CurrentTrackProgress => _networkedAudioReceiver.Time;
    public int CurrentTrackIndexInOwnersTracklist => _streamedTrackMetadata?.IndexInOwnersTracklist ?? 0;

    public bool LocalClientOwnsCurrentTrack => _streamedTrackMetadata.HasValue &&
                                               _streamedTrackMetadata.Value.OwnerId ==
                                               LocalPlayerHelper.Player.playerClientId;

    public bool IsPlaying => _networkedAudioReceiver.IsPlaying;
    public bool IsStreaming => _networkedAudioSender.IsStreaming;

    private void OnPlaybackCompleted()
    {
        if (LocalClientOwnsCurrentTrack)
        {
            StartStreamingTrack(
                AudioManager.TrackList.GetNextTrack(
                    CurrentTrackIndexInOwnersTracklist,
                    BoomboxPlaybackMode));
        }
    }

    private void OnAudioStreamTransmitStarted(ulong senderClientId, ulong networkedBoomboxId,
        TrackMetadata trackMetadata, AudioFormat audioFormat)
    {
        if (networkedBoomboxId != NetworkedBoomboxId) return;

        DiscJockeyPlugin.LogInfo("OnAudioStreamStarted: Called");
        if (senderClientId != LocalPlayerHelper.Player.playerClientId && _networkedAudioSender.IsStreaming)
        {
            DiscJockeyPlugin.LogInfo(
                "NetworkedBoombox<OnAudioStreamStarted>: We were streaming, but someone else is now. Stopping our stream.");
            StopStreamLocally();
        }

        if (_networkedAudioReceiver.IsPlaying)
        {
            DiscJockeyPlugin.LogInfo("NetworkedBoombox<OnAudioStreamStarted>: We were playing, now we're not");
            StopPlaybackLocally();
        }

        if (!_networkedAudioReceiver.CurrentAudioFormat.Equals(audioFormat))
        {
            DiscJockeyPlugin.LogInfo(
                "NetworkedBoombox<OnAudioStreamStarted>: Received AudioFormat differs to our receiver, updating and resetting encoder");
            _networkedAudioReceiver.UpdateAudioFormat(audioFormat);
        }

        if (senderClientId != LocalPlayerHelper.Player.playerClientId)
        {
            _streamedTrackMetadata = trackMetadata;
            _streamedAudioFormat = audioFormat;
        }

        DiscJockeyPlugin.LogInfo("NetworkedBoombox<OnAudioStreamStarted>: Starting playback loop.");
        _networkedAudioReceiver.StartPlayback(trackMetadata.LengthInSamples);
    }

    private void OnPlaybackModeChanged(ulong networkedBoomboxId, BoomboxPlaybackMode mode)
    {
        if (networkedBoomboxId == NetworkedBoomboxId) BoomboxPlaybackMode = mode;
    }

    private void OnBoomboxVolumeChanged(ulong networkedBoomboxId, float volume)
    {
        if (networkedBoomboxId == NetworkedBoomboxId) SetVolume(volume);
    }

    private void OnPlaybackStopped(ulong senderClientId, ulong networkedBoomboxId)
    {
        if (senderClientId != LocalPlayerHelper.Player.playerClientId) StopPlaybackLocally();
    }

    private void NotifyStreamStarted(ulong playerClientId, ulong networkedBoomboxId, TrackMetadata trackMetadata,
        AudioFormat audioFormat)
    {
        DJNetworkManager.Instance.SendAudioStreamTransmissionStartedServerRpc(playerClientId,
            networkedBoomboxId, trackMetadata, audioFormat);
    }

    public void OnAudioPacketPacketReceived(ulong networkedBoomboxId, NetworkedAudioPacket packet)
    {
        if (networkedBoomboxId != NetworkedBoomboxId) return;
        _networkedAudioReceiver.AddFrameToBuffer(packet.Frame);
    }

    private void SendFrame(byte[] frame)
    {
        if (!_streamedTrackMetadata.HasValue || !_streamedAudioFormat.HasValue || frame == null) return;
        
        DJNetworkManager.Instance.SendAudioPacketServerRpc(NetworkedBoomboxId, new NetworkedAudioPacket(
            frame,
            _streamedTrackMetadata.Value,
            _streamedAudioFormat.Value
        ));
    }

    public void StartStreamingTrack(Track track)
    {
        DiscJockeyPlugin.LogInfo($"NetworkedBoombox<StartStreamingTrack>: Requested stream for {track.Audio.Name}");

        if (_networkedAudioSender.IsStreaming)
        {
            DiscJockeyPlugin.LogInfo(
                "NetworkedBoombox<StartStreamingTrack>: We were already streaming, stopping previous stream.");
            //_networkedAudioSender.StopStreaming();
        }
        
        
        _streamedTrackMetadata = track.ExtractMetadata();
        _streamedAudioFormat = new AudioFormat(SampleRate, FrameSizeMs, track.Audio.Format.Channels);
        
        if (!_networkedAudioSender.CurrentAudioFormat.Equals(_streamedAudioFormat.Value))
        {
            DiscJockeyPlugin.LogInfo(
                "NetworkedBoombox<StartStreamingTrack>: Streamed AudioFormat differs to our sender, updating and resetting encoder");
            _networkedAudioSender.UpdateAudioFormat(_streamedAudioFormat.Value);
        }

        DiscJockeyPlugin.LogInfo(
            "NetworkedBoombox<StartStreamingTrack>: Starting stream.");
        _networkedAudioSender.StartStreaming(track.Audio);
        NotifyStreamStarted(LocalPlayerHelper.Player.playerClientId, NetworkedBoomboxId, _streamedTrackMetadata.Value,
            _streamedAudioFormat.Value);
    }

    public void StopPlaybackLocally()
    {
        _networkedAudioReceiver.StopPlayback();
        Boombox.boomboxAudio.PlayOneShot(Boombox.GetStopAudio());
    }

    public void StopStreamLocally()
    {
        if (_networkedAudioSender.IsStreaming)
        {
            _networkedAudioSender.StopStreaming();
        }
    }

    public void StopStreamAndPlaybackAndNotify()
    {
        StopStreamLocally();
        StopPlaybackLocally();
        DJNetworkManager.Instance.SendAudioStreamPlaybackStoppedServerRpc(
            LocalPlayerHelper.Player.playerClientId, NetworkedBoomboxId);
    }

    public void SetVolume(float volume)
    {
        _networkedAudioReceiver.SetVolume(volume);
    }
}