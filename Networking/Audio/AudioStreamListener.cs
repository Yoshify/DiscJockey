using System;
using DiscJockey.Audio;
using DiscJockey.Audio.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiscJockey.Networking.Audio;

public class AudioStreamListener
{
    private readonly AudioSource _audioSource;
    private AudioDecoder _audioDecoder;
    public AudioFormat CurrentAudioFormat { get; private set; }
    private AudioFrameBuffer _audioFrameBuffer;
    private int _playedSamples;
    private int _totalSamplesInPlayback;
    public Action OnPlaybackCompleted;
    private PlayingState _playingState = PlayingState.Stopped;

    public bool IsPlaying => _playingState == PlayingState.Playing;

    public enum PlayingState
    {
        Playing,
        Stopped
    }
    
    public AudioStreamListener(AudioSource audioSource, AudioFormat audioFormat)
    {
        _audioSource = audioSource;
        _audioSource.volume = DiscJockeyConfig.LocalConfig.DefaultVolume;
        _audioSource.loop = false;
        UpdateAudioFormat(audioFormat);
    }

    public float Volume => _audioSource.volume;
    public float Time => _audioSource.time;

    private void UpdateAudioFormat(AudioFormat audioFormat)
    {
        DiscJockeyPlugin.LogDebug($"Updating AudioFormat to {audioFormat}");
        CurrentAudioFormat = audioFormat;
        _audioDecoder = new AudioDecoder(audioFormat);
        _audioFrameBuffer = new AudioFrameBuffer(CurrentAudioFormat);
        InitializeAudioClip(audioFormat, audioFormat.SamplingRate * audioFormat.Channels);
    }

    private void InitializeAudioClip(AudioFormat audioFormat, int clipLength)
    {
        if (IsPlaying)
        {
            DiscJockeyPlugin.LogError("Tried to initialize the Audio Clip during playback!");
            return;
        }
        
        if (_audioSource.clip != null) Object.Destroy(_audioSource.clip);
        _audioSource.clip = AudioClip.Create("Stream", clipLength, audioFormat.Channels, audioFormat.SamplingRate, true, OnAudioRead);
        _audioSource.pitch = 1f;
        DiscJockeyPlugin.LogDebug($"AudioClip Initialized {clipLength}");
    }

    private void InitializeBuffer(int sizeInSamples)
    {
        if (_audioFrameBuffer is { Count: >= 0 })
        {
            DiscJockeyPlugin.LogWarning("Reinitializing non-empty playback buffer");
        }
        
        _audioFrameBuffer.Reset();
        _audioFrameBuffer.SizeBufferInSamples(sizeInSamples);
        DiscJockeyPlugin.LogDebug("Playback buffer initialized");
    }

    private void InitializePlaybackParameters(int totalSamplesInPlayback)
    {
        _playedSamples = 0;
        _totalSamplesInPlayback = totalSamplesInPlayback;
        DiscJockeyPlugin.LogDebug($"Playback parameters initialized - {_playedSamples} _playedSamples, {_totalSamplesInPlayback} _totalSamplesInPlayback");
    }
    
    public void StartListening(StreamInformation streamInformation)
    {
        if(IsPlaying) StopPlayback();
        if(!CurrentAudioFormat.Equals(streamInformation.AudioFormat)) UpdateAudioFormat(streamInformation.AudioFormat);
        else InitializeAudioClip(streamInformation.AudioFormat, streamInformation.TrackMetadata.LengthInSamples);
        DiscJockeyPlugin.LogInfo($"Started listening to stream for {streamInformation.TrackMetadata.Name} from {streamInformation.TrackMetadata.OwnerName}'s tracklist");
        InitializeBuffer(streamInformation.TrackMetadata.LengthInSamples);
        InitializePlaybackParameters(streamInformation.TrackMetadata.LengthInSamples);
        _audioSource.Play();
        _playingState = PlayingState.Playing;
    }

    private void InjectSilence(float[] data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = 0.0f;
        }
    }

    private void OnAudioRead(float[] data)
    {
        if (_audioFrameBuffer is not { Count: > 0 })
        {
            DiscJockeyPlugin.LogWarning("No frames left in buffer but playback is ongoing - injecting silence");
            InjectSilence(data);
            return;
        }

        if (!IsPlaying)
        {
            InjectSilence(data);
            return;
        }
        
        _audioFrameBuffer.FillPCM(data);
        _playedSamples += data.Length;
        if (_playedSamples >= _totalSamplesInPlayback)
        {
            StopPlayback();
            OnPlaybackCompleted?.Invoke();
        }
    }

    public void AddFrameToBuffer(byte[] frame)
    {
        var decodedFrame = _audioDecoder.Decode(frame);
        _audioFrameBuffer.AddFrameToBuffer(decodedFrame);
    }

    public void StopPlayback()
    {
        DiscJockeyPlugin.LogInfo("Playback stopped");
        _audioSource.Stop();
        _playingState = PlayingState.Stopped;
    }

    public void SetVolume(float volume) => _audioSource.volume = volume;
}