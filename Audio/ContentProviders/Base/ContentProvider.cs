using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace DiscJockey.Audio.ContentProviders.Base;

public abstract class ContentProvider
{
    public abstract IEnumerable<string> Hosts { get; }
    public abstract ParsedUri ParseUri(Uri uri);

    public abstract Task<RunResult<VideoData>> Prefetch(YoutubeDL downloader, ParsedUri uri);

    public abstract Task<RunResult<PlaylistInfo>> FetchPlaylistInformation(YoutubeDL downloader, ParsedUri uri,
        Action<float> onProgressCallback = null);

    public abstract Task<RunResult<string>> Download(YoutubeDL downloader, ParsedUri uri,
        Action<float> onProgressCallback = null);
}