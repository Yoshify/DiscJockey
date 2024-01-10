using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscJockey.Audio.ContentProviders.Base;

public class PlaylistInfo : IProgress<string>
{
    public List<ContentInfo> PlaylistContents = new();
    public string PlaylistId;
    public string Title;

    public PlaylistInfo(string id, string title)
    {
        PlaylistId = id;
        Title = title;
    }

    public void Report(string value)
    {
        // This TryCatch is here because sometimes YouTubeDLSharp just reports an empty string or nonsense?
        try
        {
            var deserializedContentInfo = JsonConvert.DeserializeObject<ContentInfo>(value);
            PlaylistContents.Add(deserializedContentInfo);
        }
        catch
        {
        }
    }
}