using BooWin.DI.contract;
using BooWin.DI.implement.settings;
using System;

namespace BooWin.DI.implement;

public class SettingsService : ISettingsService {
    private readonly IAppConfigService AppConfigService;
    private readonly JSONSettngStore JsonStore;

    public SettingsService(IAppConfigService appConfigService) {
        AppConfigService = appConfigService;
        JsonStore = new JSONSettngStore(AppConfigService.SettingsPath);
    }

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

    static readonly string KEY_SERVER_NAME = "ServerName";
    public string ServerName {
        get => JsonStore.Get<string>(KEY_SERVER_NAME, DEFAULT_SERVER_NAME);
        set => JsonStore.Set(KEY_SERVER_NAME, value);
    }

    //public string EnsureServerName {
    //    get {
    //        var serverName = this.ServerName;
    //        if (string.IsNullOrWhiteSpace(serverName)) {
    //            serverName = AppConfigService.AppName;
    //        }
    //        return serverName;
    //    }
    //}

    //static readonly string KEY_SERVER_PORT = "ServerPort";
    //static readonly int DEFAULT_SERVER_PORT = 3030;
    //public int ServerPort { 
    //    get => int.TryParse(DBService.KVStore[KEY_SERVER_PORT], out var port) ? port : DEFAULT_SERVER_PORT; 
    //    set => DBService.KVStore[KEY_SERVER_PORT] = value.ToString();
    //}

    //static readonly string KEY_SSL_SERVER_PORT = "SSLServerPort";
    //static readonly int DEFAULT_SSL_SERVER_PORT = 3031;
    //public int SSLServerPort {
    //    get => int.TryParse(DBService.KVStore[KEY_SSL_SERVER_PORT], out var port) ? port : DEFAULT_SSL_SERVER_PORT;
    //    set => DBService.KVStore[KEY_SSL_SERVER_PORT] = value.ToString();
    //}

    //static readonly string KEY_ENABLE_SSL = "EnableSSL";
    //public bool EnableSSL { 
    //    get => DBService.KVStore[KEY_ENABLE_SSL] == "true"; 
    //    set => DBService.KVStore[KEY_ENABLE_SSL] = value ? "true" : "false";
    //}

    //static readonly string KEY_ENABLE_MDNS_ADVERTISING = "EnableMDNSAdvertising";
    //public bool EnableMDNSAdvertising {
    //    get => DBService.KVStore[KEY_ENABLE_MDNS_ADVERTISING] == "true";
    //    set => DBService.KVStore[KEY_ENABLE_MDNS_ADVERTISING] = value ? "true" : "false";
    //}

    //static readonly string KEY_PFX_FILE_PATH = "PFXFilePath";
    //public string PFXFilePath {
    //    get => DBService.KVStore[KEY_PFX_FILE_PATH] ?? string.Empty;
    //    set => DBService.KVStore[KEY_PFX_FILE_PATH] = value;
    //}

    //static readonly string KEY_PFX_PASSWORD = "PFXPassword";
    //public string PFXPassword {
    //    get => DBService.KVStore[KEY_PFX_PASSWORD] ?? string.Empty;
    //    set => DBService.KVStore[KEY_PFX_PASSWORD] = value;
    //}

    //public void Save() {
    //    DBService.KVStore.SaveChanges();
    //}
}
