using System;

namespace DiscJockey.Utils;

public static class StringExtensions
{
    public static string EnsureUrlSchemeExists(this string str)
    {
        if (str.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            str.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return str;
        return $"https://{str}";
    }
}