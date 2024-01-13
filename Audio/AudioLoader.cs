using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscJockey.Audio.ContentProviders;
using DiscJockey.Audio.ContentProviders.Base;
using DiscJockey.Audio.Utils;
using UnityEngine;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace DiscJockey.Audio;

public class AudioLoader
{
    private static readonly List<ContentProvider> SupportedContentProviders = new()
    {
        new YouTubeContentProvider()
    };

    private static readonly YoutubeDL YouTubeDownloader = new();

    private static string _downloadedAudioDirectory;
    private static string _downloadersDirectory;
    private static int _maximumCachedDownloads;
    public static DownloadCache DownloadCache;
    private static bool _downloadCacheInUse = true;

    // param: url, error
    public static event Action<string, string> OnAudioDownloadFailed;

    // param: url, warning
    public static event Action<string, string> OnAudioDownloadWarning;

    // param: url, progress, 0 - 1
    public static event Action<string, float> OnAudioDownloadProgress;

    // param: url, title
    public static event Action<string, string> OnAudioDownloadTitleResolved;

    // param: url
    public static event Action<string> OnAudioDownloadCompleted;

    // param: filePath
    public static event Action<string> OnLoadAudioStarted;

    // param: error
    public static event Action<string> OnLoadAudioError;

    // param: the loaded AudioClip
    public static event Action<CachedAudio> OnLoadAudioCompleted;

    public static event Action OnLoadAllAudioFromDirectoryCompleted;

    // param: error
    public static event Action<string> OnLoadAllAudioFromDirectoryError;

    public static async void Init(int maximumCachedDownloads, string downloadCacheDirectory,
        string audioDownloadersDirectory = default)
    {
        if (audioDownloadersDirectory == default) audioDownloadersDirectory = downloadCacheDirectory;
        else if (!Directory.Exists(audioDownloadersDirectory)) Directory.CreateDirectory(audioDownloadersDirectory);

        if (!Directory.Exists(downloadCacheDirectory)) Directory.CreateDirectory(downloadCacheDirectory);


        if (!Directory.GetFiles(audioDownloadersDirectory).Any(file => file.Contains("yt-dl")))
            await YoutubeDLSharp.Utils.DownloadYtDlp(audioDownloadersDirectory);
        if (!Directory.GetFiles(audioDownloadersDirectory).Any(file => file.Contains("ffmpeg")))
            await YoutubeDLSharp.Utils.DownloadFFmpeg(audioDownloadersDirectory);

        DownloadCache = DownloadCache.Load(Path.Combine(downloadCacheDirectory, "cache.json"));
        _downloadedAudioDirectory = downloadCacheDirectory;
        _downloadersDirectory = audioDownloadersDirectory;
        _maximumCachedDownloads = maximumCachedDownloads;

        YouTubeDownloader.YoutubeDLPath =
            Directory.GetFiles(audioDownloadersDirectory).First(file => file.Contains("yt-dl"));
        YouTubeDownloader.FFmpegPath =
            Directory.GetFiles(audioDownloadersDirectory).First(file => file.Contains("ffmpeg"));
        YouTubeDownloader.OutputFolder = downloadCacheDirectory;
        YouTubeDownloader.OutputFileTemplate = "%(id)s.%(ext)s";

        if (DiscJockeyConfig.LocalConfig.KeepDownloadedSongsPermanently)
        {
            _downloadCacheInUse = false;
            _downloadedAudioDirectory = DiscJockeyPlugin.CustomSongsDirectory;
            YouTubeDownloader.OutputFolder = _downloadedAudioDirectory;
            YouTubeDownloader.OutputFileTemplate = "%(title)s.%(ext)s";
        }
    }

    #region Disk API

    public static async void LoadAllFromCache()
    {
        if (DownloadCache?.CachedDownloads == null || DownloadCache.CachedDownloads.Count == 0) return;
        await Task.WhenAll(DownloadCache.CachedDownloads.Select(cd => LoadAudioFromDisk(cd.Filepath, cd.Title)).ToArray());
    }

