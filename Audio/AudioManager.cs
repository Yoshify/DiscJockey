using System;
using System.IO;
using System.Linq;
using BepInEx;
using DiscJockey.Audio.Data;
using DiscJockey.Data;
using DiscJockey.Managers;
using UnityEngine;

namespace DiscJockey.Audio;

public static class AudioManager
{
    public static readonly TrackList TrackList = new();

    private static bool _hasInitialized;
    private static bool _hasFinishedLoading;

    public static float CurrentVolume => BoomboxManager.IsLookingAtOrHoldingBoombox
        ? BoomboxManager.LookedAtOrHeldBoombox.Boombox.boomboxAudio.volume
        : 0.0f;

    public static event Action<Track> OnTrackAddedToTrackList;
    public static event Action<ulong, float> OnVolumeChanged;

    private static bool _hasLoadedVanillaTracks;

    public static bool IsReady()
    {
        return _hasInitialized && _hasFinishedLoading;
    }

    public static void Init()
    {
        if (_hasInitialized) return;
        _hasInitialized = true;
        EnsureAudioDirectoryExists();

        SetupListeners();
        DiscJockeyPlugin.LogInfo(
            $"Loading audio from {DiscJockeyPlugin.CustomSongsDirectory}");
        AudioLoader.LoadAudioClipsFromDirectory(DiscJockeyPlugin.CustomSongsDirectory);
        SearchAndLoadOtherPluginSongs();

        if (DiscJockeyConfig.LocalConfig.ClearDownloadCacheAtReboot)
        {
            DiscJockeyPlugin.LogInfo(
                "Config value [ClearDownloadCacheAtReboot] is TRUE. Clearing cache.");
            AudioLoader.DownloadCache.Clear();
        }

        if (DiscJockeyConfig.LocalConfig.LoadDownloadedSongsFromCacheAtLaunch)
        {
            DiscJockeyPlugin.LogInfo(
                "Config value [LoadDownloadedSongsFromCacheAtLaunch] is TRUE. Loading cached songs.");
           AudioLoader.LoadAllFromCache();
        }
    }

    private static void SearchAndLoadOtherPluginSongs()
    {
        DiscJockeyPlugin.LogInfo(
            "Scanning for other plugins using Custom Songs...");
        var otherPluginDirectories = Directory.GetDirectories(Paths.PluginPath)
            .Where(dir => !dir.EndsWith(DiscJockeyPlugin.PluginFolderName));
        foreach (var directory in otherPluginDirectories)
        {
            var customSongPath = Path.Combine(directory, "Custom Songs");
            if (Directory.Exists(customSongPath))
            {
                DiscJockeyPlugin.LogInfo(
                    $"Loading {customSongPath}");
                AudioLoader.LoadAudioClipsFromDirectory(customSongPath);
            }
        }
    }

    private static void SetupListeners()
    {
        AudioLoader.OnLoadAudioStarted += filePath =>
        {
            DiscJockeyPlugin.LogInfo($"OnLoadAudioStarted: Loading {filePath}");
        };

        AudioLoader.OnLoadAudioError += error =>
        {
            DiscJockeyPlugin.LogError($"OnLoadAudioError: {error}");
        };

        AudioLoader.OnLoadAudioCompleted += audio =>
        {
            DiscJockeyPlugin.LogInfo($"OnLoadAudioCompleted: {audio.Name}");
            var track = TrackList.Add(audio);
            OnTrackAddedToTrackList?.Invoke(track);
        };

        AudioLoader.OnLoadAllAudioFromDirectoryCompleted += () =>
        {
            DiscJockeyPlugin.LogInfo(
                "OnLoadAllAudioFromDirectoryCompleted: All clips loaded");
            _hasFinishedLoading = true;
        };

        AudioLoader.OnLoadAllAudioFromDirectoryError += error =>
        {
            DiscJockeyPlugin.LogError($"OnLoadAllAudioFromDirectoryError: {error}");
        };

        AudioLoader.OnAudioDownloadWarning += (_, warning) =>
        {
            DiscJockeyPlugin.LogWarning($"OnAudioDownloadWarning: {warning}");
        };
    }

    public static void LoadVanillaMusicFrom(BoomboxItem boombox)
    {
        if (_hasLoadedVanillaTracks) return;

        foreach (var clip in boombox.musicAudios)
        {
            AudioLoader.ConvertAudioClipToCachedAudio(clip);
        }
        
        _hasLoadedVanillaTracks = true;
    }

    public static void RequestPlayTrack(Track track)
    {
        BoomboxManager.LookedAtOrHeldBoombox.StartStreamingTrack(track);
    }

    public static void RequestStopTrack()
    {
        BoomboxManager.LookedAtOrHeldBoombox.StopStreamAndNotify();
    }

    public static void RequestVolumeChange(float volume, bool sourceIsSlider = false)
    {
        volume = Mathf.Clamp01(volume);

        if (DiscJockeyConfig.SyncedConfig.NetworkedVolumeControl)
            DJNetworkManager.Instance.RequestBoomboxVolumeChangeServerRpc(
                BoomboxManager.LookedAtOrHeldBoombox.NetworkedBoomboxId, volume);
        else
            BoomboxManager.LookedAtOrHeldBoombox.SetVolume(volume);

        if (!sourceIsSlider) OnVolumeChanged?.Invoke(BoomboxManager.LookedAtOrHeldBoombox.NetworkedBoomboxId, volume);
    }

    public static void DownloadContent(string url)
    {
        DiscJockeyPlugin.LogInfo($"Download requested for {url}");
        AudioLoader.DownloadContent(url);
    }

    private static void EnsureAudioDirectoryExists()
    {
        if (!Directory.Exists(DiscJockeyPlugin.CustomSongsDirectory))
            Directory.CreateDirectory(DiscJockeyPlugin.CustomSongsDirectory);
    }
}