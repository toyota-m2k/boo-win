using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class PeerHostStatus : PeerHost {
    public string LastPlayingId { get; set; }
    public long LastPlayingPosition { get; set; }

    public PeerHostStatus(PeerHost peer, string lastPlayingId, long lastPlayingPosition) : base(peer.Address, peer.ServiceName, peer.Hostname, peer.IsHttps, peer.Fingerprint) { 
        LastPlayingId = lastPlayingId;
        LastPlayingPosition = lastPlayingPosition;      
    }

    [JsonConstructor]
    public PeerHostStatus(string address, string? serviceName, string? hostname, bool isHttps, string? fingerprint,
        string lastPlayingId, long lastPlayingPosition) : base(address, serviceName, hostname, isHttps, fingerprint) {
        LastPlayingId = lastPlayingId;
        LastPlayingPosition = lastPlayingPosition;
    }
}
