using Maq.Source;

namespace Maq.Syntax.Tests;

[TestClass]
public sealed class TokenizerTests
{
    [TestMethod]
    public void TestEnumeratorDoesNotConsumeTokenizer()
    {
        var tokenizer = new Tokenizer(SourceText.From("alpha + beta"));

        var tokens = tokenizer.ToArray();

        Assert.AreEqual(0, tokenizer.Position);

        CollectionAssert.AreEqual(new[]
        {
            SyntaxKind.IdentifierToken,
            SyntaxKind.PlusSignToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.EndOfFileToken
        }, tokens.Select(static token => token.Kind).ToArray());
    }

    [TestMethod]
    public void TestReadUsesUtf8ByteOffsets()
    {
        var source = SourceText.From("π + value");
        var tokenizer = new Tokenizer(source);

        var pi = tokenizer.Read();
        var plus = tokenizer.Read();
        var value = tokenizer.Read();

        Assert.AreEqual(new TextSpan(0, 2), pi.Span);
        Assert.AreEqual(new TextSpan(3, 1), plus.Span);
        Assert.AreEqual(new TextSpan(5, 5), value.Span);

        Assert.AreEqual("π", source.GetText(pi.Span));
        Assert.AreEqual("value", source.GetText(value.Span));
    }

