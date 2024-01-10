using System;

namespace DiscJockey.Audio.ContentProviders.Base;

public class ParsedUri
{
    public ParsedUri(Uri uri, string id, string url, ContentType contentType)
    {
        Uri = uri;
        Id = id;
        Url = url;
        ContentType = contentType;
    }

    public Uri Uri { get; }
    public string Id { get; }
    public string Url { get; }
    public ContentType ContentType { get; }
}