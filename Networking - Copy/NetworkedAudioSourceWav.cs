using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscJockey.Managers;
using DiscJockey.Networking.Data;
using UnityEngine;
using NAudio.Wave;
using NAudio.Utils;
using Unity.Netcode;

namespace DiscJockey.Data;

public class NetworkedAudioSourceWav
{
    private readonly string _id;
    private readonly AudioSource _audioSource;
    private WaveStream _waveStream;
    private BufferedWaveProvider _bufferedWaveProvider;
    private bool _isStreaming;
    private byte[] _buffer;
    private AudioClip _streamedClip;
    private const float DesiredLatencyInSeconds = 0.05f; // 50ms
    private bool _hasInitializedPlayback;
    private int _bufferAdjustCounter = 0;
    private const int BufferAdjustThreshold = 10;
    private Track _streamedTrack;

    public TrackMetadata CurrentTrackMetadata;
    public AudioClipMetadata CurrentAudioClipMetadata;

    public AudioStreamingStates StreamingState { get; private set; } = AudioStreamingStates.Stopped;
    
    private MemoryStream _streamedMemoryStream;

    public enum AudioStreamingStates
    {
        Streaming,
        Stopped
    }

    public float Time => _audioSource.time;
    public bool IsPlaying => _audioSource.isPlaying;
    public float Volume => _audioSource.volume;

    public event Action OnStreamStarted;
    public event Action OnStreamStopped;
    public event Action OnPlaybackStarted;
    public event Action OnPlaybackStopped;

    public NetworkedAudioSourceWav(AudioSource audioSource)
    {
        _id = Guid.NewGuid().ToString();
        _audioSource = audioSource;
        DiscJockeyNetworkManager.OnAudioStreamReceived += OnAudioStreamReceived;
        
    }

    public void SetVolume(float volume) => _audioSource.volume = volume;

    public void PlayLocally(AudioClip audioClip)
    {
        _audioSource.clip = audioClip;
        _audioSource.Play();
    }
    
    public void PlayOneShotLocally(AudioClip audioClip) => _audioSource.PlayOneShot(audioClip);

    public void StartStreamingTrack(Track track)
    {
        _streamedTrack = track;
        StartStreaming(track.AudioClip);
    }
    
    public async void StartStreaming(AudioClip audioClip)
    {
        DiscJockeyPlugin.LogInfo($"Starting Stream with {audioClip.name}");
        StreamingState = AudioStreamingStates.Streaming;
        _waveStream = await StreamFromAudioClip(audioClip);
        DiscJockeyPlugin.LogInfo($"Init Buffer");
        _buffer = new byte[(int)(_waveStream.WaveFormat.AverageBytesPerSecond * DesiredLatencyInSeconds)];
        DiscJockeyPlugin.LogInfo($"Begin Stream");
        _streamedClip = audioClip;
        
        OnStreamStarted?.Invoke();
        StreamAudioAsync();
    }

    private WaveStream StreamFromLocalFile(string filePath)
    {
        var mp3File = new Mp3FileReader(filePath);
        return WaveFormatConversionStream.CreatePcmStream(mp3File);
    }

    private async Task<WaveStream> StreamFromAudioClip(AudioClip audioClip)
    {
        var convertedData = await AudioClipToByteArrayAsync(audioClip);
        DiscJockeyPlugin.LogInfo($"Got Converted Data, Length {convertedData.Length}");
        _streamedMemoryStream = new MemoryStream(convertedData, 0, convertedData.Length);
        DiscJockeyPlugin.LogInfo($"Init MemoryStream");
        return new RawSourceWaveStream(_streamedMemoryStream, new WaveFormat(audioClip.frequency, 16, audioClip.channels));
    }

    private void AdjustBuffer()
    {
       
        var currentRtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
        
        var actualLatency = Math.Max(currentRtt / 1000f * 2, DesiredLatencyInSeconds);
        DiscJockeyPlugin.LogInfo($"Adjusting Buffer. CurrentRtt is {currentRtt} - lat {currentRtt / 1000f * 2} - bufferLat {actualLatency}");
        _buffer = new byte[(int)(_waveStream.WaveFormat.AverageBytesPerSecond * actualLatency)];
    }
    
