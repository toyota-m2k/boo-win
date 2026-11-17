namespace UnitMSTest;

[TestClass]
public class RatingSupprtTest
{
    [TestMethod]
    public void TestRatingSupport()
    {
        var jsonString = @"{""cmd"":""ratings"",""default"":3,""ratings"":[
            {""rating"":1,""label"":""Lowest"",""svg"":""M 10 7V 9H 12V 17H 14V 7H 10Z""},
            {""rating"":2,""label"":""Low"",""svg"":""M 9 7V 9H 13V 11H 11A 2,2,0,0,0,9,13V 17H 11L 15 17V 15H 11V 13H 13A 2,2,0,0,0,15,11V 9A 2,2,0,0,0,13,7H 9Z""},
            {""rating"":3,""label"":""Normal"",""svg"":""M 15 15V 13.5A 1.5,1.5,0,0,0,13.5,12 1.5,1.5,0,0,0,15,10.5V 9C 15 7.89 14.1 7 13 7H 9V 9H 13V 11H 11V 13H 13V 15H 9V 17H 13A 2,2,0,0,0,15,15""},
            {""rating"":4,""label"":""High"",""svg"":""M 9 7V 13H 13V 17H 15V 7H 13V 11H 11V 7H 9Z""},
            {""rating"":5,""label"":""Highest"",""svg"":""M 9 7V 13H 13V 15H 9V 17H 13A 2,2,0,0,0,15,15V 13A 2,2,0,0,0,13,11H 11V 9H 15V 7H 9Z""}]}";
        var ratingSupport = BooWin.model.RatingSupport.FromJson(jsonString, true);

        Assert.HasCount(5, ratingSupport.Ratings);
        Assert.AreEqual(3, ratingSupport.DefaultRating);

        Assert.AreEqual(1, ratingSupport.Ratings[0].Value);
        Assert.AreEqual("Lowest", ratingSupport.Ratings[0].Label);
        Assert.AreEqual("M 10 7V 9H 12V 17H 14V 7H 10Z", ratingSupport.Ratings[0].SvgPath);

        Assert.AreEqual(2, ratingSupport.Ratings[1].Value);
        Assert.AreEqual("Low", ratingSupport.Ratings[1].Label);
        Assert.AreEqual("M 9 7V 9H 13V 11H 11A 2,2,0,0,0,9,13V 17H 11L 15 17V 15H 11V 13H 13A 2,2,0,0,0,15,11V 9A 2,2,0,0,0,13,7H 9Z", ratingSupport.Ratings[1].SvgPath);
        Assert.AreEqual(3, ratingSupport.Ratings[2].Value);
        Assert.AreEqual("Normal", ratingSupport.Ratings[2].Label);
        Assert.AreEqual("M 15 15V 13.5A 1.5,1.5,0,0,0,13.5,12 1.5,1.5,0,0,0,15,10.5V 9C 15 7.89 14.1 7 13 7H 9V 9H 13V 11H 11V 13H 13V 15H 9V 17H 13A 2,2,0,0,0,15,15", ratingSupport.Ratings[2].SvgPath);
        Assert.AreEqual(4, ratingSupport.Ratings[3].Value);
        Assert.AreEqual("High", ratingSupport.Ratings[3].Label);
        Assert.AreEqual("M 9 7V 13H 13V 17H 15V 7H 13V 11H 11V 7H 9Z", ratingSupport.Ratings[3].SvgPath);
        Assert.AreEqual(5, ratingSupport.Ratings[4].Value);
        Assert.AreEqual("Highest", ratingSupport.Ratings[4].Label);
        Assert.AreEqual("M 9 7V 13H 13V 15H 9V 17H 13A 2,2,0,0,0,15,15V 13A 2,2,0,0,0,13,11H 11V 9H 15V 7H 9Z", ratingSupport.Ratings[4].SvgPath);
    }
}
