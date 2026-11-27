using BooWin.DI.contract;
using BooWin.model.editor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BooWin.view.dialog;

/// <summary>
/// 編集中セッションの一覧を表示し、開くセッションを選択するダイアログ
/// </summary>
public sealed partial class EditSessionListDialog : ContentDialog {
    private ObservableCollection<EditSession> Sessions { get; }
    private readonly IEditSessionStore _store;

    private EditSessionListDialog(IEditSessionStore store, List<EditSession> sessions) {
        _store = store;
        Sessions = new ObservableCollection<EditSession>(sessions);
        InitializeComponent();
    }

    private List<EditSession> SelectedSessions => SessionListView.SelectedItems.OfType<EditSession>().ToList();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        var selected = SelectedSessions;
        DeleteButton.IsEnabled = selected.Count > 0;
        IsPrimaryButtonEnabled = selected.Count > 0 && selected.All(it => it.IsValid);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e) {
        foreach (var session in SelectedSessions) {
            _store.Delete(session.SessionId, deleteCache: App.FindEditorWindow(session.SessionId) == null);
            Sessions.Remove(session);
        }
    }

    /// <summary>
    /// 編集中セッション一覧ダイアログを表示する
    /// </summary>
    /// <param name="onlyIfAny">true: セッションが１件もなければ表示しない</param>
    /// <returns>開くセッションのリスト（キャンセル時は空）</returns>
    public static async Task<List<EditSession>> ShowAsync(IDialogService dialogService, IEditSessionStore store, bool onlyIfAny = false) {
        var sessions = store.List();
        if (onlyIfAny && sessions.Count == 0) {
            return new List<EditSession>();
        }
        var dialog = new EditSessionListDialog(store, sessions);
        var result = await dialogService
            .Builder(dialog)
            .PrimaryButton("Open", true, null)
            .CloseButton("Close", false, null)
            .Customize(dlg => dlg.IsPrimaryButtonEnabled = false)
            .ShowAsync();
        if (result != ContentDialogResult.Primary) {
            return new List<EditSession>();
        }
        return dialog.SelectedSessions;
    }
}
