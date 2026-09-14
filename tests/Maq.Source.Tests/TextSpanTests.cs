namespace Maq.Source.Tests;

[TestClass]
public sealed class TextSpanTests
{
    [TestMethod]
    public void TestRangeIsHalfOpen()
    {
        var span = new TextSpan(3, 4);

        Assert.IsTrue(span.Contains(3));
        Assert.IsTrue(span.Contains(6));
        Assert.IsFalse(span.Contains(7));
    }

    [TestMethod]
    public void TestFromBoundsComputesLength()
    {
        var span = TextSpan.FromBounds(4, 9);

        Assert.AreEqual(4, span.Start);
        Assert.AreEqual(5, span.Length);
        Assert.AreEqual(9, span.End);
    }

    [TestMethod]
    public void TestAdjacentSpansDoNotOverlap()
    {
        var left = new TextSpan(0, 4);
        var right = new TextSpan(4, 2);

        Assert.IsFalse(left.Overlaps(right));
        Assert.IsNull(left.Intersection(right));
    }

    [TestMethod]
    public void TestIntersectionReturnsSharedRange()
    {
        var a = new TextSpan(2, 6);
        var b = new TextSpan(5, 6);

        Assert.AreEqual(new TextSpan(5, 3), a.Intersection(b));
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, -1)]
    public void TestConstructorRejectsNegativeValues(int start, int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextSpan(start, length));
    }
}
