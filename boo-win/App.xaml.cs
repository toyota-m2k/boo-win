using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using BooWin.model.editor;
using BooWin.view.viewmodel;
using io.github.toyota32k.BooLib.Server.client;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin {
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application {
        private MainWindow? _window;
        public static UtLog Logger = new UtLog("BooWin");

        private ServiceProvider Services { get; }
        private IServiceScope MainScope { get; }

        /// <summary>
        /// MainWindowのスコープからサービスを取得する。
        /// （シングルトンは全Window共通、スコープサービスはMainWindow用のインスタンス）
        /// </summary>
        public static T GetService<T>()
            where T : class {
            if ((App.Current as App)!.MainScope.ServiceProvider.GetService(typeof(T)) is not T service) {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }
            return service;
        }

        /// <summary>
        /// WindowごとのDIスコープを作成する。
        /// </summary>
        public static IServiceScope CreateWindowScope() {
            return (App.Current as App)!.Services.CreateScope();
        }

        /// <summary>
        /// 動画編集ウィンドウを開く（複数起動可）
        /// </summary>
        public static EditorWindow OpenEditorWindow(IEditorItem item, List<Chapter>? chapters=null, string? host=null) {
            var opened = FindEditorWindow(item.SessionId);
            if (opened != null) {
                opened.Activate();
                return opened;
            }
            // 編集中セッションがあれば復元する
            var session = GetService<IEditSessionStore>().Find(item.SessionId);
            if (session != null && !session.IsValid) {
                session = null;
            }
            var window = new EditorWindow(item, chapters, session, host);
            RegisterWindow(window);
            window.Activate();
            return window;
        }

        /// <summary>
        /// 編集中セッションから動画編集ウィンドウを開く
        /// </summary>
        public static async Task<bool> OpenEditorSession(EditSession session) {
            var opened = FindEditorWindow(session.SessionId);
            if (opened != null) {
                opened.Activate();
                return true;
            }
            if (!session.IsValid) return false;
            IEditorItem? item = session.SourceKind == EditSourceKind.File
                ? await FileMediaItem.FromPath(session.FilePath)
                : CacheMediaItem.Create(session.FilePath, session.Item, session.SourceKind);
            if (item == null) return false;
            var window = new EditorWindow(item, null, session);
            RegisterWindow(window);
            window.Activate();
            return true;
        }

        public static EditorWindow? FindEditorWindow(string sessionId) {
            return _openWindows
                .OfType<EditorWindow>()
                .FirstOrDefault(w => w.ViewModel.TargetItem.Value?.SessionId == sessionId);
        }

        private static readonly List<Window> _openWindows = new();

        /// <summary>
        /// Windowを登録し、すべてのWindowが閉じられたら保存処理を行う。
        /// </summary>
        public static void RegisterWindow(Window window) {
            _openWindows.Add(window);
            window.Closed += OnWindowClosed;
        }

        private static void OnWindowClosed(object sender, WindowEventArgs args) {
            if (sender is Window window) {
                window.Closed -= OnWindowClosed;
                _openWindows.Remove(window);
            }
            if (_openWindows.Count == 0) {
                OnAllWindowsClosed();
            }
        }

        private static void OnAllWindowsClosed() {
            GetService<ISettingsService>().Save();
            // Win2D(CanvasControl) は XAML 終了処理中に CoreApplication.Suspending の解除に失敗し、
            // 0xC000027B (stowed exception) でクラッシュするため、保存後に即座にプロセスを終了する。
            Environment.Exit(0);
        }

        //public static MainWindow MainWindow => (App.Current as App)!._window!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App() {
            InitializeComponent();
            var services = new ServiceCollection();

            services
                .AddHttpClient(PeerHost.HTTP_CLIENT_FACTORY_NAME)
                .ConfigurePrimaryHttpMessageHandler(() => CustomHttpClient.CreateHttpClientHandler());

            // Register services here
            // services.AddSingleton<IMyService, MyService>();
            services
                .AddSingleton<IAppConfigService, AppConfigService>()
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IEditSessionStore, EditSessionStore>()
                .AddSingleton<IMainThreadService, MainThreadService>()
                // Windowごとに生成
                .AddScoped<IPageService, PageService>()
                .AddScoped<IDialogService, DialogService>()
                // ClientServiceはMainWindow専用だが、DIをシンプルにするため、AddScopedで登録する。
                // AddScoped で登録したサービスは、Singletonサービスから依存しないこと。
                // ならず、Scoped or Transient サービスから依存する。これができないようなら、
                //
                //.AddSingleton<IClientService>(sp => new ClientService(
                //    sp.GetRequiredService<System.Net.Http.IHttpClientFactory>(),
                //    GetService<IDialogService>()))
                // とする。
                .AddScoped<IClientService, ClientService>()
                .AddSingleton<LogViewModel>()
                .AddSingleton<MainWindowViewModel>()
                .AddScoped<EntryPageViewModel>()
                .AddScoped<PlayerPageViewModel>()
                .AddTransient<DiscoverPeerDialogViewModel>()
                .AddTransient<EditorWindowViewModel>()
                ;

            Services = services.BuildServiceProvider();
            MainScope = Services.CreateScope();
            UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) {
            Logger.Error(e.Exception, "Unhandled Exception");
            if (e.Exception is ArgumentOutOfRangeException { Source: "WinUI.TableView" }) {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) {
            UtLog.GlobalLoggerChain.AddLogger(GetService<LogViewModel>());
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            // どの編集セッションからも参照されていないキャッシュ動画を削除
            GetService<IEditSessionStore>().CleanupOrphanCache();
            // 他プロセスからリダイレクトされたアクティベーションを受け取る
            AppInstance.GetCurrent().Activated += OnRedirectedActivated;
            HandleActivation(AppInstance.GetCurrent().GetActivatedEventArgs(), isLaunch: true);
        }

        private DispatcherQueue? _dispatcherQueue;

        private void OnRedirectedActivated(object? sender, AppActivationArguments args) {
            // バックグラウンドスレッドから呼ばれるのでUIスレッドで処理する
            _dispatcherQueue?.TryEnqueue(() => HandleActivation(args, isLaunch: false));
        }

        /// <summary>
        /// アクティベーション引数に応じてWindowを開く。
        /// 動画ファイルが指定されていればEditorWindow、そうでなければMainWindow。
        /// </summary>
        private async void HandleActivation(AppActivationArguments args, bool isLaunch) {
            var paths = GetFilePaths(args, isLaunch);
            if (paths.Count > 0) {
                await OpenMediaFilesAsync(paths);
                return;
            }
            ShowMainWindow();
        }

        /// <summary>
        /// 編集対象として扱う拡張子
        /// </summary>
        public static readonly string[] EditableExtensions = [".mp4", ".mov", ".mkv", ".avi", ".mp3", ".m4a"];

        public static bool IsEditableFile(string path) {
            return EditableExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }

        /// <summary>
        /// 動画ファイルを編集用に開く。
        /// １件なら EditorWindow を開き、２件以上なら編集中セッションとして登録して MainWindow の選択ダイアログを表示する。
        /// </summary>
        public static async Task OpenMediaFilesAsync(IList<string> paths) {
            if (paths.Count == 1) {
                var item = await FileMediaItem.FromPath(paths[0]);
                if (item != null) {
                    OpenEditorWindow(item);
                } else {
                    Logger.Error($"cannot open: {paths[0]}");
                }
                return;
            }
            var store = GetService<IEditSessionStore>();
            foreach (var path in paths) {
                var item = await FileMediaItem.FromPath(path);
                if (item == null) {
                    Logger.Error($"cannot open: {path}");
                    continue;
                }
                // 有効な既存セッション（編集途中）があれば、それを残す
                if (store.Find(item.SessionId)?.IsValid != true) {
                    store.Save(EditSession.FromItem(item));
                }
            }
            var app = (App)Current;
            var created = app._window == null;
            app.ShowMainWindow();
            if (!created) {
                // 新規作成時は EntryPage の起動処理で選択ダイアログが表示される
                await GetService<EntryPageViewModel>().ShowEditSessionsAsync(onlyIfAny: true);
            }
        }

        private void ShowMainWindow() {
            if (_window != null) {
                _window.Activate();
                return;
            }
            _window = new MainWindow();
            _window.Closed += (_, _) => _window = null;
            RegisterWindow(_window);
            _window.Activate();
            GetService<IPageService>().Startup(_window, _window.RootFrame);
        }

        private static List<string> GetFilePaths(AppActivationArguments args, bool isLaunch) {
            var result = new List<string>();
            switch (args.Kind) {
                case ExtendedActivationKind.File:
                    if (args.Data is IFileActivatedEventArgs fileArgs) {
                        foreach (var item in fileArgs.Files) {
                            if (item is IStorageFile file) {
                                result.Add(file.Path);
                            }
                        }
                    }
                    break;
                case ExtendedActivationKind.Launch:
                    // 自プロセスの起動なら Environment.GetCommandLineArgs() が確実
                    // リダイレクトされた場合は Arguments 文字列を分解する
                    IEnumerable<string> tokens = isLaunch
                        ? Environment.GetCommandLineArgs().Skip(1)
                        : SplitCommandLine((args.Data as ILaunchActivatedEventArgs)?.Arguments ?? "");
                    foreach (var token in tokens) {
                        if (!token.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(token)) {
                            result.Add(Path.GetFullPath(token));
                        }
                    }
                    break;
            }
            return result;
        }

        private static IEnumerable<string> SplitCommandLine(string commandLine) {
            var sb = new StringBuilder();
            bool inQuote = false;
            foreach (var c in commandLine) {
                if (c == '"') {
                    inQuote = !inQuote;
                } else if (char.IsWhiteSpace(c) && !inQuote) {
                    if (sb.Length > 0) {
                        yield return sb.ToString();
                        sb.Clear();
                    }
                } else {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0) {
                yield return sb.ToString();
            }
        }

        ///**
        // * HTTPS 用 HttpClientHandler を生成。peer.IsHttps の場合、mDNS TXT で得た指紋と
        // * 実際のサーバ証明書 SHA-256 を pin-to-fingerprint 比較する。
        // */
        //private HttpClientHandler CreateHttpClientHandler() {
        //    return new HttpClientHandler {
        //        ServerCertificateCustomValidationCallback = (req, cert, chain, sslErrors) => {
        //            // HTTP は証明書検証不要
        //            if (req.RequestUri?.Scheme != Uri.UriSchemeHttps) return true;
        //            if (cert == null) {
        //                Logger.Error("Sync: no certificate presented");
        //                return false;
        //            }
        //            var peer = req.Options.TryGetValue(PeerHost.KEY, out PeerHost? v) ? v : null;
        //            if (peer == null) {
        //                Logger.Error("Sync: no PeerHost info in request options");
        //                return false;
        //            }
        //            var expected = peer.Fingerprint ?? "";
        //            string actual = CertificateGenerator.ComputeSha256Fingerprint(cert);
        //            bool ok = string.Equals(
        //                PeerEndpoint.NormalizeFp(actual),
        //                PeerEndpoint.NormalizeFp(expected),
        //                StringComparison.OrdinalIgnoreCase);
        //            if (!ok) {
        //                Logger.Error($"Sync: certificate fingerprint mismatch.\n  expected={expected}\n  actual  ={actual}");
        //            }
        //            return ok;
        //        }
        //    };
        //}

    }
}
