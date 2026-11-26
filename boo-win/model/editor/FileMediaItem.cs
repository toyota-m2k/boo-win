using io.github.toyota32k.BooLib.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.model.editor;

public class FileMediaItem : IEditorItem {
    public static async Task<FileMediaItem?> FromPath(string path) {
        var analysis = await MediaProcessor.SharedInstance.AnalyzeAsync(path);
        if (analysis == null || analysis.IsEmpty) return null;
        return new FileMediaItem(path, analysis);
    }

    public string FilePath { get; }
    private IAnalysis Analysis { get; }
    private FileMediaItem(string filePath, IAnalysis analysis) {
        FilePath = filePath;
        Analysis = analysis;
    }

    public string Id => FilePath;

    public string Name => Path.GetFileName(FilePath);

    public long Size => Analysis.Size;

    public long Duration => (long)Math.Round(Analysis.Duration.TotalMilliseconds);

    public long Start => 0;

    public long End => 0;

    public string Type => Path.GetExtension(FilePath).ToLowerInvariant().Substring(1);

    //public string Media => 

    public string Label => "";

    public long DateInUnixTime => Date.ToUnixTimeMilliseconds();

    public int Rating => 0;

    public int Mark => 0;

    public string CategoryName => "";

    public DateTimeOffset Date => new DateTimeOffset(File.GetCreationTimeUtc(FilePath));

    public MediaType MediaType {
        get {
            switch(Path.GetExtension(FilePath).ToLower()) {
                case ".mp4":
                case ".mov":
                case ".avi":
                case ".mkv":
                    return MediaType.VIDEO;
                case ".mp3":
                case ".m4a":
                    return MediaType.AUDIO;
                default:
                    return MediaType.PHOTO;
            }
        }
    }
    public bool IsPhoto => this.MediaType == MediaType.PHOTO;

    public bool IsAV => !IsPhoto;
}