    [TestMethod]
    public void TestReadAcceptsUnicodeIdentifiers()
    {
        var source = SourceText.From("café Δvalue");
        var tokenizer = new Tokenizer(source);

        var first = tokenizer.Read();
        var second = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, first.Kind);
        Assert.AreEqual(SyntaxKind.IdentifierToken, second.Kind);
        Assert.AreEqual("café", source.GetText(first.Span));
        Assert.AreEqual("Δvalue", source.GetText(second.Span));
    }

    [TestMethod]
    public void TestReadUnicodeIdentifierConsumesContinuationRunes()
    {
        var source = SourceText.From("Δμέτρο");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, token.Kind);

        Assert.AreEqual("Δμέτρο", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadIdentifierAllowsAsciiContinuationCharacters()
    {
        var source = SourceText.From("value_123");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, token.Kind);
        Assert.AreEqual("value_123", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadIdentifierAllowsUnicodeDecimalDigitContinuation()
    {
        var source = SourceText.From("value٢");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, token.Kind);
        Assert.AreEqual("value٢", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadIdentifierAllowsCombiningMarkContinuation()
    {
        var source = SourceText.From("e\u0301");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, token.Kind);
        Assert.AreEqual("e\u0301", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadIdentifierStopsAtOperator()
    {
        var source = SourceText.From("left+right");
        var tokenizer = new Tokenizer(source);

        var left = tokenizer.Read();
        var plus = tokenizer.Read();
        var right = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, left.Kind);
        Assert.AreEqual(SyntaxKind.PlusSignToken, plus.Kind);
        Assert.AreEqual(SyntaxKind.IdentifierToken, right.Kind);

        Assert.AreEqual("left", source.GetText(left.Span));
        Assert.AreEqual("+", source.GetText(plus.Span));
        Assert.AreEqual("right", source.GetText(right.Span));
    }

    [TestMethod]
    public void TestReadUnicodeIdentifierStopsAtOperator()
    {
        var source = SourceText.From("π+Δ");
        var tokenizer = new Tokenizer(source);

        var left = tokenizer.Read();
        var plus = tokenizer.Read();
        var right = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, left.Kind);
        Assert.AreEqual(SyntaxKind.PlusSignToken, plus.Kind);
        Assert.AreEqual(SyntaxKind.IdentifierToken, right.Kind);

        Assert.AreEqual("π", source.GetText(left.Span));
        Assert.AreEqual("+", source.GetText(plus.Span));
        Assert.AreEqual("Δ", source.GetText(right.Span));
    }

    [TestMethod]
    public void TestReadIntegerLiteralConsumesDigits()
    {
        var source = SourceText.From("123456");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IntegerLiteralToken, token.Kind);
        Assert.AreEqual("123456", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadIntegerLiteralAllowsUnderscores()
    {
        var source = SourceText.From("1_000_000");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IntegerLiteralToken, token.Kind);
        Assert.AreEqual("1_000_000", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadIntegerLiteralStopsBeforeIdentifier()
    {
        var source = SourceText.From("123abc");
        var tokenizer = new Tokenizer(source);

        var integer = tokenizer.Read();
        var identifier = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IntegerLiteralToken, integer.Kind);
        Assert.AreEqual(SyntaxKind.IdentifierToken, identifier.Kind);

        Assert.AreEqual("123", source.GetText(integer.Span));
        Assert.AreEqual("abc", source.GetText(identifier.Span));
    }

    [TestMethod]
    public void TestReadIntegerLiteralStopsAtOperator()
    {
        var source = SourceText.From("123+456");
        var tokenizer = new Tokenizer(source);

        var left = tokenizer.Read();
        var plus = tokenizer.Read();
        var right = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IntegerLiteralToken, left.Kind);
        Assert.AreEqual(SyntaxKind.PlusSignToken, plus.Kind);
        Assert.AreEqual(SyntaxKind.IntegerLiteralToken, right.Kind);

        Assert.AreEqual("123", source.GetText(left.Span));
        Assert.AreEqual("+", source.GetText(plus.Span));
        Assert.AreEqual("456", source.GetText(right.Span));
    }

    [TestMethod]
    public void TestReadTokenBoundariesPreserveUtf8ByteOffsets()
    {
        var source = SourceText.From("π123+42");
        var tokenizer = new Tokenizer(source);

        var identifier = tokenizer.Read();
        var plus = tokenizer.Read();
        var integer = tokenizer.Read();

        Assert.AreEqual(new TextSpan(0, 5), identifier.Span);
        Assert.AreEqual(new TextSpan(5, 1), plus.Span);
        Assert.AreEqual(new TextSpan(6, 2), integer.Span);

        Assert.AreEqual("π123", source.GetText(identifier.Span));
        Assert.AreEqual("+", source.GetText(plus.Span));
        Assert.AreEqual("42", source.GetText(integer.Span));
    }

    [TestMethod]
    public void TestReadUsesMaximalMunchForCompoundOperators()
    {
        var tokenizer = new Tokenizer(SourceText.From(":: -> && || != == => <= << >= >>"));

        CollectionAssert.AreEqual(new[]
        {
            SyntaxKind.ColonColonToken,
            SyntaxKind.HyphenMinusGreaterThanToken,
            SyntaxKind.AmpersandAmpersandToken,
            SyntaxKind.VerticalLineVerticalLineToken,
            SyntaxKind.ExclamationMarkEqualsToken,
            SyntaxKind.EqualsEqualsToken,
            SyntaxKind.EqualsGreaterThanToken,
            SyntaxKind.LessThanEqualsToken,
            SyntaxKind.LessThanLessThanToken,
            SyntaxKind.GreaterThanEqualsToken,
            SyntaxKind.GreaterThanGreaterThanToken,
            SyntaxKind.EndOfFileToken,
        }, tokenizer.Select(static token => token.Kind).ToArray());
    }

    [TestMethod]
    public void TestReadUnexpectedUnicodeCharacterConsumesWholeRune()
    {
        var source = SourceText.From("🐒");
        var tokenizer = new Tokenizer(source);

        var token = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.BadToken, token.Kind);
        Assert.AreEqual(new TextSpan(0, 4), token.Span);
        Assert.AreEqual("🐒", source.GetText(token.Span));
    }

    [TestMethod]
    public void TestReadAfterEndReturnsStableEndOfFile()
    {
        var tokenizer = new Tokenizer(SourceText.From(string.Empty));

        var first = tokenizer.Read();
        var second = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.EndOfFileToken, first.Kind);
        Assert.AreEqual(first, second);
        Assert.AreEqual(0, tokenizer.Position);
    }

    [TestMethod]
    public void TestReadFilterOverrideCanIncludeSpacing()
    {
        var source = SourceText.From("alpha beta");

        var tokenizer = new Tokenizer(source, TokenFilter.Spacing);

        var alpha = tokenizer.Read();
        var spacing = tokenizer.Read(TokenFilter.None);
        var beta = tokenizer.Read();

        Assert.AreEqual(SyntaxKind.IdentifierToken, alpha.Kind);
        Assert.AreEqual(SyntaxKind.SpacingToken, spacing.Kind);
        Assert.AreEqual(SyntaxKind.IdentifierToken, beta.Kind);
    }
}
