using System;
using System.Text.Json.Serialization;

namespace BooWin.model; 

public interface IMediaItem {
    string Id { get; }
    string Name { get; }
    long Size { get; }
    long Duration { get; }
    long Start { get; }
    long End { get; }
    string Type { get; }
    string Media { get; }
    string Label { get; }
    long DateInUnixTime { get; }
    int Rating { get; }
    int Mark { get; }
    string CategoryName { get; }
    DateTimeOffset Date { get; }
    MediaType MediaType { get; }

    bool IsPhoto { get; }
    bool IsAV { get; }
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
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Size { get; set; }

    [JsonPropertyName("duration")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Duration { get; set; }

    [JsonPropertyName("start")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Start { get; set; }

    [JsonPropertyName("end")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long End { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";  
    [JsonPropertyName("media")]
    public string Media { get; set; } = "";
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
    [JsonPropertyName("date")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long DateInUnixTime { get; set; }
    [JsonPropertyName("rating")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Rating { get; set; } = 0;
    [JsonPropertyName("mark")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Mark { get; set; } = 0;
    [JsonPropertyName("category")]
    public string CategoryName { get; set; } = "";

    [JsonIgnore]
    public DateTimeOffset Date => DateTimeOffset.FromUnixTimeMilliseconds(DateInUnixTime);
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
    [JsonIgnore]
    public bool IsPhoto => this.MediaType == MediaType.PHOTO;
    public bool IsAV => this.MediaType == MediaType.VIDEO || this.MediaType == MediaType.AUDIO;

    #region Display helpers
    [JsonIgnore]
    public bool HasDuration => (MediaType & (MediaType.VIDEO | MediaType.AUDIO)) != 0 && Duration > 0;
    [JsonIgnore]
    public string DurationText {
        get {
            var t = TimeSpan.FromMilliseconds(Duration);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t:mm\\:ss}" : $"{t:m\\:ss}";
        }
    }
    [JsonIgnore]
    public string DateText => DateInUnixTime > 0 ? Date.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : "";
    [JsonIgnore]
    public string SizeText {
        get {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double s = Size;
            int i = 0;
            while (s >= 1024 && i < units.Length - 1) { s /= 1024; i++; }
            return i == 0 ? $"{Size} B" : $"{s:0.#} {units[i]}";
        }
    }
    [JsonIgnore]
    public string MediaTypeGlyph =>
        (MediaType & MediaType.VIDEO) != 0 ? "\uE714" :
        (MediaType & MediaType.AUDIO) != 0 ? "\uE8D6" :
        (MediaType & MediaType.PHOTO) != 0 ? "\uEB9F" : "\uE8A5";
    [JsonIgnore]
    public string MediaTypeLabel =>
        (MediaType & MediaType.VIDEO) != 0 ? "Video" :
        (MediaType & MediaType.AUDIO) != 0 ? "Audio" :
        (MediaType & MediaType.PHOTO) != 0 ? "Photo" : "File";
    #endregion

}
