using BooWin.client;
using BooWin.DI.contract;
using BooWin.model;
using BooWin.model.editor;
using BooWin.model.google;
using BooWin.view.dialog;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Windows.Storage.Pickers;
using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel; 

public class EntryPageViewModel {
    public UtLog Logger = new UtLog("EPVM");
    private readonly ISettingsService _settingsService;
    private readonly IMainThreadService _mainThreadService;
    private readonly IDialogService _dialogService;
    private readonly IPageService _pageService;
    private readonly IClientService _clientService;
    private readonly IAppConfigService _appConfigService;
    private readonly IEditSessionStore _sessionStore;

    public ObservableCollection<PeerHost> HostList { get; }
    public ReactiveProperty<PeerHost?> CurrentHost { get; } = new();
    public ReactiveProperty<bool> IsHostAvailable { get; } = new(false);
    public ReactiveProperty<bool> AutoStartPlayer { get; } = new(false);
    public bool FirstTime { get; set; } = true;

    public ReadOnlyReactiveProperty<bool> CanOpenPlayerPage { get; }

    public ReactiveCommand AddHostCommand { get; } = new();
    public ReactiveCommand DiscoverHostCommand { get; } = new();
    public ReactiveCommand<PeerHost> DeleteHostCommand { get; } = new();
    public ReactiveCommand<PeerHost> EditHostCommand { get; } = new();
    public ReactiveCommand GotoPlayerPageCommand { get; } = new();
    public ReactiveCommand OpenEditorCommand { get; } = new();
    public ReactiveCommand OpenGooglePhotoCommand { get; } = new();
    public ReactiveCommand ShowEditSessionsCommand { get; } = new();

    private bool HostListModified = false;

    public EntryPageViewModel(
        ISettingsService settingsService, 
        IMainThreadService mainThreadService,
        IDialogService dialogService,
        IPageService pageService,
        IClientService clientService,
        IAppConfigService appConfigService,
        IEditSessionStore sessionStore
        ) {
        _appConfigService = appConfigService;
        _sessionStore = sessionStore;
        _settingsService = settingsService;
        _mainThreadService = mainThreadService;
        _dialogService = dialogService;
        _pageService = pageService;
        _clientService = clientService;

        AddHostCommand.Subscribe(AddHost);
        DiscoverHostCommand.Subscribe(DiscoverPeer);
        DeleteHostCommand.Subscribe(DeleteHost);
        EditHostCommand.Subscribe(EditHost);
        GotoPlayerPageCommand.Subscribe(GotoPlayerPage);
        OpenEditorCommand.Subscribe(OpenEditor);
        OpenGooglePhotoCommand.Subscribe(OpenGooglePhoto);
        ShowEditSessionsCommand.Subscribe(() => _ = ShowEditSessionsAsync(onlyIfAny: false));

        CurrentHost.Value = _settingsService.CurrentHost;
        CanOpenPlayerPage = CurrentHost.CombineLatest(IsHostAvailable, (host,a) => host != null && a).ToReadOnlyReactiveProperty();
        CurrentHost.Subscribe(CheckHostAvailability);

        HostList = new ObservableCollection<PeerHost>(settingsService.Hosts);

        AutoStartPlayer.Value = settingsService.AutoStartPlayer;
        AutoStartPlayer.Subscribe(v => {
            _settingsService.AutoStartPlayer = v;
            _settingsService.Save();
        });
    }

    public async void DiscoverPeer() {
        var peer = await DiscoverPeerDialog.Discover(_dialogService);
        if (peer != null) {
            if (!HostList.Any(it => it.Address == peer.Address)) {
                HostList.Add(peer);
                HostListModified = true;
            }
        }
    }

    public void Save() {
        _settingsService.UpdateHostList(HostList, CurrentHost.Value);
        _settingsService.Save();
        HostListModified = false;
    }
    
    private void DeleteHost(PeerHost host) {
        HostList.Remove(host);
        HostListModified = true;
    }

    private async void EditHost(PeerHost host) {
        var modified = await EditHostDialog.EditHost(_dialogService, host);
        if (modified != null) {
            var i = HostList.IndexOf(host);
            if (i >= 0) {
                HostList[i] = modified;
                HostListModified = true;
            }
        }
    }

    public async void AddHost() {
        var modified = await EditHostDialog.NewHost(_dialogService);
        if (modified != null) {
            HostList.Add(modified);
            HostListModified = true;
        }
    }

    public void GotoPlayerPage() {
        if (CurrentHost.Value != null && CanOpenPlayerPage.Value) {
            Save();
            _pageService.GoToPlayerPage();
        }
    }

