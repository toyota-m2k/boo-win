using BooWin.DI.implement.settings;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;

namespace UnitMSTest;

[TestClass]
public class JSONSettingStoreTest
{
    [TestMethod]
    public void TestPeerHostStatus()
    {
        var peerHost = new BooWin.model.PeerHost("http://example.com", "service", "hostname", true, "fingerprint");
        var peerHostStatus = new BooWin.model.PeerHostStatus(peerHost, "lastPlayingId", 12345);
        var path = "test_peer_host_status.json";
        var settingsStore = new JSONSettngStore(path);
        settingsStore.Set("phs", peerHostStatus);
        settingsStore.Save();
        var settingsStore2 = new JSONSettngStore(path);
        var loadedPeerHostStatus = settingsStore2.GetOrNull<BooWin.model.PeerHostStatus>("phs");
        Assert.IsNotNull(loadedPeerHostStatus);
        Assert.AreEqual(peerHostStatus.Address, loadedPeerHostStatus!.Address);
        Assert.AreEqual(peerHostStatus.ServiceName, loadedPeerHostStatus.ServiceName);
        Assert.AreEqual(peerHostStatus.Hostname, loadedPeerHostStatus.Hostname);
        Assert.AreEqual(peerHostStatus.IsHttps, loadedPeerHostStatus.IsHttps);
        Assert.AreEqual(peerHostStatus.Fingerprint, loadedPeerHostStatus.Fingerprint);
        Assert.AreEqual(peerHostStatus.LastPlayingId, loadedPeerHostStatus.LastPlayingId);
        Assert.AreEqual(peerHostStatus.LastPlayingPosition, loadedPeerHostStatus.LastPlayingPosition);
        FileUtil.SafeDelete(path);

        Assert.IsTrue(loadedPeerHostStatus.Equals(peerHostStatus));
        Assert.IsTrue(peerHost.Equals(loadedPeerHostStatus));
        Assert.IsTrue(loadedPeerHostStatus.Equals(peerHost));
    }

    [TestMethod]
    public void TestPeerHostStatusList()
    {
        var peerHost1 = new BooWin.model.PeerHost("http://example.com", "service", "hostname", true, "fingerprint");
        var peerHostStatus1 = new BooWin.model.PeerHostStatus(peerHost1, "lastPlayingId", 12345);
        var peerHost2 = new BooWin.model.PeerHost("http://example.org", "service2", "hostname2", false, "fingerprint2");
        var peerHostStatus2 = new BooWin.model.PeerHostStatus(peerHost2, "lastPlayingId2", 67890);
        var peerHost3 = new BooWin.model.PeerHost("http://example.net", "service3", "hostname3", true, "fingerprint3");
        var peerHostStatus3 = new BooWin.model.PeerHostStatus(peerHost3, "lastPlayingId3", 54321);

        var peerHostStatusList = new BooWin.model.PeerHostStatusList();
        Assert.IsFalse(peerHostStatusList.IsDirty);
        peerHostStatusList.AddOrUpdate(peerHostStatus1);
        Assert.IsTrue(peerHostStatusList.IsDirty);
        peerHostStatusList.AddOrUpdate(peerHostStatus2);
        peerHostStatusList.AddOrUpdate(peerHostStatus3);
        peerHostStatusList.CurrentHost = peerHostStatus2;
        Assert.HasCount(3, peerHostStatusList.Hosts);
        peerHostStatusList.UpdateCurrentStatus("lastPlayingId2_updated", 98765);
        Assert.HasCount(3, peerHostStatusList.Hosts);
        Assert.AreEqual("lastPlayingId2_updated", peerHostStatusList.CurrentHost!.LastPlayingId);
        Assert.AreEqual(98765, peerHostStatusList.CurrentHost!.LastPlayingPosition);

        var path = "test_peer_host_status_list.json";
        var settingsStore = new JSONSettngStore(path);
        settingsStore.ResetAll();

        peerHostStatusList.SaveTo(settingsStore);
        settingsStore.Save();

        var settingsStore2 = new JSONSettngStore(path);
        var loadedPeerHostStatusList = PeerHostStatusList.LoadFrom(settingsStore2);
        Assert.IsNotNull(loadedPeerHostStatusList);
        Assert.HasCount(peerHostStatusList.Hosts.Count, loadedPeerHostStatusList!.Hosts);
        Assert.AreEqual("lastPlayingId2_updated", loadedPeerHostStatusList.CurrentHost!.LastPlayingId);
        Assert.AreEqual(98765, loadedPeerHostStatusList.CurrentHost!.LastPlayingPosition);
        Assert.HasCount(3, loadedPeerHostStatusList.Hosts);
        var hs1 = loadedPeerHostStatusList.Hosts[0];
        Assert.AreEqual(peerHostStatus1.Address, hs1.Address);
        var hs2 = loadedPeerHostStatusList.Hosts[1];
        Assert.AreEqual(peerHostStatus2.Address, hs2.Address);
        var hs3 = loadedPeerHostStatusList.Hosts[2];
        Assert.AreEqual(peerHostStatus3.Address, hs3.Address);
        Assert.IsFalse(peerHostStatusList.IsDirty);

        FileUtil.SafeDelete(path);
    }
}
