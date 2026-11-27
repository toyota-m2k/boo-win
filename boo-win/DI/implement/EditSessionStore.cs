using BooWin.DI.contract;
using BooWin.model.editor;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BooWin.DI.implement;

public class EditSessionStore : IEditSessionStore {
    private static readonly UtLog Logger = new UtLog("Session");
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _cachePath;
    private readonly string _sessionPath;
    private readonly object _lock = new();

    public EditSessionStore(IAppConfigService appConfigService) {
        _cachePath = appConfigService.CachePath;
        _sessionPath = Path.Combine(_cachePath, "sessions");
        if (!Directory.Exists(_sessionPath)) {
            Directory.CreateDirectory(_sessionPath);
        }
    }

    private string PathOf(string sessionId) => Path.Combine(_sessionPath, $"{sessionId}.json");

    private static EditSession? Load(string path) {
        try {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditSession>(json, JsonOptions);
        } catch (Exception e) {
            Logger.Error(e, $"cannot load session: {path}");
            return null;
        }
    }

    public List<EditSession> List() {
        lock (_lock) {
            return Directory.EnumerateFiles(_sessionPath, "*.json")
                .Select(Load)
                .OfType<EditSession>()
                .OrderByDescending(it => it.UpdatedAt)
                .ToList();
        }
    }

    public EditSession? Find(string sessionId) {
        lock (_lock) {
            var path = PathOf(sessionId);
            return File.Exists(path) ? Load(path) : null;
        }
    }

    public bool Exists(string sessionId) {
        lock (_lock) {
            return File.Exists(PathOf(sessionId));
        }
    }

    public void Save(EditSession session) {
        lock (_lock) {
            session.UpdatedAt = DateTime.UtcNow;
            var path = PathOf(session.SessionId);
            var tmp = path + ".tmp";
            try {
                File.WriteAllText(tmp, JsonSerializer.Serialize(session, JsonOptions));
                File.Move(tmp, path, overwrite: true);
            } catch (Exception e) {
                Logger.Error(e, $"cannot save session: {path}");
                FileUtil.SafeDelete(tmp);
            }
        }
    }

    public void Delete(string sessionId, bool deleteCache) {
        lock (_lock) {
            var path = PathOf(sessionId);
            if (deleteCache) {
                var session = File.Exists(path) ? Load(path) : null;
                if (session != null && session.IsCache) {
                    DeleteCacheFile(session.FilePath);
                }
            }
            FileUtil.SafeDelete(path);
        }
    }

    public void DiscardItem(IEditorItem item) {
        lock (_lock) {
            FileUtil.SafeDelete(PathOf(item.SessionId));
            if (item.SourceKind != EditSourceKind.File) {
                DeleteCacheFile(item.FilePath);
            }
        }
    }

    /// <summary>
    /// 安全のため、cache フォルダ直下のファイルだけを削除する
    /// </summary>
    private void DeleteCacheFile(string filePath) {
        try {
            var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (string.Equals(dir, Path.GetFullPath(_cachePath), StringComparison.OrdinalIgnoreCase)) {
                FileUtil.SafeDelete(filePath);
            }
        } catch (Exception e) {
            Logger.Error(e);
        }
    }

    public void CleanupOrphanCache(IEnumerable<string>? inUsePaths = null) {
        lock (_lock) {
            try {
                var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in Directory.EnumerateFiles(_sessionPath, "*.json").Select(Load).OfType<EditSession>()) {
                    keep.Add(Path.GetFullPath(s.FilePath));
                }
                if (inUsePaths != null) {
                    foreach (var p in inUsePaths) keep.Add(Path.GetFullPath(p));
                }
                foreach (var file in Directory.EnumerateFiles(_cachePath)) {
                    if (!keep.Contains(Path.GetFullPath(file))) {
                        FileUtil.SafeDelete(file);
                    }
                }
                foreach (var tmp in Directory.EnumerateFiles(_sessionPath, "*.tmp")) {
                    FileUtil.SafeDelete(tmp);
                }
            } catch (Exception e) {
                Logger.Error(e);
            }
        }
    }
}
