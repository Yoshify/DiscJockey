using Newtonsoft.Json;

namespace DiscJockey.Audio.ContentProviders.Base;

public class ContentInfo
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("url")] public string Url { get; set; }

    [JsonProperty("title")] public string Title { get; set; }
}