using BooWin.model.editor;
using System.Collections.Generic;

namespace BooWin.DI.contract;

/// <summary>
/// EditorWindow の編集中セッションを cache\sessions フォルダに JSON として永続化する
/// </summary>
public interface IEditSessionStore {
    /// <summary>
    /// 全セッションを更新日時の新しい順に取得
    /// </summary>
    List<EditSession> List();
    EditSession? Find(string sessionId);
    bool Exists(string sessionId);
    void Save(EditSession session);
    /// <summary>
    /// セッション情報を削除する。deleteCache=true ならキャッシュ由来の動画ファイルも削除する。
    /// </summary>
    void Delete(string sessionId, bool deleteCache);
    /// <summary>
    /// セッションのないアイテムを破棄する（キャッシュ由来なら動画ファイルを削除）
    /// </summary>
    void DiscardItem(IEditorItem item);
    /// <summary>
    /// どのセッションからも参照されていないキャッシュ動画を削除する
    /// </summary>
    void CleanupOrphanCache(IEnumerable<string>? inUsePaths = null);
}
