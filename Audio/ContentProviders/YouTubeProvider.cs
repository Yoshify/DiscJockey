using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using DiscJockey.Audio.ContentProviders.Base;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace DiscJockey.Audio.ContentProviders;

public class YouTubeContentProvider : ContentProvider
{
    public override IEnumerable<string> Hosts => new[] { "youtube.com", "www.youtube.com", "youtu.be", "www.youtu.be" };

    public override ParsedUri ParseUri(Uri uri)
    {
        if (uri.Host.Contains("youtu.be"))
            return new ParsedUri(uri, uri.AbsolutePath[1..], uri.Host + uri.AbsolutePath, ContentType.Standard);

        var contentType = ContentType.Standard;
        var queryStrings = HttpUtility.ParseQueryString(uri.Query);
        var id = queryStrings.Get("v");

        if (id == null)
        {
            id = queryStrings.Get("list");
            contentType = ContentType.Playlist;
        }

        return string.IsNullOrEmpty(id) ? null : new ParsedUri(uri, id, uri.OriginalString, contentType);
    }

    public override async Task<RunResult<VideoData>> Prefetch(YoutubeDL downloader, ParsedUri uri)
    {
        return await downloader.RunVideoDataFetch(uri.Url, overrideOptions: new OptionSet
        {
            DumpJson = false,
            ForceOverwrites = true,
            SocketTimeout = 120,
        });
    }

    public override async Task<RunResult<PlaylistInfo>> FetchPlaylistInformation(YoutubeDL downloader, ParsedUri uri,
        Action<float> onProgressCallback = null)
    {
        var progress = new Progress<DownloadProgress>(downloadProgress =>
        {
            onProgressCallback?.Invoke(downloadProgress.Progress);
        });

        var outputCache = new PlaylistInfo(uri.Id, "YouTube Playlist");
        var result = await downloader.RunAudioPlaylistDownload(uri.Url, format: AudioConversionFormat.Mp3,
            progress: progress, output: outputCache, overrideOptions: new OptionSet
            {
                FlatPlaylist = true,
                DumpJson = true,
                ForceOverwrites = true,
                SocketTimeout = 60,
            });
        return new RunResult<PlaylistInfo>(result.Success, result.ErrorOutput, outputCache);
    }

    public override async Task<RunResult<string>> Download(YoutubeDL downloader, ParsedUri uri,
        Action<float> onProgressCallback = null)
    {
        var progress = new Progress<DownloadProgress>(downloadProgress =>
        {
            onProgressCallback?.Invoke(downloadProgress.Progress);
        });

        return await downloader.RunAudioDownload(uri.Url, AudioConversionFormat.Mp3, progress: progress, overrideOptions: new OptionSet
        {
            DumpJson = false,
            ForceOverwrites = true,
            SocketTimeout = 60,
        });
    }
}