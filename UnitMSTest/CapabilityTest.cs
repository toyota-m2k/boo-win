using BooWin.model;
using System.Text.Json;

namespace UnitMSTest;

[TestClass]
public class CapabilityTest
{
    [TestMethod]
    public void TestCapability()
    {
        var dic = new Dictionary<string, object> {
                        {"cmd", "capability"},
                        {"serverName", "xyz"},
                        {"version", 3},
                        {"root", "/" },
                        {"category", 1},
                        {"rating", 2},
                        {"mark", 0},
                        {"chapter", true},
                        {"reputation", 0 },             // reputation (category/mark/rating) コマンド対応 1:RO /2:RW
                        {"diff", false},                // date以降の更新チェック(check)、差分リスト取得に対応
                        {"sync", false },               // 端末間同期
                        {"acceptRequest", false },        // register command をサポートする
                        {"hasView", false },              // current get/set をサポートする
                        {"authentication", true },
                        {"challenge",  "abcd" },
                        {"types", "vp" },                // v: video, a: audio, p: photo
                    };
        var json = JsonSerializer.Serialize(dic);
        var cap = Capability.FromJson(json);
        Assert.IsNotNull(cap);
        Assert.AreEqual(dic["serverName"], cap.ServerName);
        Assert.AreEqual(dic["version"], cap.Version);
        Assert.AreEqual(dic["category"], cap.Category);
    }
}
