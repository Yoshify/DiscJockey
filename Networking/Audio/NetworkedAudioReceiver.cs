using System;
using DiscJockey.Audio;
using DiscJockey.Audio.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiscJockey.Networking.Audio;

public class NetworkedAudioReceiver
{
    private readonly AudioSource _audioSource;
    private AudioDecoder _audioDecoder;
    private AudioFormat _audioFormat;
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
    
    public NetworkedAudioReceiver(AudioSource audioSource, AudioFormat audioFormat)
    {
        UpdateAudioFormat(audioFormat);
        _audioSource = audioSource;
        _audioSource.volume = DiscJockeyConfig.LocalConfig.DefaultVolume;
    }

    public float Volume => _audioSource.volume;
    public float Time => _audioSource.time;
    public AudioFormat CurrentAudioFormat => _audioFormat;

    public void UpdateAudioFormat(AudioFormat audioFormat)
    {
        _audioFormat = audioFormat;
        _audioDecoder = new AudioDecoder(audioFormat);
        _audioFrameBuffer = new AudioFrameBuffer(_audioFormat);
    }

    public void StartPlayback(int sampleLength)
    {
        DiscJockeyPlugin.LogInfo($"NetworkedAudioReceiver<StartPlayback>: Starting playback - buffer resizing buffer to new sampleLength {sampleLength}");
        _audioFrameBuffer.Reset();
        _audioFrameBuffer.SizeBufferInSamples(sampleLength);
        _playedSamples = 0;
        _totalSamplesInPlayback = sampleLength;
        _audioSource.clip = AudioClip.Create("Stream", sampleLength, _audioFormat.Channels, _audioFormat.SamplingRate,
            true, OnAudioRead);
        _audioSource.pitch = 1f;
        _audioSource.loop = false;
        _audioSource.Play();
        _playingState = PlayingState.Playing;
    }

    private void OnAudioRead(float[] data)
    {
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
        _audioFrameBuffer.Reset();
        DiscJockeyPlugin.LogInfo("NetworkedAudioSender<StopPlayback>: Playback stopped, buffer cleared, decoder reset");
        Object.Destroy(_audioSource.clip);
        _audioSource.Stop();
        _audioSource.clip = null;
        _playingState = PlayingState.Stopped;
    }

    public void SetVolume(float volume)
    {
        _audioSource.volume = volume;
    }
}