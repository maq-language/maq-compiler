using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Text;
using Maq.Source;

namespace Maq.Syntax;

/// <summary>
/// Tokenizes source text into a forward-only stream of syntax tokens.
/// </summary>
public class Tokenizer : IEnumerable<SyntaxToken>
{
    private readonly SourceText _source;
    private readonly SyntaxToken _endOfFileToken;  // NOTE(alex): Optimization! This token will always exist.
    private readonly TokenFilter _filter;
    private int _position;

    public Tokenizer(SourceText source, TokenFilter filter = TokenFilter.Trivia)
        : this(source, 0, filter)
    {
    }

    private Tokenizer(SourceText source, int position, TokenFilter filter)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _position = position;
        _filter = filter;

        _endOfFileToken = new SyntaxToken(SyntaxKind.EndOfFileToken, new TextSpan(_source.Length, 0));
    }

    public SourceText Source => _source;

    public int Position => _position;

    public bool IsAtEnd => _position >= _source.Length;

    public TokenFilter DefaultFilter => _filter;

    public SyntaxToken EndOfFileToken => _endOfFileToken;

    /// <summary>
    /// Reads a token and advances the current source position, respecting the default filter.
    /// </summary>
    public SyntaxToken Read() => Read(_filter);

    /// <summary>
    /// Reads a token and advances the current source position, bypassing the default filter.
    /// </summary>
    public SyntaxToken Read(TokenFilter filter)
    {
        SyntaxToken token;

        do
        {
            token = ReadInternal();
        } while(IsFiltered(filter, token.Kind));

        return token;
    }

    private SyntaxToken ReadInternal()
    {
        if (IsAtEnd)
            return _endOfFileToken;

        var start = _position;
        var current = Current;

        // NOTE(alex): Consume only the first byte.
        // Multi-character tokens are handled explicity in the switch case below.
        AdvanceChars(1);

        var kind = current switch
        {
            //   9 \t
            //  11 \v
            //  12 \f
            //  32 space
            (byte)'\t' or
            (byte)'\v' or
            (byte)'\f' or
            (byte)' ' => ScanSpacing(),

            //  10 \n
            //  13 \r
            (byte)'\n' or
            (byte)'\r' => ScanEndOfLine(current),

            //  33 !
            (byte)'!' => TryConsume((byte)'=')
                ? SyntaxKind.ExclamationMarkEqualsToken
                : SyntaxKind.ExclamationMarkToken,

            //  34 "
            (byte)'"' => ScanString(),

            //  37 %
            (byte)'%' => SyntaxKind.PercentSignToken,

            //  38 &
            (byte)'&' => TryConsume((byte)'&')
                ? SyntaxKind.AmpersandAmpersandToken
                : SyntaxKind.AmpersandToken,

            //  40 (
            (byte)'(' => SyntaxKind.LeftParenthesisToken,

            //  41 )
            (byte)')' => SyntaxKind.RightParenthesisToken,

            //  42 *
            (byte)'*' => SyntaxKind.AsteriskToken,

            //  43 +
            (byte)'+' => SyntaxKind.PlusSignToken,

            //  44 ,
            (byte)',' => SyntaxKind.CommaToken,

            //  45 -
            (byte)'-' => TryConsume((byte)'>')
                ? SyntaxKind.HyphenMinusGreaterThanToken
                : SyntaxKind.HyphenMinusToken,

            //  46 .
            (byte)'.' => SyntaxKind.FullStopToken,

            //  47 /
            (byte)'/' => SyntaxKind.SolidusToken,

            //  58 :
            (byte)':' => TryConsume((byte)':')
                ? SyntaxKind.ColonColonToken
                : SyntaxKind.ColonToken,

            //  59 ;
            (byte)';' => SyntaxKind.SemicolonToken,

            //  60 <
            (byte)'<' => TryConsume((byte)'=')
                ? SyntaxKind.LessThanEqualsToken
                : TryConsume((byte)'<')
                    ? SyntaxKind.LessThanLessThanToken
                    : SyntaxKind.LessThanSignToken,

            //  61 =
            (byte)'=' => TryConsume((byte)'=')
                ? SyntaxKind.EqualsEqualsToken
                : TryConsume((byte)'>')
                    ? SyntaxKind.EqualsGreaterThanToken
                    : SyntaxKind.EqualsSignToken,

            //  62 >
            (byte)'>' => TryConsume((byte)'=')
                ? SyntaxKind.GreaterThanEqualsToken
                : TryConsume((byte)'>')
                    ? SyntaxKind.GreaterThanGreaterThanToken
                    : SyntaxKind.GreaterThanSignToken,

            //  63 ?
            (byte)'?' => SyntaxKind.QuestionMarkToken,

            //  91 [
            (byte)'[' => SyntaxKind.LeftSquareBracketToken,

            //  93 ]
            (byte)']' => SyntaxKind.RightSquareBracketToken,

            //  94 ^
            (byte)'^' => SyntaxKind.CircumflexAccentToken,

            // 123 {
            (byte)'{' => SyntaxKind.LeftCurlyBracketToken,

            // 124 |
            (byte)'|' => TryConsume((byte)'|')
                ? SyntaxKind.VerticalLineVerticalLineToken
                : SyntaxKind.VerticalLineToken,

            // 125 }
            (byte)'}' => SyntaxKind.RightCurlyBracketToken,

            // 126 ~
            (byte)'~' => SyntaxKind.TildeToken,

            _ => ScanIdentifier(current, start)
        };

        return new SyntaxToken(kind, TextSpan.FromBounds(start, _position));
    }

    /// <summary>
    /// Creates a new independent tokenizer at the same byte offset.
    /// </summary>
    public Tokenizer Fork() => new(_source, _position, _filter);

    public IEnumerator<SyntaxToken> GetEnumerator()
    {
        var cursor = Fork();

        while (true)
        {
            var token = cursor.Read();
            yield return token;

            if (token.Kind == SyntaxKind.EndOfFileToken)
                yield break;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private byte Current => _source[_position];

    private bool HasByte(int offset = 0) => _position + offset < _source.Length;

    private void AdvanceChars(int count)
    {
        if (_position + count > _source.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        _position += count;
    }

    private bool TryConsume(byte value)
    {
        if (IsAtEnd || Current != value)
            return false;

        AdvanceChars(1);
        return true;
    }

    private static bool IsFiltered(TokenFilter filter, SyntaxKind kind)
    {
        var flag = kind switch
        {
            SyntaxKind.SpacingToken => TokenFilter.Spacing,
            SyntaxKind.EndOfLineToken => TokenFilter.EndOfLine,
            SyntaxKind.CommentToken => TokenFilter.Comment,
            _ => TokenFilter.None,
        };

        // NOTE(alex): You cannot use Filter.HasFlag(...) here by itself, because it checks that
        // (Filter & filter) == filter, which for the case of TokenFilter.None (zero) will return true.
        return flag is not TokenFilter.None && filter.HasFlag(flag);
    }

    private SyntaxKind ScanSpacing()
    {
        while (!IsAtEnd && Current is (byte)'\t' or (byte)'\v' or (byte)'\f' or (byte)' ')
        {
            AdvanceChars(1);
        }

        return SyntaxKind.SpacingToken;
    }

    private SyntaxKind ScanEndOfLine(byte first)
    {
        // NOTE(alex): The first byte was consumed already by Read().
        if (first == (byte)'\r' && !IsAtEnd && Current == (byte)'\n')
        {
            AdvanceChars(1);
        }

        return SyntaxKind.EndOfLineToken;
    }

    private SyntaxKind ScanString()
    {
        while (!IsAtEnd && Current != (byte)'"')
        {
            if (Current == (byte)'\\' && HasByte(1))
                AdvanceChars(1);

            AdvanceRune();
        }

        if (Current == (byte)'"')
        {
            AdvanceChars(1);
        }

        return SyntaxKind.StringLiteralToken;
    }

    private SyntaxKind ScanIdentifier(byte first, int start)
    {
        if (Char.IsDigit((char)first))
        {
            while (!IsAtEnd && (Char.IsDigit((char)Current) || Current == (byte)'_'))
                AdvanceChars(1);

            return SyntaxKind.IntegerLiteralToken;
        }

        // NOTE(alex): We consumed the first byte before entering the switch,
        // so skip the rest of the rune so a bad Unicode character is one bad token.
        var rune = FinishRune(first, start);

        if (IsIdentifierStart(rune))
        {
            while (!IsAtEnd && IsIdentifierContinuationAt(_position))
                AdvanceRune();

            return SyntaxKind.IdentifierToken;
        }

        return SyntaxKind.BadToken;
    }

    private void AdvanceRune()
    {
        if (Current < 0x80)
        {
            AdvanceChars(1);
            return;
        }

        _ = DecodeRune(_position, out var width);
        AdvanceChars(width);
    }

    private Rune DecodeRune(int position, out int width)
    {
        var status = Rune.DecodeFromUtf8(_source.Bytes[position..], out var rune, out width);

        // NOTE(alex): SourceText validates UTF-8 on construction, so the tokenizer
        // should never encounter malformed input here.
        if (status != OperationStatus.Done)
            throw new InvalidOperationException("SourceText contained invalid UTF-8 (or decoding failed).");

        return rune;
    }

    private Rune FinishRune(byte first, int start)
    {
        if (first < 0x80)
            return new Rune(first);

        var rune = DecodeRune(start, out var width);

        // NOTE(alex): Read() already consumed the first byte.
        _position = start + width;

        return rune;
    }

    private bool IsIdentifierContinuationAt(int position)
    {
        var value = _source[position];

        if (value < 0x80)
        {
            return value is (byte)'_' ||
                   value is >= (byte)'A' and <= (byte)'Z' ||
                   value is >= (byte)'a' and <= (byte)'z' ||
                   value is >= (byte)'0' and <= (byte)'9';
        }

        var rune = DecodeRune(position, out _);

        return IsIdentifierContinuation(rune);
    }

    private static readonly HashSet<UnicodeCategory> _identifierStarts = [UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.ModifierLetter, UnicodeCategory.OtherLetter, UnicodeCategory.LetterNumber];
    private static readonly HashSet<UnicodeCategory> _identifierConnectors = [UnicodeCategory.DecimalDigitNumber, UnicodeCategory.NonSpacingMark, UnicodeCategory.SpacingCombiningMark, UnicodeCategory.ConnectorPunctuation];
    private static readonly HashSet<UnicodeCategory> _identifierContinuations = new HashSet<UnicodeCategory>(_identifierStarts.Union(_identifierConnectors));

    private static bool IsIdentifierStart(Rune rune)
    {
        if (rune.Value == '_')
            return true;

        return _identifierStarts.Contains(Rune.GetUnicodeCategory(rune));
    }

    private static bool IsIdentifierContinuation(Rune rune)
    {
        return _identifierContinuations.Contains(Rune.GetUnicodeCategory(rune));
    }
}
