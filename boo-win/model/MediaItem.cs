using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BooWin.model; 

public interface IMediaItem {
    string Id { get; }
    string Name { get; }
    long Size { get; }
    string Type { get; }
    string Media { get; }
    string Label { get; }
    long DateInUnixTime { get; }
    int Rating { get; }
    int Mark { get; }
    string CategoryName { get; }
    DateTimeOffset Date { get; }
}

public class NumberOrStringConverter : JsonConverter<string> {
    public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) {
        return reader.TokenType switch {
            System.Text.Json.JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
            System.Text.Json.JsonTokenType.String => reader.GetString() ?? "",
            System.Text.Json.JsonTokenType.Null => "",
            _ => throw new System.Text.Json.JsonException($"Unexpected token {reader.TokenType} for id"),
        };
    }
    public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options) {
        writer.WriteStringValue(value);
    }
}

public class MediaItem : IMediaItem {
    [JsonPropertyName("id")]
    [JsonConverter(typeof(NumberOrStringConverter))]
    public string Id { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; }
    [JsonPropertyName("end")]
    public long End { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";  
    [JsonPropertyName("media")]
    public string Media { get; set; } = "";
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
    [JsonPropertyName("date")]
    public long DateInUnixTime { get; set; }
    [JsonPropertyName("rating")]
    public int Rating { get; set; } = 0;
    [JsonPropertyName("mark")]
    public int Mark { get; set; } = 0;
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = "";

    [JsonIgnore]
    public DateTimeOffset Date => DateTimeOffset.FromUnixTimeSeconds(DateInUnixTime);
    [JsonIgnore]
    public MediaType MediaType => MediaTypeFromString(Media);

    public static string MediaTypeToString(MediaType mediaType) {
        string f = "";
        if ((mediaType & MediaType.VIDEO) != 0) f += "v";
        if ((mediaType & MediaType.AUDIO) != 0) f += "a";
        if ((mediaType & MediaType.PHOTO) != 0) f += "p";
        return f;
    }
    public static MediaType MediaTypeFromString(string s) {
        MediaType mediaType = 0;
        if (s.Contains('v')) mediaType |= MediaType.VIDEO;
        if (s.Contains('a')) mediaType |= MediaType.AUDIO;
        if (s.Contains('p')) mediaType |= MediaType.PHOTO;
        return mediaType;
    }

}
