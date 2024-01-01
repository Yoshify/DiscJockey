using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using DiscJockey.API;
using DiscJockey.Data;
using DiscJockey.Utils;

namespace DiscJockey.Managers
{
    public static class DiscJockeyAudioManager
    {
        public static readonly TrackList TrackList = new TrackList();
        public static event Action<Track, AudioLoaderAPI.FileSource, string> OnTrackAddedToTrackList;

        private static bool _hasInitialized;
        private static bool _hasFinishedLoading;

        public static bool IsReady() => _hasInitialized && _hasFinishedLoading;

        public static void Init()
        {
            if (_hasInitialized) return;
            _hasInitialized = true;
            EnsureAudioDirectoryExists();
            
            SetupListeners();
            DiscJockeyPlugin.LogInfo($"DiscJockeyAudioManager<Init>: Loading audio from {DiscJockeyPlugin.CustomSongsDirectory}");
            AudioLoaderAPI.LoadAudioClipsFromDirectory(DiscJockeyPlugin.CustomSongsDirectory);
            SearchAndLoadOtherPluginSongs();

            if (DiscJockeyConfig.ClearDownloadCacheAtReboot.Value)
            {
                DiscJockeyPlugin.LogInfo($"DiscJockeyAudioManager<Init>: Config value [ClearDownloadCacheAtReboot] is TRUE. Clearing cache.");
                AudioLoaderAPI.ClearDownloadCache();
            }

            if (DiscJockeyConfig.LoadDownloadedSongsFromCacheAtLaunch.Value)
            {
                DiscJockeyPlugin.LogInfo($"DiscJockeyAudioManager<Init>: Config value [LoadDownloadedSongsFromCacheAtLaunch] is TRUE. Loading cached songs.");
                AudioLoaderAPI.LoadAudioClipsFromDirectory(DiscJockeyPlugin.DownloadedAudioDirectory);
            }
        }

        private static void SearchAndLoadOtherPluginSongs()
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyAudioManager<SearchAndLoadOtherPluginSongs>: Scanning for other plugins using Custom Songs...");
            foreach (var directory in Directory.GetDirectories(Paths.PluginPath))
            {
                var customSongPath = Path.Combine(directory, "Custom Songs");
                if (Directory.Exists(customSongPath))
                {
                    DiscJockeyPlugin.LogInfo($"DiscJockeyAudioManager<SearchAndLoadOtherPluginSongs>: Found. Loading {customSongPath}");
                    AudioLoaderAPI.LoadAudioClipsFromDirectory(customSongPath);
                }
            }
        }

        private static void SetupListeners()
        {
            AudioLoaderAPI.OnLoadAudioStarted += (filePath, source, taskId) =>
            {
                DiscJockeyPlugin.LogInfo($"OnLoadAudioFromDiskStarted: Loading {filePath}");
            };

            AudioLoaderAPI.OnLoadAudioError += (error, source, taskId) =>
            {
                DiscJockeyPlugin.LogError($"OnLoadAudioFromDiskError: {error}");
            };

            AudioLoaderAPI.OnLoadAudioCompleted += (audioClip, source, taskId) =>
            {
                DiscJockeyPlugin.LogInfo($"OnLoadAudioFromDiskCompleted: Loaded {audioClip.name}");
                var track = TrackList.Add(audioClip);
                OnTrackAddedToTrackList?.Invoke(track, source, taskId);
            };

            AudioLoaderAPI.OnLoadAllAudioFromDirectoryCompleted += () =>
            {
                DiscJockeyPlugin.LogInfo($"OnLoadAllAudioFromDirectoryCompleted: All clips loaded");
                _hasFinishedLoading = true;
            };

            AudioLoaderAPI.OnLoadAllAudioFromDirectoryError += (error) =>
            {
                DiscJockeyPlugin.LogError($"OnLoadAllAudioFromDirectoryError: {error}");
            };
        }
        
        public static void RequestPlayTrack(Track track)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyAudioManager<RequestPlayTrack>: Requesting {track.Metadata.Name} at {track.Metadata.Index}");
            DiscJockeyNetworkManager.Instance.RequestPlayForBoomboxServerRpc(
                DiscJockeyBoomboxManager.ActiveBoombox.NetworkObjectId,
                new BoomboxMetadata(
                    track.Metadata,
                    DiscJockeyBoomboxManager.ActiveBoomboxMetadata.TrackMode
                )
            );
        }

        public static void RequestStopTrack()
        {
            DiscJockeyPlugin.LogInfo(
                $"DiscJockeyAudioManager<RequestStopTrack>: Stop Requested");
            DiscJockeyNetworkManager.Instance.RequestStopForBoomboxServerRpc(DiscJockeyBoomboxManager.ActiveBoombox.NetworkObjectId);
        }

        public static void DownloadYouTubeAudio(string url, string taskId)
        {
            DiscJockeyPlugin.LogInfo(
                $"DiscJockeyAudioManager<DownloadYouTubeAudio>: Download requested for {url}, assigned taskId of {taskId}");
            DiscJockeyNetworkManager.Instance.DownloadYouTubeAudioServerRpc(LocalPlayerHelper.Player.playerClientId, taskId, url);
        }

        private static void EnsureAudioDirectoryExists()
        {
            if(!Directory.Exists(DiscJockeyPlugin.CustomSongsDirectory))
            {
                Directory.CreateDirectory(DiscJockeyPlugin.CustomSongsDirectory);
            }
        }
    }
}
