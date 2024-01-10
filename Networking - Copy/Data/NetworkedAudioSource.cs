using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Concentus.Enums;
using Concentus.Structs;
using DiscJockey.Managers;
using DiscJockey.Networking.Data;
using UnityEngine;
using NAudio.Wave;
using NAudio.Utils;
using Unity.Netcode;

namespace DiscJockey.Data;

public class NetworkedAudioSource
{
    private readonly string _id;
    private readonly AudioSource _audioSource;
    private BufferedWaveProvider _bufferedWaveProvider;
    private bool _isStreaming;
    private byte[] _buffer;
    private AudioClip _streamedClip;
    private const float DesiredLatencyInSeconds = 0.05f; // 50ms
    private bool _hasInitializedPlayback;
    private Track _streamedTrack;

    public TrackMetadata CurrentTrackMetadata;
    public AudioClipMetadata CurrentAudioClipMetadata;

    public AudioStreamingStates StreamingState { get; private set; } = AudioStreamingStates.Stopped;

    private OpusEncoder _opusEncoder;
    private OpusDecoder _opusDecoder;
    private const int SampleRate = 48000; // Common sample rate for Opus
    private const int Channels = 2; // Stereo
    private const int FrameSizeMs = 20; // Frame size in milliseconds
    private const int FrameSizeSamples = (SampleRate / 1000) * FrameSizeMs * Channels; // Frame size in samples


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

    public NetworkedAudioSource(AudioSource audioSource)
    {
        _id = Guid.NewGuid().ToString();
        _audioSource = audioSource;
        DiscJockeyPlugin.LogInfo("CREATING ENCODER");
        _opusEncoder = new OpusEncoder(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_AUDIO);
        DiscJockeyPlugin.LogInfo("CREATING DECODER");
        _opusDecoder = new OpusDecoder(SampleRate, Channels);
        DiscJockeyPlugin.LogInfo("INIT EVENT");
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
        DiscJockeyPlugin.LogInfo("StartStreamingTrack");
        _streamedTrack = track;
        StartStreaming(track.AudioClip);
    }
    
    public async void StartStreaming(AudioClip audioClip)
    {
        _streamedClip = audioClip;
        StreamingState = AudioStreamingStates.Streaming;
        DiscJockeyPlugin.LogInfo("Converting to Opus");
        var opusData = await ConvertAudioClipToOpusAsync(audioClip);

        DiscJockeyPlugin.LogInfo("Invoking Stream");
        OnStreamStarted?.Invoke();
        StreamAudioAsync(opusData);
    }
    
    private async Task<byte[]> ConvertAudioClipToOpusAsync(AudioClip audioClip)
    {
        DiscJockeyPlugin.LogInfo("Converting Clip to Short");
        short[] pcmData = await AudioClipToShortArrayAsync(audioClip);

        MemoryStream opusStream = new MemoryStream();
        for (int offset = 0; offset < pcmData.Length; offset += FrameSizeSamples)
        {
            DiscJockeyPlugin.LogInfo($"Encoding, offset is {offset}");
            int frameSize = Math.Min(FrameSizeSamples, pcmData.Length - offset);
            byte[] opusFrame = new byte[FrameSizeSamples * 2]; // Estimate size
            int byteCount = _opusEncoder.Encode(pcmData, offset, frameSize, opusFrame, 0, opusFrame.Length);
            DiscJockeyPlugin.LogInfo("Writing to stream");
            try
            {
                opusStream.Write(opusFrame, 0, byteCount);
            }
            catch (Exception e)
            {
                DiscJockeyPlugin.LogError(e.ToString());
                throw;
            }
        }
        
        DiscJockeyPlugin.LogInfo("Returning");
        return opusStream.ToArray();
    }

    private async Task<short[]> AudioClipToShortArrayAsync(AudioClip audioClip)
    {
        return await Task.Run(() =>
        {
            var samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);

            var shortData = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                shortData[i] = (short)(samples[i] * 32767);
            }

            return shortData;
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

        if (data.EndOfStream)
        {
            StopPlayback();
            return;
        }

        if (!_audioSource.isPlaying)
        {
            InitializeStreamPlayback(data);
        }

        DecodeOpusData(data.Fragment);
    }
    
    private void DecodeOpusData(byte[] opusData)
    {
        // Add the decoded PCM data to the buffered wave provider
        _bufferedWaveProvider.AddSamples(opusData, 0, opusData.Length);
    }
    
    private void OnAudioRead(float[] data)
    {
        // Assuming _opusDataBuffer contains the received Opus encoded data
        byte[] opusDataBuffer = new byte[_bufferedWaveProvider.BufferedBytes]; // You may need to adjust the size
        int bytesRead = _bufferedWaveProvider.Read(opusDataBuffer, 0, opusDataBuffer.Length);

        // Decoding Opus data to PCM
        short[] pcmBuffer = new short[data.Length];
        int frameSize = _opusDecoder.Decode(opusDataBuffer, 0, bytesRead, pcmBuffer, 0, pcmBuffer.Length, false);

        // Convert from short PCM data to float for the AudioClip
        for (int i = 0; i < frameSize; i++)
        {
            data[i] = pcmBuffer[i] / 32768f; // Convert from short to float
        }
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

    private async void StreamAudioAsync(byte[] opusData)
    {
        int offset = 0;
        while (StreamingState == AudioStreamingStates.Streaming && offset < opusData.Length)
        {
            DiscJockeyPlugin.LogInfo("In Stream");
            int chunkSize = Math.Min(_buffer.Length, opusData.Length - offset);
            DiscJockeyPlugin.LogInfo($"CS: {chunkSize}");
            Buffer.BlockCopy(opusData, offset, _buffer, 0, chunkSize);
            
            DiscJockeyPlugin.LogInfo("sending fragment");
            DiscJockeyNetworkManager.Instance.StreamAudioFragmentServerRpc(_id, new NetworkedAudioPacket(
                _buffer, false, new AudioClipMetadata(_streamedClip.name, _streamedClip.frequency, _streamedClip.channels, _streamedClip.length),
                new TrackMetadata(_streamedTrack.OwnerId, _streamedTrack.OwnerName)));

            offset += chunkSize;
            DiscJockeyPlugin.LogInfo($"incrementing offset to {offset}");
            await Task.Delay(FrameSizeMs); // Delay based on frame size
        }
            
        if (StreamingState == AudioStreamingStates.Stopped)
        {
            DiscJockeyNetworkManager.Instance.StreamAudioFragmentServerRpc(_id, new NetworkedAudioPacket(
                Array.Empty<byte>(), true, new AudioClipMetadata(_streamedClip.name, _streamedClip.frequency, _streamedClip.channels, _streamedClip.length),
                new TrackMetadata(_streamedTrack.OwnerId, _streamedTrack.OwnerName)));
        }
    }
}