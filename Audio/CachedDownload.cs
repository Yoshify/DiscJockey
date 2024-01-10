namespace DiscJockey.Audio;

public class CachedDownload
{
    public string Id { get; private set; }
    public string Title { get; private set; }
    public string Filepath { get; private set; }

    public CachedDownload(string id, string title, string filepath)
    {
        Id = id;
        Title = title;
        Filepath = filepath;
    }
}