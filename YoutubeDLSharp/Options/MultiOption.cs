using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDLSharp.Options;

/// <summary>
///     Represents a yt-dlp option that can be set multiple times.
/// </summary>
/// <typeparam name="T">The type of the option.</typeparam>
public class MultiOption<T> : IOption
{
    private MultiValue<T> value;

    public MultiOption(params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
    }

    public MultiOption(bool isCustom, params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
        IsCustom = isCustom;
    }

    public MultiValue<T> Value
    {
        get => value;
        set
        {
            IsSet = !Equals(value, default(T));
            this.value = value;
        }
    }

    public string DefaultOptionString => OptionStrings.Last();

    public string[] OptionStrings { get; }

    public bool IsSet { get; private set; }

    public bool IsCustom { get; }

    public void SetFromString(string s)
    {
        var split = s.Split(' ');
        var stringValue = s.Substring(split[0].Length).Trim().Trim('"');
        if (!OptionStrings.Contains(split[0]))
            throw new ArgumentException("Given string does not match required format.");
        // Set as initial value or append to existing
        var newValue = Utils.OptionValueFromString<T>(stringValue);
        if (!IsSet)
            Value = newValue;
        else
            Value.Values.Add(newValue);
    }

    public IEnumerable<string> ToStringCollection()
    {
        if (!IsSet) return new[] { "" };
        var strings = new List<string>();
        foreach (var value in Value.Values) strings.Add(DefaultOptionString + Utils.OptionValueToString(value));
        return strings;
    }

    public override string ToString()
    {
        return string.Join(" ", ToStringCollection());
    }
}