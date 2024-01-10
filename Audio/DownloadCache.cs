using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DiscJockey.Audio;

public class DownloadCache
{
    private string _filePath;
    public List<CachedDownload> CachedDownloads { get; private set; } = new ();

    public bool IdExistsInCache(string id)
    {
        var match = CachedDownloads.FirstOrDefault(cd => cd.Id == id);
        if (match != null)
        {
            if (!File.Exists(match.Filepath))
            {
                DiscJockeyPlugin.LogWarning("DownloadCache<IdExistsInCache>: Orphaned file in cache");
                CachedDownloads.Remove(match);
                Save();
                return false;
            }

            return true;
        }

        return false;
    }
    
    public CachedDownload GetDownloadFromCache(string id) => CachedDownloads.First(cd => cd.Id == id);

    public static DownloadCache Load(string cacheFilePath)
    {
        if (!File.Exists(cacheFilePath))
        {
            File.Create(cacheFilePath);
            var newCache = new DownloadCache
            {
                _filePath = cacheFilePath
            };
            return newCache;
        }
        
        try
        {
            using var fileReader = File.OpenText(cacheFilePath);
            var downloadCache = JsonConvert.DeserializeObject<DownloadCache>(fileReader.ReadToEnd());
            downloadCache._filePath = cacheFilePath;
            return downloadCache;
        }
        catch (Exception e)
        {
            DiscJockeyPlugin.LogWarning($"DownloadCache<Load>: Error occurred while loading the Download Cache, resetting it.");
            File.Delete(cacheFilePath);

            var directory = Path.GetDirectoryName(cacheFilePath);
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            
            File.Create(cacheFilePath);
            
            var newCache = new DownloadCache
            {
                _filePath = cacheFilePath
            };
            return newCache;
        }
    }

    public void Clear()
    {
        foreach (var cachedDownload in CachedDownloads)
        {
            if (File.Exists(cachedDownload.Filepath))
            {
                File.Delete(cachedDownload.Filepath);
            }
            else
            {
                DiscJockeyPlugin.LogWarning("DownloadCache<Clear>: Orphaned file in cache");
            }
        }
        CachedDownloads.Clear();
        Save();
    }

    public void AddToCache(CachedDownload download)
    {
        TrimCacheIfNecessary();
        CachedDownloads.Add(download);
        Save();
    }

    private void TrimCacheIfNecessary()
    {
        if (CachedDownloads.Count >= DiscJockeyConfig.LocalConfig.MaxCachedDownloads)
        {
            while (CachedDownloads.Count >= DiscJockeyConfig.LocalConfig.MaxCachedDownloads)
            {
                if (!File.Exists(CachedDownloads[0].Filepath))
                {
                    DiscJockeyPlugin.LogWarning("DownloadCache<TrimCacheIfNecessary>: Orphaned file in cache");
                    CachedDownloads.RemoveAt(0);
                    continue;
                }
                
                File.Delete(CachedDownloads[0].Filepath);
                CachedDownloads.RemoveAt(0);
            }
        }
    }

    public void Save() => File.WriteAllText(_filePath, JsonConvert.SerializeObject(this));
}