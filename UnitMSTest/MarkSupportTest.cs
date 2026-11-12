namespace UnitMSTest;

[TestClass]
public class MarkSupportTest
{
    [TestMethod]
    public void TestMarkSupport()
    {
        var jsonString = @"{
            ""cmd"":""marks"",
            ""marks"":[
                {""mark"":1,""label"":""Star"",""svg"":""M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z""},
                {""mark"":2,""label"":""Flag"",""svg"":""M14.4,6L14,4H5V21H7V14H12.6L13,16H20V6H14.4Z""},
                {""mark"":3,""label"":""Heart"",""svg"":""M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z""}
            ]}";
        var markSupport = BooWin.model.MarkSupport.FromJson(jsonString);

        Assert.HasCount(3, markSupport.Marks);
        Assert.AreEqual(1, markSupport.Marks[0].Value);
        Assert.AreEqual("Star", markSupport.Marks[0].Label);
        Assert.AreEqual("M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z", markSupport.Marks[0].SvgPath);

        Assert.AreEqual(2, markSupport.Marks[1].Value);
        Assert.AreEqual("Flag", markSupport.Marks[1].Label);
        Assert.AreEqual("M14.4,6L14,4H5V21H7V14H12.6L13,16H20V6H14.4Z", markSupport.Marks[1].SvgPath);

        Assert.AreEqual(3, markSupport.Marks[2].Value);
        Assert.AreEqual("Heart", markSupport.Marks[2].Label);
        Assert.AreEqual("M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z", markSupport.Marks[2].SvgPath);
    }
}
