using System;
using System.Runtime.InteropServices;

namespace YoutubeDLSharp.Helpers;

internal static class OSHelper
{
    public static bool IsWindows => GetOSVersion() == OSVersion.Windows;

    /// <summary>
    ///     Gets the <see cref="OSVersion" /> depending on what platform you are on
    /// </summary>
    /// <returns>Returns the OS Version</returns>
    /// <exception cref="Exception"></exception>
    internal static OSVersion GetOSVersion()
    {
#if NET45
            return OSVersion.Windows;
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSVersion.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSVersion.OSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSVersion.Linux;
        throw new Exception("Your OS isn't supported");
#endif
    }
}

internal enum OSVersion
{
    Windows,
    OSX,
    Linux
}