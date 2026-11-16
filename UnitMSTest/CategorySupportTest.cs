namespace UnitMSTest;

[TestClass]
public class CategorySupportTest
{
    [TestMethod]
    public void TestCategorySupport()
    {
        var jsonString = @"{
            ""cmd"":""categories"",
            ""categories"":[
                {""label"":""Default"",""color"":""0"",""sort"":""0"",""svg"":""""},
                {""label"":""hoge"",""color"":""0"",""sort"":""0"",""svg"":""""}]
            }";
        var categorySupport = BooWin.model.CategorySupport.FromJson(jsonString, true);
        Assert.IsTrue(categorySupport.IsSupported);
        Assert.HasCount(2, categorySupport.Categories);
        Assert.AreEqual("Default", categorySupport.Categories[0].Name);
        Assert.AreEqual(0, categorySupport.Categories[0].SortIndex);
        Assert.AreEqual("hoge", categorySupport.Categories[1].Name);
        Assert.AreEqual(0, categorySupport.Categories[1].SortIndex);

        jsonString = @"{""cmd"":""categories"",""categories"":[{""label"":""Default"",""color"":""0"",""sort"":""0"",""svg"":""""},{""label"":""hoge"",""color"":""0"",""sort"":""1"",""svg"":""""}]}";
        categorySupport = BooWin.model.CategorySupport.FromJson(jsonString, true);
        Assert.IsTrue(categorySupport.IsSupported);
        Assert.HasCount(2, categorySupport.Categories);
        Assert.AreEqual("hoge", categorySupport.Categories[0].Name);
        Assert.AreEqual(1, categorySupport.Categories[0].SortIndex);
        Assert.AreEqual("Default", categorySupport.Categories[1].Name);
        Assert.AreEqual(0, categorySupport.Categories[1].SortIndex);
    }
}
