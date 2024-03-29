﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YoutubeDLSharp.Converters;

public class StringToEnumConverter<T> : JsonConverter<T> where T : Enum
{
    public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var value = (string)reader.Value;
        if (value == null) return default;
        var jsonString = $"'{value.ToLower()}'";
        var enumValue = JsonConvert.DeserializeObject<T>(jsonString, new StringEnumConverter());
        return enumValue;
    }

    public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }
}