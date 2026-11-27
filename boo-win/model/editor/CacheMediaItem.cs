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

    private CacheMediaItem(string path, IMediaItem src) {
        FilePath = path;
        SourceItem = src;
    }


    public static CacheMediaItem Create(string path, IMediaItem src) {
        return new CacheMediaItem(path, src);
    }

    public void Dispose() {
        FileUtil.SafeDelete(FilePath);
    }
}
