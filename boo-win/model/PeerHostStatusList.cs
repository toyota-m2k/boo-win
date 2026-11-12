using BooWin.DI.implement.settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class PeerHostStatusList {

    public const string SETTING_KEY = "PHSL";
    public List<PeerHostStatus> Hosts { get; set; } = new ();
    public int CurrentIndex { get; set; } = -1;

    public void SaveTo(JSONSettngStore store) {
        if (IsDirty) {
            store.Set<PeerHostStatusList>(SETTING_KEY, this);
            IsDirty = false;
        }
    }

    public static PeerHostStatusList LoadFrom(JSONSettngStore store) {
        var list = store.GetOrNull<PeerHostStatusList>(SETTING_KEY);
        if (list == null) {
            list = new PeerHostStatusList();
        }
        return list;
    }

    #region List

    [JsonIgnore]
    public int Count => Hosts.Count;

    public PeerHostStatus? this[int index] {
        get {
            if (index >= 0 && index < Hosts.Count) {
                return Hosts[index];
            }
            return null;
        }
    }

    [JsonIgnore]
    public bool IsDirty { get; set; } = false;

    public int AddOrUpdate(PeerHostStatus? hostStatus) {
        if (hostStatus == null) return -1;
        var existing = Hosts.FindIndex(h => h != null && h.Address.Equals(hostStatus.Address));
        if (existing != -1) {
            Hosts[existing] = hostStatus;
            return existing;
        } else {
            IsDirty = true;
            Hosts.Add(hostStatus);
            return Hosts.Count - 1;
        }
    }

    public void Remove(PeerHostStatus? hostStatus) {
        if (hostStatus == null) return;
        var existing = Hosts.FindIndex(h => h != null && h.Address.Equals(hostStatus.Address));
        if (existing != -1) {
            if (existing == CurrentIndex) {
                CurrentIndex = -1;
                Hosts.RemoveAt(existing);
                IsDirty = true;
            }
        }
    }

    public void UpdateList(IEnumerable<PeerHost> newList) {
        foreach(var newHost in newList) {
            var existing = Hosts.FindIndex(h => h != null && h.Address.Equals(newHost.Address));
            if (existing == -1) {
                // 新しく追加されたホスト
                Hosts.Add(new PeerHostStatus(newHost, null, 0));
                IsDirty = true;
            } else {
                // 既存ホスト ... パラメータが変更されていれば更新する
                var org = Hosts[existing];
                if (!org.Equals(newHost)) {
                    var lastId = (org as PeerHostStatus)?.LastPlayingId;
                    var pos = (org as PeerHostStatus)?.LastPlayingPosition ?? 0;
                    Hosts[existing] = new PeerHostStatus(newHost, lastId, pos);
                }
            }
        }
        foreach(var existingHost in Hosts.ToArray()) {
            if (!newList.Any(n => n.Address.Equals(existingHost.Address))) {
                // 削除されたホスト
                Hosts.Remove(existingHost);
                IsDirty = true;
            }
        }
    }

    #endregion

    #region Current

    [JsonIgnore]
    public PeerHostStatus? CurrentHost {
        get {
            if (CurrentIndex >= 0 && CurrentIndex < Hosts.Count) {
                return Hosts[CurrentIndex];
            }
            return null;
        }
        set {
            if (value == null) {
                CurrentIndex = -1;
            }
            else {
                var index = Hosts.FindIndex(h => h != null && h.Address.Equals(value.Address));
                if (index != -1) {
                    if (index != CurrentIndex) {
                        CurrentIndex = index;
                        IsDirty = true;
                    }
                }
                else {
                    Hosts.Add(value);
                    CurrentIndex = Hosts.Count - 1;
                    IsDirty = true;
                }
            }
        }
    }

    public void UpdateCurrentStatus(string lastPlayingId, long lastPlayingPosition) {
        var currentHost = CurrentHost;
        if (currentHost != null) {
            var updatedHost = currentHost.UpdateStatus(lastPlayingId, lastPlayingPosition);
            Hosts[CurrentIndex] = (PeerHostStatus)updatedHost;
            IsDirty = true;
        }
    }

    #endregion
}
