using BooWin.model;
using System;

namespace BooWin.DI.contract;

public interface ISettingsService {
    // FFMpeg
    bool UseFFMpeg { get; set; }
    string FFMpegPath { get; set; }
    string WorkingPath { get; set; }

    // Server
    PeerHost CurrentPeer { get; set; }

    

    void Save();
}
