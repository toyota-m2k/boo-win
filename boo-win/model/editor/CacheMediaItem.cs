using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace BooWin.model.editor;

public class CacheMediaItem : IEditorItem {
    public string FilePath { get; }
    private IMediaItem SourceItem { get; }

    public string Id => SourceItem.Id;

    public string Name => SourceItem.Name;

    public long Size => SourceItem.Size;

    public long Duration => SourceItem.Duration;

    public long Start => SourceItem.Start;

    public long End => SourceItem.End;

    public string Type => SourceItem.Type;

    public string Label => SourceItem.Label;

    public long DateInUnixTime => SourceItem.DateInUnixTime;

    public int Rating => SourceItem.Rating;

    public int Mark => SourceItem.Mark;

    public string CategoryName => SourceItem.CategoryName;

    public DateTimeOffset Date => SourceItem.Date;

    public MediaType MediaType => SourceItem.MediaType;

    public bool IsPhoto => SourceItem.IsPhoto;

    public bool IsAV => SourceItem.IsAV;

    public string SessionId => System.IO.Path.GetFileNameWithoutExtension(FilePath);

    public EditSourceKind SourceKind { get; }

    private CacheMediaItem(string path, IMediaItem src, EditSourceKind kind) {
        FilePath = path;
        SourceItem = src;
        SourceKind = kind;
    }


    public static CacheMediaItem Create(string path, IMediaItem src, EditSourceKind kind) {
        return new CacheMediaItem(path, src, kind);
    }

    /// <summary>
    /// キャッシュファイルの削除は IEditSessionStore が判断する（セッションを残す場合は削除しない）
    /// </summary>
    public void Dispose() {
    }
}
