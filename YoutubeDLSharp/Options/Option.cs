﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDLSharp.Options;

/// <summary>
///     Represents one yt-dlp option.
/// </summary>
/// <typeparam name="T">The type of the option.</typeparam>
public class Option<T> : IOption
{
    private T value;

    /// <summary>
    ///     Creates a new instance of class Option.
    /// </summary>
    public Option(params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
    }

    public Option(bool isCustom, params string[] optionStrings)
    {
        OptionStrings = optionStrings;
        IsSet = false;
        IsCustom = isCustom;
    }

    /// <summary>
    ///     The option value.
    /// </summary>
    public T Value
    {
        get => value;
        set
        {
            IsSet = !Equals(value, default(T));
            this.value = value;
        }
    }

    /// <summary>
    ///     The default string representation of the option flag.
    /// </summary>
    public string DefaultOptionString => OptionStrings.Last();

    /// <summary>
    ///     An array of all possible string representations of the option flag.
    /// </summary>
    public string[] OptionStrings { get; }

    /// <summary>
    ///     True if the option flag is set; false otherwise.
    /// </summary>
    public bool IsSet { get; private set; }

    /// <summary>
    ///     True if this option is custom.
    /// </summary>
    public bool IsCustom { get; }

    /// <summary>
    ///     Sets the option value from a given string representation.
    /// </summary>
    /// <param name="s">The string (including the option flag).</param>
    public void SetFromString(string s)
    {
        var split = s.Split(' ');
        var stringValue = s.Substring(split[0].Length).Trim().Trim('"');
        if (!OptionStrings.Contains(split[0]))
            throw new ArgumentException("Given string does not match required format.");
        Value = Utils.OptionValueFromString<T>(stringValue);
    }

    public IEnumerable<string> ToStringCollection()
    {
        return new[] { ToString() };
    }

    public override string ToString()
    {
        if (!IsSet) return string.Empty;
        var val = Utils.OptionValueToString(Value);
        return DefaultOptionString + val;
    }
}