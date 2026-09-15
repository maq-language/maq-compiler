namespace Maq.Source.Tests;

[TestClass]
public sealed class SourceRangeTests
{
    [TestMethod]
    public void TestFileIdRejectsReservedZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileId(0));
    }

    [TestMethod]
    public void TestRangeExposesStartAndEndLocation()
    {
        var file = new FileId(7);
        var range = new SourceRange(file, new TextSpan(10, 5));

        Assert.AreEqual(new SourceLocation(file, 10), range.Start);
        Assert.AreEqual(new SourceLocation(file, 15), range.End);
    }

    [TestMethod]
    public void TestRangeDoesNotContainLocationFromAnotherFile()
    {
        var range = new SourceRange(new FileId(1), new TextSpan(0, 10));
        var location = new SourceLocation(new FileId(2), 5);

        Assert.IsFalse(range.Contains(location));
    }
}
