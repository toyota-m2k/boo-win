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
    private IEditSessionStore _sessionStore;
    private EditSession? _session;
    private string? _host;
    private List<Chapter>? _initialChapters;
    private IDisposable? _autoSaveSubscription;
    private bool _sessionClosed = false;

    /// <summary>
    /// 編集セッションが保存されているか
    /// </summary>
    public bool HasSession => _session != null;


    public ReactivePropertySlim<IEditorItem?> TargetItem { get; } = new ReactivePropertySlim<IEditorItem?>();
    public ReadOnlyReactivePropertySlim<string> WindowTitle { get; }
    public PlayerViewModel PlayerModel { get; private set; }
    public MediaConvertViewModel ConvertModel { get; private set; }
    public List<Chapter> OrgChapters { get; private set; } = new();
    public string? OutputFilePath { get; private set; }
    public ReactiveCommand HelpCommand { get; } = new ReactiveCommand();
    public EditorKeyCommand KeyCommand { get; }


    public string? SourceFilePath => (TargetItem.Value as IEditorItem)?.FilePath;

    public EditorWindowViewModel(
        IDialogService dialogService,
        IPageService pageService,
        ISettingsService settingsService,
        IEditSessionStore sessionStore
        ) {
        _dialogService = dialogService;
        _pageService = pageService;
        _settingsService = settingsService;
        _sessionStore = sessionStore;
        PlayerModel = new PlayerViewModel(this, dialogService, pageService, settingsService, forEditor: true);
        ConvertModel = new MediaConvertViewModel(settingsService, dialogService);
        ConvertModel.SaveAsCommand.Subscribe(SaveAsAsync);
        ConvertModel.SetupFFMpegPathCommand.Subscribe(SetupFFMpegPath);
        WindowTitle = TargetItem.Select(item => {
            var name = item?.Name;
            return string.IsNullOrEmpty(name) ? "Boo Editor" : name;
        }).ToReadOnlyReactivePropertySlim<string>();
        HelpCommand.Subscribe(KeyCommandHelp);
        KeyCommand = new EditorKeyCommand(this);
    }

    public void SetTargetItem(IEditorItem item, List<Chapter>? initialChapters, EditSession? session = null, string? host = null) {
        TargetItem.Value = item;
        _session = session;
        _host = session?.Host ?? host;
        OrgChapters = session?.OrgChapters ?? initialChapters ?? new List<Chapter>();
        _initialChapters = session?.Chapters ?? OrgChapters;
        if (session?.Convert != null) {
            ConvertModel.ApplySettings(session.Convert);
        }
        PlayerModel.SetSource(item);
        _autoSaveSubscription?.Dispose();
        _autoSaveSubscription = Observable.Merge(
                PlayerModel.ChapterModel.ChapterList.Select(_ => System.Reactive.Unit.Default),
                ConvertModel.SettingsChanged)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(_ => AutoSaveSession());
    }

    public Task<List<Chapter>> LoadChapterAsync(IMediaItem item) {
        return Task.FromResult(_initialChapters ?? OrgChapters);
    }

    #region Edit Session

    private void AutoSaveSession() {
        if (_sessionClosed) return;
        if (!PlayerModel.ChapterModel.IsDirty && _session == null) return;
        SaveSession();
    }

    /// <summary>
    /// 現在の編集状態をセッションファイルに保存する
    /// </summary>
    public void SaveSession() {
        if (TargetItem.Value is not IEditorItem item) return;
        lock (this) {
            if (_sessionClosed) return;
            var session = _session ?? EditSession.FromItem(item, _host);
            session.OrgChapters = OrgChapters;
            session.Chapters = PlayerModel.ChapterModel.ChapterList.Value.ToList();
            session.Convert = ConvertModel.ToSettings();
            _sessionStore.Save(session);
            _session = session;
        }
    }

    /// <summary>
    /// ウィンドウを閉じるときに呼ぶ。keep=true ならセッションを残し、false ならセッションとキャッシュを破棄する。
    /// </summary>
    public void CloseSession(bool keep) {
        _autoSaveSubscription?.Dispose();
        _autoSaveSubscription = null;
        if (keep) {
            SaveSession();
        }
        lock (this) {
            _sessionClosed = true;
            if (!keep && TargetItem.Value is IEditorItem item) {
                _sessionStore.DiscardItem(item);
                _session = null;
            }
        }
    }

    #endregion

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
                MaxLengthInPixel = ConvertModel.MaxLengthInPixel,
                MaxBitRate = ConvertModel.MaxBitRateDirective,
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

    private async void SetupFFMpegPath() {
        var path = await FFMpegPathDialog.ShowAsync(_dialogService, _pageService.Window, _settingsService.FFMpegPath);
        if (path != null) {
            _settingsService.FFMpegPath = path;
        }
    }

    private async void KeyCommandHelp() {
        await _dialogService.ShowMessage("Key Map", KeyCommand.Help());
    }
}
