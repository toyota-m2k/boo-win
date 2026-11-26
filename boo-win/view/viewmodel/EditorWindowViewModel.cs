using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using BooWin.model.editor;
using BooWin.view.dialog;
using HttpMultipartParser;
using io.github.toyota32k.BooLib.Media;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Windows.Storage.Pickers;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel; 
public class EditorWindowViewModel : IItemService {
    public UtLog Logger = new UtLog("Editor");
    private IDialogService _dialogService;
    private IPageService _pageService;
    private ISettingsService _settingsService;


    public ReactivePropertySlim<IEditorItem?> TargetItem { get; } = new ReactivePropertySlim<IEditorItem?>();
    public ReadOnlyReactivePropertySlim<string> WindowTitle { get; }
    public PlayerViewModel PlayerModel { get; private set; }
    public MediaConvertViewModel ConvertModel { get; private set; }
    public List<Chapter> OrgChapters { get; private set; } = new();
    public string? OutputFilePath { get; private set; }


    public string? SourceFilePath => (TargetItem.Value as IEditorItem)?.FilePath;

    public EditorWindowViewModel(
        IDialogService dialogService,
        IPageService pageService,
        ISettingsService settingsService
        ) {
        _dialogService = dialogService;
        _pageService = pageService;
        _settingsService = settingsService;
        PlayerModel = new PlayerViewModel(this, dialogService, pageService, settingsService, forEditor: true);
        ConvertModel = new MediaConvertViewModel(settingsService, dialogService);
        ConvertModel.SaveAsCommand.Subscribe(SaveAsAsync);
        WindowTitle = TargetItem.Select(item => {
            var name = item?.Name;
            return string.IsNullOrEmpty(name) ? "Boo Editor" : name;
        }).ToReadOnlyReactivePropertySlim<string>();
    }

    public void SetTargetItem(IEditorItem item, List<Chapter>? initialChapters, string? outputFilePath=null) {
        TargetItem.Value = item;
        OrgChapters = initialChapters ?? new List<Chapter>();
        OutputFilePath = outputFilePath;
        PlayerModel.SetSource(item);
    }

    public Task<List<Chapter>> LoadChapterAsync(IMediaItem item) {
        return Task.FromResult(OrgChapters);
    }

    public Task<bool> SaveChapterAsync(IMediaItem item, List<Chapter> chapters) {
        return Task.FromResult(true);
    }

    public Uri? GetItemUrl(IMediaItem item, string cmdName = "item") {
        if (item is IEditorItem editorItem) {
            return new Uri(Path.GetFullPath(editorItem.FilePath));
        } else {
            return null;
        }
    }

    public async void SaveAsAsync() {
        if (TargetItem.Value is IEditorItem item) {
            var win = _pageService.Window;
            if (win == null) return;
            var outputFilePath = OutputFilePath;
            if (string.IsNullOrEmpty(outputFilePath)) {
                outputFilePath = await FileSavePickerBuilder.Create(win)
                    .SetTitle("Save Video As")
                    .SetIdentifier("SaveVideoAs")
                    .AddFileType("Video", ".mp4")
                    .SetDefaultFileExtension(".mp4")
                    .SetSuggestedStartLocation(PickerLocationId.VideosLibrary)
                    .ShowOverwritePrompt(true)
                    .PickAsync();
                if (outputFilePath == null) return;
            }
            var chapters = PlayerModel.ChapterModel.ChapterList.Value;
            var ranges = chapters.Any() ? PlayerModel.ChapterModel.EnabledRanges : null;
            var processor = new MediaProcessor(ConvertModel.UseFFMpeg.Value) {
                MaxFrameRate = ConvertModel.MaxFrameRate.Value,
                MaxLengthInPixel = ConvertModel.MaxLengthInPixel.Value,
                CRF = ConvertModel.CRF.Value,
                PreferCodec = ConvertModel.CodecOption.Value
            };  

            var r = await ConvertProgressDialog.ShowAsync(_dialogService, "Saving Video", async (ct, progress) => {
                if (ranges!=null) { 
                    return await processor.TrimmingAsync(item.FilePath, outputFilePath, ranges.Select(it=>new PlayRange((uint)it.Start, (uint)it.End)).ToList(), progress, ct);
                } else {
                    return await processor.CompressAsync(item.FilePath, outputFilePath, progress, ct);
                }
            });

            if (r?.Result==true) { 
                Logger.Info(r.ToString()!);
                await _dialogService.ShowMessage("Saved", $"Video saved to:\n{outputFilePath}");
            }
        }
    }
}
