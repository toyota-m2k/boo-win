using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;
using System.Collections.Generic;

namespace BooWin.DI.implement;

public class SettingsService : ISettingsService {
    private readonly IAppConfigService AppConfigService;
    private readonly JSONSettngStore JsonStore;
    private PeerHostStatusList HostList { get; set; }

    public SettingsService(IAppConfigService appConfigService) {
        AppConfigService = appConfigService;
        JsonStore = new JSONSettngStore(AppConfigService.SettingsPath);
        HostList = PeerHostStatusList.LoadFrom(store: JsonStore);
    }

    #region FFMpeg

    static readonly string KEY_USE_FFMPEG = "UseFFMpeg";

    public bool UseFFMpeg {
        get => JsonStore.Get<bool>(KEY_USE_FFMPEG, false);
        set => JsonStore.Set(KEY_USE_FFMPEG, value);
    }
    static readonly string KEY_FFMPEG_PATH = "FFMpegPath";
    public string FFMpegPath {
        get => JsonStore.Get<string>(KEY_FFMPEG_PATH, string.Empty);
        set => JsonStore.Set(KEY_FFMPEG_PATH, value);
    }
    static readonly string KEY_WORKING_PATH = "WorkingPath";
    public string WorkingPath {
        get => JsonStore.Get<string>(KEY_WORKING_PATH, string.Empty);
        set => JsonStore.Set(KEY_WORKING_PATH, value);
    }

    static readonly string KEY_AUTO_START_PLAYER= "AutoStart";
    public bool AutoStartPlayer {
        get => JsonStore.Get<bool>(KEY_AUTO_START_PLAYER, false);
        set => JsonStore.Set(KEY_AUTO_START_PLAYER, value);
    }


    #endregion

    #region Hosts

    /**
     * 登録済みホストリスト
     */
    public IReadOnlyList<PeerHost> Hosts => HostList.Hosts;
    
    /**
     * 現在選択中のホスト情報
     */
    public PeerHostStatus? CurrentHost {
        get => HostList.CurrentHost;
    }

    public void SetCurrentHost(PeerHost? host) {
        if (host == null) {
            HostList.CurrentIndex = -1;
        }
        else {
            var index = HostList.Hosts.FindIndex(h => h.Address.Equals(host.Address));
            if (index != -1) {
                HostList.CurrentIndex = index;
            }
        }
    }

    /**
     * 現在選択中のホストの再生状態を更新する
     */
    public void UpdateCurrentHostStatus(string lastPlayingId, long lastPlayingPosition, bool save=true) {
        if (CurrentHost != null) {
            var updated = CurrentHost.UpdateStatus(lastPlayingId, lastPlayingPosition);
            HostList.AddOrUpdate((PeerHostStatus)updated);
            if (save) {
                HostList.SaveTo(JsonStore);
            }
        }
    }

    /**
     * ホストリストを新しいリストで更新する
     */
    public void UpdateHostList(IEnumerable<PeerHost> newHostList, PeerHost? newHost) {
        HostList.UpdateList(newHostList);
        if (newHost!=null) {
            SetCurrentHost(newHost);
        }
        HostList.SaveTo(JsonStore);
    }

    #endregion

    public void Save() {
        JsonStore.Save();
    }
}
