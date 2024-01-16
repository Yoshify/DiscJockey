using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YoutubeDLSharp.Helpers;

namespace YoutubeDLSharp;

/// <summary>
///     Utility methods.
/// </summary>
public static class Utils
{
    private static HttpClient _client;

    // With UseCookies we'll get Illegal Byte Sequence exceptions thrown on request, all of which goes away if we set UseCookies to false.
    // We don't need cookies anyway, we're just firing and forgetting a download request.
    private static HttpClient Client =>
        _client ??= new HttpClient(new HttpClientHandler
        {
            UseCookies = false
        });

    private static readonly Regex rgxTimestamp = new("[0-9]+(?::[0-9]+)+", RegexOptions.Compiled);

    private static readonly Dictionary<char, string> accentChars
        = "ÂÃÄÀÁÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖŐØŒÙÚÛÜŰÝÞßàáâãäåæçèéêëìíîïðñòóôõöőøœùúûüűýþÿ"
            .Zip(new[]
                {
                    "A", "A", "A", "A", "A", "A", "AE", "C", "E", "E", "E", "E", "I", "I", "I", "I", "D", "N",
                    "O", "O", "O", "O", "O", "O", "O", "OE", "U", "U", "U", "U", "U", "Y", "P", "ss",
                    "a", "a", "a", "a", "a", "a", "ae", "c", "e", "e", "e", "e", "i", "i", "i", "i", "o", "n",
                    "o", "o", "o", "o", "o", "o", "o", "oe", "u", "u", "u", "u", "u", "y", "p", "y"
                },
                (c, s) => new { Key = c, Val = s }).ToDictionary(o => o.Key, o => o.Val);

    /// <summary>
    ///     Sanitize a string to be a valid file name.
    ///     Ported from:
    ///     https://github.com/ytdl-org/youtube-dl/blob/33c1c7d80fd99024879a5f087b55b24374385e43/youtube_dl/utils.py#L2067
    /// </summary>
    /// <returns></returns>
    public static string Sanitize(string s, bool restricted = false)
    {
        rgxTimestamp.Replace(s, m => m.Groups[0].Value.Replace(':', '_'));
        var result = string.Join("", s.Select(c => sanitizeChar(c, restricted)));
        result = result.Replace("__", "_").Trim('_');
        if (restricted && result.StartsWith("-_"))
            result = result.Substring(2);
        if (result.StartsWith("-"))
            result = "_" + result.Substring(1);
        result = result.TrimStart('.');
        if (string.IsNullOrWhiteSpace(result))
            result = "_";
        return result;
    }

    private static string sanitizeChar(char c, bool restricted)
    {
        if (restricted && accentChars.ContainsKey(c))
            return accentChars[c];
        if (c == '?' || c < 32 || c == 127)
            return "";
        if (c == '"')
            return restricted ? "" : "\'";
        if (c == ':')
            return restricted ? "_-" : " -";
        if ("\\/|*<>".Contains(c))
            return "_";
        if (restricted && "!&\'()[]{}$;`^,# ".Contains(c))
            return "_";
        if (restricted && c > 127)
            return "_";
        return c.ToString();
    }

