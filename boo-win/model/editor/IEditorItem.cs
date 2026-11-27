using System;
using System.Collections.Generic;
using System.Text;

namespace BooWin.model.editor; 
public interface IEditorItem : IMediaItem, IDisposable {
    string FilePath { get; }
    /// <summary>
    /// 編集セッションID（同じ動画なら同じ値になる）
    /// </summary>
    string SessionId { get; }
    EditSourceKind SourceKind { get; }
}
