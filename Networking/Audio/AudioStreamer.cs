using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DiscJockey.Audio;
using DiscJockey.Audio.Data;
using UnityEngine.LowLevel;

namespace DiscJockey.Networking.Audio;

public class AudioStreamer
{
    private AudioEncoder _audioEncoder;
    private CancellationTokenSource _sendAudioCancellationToken;
    private CachedAudio _streamedAudio;
    private CachedAudioSampleProvider _streamedAudioSampleProvider;
    private StreamingState _streamingState = StreamingState.Stopped;
    public bool IsStreaming => _streamingState == StreamingState.Streaming;
    public AudioFormat CurrentAudioFormat { get; private set; }

    public event Action<byte[]> OnFrameReadyToSend;
    
    private enum StreamingState
    {
        Streaming,
        Stopped
    }

    public AudioStreamer(AudioFormat audioFormat)
    {
        UpdateAudioFormat(audioFormat);
    }

    public void UpdateAudioFormat(AudioFormat audioFormat)
    {
        _audioEncoder = new AudioEncoder(audioFormat, 96000, 10);
        CurrentAudioFormat = audioFormat;
        _sendAudioCancellationToken = new CancellationTokenSource();
    }
    
    public void StopStreaming()
    {
        DiscJockeyPlugin.LogInfo($"Stream stopped");
        _sendAudioCancellationToken.Cancel();
        _sendAudioCancellationToken.Dispose();
        _sendAudioCancellationToken = new CancellationTokenSource();
        _streamingState = StreamingState.Stopped;
        _streamedAudio = null;
    }
    
    public void StartStreaming(CachedAudio audio, AudioFormat audioFormat)
    {
        if (_streamingState == StreamingState.Streaming)
        {
            StopStreaming();
        }

        if (!CurrentAudioFormat.Equals(audioFormat))
        {
            UpdateAudioFormat(audioFormat);
        }

        _streamedAudio = audio;
        _streamedAudioSampleProvider = new CachedAudioSampleProvider(_streamedAudio);
        _streamingState = StreamingState.Streaming;
        UniTask.Void(SendAudio, _sendAudioCancellationToken.Token);
    }

    private void SendFrame(float[] frame)
    {
        var compressedFrame = _audioEncoder.Encode(frame);
        OnFrameReadyToSend?.Invoke(compressedFrame);
    }

    private async UniTaskVoid SendAudio(CancellationToken ctx)
    {
        var frame = new float[CurrentAudioFormat.FrameSize * CurrentAudioFormat.Channels];
        var offsetSamples = 0;
        while (IsStreaming)
        {
            if (offsetSamples > _streamedAudio.AudioData.Length)
            {
                DiscJockeyPlugin.LogInfo("End of stream reached");
                StopStreaming();
                break;
            }

            _streamedAudioSampleProvider.Read(frame, offsetSamples, frame.Length);
            offsetSamples += CurrentAudioFormat.FrameSize * CurrentAudioFormat.Channels;
            SendFrame(frame);
            if (ctx.IsCancellationRequested) break;
            await UniTask.NextFrame(ctx, cancelImmediately:true);
        }
    }
}