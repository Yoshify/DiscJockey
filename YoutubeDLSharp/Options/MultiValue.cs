using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeDLSharp.Options;

public class MultiValue<T>
{
    public MultiValue(params T[] values)
    {
        Values = values.ToList();
    }

    public List<T> Values { get; }

    public static implicit operator MultiValue<T>(T value)
    {
        return new MultiValue<T>(value);
    }

    public static implicit operator MultiValue<T>(T[] values)
    {
        return new MultiValue<T>(values);
    }

    public static explicit operator T(MultiValue<T> value)
    {
        if (value.Values.Count == 1)
            return value.Values[0];
        throw new InvalidCastException($"Cannot cast sequence of values to {typeof(T)}.");
    }

    public static explicit operator T[](MultiValue<T> value)
    {
        return value.Values.ToArray();
    }
}