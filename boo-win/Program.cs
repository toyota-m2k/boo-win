using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin;

/// <summary>
/// エントリポイント
/// 単一インスタンス化：既に起動中のプロセスがあれば、そちらにアクティベーションをリダイレクトして終了する。
/// </summary>
public static class Program {
    public const string INSTANCE_KEY = "boo-win-main";

    [STAThread]
    static int Main(string[] args) {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        if (RedirectIfNeeded()) {
            return 0;
        }
        Application.Start(p => {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    /// <summary>
    /// 既存インスタンスがあればリダイレクトする。
    /// </summary>
    /// <returns>true: リダイレクトした（このプロセスは終了する）</returns>
    private static bool RedirectIfNeeded() {
        var keyInstance = AppInstance.FindOrRegisterForKey(INSTANCE_KEY);
        if (keyInstance.IsCurrent) {
            return false;
        }
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        // STAスレッドをブロックしないよう、別スレッドでリダイレクトを待つ
        Task.Run(() => keyInstance.RedirectActivationToAsync(activatedArgs).AsTask()).Wait();
        return true;
    }
}