    public static async void LoadAudioClipsFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            OnLoadAllAudioFromDirectoryError?.Invoke($"Cannot load from {directory} as it doesn't exist");
            return;
        }

        var files = Directory.GetFiles(directory);

        await Task.WhenAll(files.Select(file => LoadAudioFromDisk(file, Path.GetFileNameWithoutExtension(file))).ToArray());
        OnLoadAllAudioFromDirectoryCompleted?.Invoke();
    }

    public static void ConvertAudioClipToCachedAudio(AudioClip audioClip)
    {
        OnLoadAudioStarted?.Invoke(audioClip.name);
        var audio = CachedAudio.FromAudioClip(audioClip);
        OnLoadAudioCompleted?.Invoke(audio);
    }

    public static async Task LoadAudioFromDisk(string filePath, string clipNameOverride = default)
    {
        OnLoadAudioStarted?.Invoke(filePath);

        if (!AudioUtils.IsValidAudioType(filePath))
        {
            OnLoadAudioError?.Invoke($"Unknown or invalid audio file {filePath}");
            return;
        }

        var audio = await CachedAudio.FromFilePath(filePath, clipNameOverride);
        OnLoadAudioCompleted?.Invoke(audio);
    }

    #endregion

    #region Downloader API

    

    private static string Sanitize(string fileName) => YoutubeDLSharp.Utils.Sanitize(fileName);

    private static async Task PerformContentDownload(ContentProvider provider, ParsedUri uri,
        string contentTitle = default, Action<float> onProgress = null)
    {
        if (string.IsNullOrEmpty(contentTitle))
        {
            DiscJockeyPlugin.LogInfo("Fetching content title");
            var videoDataResult = await provider.Prefetch(YouTubeDownloader, uri);

            if (!videoDataResult.Success)
            {
                OnAudioDownloadFailed?.Invoke(uri.Url, string.Join("\n", videoDataResult.ErrorOutput));
                return;
            }

            contentTitle = Sanitize(videoDataResult.Data.Title);
            DiscJockeyPlugin.LogInfo($"Resolved to {contentTitle}");
            OnAudioDownloadTitleResolved?.Invoke(uri.Url, contentTitle);
        }

        onProgress ??= progress => OnAudioDownloadProgress?.Invoke(uri.Url, progress);

        if (_downloadCacheInUse && DownloadCache.IdExistsInCache(uri.Id))
        {
            var cachedDownload = DownloadCache.GetDownloadFromCache(uri.Id);
            DiscJockeyPlugin.LogInfo($"{contentTitle} exists in our cache");
            await LoadAudioFromDisk(cachedDownload.Filepath, cachedDownload.Title);
            OnAudioDownloadCompleted?.Invoke(uri.Url);
        }
        else
        {
            DiscJockeyPlugin.LogInfo($"{contentTitle} isn't cached, downloading it");
            var result = await provider.Download(YouTubeDownloader, uri, onProgress);
            if (result.Success)
            {
                await LoadAudioFromDisk(result.Data, contentTitle);
                DownloadCache.AddToCache(new CachedDownload(uri.Id, contentTitle, result.Data));
                OnAudioDownloadCompleted?.Invoke(uri.Url);
            }
            else
            {
                OnAudioDownloadFailed?.Invoke(uri.Url, string.Join("\n", result.ErrorOutput));
            }
        }
    }

    public static async void DownloadContent(string url)
    {
        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (Exception e)
        {
            OnAudioDownloadFailed?.Invoke(url, e.ToString());
            return;
        }
        
        var supportedProvider = SupportedContentProviders.FirstOrDefault(p => p.Hosts.Contains(uri.Host));

        if (supportedProvider == null)
        {
            DownloadUnsupportedContent(url);
            return;
        }

        var parsedUri = supportedProvider.ParseUri(uri);

        if (parsedUri.ContentType == ContentType.Playlist)
        {
            DiscJockeyPlugin.LogInfo("Content is a playlist");
            var playlistInfo = await supportedProvider.FetchPlaylistInformation(YouTubeDownloader, parsedUri);
            if (playlistInfo.Success)
            {
                var downloaded = 0;
                var count = playlistInfo.Data.PlaylistContents.Count;
                OnAudioDownloadTitleResolved?.Invoke(parsedUri.Url,
                    $"{playlistInfo.Data.Title} ({downloaded}/{count})");
                foreach (var item in playlistInfo.Data.PlaylistContents)
                {
                    await PerformContentDownload(supportedProvider, supportedProvider.ParseUri(new Uri(item.Url)),
                        Sanitize(item.Title), progress => OnAudioDownloadProgress?.Invoke(parsedUri.Url, progress));
                    downloaded++;
                    OnAudioDownloadTitleResolved?.Invoke(parsedUri.Url,
                        $"{playlistInfo.Data.Title} ({downloaded}/{count})");
                }
                
                OnAudioDownloadCompleted?.Invoke(parsedUri.Url);
            }
            else
            {
                OnAudioDownloadFailed?.Invoke(url, string.Join("\n", playlistInfo.ErrorOutput));
            }
        }
        else
        {
            DiscJockeyPlugin.LogInfo("Content is standard");
            await PerformContentDownload(supportedProvider, parsedUri);
        }
    }

    public static async Task<RunResult<VideoData>> PrefetchUnsupportedContentInformation(string url)
    {
        return await YouTubeDownloader.RunVideoDataFetch(url);
    }

    private static async void DownloadUnsupportedContent(string url)
    {
        OnAudioDownloadWarning?.Invoke(url,
            $"Downloading content from {new Uri(url).Host} is not directly supported. Download is proceeding, but issues may occur. Please report a bug if you encounter problems so that we can look into directly supporting this content provider.");

        var videoDataResult = await PrefetchUnsupportedContentInformation(url);

        if (!videoDataResult.Success)
        {
            OnAudioDownloadFailed?.Invoke(url, string.Join("\n", videoDataResult.ErrorOutput));
            return;
        }

        if (_downloadCacheInUse && DownloadCache.IdExistsInCache(videoDataResult.Data.ID))
        {
            var cachedDownload = DownloadCache.GetDownloadFromCache(videoDataResult.Data.ID);
            await LoadAudioFromDisk(cachedDownload.Filepath,
                cachedDownload.Title);
            OnAudioDownloadCompleted?.Invoke(url);
        }
        else
        {
            var progress = new Progress<DownloadProgress>(downloadProgress =>
            {
                OnAudioDownloadProgress?.Invoke(url, downloadProgress.Progress);
            });

            var result = await YouTubeDownloader.RunAudioDownload(url, AudioConversionFormat.Mp3, progress:progress);

            if (result.Success)
            {
                var title = Sanitize(videoDataResult.Data.Title);
                await LoadAudioFromDisk(result.Data, title);
                DownloadCache.AddToCache(new CachedDownload(videoDataResult.Data.ID, title, result.Data));

                OnAudioDownloadCompleted?.Invoke(url);
            }
            else
            {
                OnAudioDownloadFailed?.Invoke(url, string.Join("\n", result.ErrorOutput));
            }
        }
    }

    #endregion
}