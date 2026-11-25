using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using BooWin.view.viewmodel;
using io.github.toyota32k.BooLib.Server.client;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;

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
        public static EditorWindow OpenEditorWindow() {
            var window = new EditorWindow();
            RegisterWindow(window);
            window.Activate();
            return window;
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
            _window = new MainWindow();
            RegisterWindow(_window);
            _window.Activate();
            UtLog.GlobalLoggerChain.AddLogger(GetService<LogViewModel>());
            GetService<IPageService>().Startup(_window, _window.RootFrame);
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
