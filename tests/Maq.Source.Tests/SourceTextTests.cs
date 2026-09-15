using System.Text;

namespace Maq.Source.Tests;

[TestClass]
public sealed class SourceTextTests
{
    [TestMethod]
    public void TestFromStringStoresUtf8()
    {
        var text = SourceText.From("6🐒7");

        Assert.AreEqual(6, text.Length);
        Assert.AreEqual("🐒", text.GetText(new TextSpan(1, 4)));
    }

    [TestMethod]
    public void TestFromUtf8CopiesInput()
    {
        var bytes = Encoding.UTF8.GetBytes("maq");
        var text = SourceText.FromUtf8(bytes);

        bytes[0] = (byte)'x';

        Assert.AreEqual("maq", text.ToString());
    }

    [TestMethod]
    public void TestFromUtf8RejectsInvalidUtf8()
    {
        byte[] invalid = [0xC3, 0x28];

        Assert.Throws<DecoderFallbackException>(() => SourceText.FromUtf8(invalid));
    }

    [TestMethod]
    public void TestLinesHandlesMixedLineEndings()
    {
        var text = SourceText.From("a\r\nb\nc\rd");

        Assert.AreEqual(4, text.LineCount);
        Assert.AreEqual("a", text.GetText(text.GetLine(0).Span));
        Assert.AreEqual("b", text.GetText(text.GetLine(1).Span));
        Assert.AreEqual("c", text.GetText(text.GetLine(2).Span));
        Assert.AreEqual("d", text.GetText(text.GetLine(3).Span));

        Assert.AreEqual("a\r\n", text.GetText(text.GetLine(0).SpanIncludingLineBreak));
        Assert.AreEqual("b\n", text.GetText(text.GetLine(1).SpanIncludingLineBreak));
        Assert.AreEqual("c\r", text.GetText(text.GetLine(2).SpanIncludingLineBreak));
    }

    [TestMethod]
    public void TestTrailingNewlineCreatesEmptyFinalLine()
    {
        var text = SourceText.From("maq\n");

        Assert.AreEqual(2, text.LineCount);
        Assert.AreEqual("maq", text.GetText(text.GetLine(0).Span));

        Assert.IsTrue(text.GetLine(1).Span.IsEmpty);
        Assert.AreEqual(text.Length, text.GetLine(1).Span.Start);
    }

    [TestMethod]
    public void TestLineLookupByteOffsets()
    {
        var text = SourceText.From("🐒\nmaq");

        Assert.AreEqual(0, text.GetLineIndex(0));
        Assert.AreEqual(0, text.GetLineIndex(4)); // NOTE(alex): newline byte
        Assert.AreEqual(1, text.GetLineIndex(5)); // NOTE(alex): first byte of 'm'

        Assert.AreEqual(1, text.GetLineIndex(text.Length));
    }

    [TestMethod]
    public void TestSliceRejectsRangePastEnd()
    {
        var text = SourceText.From("maq");

        Assert.Throws<ArgumentOutOfRangeException>(() => text.GetText(new TextSpan(2, 2)));
    }
}