    private async Task<byte[]> AudioClipToByteArrayAsync(AudioClip audioClip)
    {
        return await Task.Run(() =>
        {
            var samples = new float[audioClip.samples];
            audioClip.GetData(samples, 0);

            var bytesData = new byte[samples.Length * 2]; // 2 bytes per sample (Int16)
            int rescaleFactor = 32767; // to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                short intSample = (short)(samples[i] * rescaleFactor);
                BitConverter.GetBytes(intSample).CopyTo(bytesData, i * 2);
            }

            return bytesData;
        });
    }
    
    private byte[] AudioClipToByteArray(AudioClip audioClip)
    {
        var samples = new float[audioClip.samples];
        audioClip.GetData(samples, 0);

        var intData = new Int16[samples.Length];
        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

        var bytesData = new Byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i<samples.Length; i++) {
            intData[i] = (short) (samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        return bytesData;
    }

    public void StopStreaming()
    {
        DiscJockeyNetworkManager.Instance.StreamAudioFragmentServerRpc(_id, new NetworkedAudioPacket(Array.Empty<byte>(),
            true,
            new AudioClipMetadata(_streamedClip.name, _streamedClip.frequency, _streamedClip.channels, _streamedClip.length),
            new TrackMetadata(_streamedTrack.OwnerId, _streamedTrack.OwnerName)
        ));
        StreamingState = AudioStreamingStates.Stopped;
        _streamedClip = null;
        _waveStream.Dispose();
        _streamedMemoryStream.Dispose();
        OnStreamStopped?.Invoke();
    }

    private void StopPlayback()
    {
        _audioSource.Stop();
        _audioSource.clip = null;
        _bufferedWaveProvider.ClearBuffer();
        OnPlaybackStopped?.Invoke();
    }

    private void InitializeStreamPlayback(NetworkedAudioPacket data)
    {
        CurrentAudioClipMetadata = data.AudioClipMetadata;
        CurrentTrackMetadata = data.TrackMetadata;
        
        _bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(CurrentAudioClipMetadata.Frequency, 16, CurrentAudioClipMetadata.Channels))
            {
                BufferDuration = TimeSpan.FromSeconds(60)
            };

        DiscJockeyPlugin.LogInfo($"InitializeStreamPlayback: Inited for {CurrentAudioClipMetadata.Name}");
        _audioSource.pitch = 1f;
        _audioSource.clip = AudioClip.Create(CurrentAudioClipMetadata.Name, int.MaxValue, CurrentAudioClipMetadata.Channels, CurrentAudioClipMetadata.Frequency, true, OnAudioRead);
        _audioSource.Play();
        OnPlaybackStarted?.Invoke();
    }

    private void OnAudioStreamReceived(string targetId, NetworkedAudioPacket data)
    {
        if (targetId != _id) return;
        
        if (!_audioSource.isPlaying)
        {
            InitializeStreamPlayback(data);
        }

        if (data.EndOfStream)
        {
            StopPlayback();
            return;
        }
        
        _bufferedWaveProvider.AddSamples(data.Fragment, 0, data.FragmentLength);
    }
    
    private void OnAudioRead(float[] data)
    {
        DiscJockeyPlugin.LogInfo($"OnAudioRead. Data len {data.Length} - _bufferedWaveProvider available ? {_bufferedWaveProvider != null}");
        _bufferedWaveProvider.ToSampleProvider().Read(data, 0, data.Length);
    }
    
    private byte[] ConvertAudioClipToPCM(AudioClip audioClip)
    {
        var samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        // Convert the float array into a byte array
        var pcmData = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, pcmData, 0, pcmData.Length);
        return pcmData;
    }

    private async void StreamAudioAsync()
    {
        while (StreamingState == AudioStreamingStates.Streaming)
        {
            if (_streamedMemoryStream is not { CanRead: true })
            {
                // we probably disposed of this already, the stream was stopped
                DiscJockeyPlugin.LogInfo("StreamAudioAsync: Breaking Stream loop, streams were disposed of.");
                StopStreaming();
                break;
            }

            if (_waveStream is not { CanRead: true })
            {
                // we probably disposed of this already, the stream was stopped
                DiscJockeyPlugin.LogInfo("StreamAudioAsync: Breaking Stream loop, streams were disposed of.");
                StopStreaming();
                break;
            }

            if (_streamedClip == null || _streamedTrack == null)
            {
                // we probably disposed of these already, the stream was stopped
                DiscJockeyPlugin.LogInfo("StreamAudioAsync: Breaking Stream loop, streams were disposed of.");
                StopStreaming();
                break;
            }
            
            var bytesRead = await _waveStream.ReadAsync(_buffer, 0, _buffer.Length);
            
            
            var sampleSize = sizeof(float) * _streamedClip.channels; // Assuming 16-bit (2 bytes) per sample per channel
            var samplesInBuffer = bytesRead / sampleSize;
            var bufferDuration = (double)samplesInBuffer / _streamedClip.frequency;
            var delayMilliseconds = (int)(bufferDuration * 1000);
            
            
            if (bytesRead <= 0)
            {
                DiscJockeyPlugin.LogInfo("StreamAudioAsync: Stopping Stream, no bytes read. Sending EndOfStream packet.");
                
                StopStreaming();
                break;
            }
            
            DiscJockeyNetworkManager.Instance.StreamAudioFragmentServerRpc(_id, new NetworkedAudioPacket(
                    _buffer,
                    false,
                    new AudioClipMetadata(_streamedClip.name, _streamedClip.frequency, _streamedClip.channels, _streamedClip.length),
                    new TrackMetadata(_streamedTrack.OwnerId, _streamedTrack.OwnerName)
                ));
            
            DiscJockeyPlugin.LogInfo($"Waiting {delayMilliseconds}ms");
            await Task.Delay(delayMilliseconds);
        }        
    }
}