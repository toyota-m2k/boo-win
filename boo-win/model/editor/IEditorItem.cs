using System;
using System.Collections.Generic;
using System.Text;

namespace BooWin.model.editor; 
public interface IEditorItem : IMediaItem {
    string FilePath { get; }
}