    /// <summary>
    ///     Returns the absolute path for the specified path string.
    ///     Also searches the environment's PATH variable.
    /// </summary>
    /// <param name="fileName">The relative path string.</param>
    /// <returns>The absolute path or null if the file was not found.</returns>
    public static string GetFullPath(string fileName)
    {
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        var values = Environment.GetEnvironmentVariable("PATH");
        foreach (var p in values.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(p, fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    #region Download Helpers

    private static readonly Dictionary<OSVersion, FFBinaryDownloadMetadata> FfmpegDownloads =
        new()
        {
            {
                OSVersion.Windows,
                new FFBinaryDownloadMetadata(
                    "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip",
                    "04807e036638e2ad95f42dc8f7ec426d")
            },
            {
                OSVersion.OSX,
                new FFBinaryDownloadMetadata(
                    "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-macos-64.zip",
                    "3442106c85dea60302ae3a972494327d")
            },
            {
                OSVersion.Linux,
                new FFBinaryDownloadMetadata(
                    "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-64.zip",
                    "bf25e58a1799882782a5106b104e2673")
            }
        };

    private static readonly Dictionary<OSVersion, FFBinaryDownloadMetadata> FfprobeDownloads =
        new()
        {
            {
                OSVersion.Windows,
                new FFBinaryDownloadMetadata(
                    "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-win-64.zip",
                    "412447f53830826bf91406c88f72d88a")
            },
            {
                OSVersion.OSX,
                new FFBinaryDownloadMetadata(
                    "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-macos-64.zip",
                    "ce0d21460e432a84398fccaf46b89327")
            },
            {
                OSVersion.Linux,
                new FFBinaryDownloadMetadata(
                    "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-linux-64.zip",
                    "0dff2a6d2410a9c5d98684a087473c54")
            }
        };

    public struct FFBinaryDownloadMetadata
    {
        public string URL;
        public string ExpectedMD5Checksum;

        public FFBinaryDownloadMetadata(string url, string expectedMD5Checksum)
        {
            URL = url;
            ExpectedMD5Checksum = expectedMD5Checksum;
        }
    }

    public static string YtDlpBinaryName => GetYtDlpBinaryName();
    public static string FfmpegBinaryName => GetFfmpegBinaryName();
    public static string FfprobeBinaryName => GetFfprobeBinaryName();

    public static async Task DownloadBinaries(bool skipExisting = true, string directoryPath = "")
    {
        if (skipExisting)
        {
            if (!File.Exists(Path.Combine(directoryPath, GetYtDlpBinaryName()))) await DownloadYtDlp(directoryPath);
            if (!File.Exists(Path.Combine(directoryPath, GetFfmpegBinaryName()))) await DownloadFFmpeg(directoryPath);
            if (!File.Exists(Path.Combine(directoryPath, GetFfprobeBinaryName()))) await DownloadFFprobe(directoryPath);
        }
        else
        {
            await DownloadYtDlp(directoryPath);
            await DownloadFFmpeg(directoryPath);
            await DownloadFFprobe(directoryPath);
        }
    }

    private static string GetYtDlpDownloadUrl()
    {
        const string BASE_GITHUB_URL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp";

        string downloadUrl;
        switch (OSHelper.GetOSVersion())
        {
            case OSVersion.Windows:
                downloadUrl = $"{BASE_GITHUB_URL}.exe";
                break;
            case OSVersion.OSX:
                downloadUrl = $"{BASE_GITHUB_URL}_macos";
                break;
            case OSVersion.Linux:
                downloadUrl = BASE_GITHUB_URL;
                break;
            default:
                throw new Exception("Your OS isn't supported");
        }

        return downloadUrl;
    }

    private static string GetYtDlpBinaryName()
    {
        var ytdlpDownloadPath = GetYtDlpDownloadUrl();
        return Path.GetFileName(ytdlpDownloadPath);
    }

    private static string GetFfmpegBinaryName()
    {
        switch (OSHelper.GetOSVersion())
        {
            case OSVersion.Windows:
                return "ffmpeg.exe";
            case OSVersion.OSX:
            case OSVersion.Linux:
                return "ffmpeg";
            default:
                throw new Exception("Your OS isn't supported");
        }
    }

    private static string GetFfprobeBinaryName()
    {
        switch (OSHelper.GetOSVersion())
        {
            case OSVersion.Windows:
                return "ffprobe.exe";
            case OSVersion.OSX:
            case OSVersion.Linux:
                return "ffprobe";
            default:
                throw new Exception("Your OS isn't supported");
        }
    }

    public static async Task DownloadYtDlp(string directoryPath = "")
    {
        var downloadUrl = GetYtDlpDownloadUrl();

        if (string.IsNullOrEmpty(directoryPath)) directoryPath = Directory.GetCurrentDirectory();

        var downloadLocation = Path.Combine(directoryPath, Path.GetFileName(downloadUrl));
        var data = await DownloadFileBytesAsync(downloadUrl);
        File.WriteAllBytes(downloadLocation, data);
    }

    public static async Task DownloadFFmpeg(string directoryPath = "")
    {
        await FFDownloader(directoryPath);
    }

    public static async Task DownloadFFprobe(string directoryPath = "")
    {
        await FFDownloader(directoryPath, FFmpegApi.BinaryType.FFprobe);
    }

    private static async Task FFDownloader(string directoryPath = "",
        FFmpegApi.BinaryType binary = FFmpegApi.BinaryType.FFmpeg)
    {
        if (string.IsNullOrEmpty(directoryPath)) directoryPath = Directory.GetCurrentDirectory();

        var os = OSHelper.GetOSVersion();
        var downloadMetadata = binary == FFmpegApi.BinaryType.FFmpeg ? FfmpegDownloads[os] : FfprobeDownloads[os];
        var dataBytes = await DownloadFileBytesAsync(downloadMetadata.URL);
        using (var md5 = MD5.Create())
        {
            var checksumBytes = md5.ComputeHash(dataBytes);
            var actualChecksum = BitConverter.ToString(checksumBytes).Replace("-", string.Empty);

            if (string.Equals(actualChecksum, downloadMetadata.ExpectedMD5Checksum, StringComparison.OrdinalIgnoreCase))
            {
                using (var stream = new MemoryStream(dataBytes))
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        if (archive.Entries.Count > 0)
                            archive.Entries[0].ExtractToFile(Path.Combine(directoryPath, archive.Entries[0].FullName),
                                true);
                    }
                }
            }
            else
            {
                throw new Exception(
                    $"Invalid file checksum at {downloadMetadata.URL} - expected {downloadMetadata.ExpectedMD5Checksum}, got {actualChecksum}");
            }
        }
    }

    private static async Task<byte[]> DownloadFileBytesAsync(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var _))
            throw new InvalidOperationException("URI is invalid.");

        var bytes = await Client.GetByteArrayAsync(uri);
        return bytes;
    }


    internal class FFmpegApi
    {
        public enum BinaryType
        {
            [EnumMember(Value = "ffmpeg")] FFmpeg,
            [EnumMember(Value = "ffprobe")] FFprobe
        }

        public class Root
        {
            [JsonProperty("version")] public string Version { get; set; }

            [JsonProperty("permalink")] public string Permalink { get; set; }

            [JsonProperty("bin")] public Bin Bin { get; set; }
        }

        public class Bin
        {
            [JsonProperty("windows-64")] public OsBinVersion Windows64 { get; set; }

            [JsonProperty("linux-64")] public OsBinVersion Linux64 { get; set; }

            [JsonProperty("osx-64")] public OsBinVersion Osx64 { get; set; }
        }

        public class OsBinVersion
        {
            [JsonProperty("ffmpeg")] public string Ffmpeg { get; set; }

            [JsonProperty("ffprobe")] public string Ffprobe { get; set; }
        }
    }

    #endregion
}