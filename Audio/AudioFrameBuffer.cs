using System;
using System.Collections.Generic;
using DiscJockey.Audio.Data;

namespace DiscJockey.Audio;

public class AudioFrameBuffer
{
    private readonly AudioFormat _audioFormat;
    private readonly LinkedList<float[]> _frameBuffer = new();

    public AudioFrameBuffer(AudioFormat audioFormat)
    {
        _audioFormat = audioFormat;
    }

    public int MaxFramesInBuffer { get; set; } = 5000;

    public int Count => _frameBuffer.Count;

    public bool HasFramesInBuffer => Count > 0;

    public void Reset()
    {
        _frameBuffer.Clear();
    }

    public void SetBufferSizeMs(int targetMs)
    {
        MaxFramesInBuffer = targetMs / _audioFormat.MillisecondsPerFrame;
    }

    public void SizeBufferInSamples(int samples)
    {
        MaxFramesInBuffer = samples / (_audioFormat.FrameSize * _audioFormat.Channels);
    }

    public void AddFrameToBuffer(float[] frame)
    {
        if (Count > MaxFramesInBuffer)
            while (Count > MaxFramesInBuffer / 2)
            {
                DiscJockeyPlugin.LogInfo($"BUFFER FULL. Max size is {MaxFramesInBuffer}. Dumping frames");
                _frameBuffer.RemoveFirst();
            }

        try
        {
            _frameBuffer.AddLast(frame);
        }
        catch
        {
        }
    }

    public void FillPCM(float[] pcm)
    {
        var requiredLength = pcm.Length;
        var filledLength = 0;

        while (filledLength < requiredLength)
        {
            if (_frameBuffer.Count == 0)
            {
                // Buffer is empty, fill the rest of pcm with zeros
                Array.Clear(pcm, filledLength, requiredLength - filledLength);
                break;
            }

            var diff = requiredLength - filledLength;
            var nextFrame = GetNextFrameFromBuffer();

            if (filledLength + nextFrame.Length <= requiredLength)
            {
                // Entire frame fits into the pcm
                Array.Copy(nextFrame, 0, pcm, filledLength, nextFrame.Length);
                filledLength += nextFrame.Length;
            }
            else
            {
                // Only part of the frame fits, split the frame
                Array.Copy(nextFrame, 0, pcm, filledLength, diff);

                // Create a new frame with the remaining part
                var remainingFrame = new float[nextFrame.Length - diff];
                Array.Copy(nextFrame, diff, remainingFrame, 0, remainingFrame.Length);

                // Add the remaining frame back to the buffer at the front
                _frameBuffer.AddFirst(remainingFrame);
                break; // Since pcm is now filled, we exit the loop
            }
        }
    }

    public float[] GetNextFrameFromBuffer()
    {
        var frame = _frameBuffer.First;
        _frameBuffer.RemoveFirst();
        return frame.Value;
    }
}