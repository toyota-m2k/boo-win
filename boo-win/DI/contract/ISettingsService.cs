using BooWin.model;
using System;
using System.Collections.Generic;

namespace BooWin.DI.contract;

public interface ISettingsService {
    // FFMpeg
    bool UseFFMpeg { get; set; }
    string FFMpegPath { get; set; }
    string WorkingPath { get; set; }

    // Hosts
    IReadOnlyList<PeerHost> Hosts { get; }
    PeerHostStatus? CurrentHost { get; }
    void SetCurrentHost(PeerHost? host);
    void UpdateCurrentHostStatus(string lastPlayingId, long lastPlayingPosition, bool save = true);
    void UpdateHostList(IEnumerable<PeerHost> newHostList, PeerHost? newHost);

    void Save();
}
