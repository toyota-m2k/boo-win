using io.github.toyota32k.BooLib.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace BooWin.model.editor;

/// <summary>
/// 編集対象動画の取得元
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EditSourceKind>))]
public enum EditSourceKind {
    File,       // ローカルファイル（キャッシュではない）
    Boo,        // Boo サーバーからダウンロードしたキャッシュ
    Google,     // Google Photo からダウンロードしたキャッシュ
}

/// <summary>
/// 変換設定（セッションごとに保持）
/// </summary>
public class EditConvertSettings {
    [JsonPropertyName("useFFMpeg")]
    public bool UseFFMpeg { get; set; }
    [JsonPropertyName("maxLengthInPixel")]
    public int MaxLengthInPixel { get; set; }
    [JsonPropertyName("crf")]
    public int CRF { get; set; }
    [JsonPropertyName("maxFrameRate")]
    public int MaxFrameRate { get; set; }
    [JsonPropertyName("maxBitRate")]
    public int MaxBitRate { get; set; }
    [JsonPropertyName("autoBitRate")]
    public bool AutoBitRate { get; set; }
    [JsonPropertyName("autoQuality")]
    [JsonConverter(typeof(JsonStringEnumConverter<PresetQuality>))]
    public PresetQuality AutoQuality { get; set; }
    [JsonPropertyName("codec")]
    [JsonConverter(typeof(JsonStringEnumConverter<VideoCodecOption>))]
    public VideoCodecOption CodecOption { get; set; }
}

/// <summary>
/// 編集中セッション情報（cache\sessions\{SessionId}.json）
/// </summary>
public class EditSession {
    public const int CURRENT_VERSION = 1;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CURRENT_VERSION;
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = "";
    [JsonPropertyName("sourceKind")]
    public EditSourceKind SourceKind { get; set; }
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }
    [JsonPropertyName("fileLastWrite")]
    public DateTime FileLastWrite { get; set; }
    [JsonPropertyName("item")]
    public MediaItem Item { get; set; } = new();
    [JsonPropertyName("host")]
    public string? Host { get; set; }
    [JsonPropertyName("orgChapters")]
    public List<Chapter> OrgChapters { get; set; } = new();
    [JsonPropertyName("chapters")]
    public List<Chapter>? Chapters { get; set; }
    [JsonPropertyName("convert")]
    public EditConvertSettings? Convert { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    #region Display helpers
    [JsonIgnore]
    public string Name => string.IsNullOrEmpty(Item.Name) ? Path.GetFileName(FilePath) : Item.Name;
    [JsonIgnore]
    public bool IsCache => SourceKind != EditSourceKind.File;
    [JsonIgnore]
    public int ChapterCount => Chapters?.Count ?? OrgChapters.Count;
    [JsonIgnore]
    public int SkipCount => (Chapters ?? OrgChapters).Count(it => it.Skip);
    [JsonIgnore]
    public string UpdatedAtText => UpdatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
    [JsonIgnore]
    public string Summary => $"{SourceKind}  |  {ChapterCount} chapters ({SkipCount} skip)  |  {UpdatedAtText}";

    /// <summary>
    /// 動画ファイルが存在し、記録時から変更されていないか
    /// </summary>
    [JsonIgnore]
    public bool IsValid {
        get {
            try {
                var fi = new FileInfo(FilePath);
                if (!fi.Exists) return false;
                if (FileSize > 0 && fi.Length != FileSize) return false;
                if (FileLastWrite != default && fi.LastWriteTimeUtc != FileLastWrite) return false;
                return true;
            } catch {
                return false;
            }
        }
    }
    [JsonIgnore]
    public bool IsInvalid => !IsValid;
    [JsonIgnore]
    public string StatusText => IsValid ? "" : "File not found or modified";
    #endregion

    public static EditSession FromItem(IEditorItem item, string? host = null) {
        var fi = new FileInfo(item.FilePath);
        return new EditSession {
            SessionId = item.SessionId,
            SourceKind = item.SourceKind,
            FilePath = fi.FullName,
            FileSize = fi.Exists ? fi.Length : 0,
            FileLastWrite = fi.Exists ? fi.LastWriteTimeUtc : default,
            Host = host,
            Item = new MediaItem {
                Id = item.Id,
                Name = item.Name,
                Size = item.Size,
                Duration = item.Duration,
                Start = item.Start,
                End = item.End,
                Type = item.Type,
                Media = MediaItem.MediaTypeToString(item.MediaType),
                Label = item.Label,
                DateInUnixTime = item.DateInUnixTime,
                Rating = item.Rating,
                Mark = item.Mark,
                CategoryName = item.CategoryName,
            },
        };
    }
}
