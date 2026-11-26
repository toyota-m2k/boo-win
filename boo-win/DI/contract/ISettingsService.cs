using BooWin.model;
using io.github.toyota32k.BooLib.Media;
using System;
using System.Collections.Generic;

namespace BooWin.DI.contract;

public interface ISettingsService {
    // FFMpeg 
    bool UseFFMpeg { get; set; }
    string FFMpegPath { get; set; }
    string WorkingPath { get; set; }
    
    // Converter
    int MaxLengthInPixel { get; set; }
    int MaxFrameRate { get; set; }
    int MaxBitRate { get; set; }
    int CRF { get; set; }
    VideoCodecOption OutputVideoCodec { get; set; }


    // Player
    bool AutoStartPlayer { get; set; }

    // Hosts
    IReadOnlyList<PeerHost> Hosts { get; }
    PeerHostStatus? CurrentHost { get; }
    void SetCurrentHost(PeerHost? host);
    void UpdateCurrentHostStatus(string lastPlayingId, long lastPlayingPosition, bool save = true);
    void UpdateHostList(IEnumerable<PeerHost> newHostList, PeerHost? newHost);

    // Additionals
    HostAdditionalInfoMap Additionals { get; }
    void SaveAdditionals();

    void Save();
}
