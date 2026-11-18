using System;

namespace BooWin.model;

[Flags]
public enum MediaType {
    VIDEO = 1,
    AUDIO = 2,
    PHOTO = 4,
    ALL = VIDEO | AUDIO | PHOTO,
}
