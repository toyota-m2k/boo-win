using BooWin.model;

namespace UnitMSTest;

[TestClass]
public class MediaItemListTest
{
    [TestMethod]
    public void TestMethod1()
    {
        var json = @"{
            ""cmd"":""list"",""date"":1794266549975,""list"":[
	        {""id"":""1"",""name"":""Name-1"",""size"":285720,""type"":""jpg"",""media"":""p"",""date"":1744684809938,""rating"":0,""mark"":0,""label"":""a"",""categoryName"":""x""},
	        {""id"":""2"",""name"":""Name-2"",""size"":4943447,""type"":""m4a"",""media"":""a"",""date"":1744628987480,""rating"":4,""mark"":1,""label"":""b"",""categoryName"":""y""},
	        {""id"":""3"",""name"":""Name-3"",""size"":3262496,""type"":""mp4"",""media"":""v"",""date"":1744606440730,""rating"":1,""mark"":2,""label"":""c"",""categoryName"":""z""}]            
        }";

        var mediaItemList = MediaItemList.FromJson(json);

        Assert.HasCount(3, mediaItemList.Items);
        Assert.AreEqual("1", mediaItemList.Items[0].Id);
        Assert.AreEqual("Name-1", mediaItemList.Items[0].Name);
        Assert.AreEqual("a", mediaItemList.Items[0].Label);
        Assert.AreEqual("x", mediaItemList.Items[0].CategoryName);
        Assert.AreEqual(0, mediaItemList.Items[0].Mark);
        Assert.AreEqual(MediaType.PHOTO, mediaItemList.Items[0].MediaType);
        Assert.AreEqual(BooWin.model.MediaType.PHOTO, mediaItemList.Items[0].MediaType);
        Assert.AreEqual(285720, mediaItemList.Items[0].Size);
        Assert.AreEqual(1744684809938, mediaItemList.Items[0].DateInUnixTime);
        Assert.AreEqual(0, mediaItemList.Items[0].Rating);

        Assert.AreEqual("2", mediaItemList.Items[1].Id);
        Assert.AreEqual("Name-2", mediaItemList.Items[1].Name);
        Assert.AreEqual("b", mediaItemList.Items[1].Label);
        Assert.AreEqual("y", mediaItemList.Items[1].CategoryName);
        Assert.AreEqual(1, mediaItemList.Items[1].Mark);
        Assert.AreEqual(BooWin.model.MediaType.AUDIO, mediaItemList.Items[1].MediaType);
        Assert.AreEqual(4943447, mediaItemList.Items[1].Size);
        Assert.AreEqual(1744628987480, mediaItemList.Items[1].DateInUnixTime);
        Assert.AreEqual(4, mediaItemList.Items[1].Rating);

        Assert.AreEqual("3", mediaItemList.Items[2].Id);
        Assert.AreEqual("Name-3", mediaItemList.Items[2].Name);
        Assert.AreEqual("c", mediaItemList.Items[2].Label);
        Assert.AreEqual("z", mediaItemList.Items[2].CategoryName);
        Assert.AreEqual(2, mediaItemList.Items[2].Mark);
        Assert.AreEqual(BooWin.model.MediaType.VIDEO, mediaItemList.Items[2].MediaType);
        Assert.AreEqual(3262496, mediaItemList.Items[2].Size);
        Assert.AreEqual(1744606440730, mediaItemList.Items[2].DateInUnixTime);
        Assert.AreEqual(1, mediaItemList.Items[2].Rating);

    }
}