    private CancellationTokenSource? previousTask = null;
    public void CheckHostAvailability(PeerHost? host) {
        lock (this) {
            if (previousTask != null) {
                previousTask.Cancel();
                previousTask = null;
            }
        }
        IsHostAvailable.Value = false;
        if (host != null) {
            var tcs = new CancellationTokenSource();
            lock (this) {
                previousTask = tcs;
            }
            Task.Run(async () => {
                if (await _clientService.CheckHostAvailabilityAsync(host)) {
                    if (!tcs.IsCancellationRequested) {
                        _mainThreadService.Run(() => {
                            IsHostAvailable.Value = true;
                        });
                    }
                }
            }, tcs.Token);
        }
    }

    private async void OpenEditor() {
        var win = _pageService.Window;
        if (win==null) {
            Logger.Error("Window is null");
            return;
        }
        var path = await FileOpenPickerBuilder
            .Create(win)
            .AddFileType("Video", ".mp4", ".mov", "mkv")
            .AddFileType("Audio", ".mp3", "m4a")
            .SetIdentifier("OpenFileToEdit")
            .PickAsync();
        if (path != null) {
            var item = await FileMediaItem.FromPath(path);
            if (item == null) {
                await _dialogService.ShowMessage("Error", "Failed to open file.");
                return;
            }
            App.OpenEditorWindow(item);
        }
    }

    /// <summary>
    /// 編集中セッションの一覧を表示し、選択されたセッションを EditorWindow で開く
    /// </summary>
    public async Task ShowEditSessionsAsync(bool onlyIfAny) {
        var sessions = await EditSessionListDialog.ShowAsync(_dialogService, _sessionStore, onlyIfAny);
        var failed = new List<string>();
        foreach (var session in sessions) {
            if (!await App.OpenEditorSession(session)) {
                failed.Add(session.Name);
            }
        }
        if (failed.Count > 0) {
            await _dialogService.ShowMessage("Error", "Failed to open:\n" + string.Join("\n", failed));
        }
    }

    private GooglePhotosPickerService? _googlePhotos;

    private async void OpenGooglePhoto() {
        _googlePhotos ??= new GooglePhotosPickerService(Path.Combine(_appConfigService.AppDataPath, "google"));
        var service = _googlePhotos;

        // 認証 → セッション作成 → ブラウザで Picker を開き、選択完了をポーリング
        List<GooglePhotosPickerService.PickedMediaItem>? items = null;
        string? sessionId = null;
        Exception? error = null;
        await DownloadProgressDialog.ShowAsync(_dialogService, "Google Photo", "Sign in and select a video in the browser.",
            async (token, _) => {
                try {
                    await service.AuthorizeAsync(token);
                    var session = await service.CreateSessionAsync(token);
                    sessionId = session.Id;
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(session.PickerUri.TrimEnd('/') + "/autoclose"));
                    await service.WaitForSelectionAsync(session, token);
                    items = await service.ListMediaItemsAsync(session.Id, token);
                    return true;
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception ex) {
                    Logger.Error(ex);
                    error = ex;
                    return false;
                }
            });
        if (sessionId != null) {
            _ = service.DeleteSessionAsync(sessionId);
        }
        if (error != null) {
            await _dialogService.ShowMessage("Error", error.Message);
            return;
        }
        if (items == null) {
            return; // canceled
        }
        var picked = items.FirstOrDefault(it => it.IsVideo && it.MediaFile != null);
        if (picked == null) {
            await _dialogService.ShowMessage("Google Photo", "No video was selected.");
            return;
        }

        var filename = picked.MediaFile!.Filename;
        var ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = "mp4";
        var hash = HashBuilder.MD5.Append("google-photo").Append(picked.Id).Build().AsHexString;
        var path = Path.Combine(_appConfigService.CachePath, $"{hash}.{ext}");
        // 編集中セッションがあれば、ダウンロードせずに復元する
        var opened = App.FindEditorWindow(hash);
        if (opened != null) {
            opened.Activate();
            return;
        }
        var session = _sessionStore.Find(hash);
        if (session != null && session.IsValid) {
            await App.OpenEditorSession(session);
            return;
        }
        var result = await DownloadProgressDialog.ShowAsync(_dialogService, "Download", filename,
            (token, progress) => service.DownloadAsync(picked, path, token, progress));
        if (result) {
            var mediaItem = new MediaItem {
                Id = picked.Id,
                Name = Path.GetFileNameWithoutExtension(filename),
                Size = new FileInfo(path).Length,
                Type = ext,
                Media = "v",
                DateInUnixTime = picked.CreateTime.ToUnixTimeMilliseconds(),
            };
            App.OpenEditorWindow(CacheMediaItem.Create(path, mediaItem, EditSourceKind.Google));
        }
    }
}
