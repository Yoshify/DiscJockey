using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscJockey.Data;
using DiscJockey.Utils;
using UnityEngine;
using UnityEngine.Networking;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace DiscJockey.API
{
    public class AudioLoaderAPI
    {
        private static readonly YoutubeDL YouTubeDownloader = new YoutubeDL();

        // param: taskId, url
        public static event Action<string, string> OnAudioDownloadStarted;
        // param: taskId, the resolved video data
        public static event Action<string, VideoData> OnAudioDownloadInitialResolutionCompleted;
        // param: taskId, error
        public static event Action<string, string> OnAudioDownloadFailed;
        // param: taskId, progress, 0 - 1
        public static event Action<string, float> OnAudioDownloadProgress;
        // param: taskId
        public static event Action<string> OnAudioDownloadCompleted;
        
        // param: filePath
        public static event Action<string, FileSource, string> OnLoadAudioStarted;
        // param: error
        public static event Action<string, FileSource, string> OnLoadAudioError;
        // param: the loaded AudioClip
        public static event Action<AudioClip, FileSource, string> OnLoadAudioCompleted;

        public static event Action OnLoadAllAudioFromDirectoryCompleted;
        // param: error
        public static event Action<string> OnLoadAllAudioFromDirectoryError;

        public enum FileSource
        {
            LocalFile,
            DownloadedFile
        }

        private static string _downloadedAudioDirectory;
        private static string _downloadersDirectory;
        private static int _maximumCachedDownloads;
        
        public static async void Init(int maximumCachedDownloads, string downloadedAudioDirectory, string audioDownloadersDirectory = default)
        {
            if (audioDownloadersDirectory == default) audioDownloadersDirectory = downloadedAudioDirectory;
            else if(!Directory.Exists(audioDownloadersDirectory)) Directory.CreateDirectory(audioDownloadersDirectory);

            if(!Directory.Exists(downloadedAudioDirectory)) Directory.CreateDirectory(downloadedAudioDirectory);
            
            
            if (!Directory.GetFiles(audioDownloadersDirectory).Any(file => file.Contains("yt-dl"))) await YoutubeDLSharp.Utils.DownloadYtDlp(audioDownloadersDirectory);
            if (!Directory.GetFiles(audioDownloadersDirectory).Any(file => file.Contains("ffmpeg"))) await YoutubeDLSharp.Utils.DownloadFFmpeg(audioDownloadersDirectory);

            _downloadedAudioDirectory = downloadedAudioDirectory;
            _downloadersDirectory = audioDownloadersDirectory;
            _maximumCachedDownloads = maximumCachedDownloads;
            
            YouTubeDownloader.YoutubeDLPath = Directory.GetFiles(audioDownloadersDirectory).First(file => file.Contains("yt-dl"));
            YouTubeDownloader.FFmpegPath = Directory.GetFiles(audioDownloadersDirectory).First(file => file.Contains("ffmpeg"));
            YouTubeDownloader.OutputFolder = downloadedAudioDirectory;
            YouTubeDownloader.OutputFileTemplate = "%(title)s.%(ext)s";
        }

        public static string GetUniqueTaskId() => Guid.NewGuid().ToString();

        #region Disk API

        // public static void LoadAudioClipsFromDirectory(string directory)
        // {
        //     audioLoaderCoroutineHelper.StartCoroutine(LoadAudioClipsFromDirectoryRoutine(directory));
        // }

        // private static IEnumerator LoadAudioClipsFromDirectoryRoutine(string directory)
        // {
        //     DiscJockeyPlugin.LogInfo("LoadAudioClipsFromDirectoryRoutine called");
        //     if (!Directory.Exists(directory))
        //     {
        //         OnLoadAllAudioFromDirectoryError?.Invoke($"Cannot load from {directory} as it doesn't exist");
        //         yield break;
        //     }
        //
        //     var files = Directory.GetFiles(directory);
        //
        //     if(files.Length == 0)
        //     {
        //         OnLoadAllAudioFromDirectoryError?.Invoke($"Found no files to load in {directory}");
        //         yield break;
        //     }
        //
        //     foreach(var file in files)
        //     {
        //         yield return audioLoaderCoroutineHelper.StartCoroutine(LoadAudioClipFromDiskRoutine(file));
        //     }
        // }
        
        public static async void LoadAudioClipsFromDirectory(string directory)
        {
            DiscJockeyPlugin.LogInfo("LoadAudioClipsFromDirectoryRoutine called");
            if (!Directory.Exists(directory))
            {
                OnLoadAllAudioFromDirectoryError?.Invoke($"Cannot load from {directory} as it doesn't exist");
                return;
            }

            var files = Directory.GetFiles(directory);

            if(files.Length == 0)
            {
                OnLoadAllAudioFromDirectoryError?.Invoke($"Found no files to load in {directory}");
                return;
            }

            var taskList = new List<Task>();

            foreach(var file in files)
            {
                //Task.WaitAll()
                taskList.Add(LoadAudioClipFromDisk(file, GetUniqueTaskId()));
                //yield return audioLoaderCoroutineHelper.StartCoroutine(LoadAudioClipFromDiskRoutine(file));
            }

            await Task.WhenAll(taskList.ToArray());
        }

        // public static void LoadAudioClipFromDisk(string filePath)
        // {
        //     audioLoaderCoroutineHelper.StartCoroutine(LoadAudioClipFromDiskRoutine(filePath));
        // }

        // private static IEnumerator LoadAudioClipFromDiskRoutine(string filePath)
        // {
        //     OnLoadAudioStarted?.Invoke(filePath);
        //     
        //     if (!AudioUtils.IsValidAudioType(filePath))
        //     {
        //         OnLoadAudioError?.Invoke($"Unknown or invalid audio file {filePath}");
        //         yield break;
        //     }
        //     
        //     using (var audioRequest = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioUtils.GetAudioType(filePath)))
        //     {
        //         ((DownloadHandlerAudioClip)audioRequest.downloadHandler).compressed = true;
        //
        //         yield return audioRequest.SendWebRequest();
        //         
        //         if (!string.IsNullOrEmpty(audioRequest.error))
        //         {
        //             OnLoadAudioError?.Invoke($"Failed to load AudioClip at {filePath}\n{audioRequest.error}");
        //         }
        //         else
        //         {
        //             var audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
        //             if (audioClip && audioClip.loadState == AudioDataLoadState.Loaded)
        //             {
        //                 audioClip.name = AudioUtils.CleanAudioFilename(filePath);
        //                 OnLoadAudioCompleted?.Invoke(audioClip);
        //             }
        //             else
        //             {
        //                 OnLoadAudioError?.Invoke($"Failed to load AudioClip at {filePath}");
        //             }
        //         }
        //     }
        // }

        public static void ClearDownloadCache()
        {
            var files = Directory.GetFiles(_downloadedAudioDirectory);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        public static void TrimDownloadCache() => new DirectoryInfo(_downloadedAudioDirectory)
            .EnumerateFiles()
            .OrderByDescending(file => file.CreationTime)
            .Skip(_maximumCachedDownloads)
            .ToList()
            .ForEach(file => file.Delete());
        
        public static async Task LoadAudioClipFromDisk(string filePath, string taskId, string clipNameOverride = default, FileSource fileSource = FileSource.LocalFile)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                taskId = GetUniqueTaskId();
            }
            
            OnLoadAudioStarted?.Invoke(filePath, fileSource, taskId);
            
            if (!AudioUtils.IsValidAudioType(filePath))
            {
                OnLoadAudioError?.Invoke($"Unknown or invalid audio file {filePath}", fileSource, taskId);
                return;
            }
            
            using (var audioRequest = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioUtils.GetAudioType(filePath)))
            {
                ((DownloadHandlerAudioClip)audioRequest.downloadHandler).compressed = true;

                await audioRequest.SendWebRequest();
                
                if (!string.IsNullOrEmpty(audioRequest.error))
                {
                    OnLoadAudioError?.Invoke($"Failed to load AudioClip at {filePath}\n{audioRequest.error}", fileSource, taskId);
                }
                else
                {
                    var audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    if (audioClip && audioClip.loadState == AudioDataLoadState.Loaded)
                    {
                        audioClip.name = string.IsNullOrEmpty(clipNameOverride)
                            ? AudioUtils.CleanAudioFilename(filePath)
                            : clipNameOverride;
                        OnLoadAudioCompleted?.Invoke(audioClip, fileSource, taskId);
                    }
                    else
                    {
                        OnLoadAudioError?.Invoke($"Failed to load AudioClip at {filePath}", fileSource, taskId);
                    }
                }
            }
        }

        #endregion

        #region Downloader API

        private static string GetCachedYouTubeFilePath(string videoTitle) =>
            Path.Combine(DiscJockeyPlugin.DownloadedAudioDirectory, YoutubeDLSharp.Utils.Sanitize(videoTitle) + ".mp3");
        
        private static bool YouTubeAudioExistsInDownloadCache(string videoTitle) =>
            File.Exists(GetCachedYouTubeFilePath(videoTitle));

        public static async Task<RunResult<VideoData>> PrefetchVideoData(string url) =>
            await YouTubeDownloader.RunVideoDataFetch(url);

        public static async void DownloadAudioFromYouTubeUrl(string taskId, string url)
        {
            TrimDownloadCache();
            
            var videoDataResult = await PrefetchVideoData(url);

            if (videoDataResult.Success && videoDataResult.Data.Duration != null)
            {
                OnAudioDownloadInitialResolutionCompleted?.Invoke(taskId, videoDataResult.Data);
            }
            else
            {
                OnAudioDownloadFailed?.Invoke(taskId, string.Join("\n", videoDataResult.ErrorOutput));
            }

            if (YouTubeAudioExistsInDownloadCache(videoDataResult.Data.Title))
            {
                await LoadAudioClipFromDisk(GetCachedYouTubeFilePath(videoDataResult.Data.Title), taskId, videoDataResult.Data.Title, FileSource.DownloadedFile);
                OnAudioDownloadCompleted?.Invoke(taskId);
            }
            else
            {
                var progress = new Progress<DownloadProgress>((downloadProgress) =>
                {
                    OnAudioDownloadProgress?.Invoke(taskId, downloadProgress.Progress);
                });

                var res = await YouTubeDownloader.RunAudioDownload(url, YoutubeDLSharp.Options.AudioConversionFormat.Mp3, default, progress);

                if(res.Success)
                {
                    await LoadAudioClipFromDisk(res.Data, taskId, videoDataResult.Data.Title, FileSource.DownloadedFile);
                    OnAudioDownloadCompleted?.Invoke(taskId);
                }
                else
                {
                    OnAudioDownloadFailed?.Invoke(taskId, string.Join("\n", res.ErrorOutput));
                }
            }
        }

        #endregion
    }
}
