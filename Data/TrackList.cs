using System;
using System.Collections.Generic;
using DiscJockey.API;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DiscJockey.Data
{
    public class TrackList
    {
        private readonly List<Track> _trackList = new List<Track>();
        
        private int GetValidTrackIndex(int trackIndex)
        {
            if (trackIndex >= _trackList.Count)
            {
                return 0;
            }

            if (trackIndex < 0)
            {
                return _trackList.Count - 1;
            }

            return trackIndex;
        }
        
        private int GetRandomTrackIndex() => Mathf.FloorToInt(Random.Range(0, _trackList.Count));

        private int GetRandomTrackIndex(int excluding)
        {
            var rand = Mathf.FloorToInt(Random.Range(0, _trackList.Count));
            return rand == excluding ? GetRandomTrackIndex(excluding) : rand;
        }

        public Track GetTrackAtIndex(int trackIndex)
        {
            return _trackList[GetValidTrackIndex(trackIndex)];
        }

        public Track Add(AudioClip audioClip, string id = null)
        {
            var track = new Track(
                audioClip,
                new TrackMetadata(
                    audioClip.name, -1, audioClip.length
                ));
            _trackList.Add(track);
            return track;
        }

        public Track GetTrack(int trackIndex) => _trackList[trackIndex];

        public int GetNextTrackIndex(int currentTrackIndex, TrackMode trackMode, bool ignoreRepeat = false)
        {
            switch (trackMode)
            {
                case TrackMode.Sequential:
                    return GetValidTrackIndex(currentTrackIndex + 1);
                case TrackMode.Shuffle:
                    return GetValidTrackIndex(GetRandomTrackIndex(currentTrackIndex));
                case TrackMode.Repeat:
                    return ignoreRepeat ? GetValidTrackIndex(currentTrackIndex + 1) : currentTrackIndex;
                default:
                    return GetValidTrackIndex(currentTrackIndex + 1);
            }
        }

        public Track GetRandomTrack() => _trackList[GetRandomTrackIndex()];
        
        public int GetPreviousTrackIndex(int currentTrackIndex) => GetValidTrackIndex(currentTrackIndex - 1);

        public Track GetNextTrack(int currentTrackIndex, TrackMode trackMode, bool ignoreRepeat = false) =>
            _trackList[GetNextTrackIndex(currentTrackIndex, trackMode, ignoreRepeat)];

        public Track GetPreviousTrack(int currentTrackIndex) =>
            _trackList[GetPreviousTrackIndex(currentTrackIndex)];
    }
}