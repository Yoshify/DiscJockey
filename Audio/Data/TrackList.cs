using System;
using System.Collections.Generic;
using DiscJockey.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DiscJockey.Audio.Data;

public class TrackList
{
    private readonly List<Track> _trackList = new();

    public event Action OnTracklistSorted;

    public bool HasAnyTracks => _trackList.Count > 0;

    public void TakeOwnershipOfTracklist(ulong ownerId, string ownerName)
    {
        foreach (var track in _trackList)
        {
            DiscJockeyPlugin.LogInfo(
                $"TrackList<TakeOwnershipOfTracklist>: Setting ownership of track to ID<{ownerId}>, Name<{ownerName}>");
            track.TakeOwnership(ownerId, ownerName);
        }
    }

    private void SortTracklist()
    {
        _trackList.Sort((a, b) => string.CompareOrdinal(a.Audio.Name, b.Audio.Name));
        for (var i = 0; i < _trackList.Count; i++) _trackList[i].IndexInTracklist = i;
        OnTracklistSorted?.Invoke();
    }

    private int GetValidTrackIndex(int trackIndex)
    {
        if (trackIndex >= _trackList.Count) return 0;

        if (trackIndex < 0) return _trackList.Count - 1;

        return trackIndex;
    }

    private int GetRandomTrackIndex() => Mathf.FloorToInt(Random.Range(0, _trackList.Count));

    private int GetRandomTrackIndex(int excluding)
    {
        var rand = Mathf.FloorToInt(Random.Range(0, _trackList.Count));
        return rand == excluding ? GetRandomTrackIndex(excluding) : rand;
    }

    public Track GetTrackAtIndex(int trackIndex) => _trackList[GetValidTrackIndex(trackIndex)];

    public int GetIndexOfTrack(Track track) => _trackList.IndexOf(track);

    public Track Add(CachedAudio audio)
    {
        var track = new Track(audio)
        {
            IndexInTracklist = _trackList.Count
        };
        _trackList.Add(track);
        SortTracklist();
        return track;
    }

    public Track Get(int trackIndex) => _trackList[trackIndex];

    private int GetNextTrackIndex(int currentTrackIndex, BoomboxPlaybackMode boomboxPlaybackMode,
        bool ignoreRepeat = false)
    {
        return boomboxPlaybackMode switch
        {
            BoomboxPlaybackMode.Sequential => GetValidTrackIndex(currentTrackIndex + 1),
            BoomboxPlaybackMode.Shuffle => GetValidTrackIndex(GetRandomTrackIndex(currentTrackIndex)),
            BoomboxPlaybackMode.Repeat => ignoreRepeat ? GetValidTrackIndex(currentTrackIndex + 1) : currentTrackIndex,
            _ => GetValidTrackIndex(currentTrackIndex + 1)
        };
    }

    public Track GetRandomTrack() => _trackList[GetRandomTrackIndex()];

    private int GetPreviousTrackIndex(int currentTrackIndex) => GetValidTrackIndex(currentTrackIndex - 1);

    public Track GetNextTrack(int currentTrackIndex, BoomboxPlaybackMode boomboxPlaybackMode, bool ignoreRepeat = false) => 
        _trackList[GetNextTrackIndex(currentTrackIndex, boomboxPlaybackMode, ignoreRepeat)];
    
    public Track GetPreviousTrack(int currentTrackIndex) => _trackList[GetPreviousTrackIndex(currentTrackIndex)];
}