using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using BooWin.view.viewmodel;
using io.github.toyota32k.BooLib.Server.client;
using io.github.toyota32k.BooLib.Server.mdns;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        public static T GetService<T>()
            where T : class {
            if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service) {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }
            return service;
        }

        public static MainWindow MainWindow => (App.Current as App)!._window!;

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
                .AddSingleton<IPageService, PageService>()
                .AddSingleton<IDialogService, DialogService>()
                .AddSingleton<IClientService, ClientService>()
                .AddSingleton<LogViewModel>()
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<EntryPageViewModel>()
                .AddSingleton<PlayerPageViewModel>()
                .AddTransient<DiscoverPeerDialogViewModel>()
                ;

            Services = services.BuildServiceProvider();
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
            _window.Activate();
            _window.AppWindow.Closing += AppWindow_Closing;
            UtLog.GlobalLoggerChain.AddLogger(GetService<LogViewModel>());
            GetService<IPageService>().Startup(_window.RootFrame);
        }

        private bool closeConfirmed = false;
        private async void ConfirmClose(Window window) {
            var r = await GetService<IDialogService>()
                .ShowYesNo("Confirm Exit", "Are you sure you want to exit?", yesAsDefault: true);
            if (r == true) {
                closeConfirmed = true;
                window.Close();
            }
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args) {
            if (_window == null) return;
            GetService<EntryPageViewModel>().Save();
            GetService<ISettingsService>().Save();

            var appConfigService = GetService<IAppConfigService>();
            if (appConfigService.NeedsConfirmOnExit && !closeConfirmed) {
                args.Cancel = true;
                ConfirmClose(_window);
                return;
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
