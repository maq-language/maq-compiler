namespace Maq.Source;

/// <summary>
/// A line in a <see cref="SourceText"/>. The spans use UTF-8 byte offsets.
/// </summary>
public readonly record struct SourceLine
{
    internal SourceLine(int index, TextSpan span, TextSpan spanIncludingLineBreak)
    {
        Index = index;
        Span = span;
        SpanIncludingLineBreak = spanIncludingLineBreak;
    }

    public int Index { get; }
    public TextSpan Span { get; }
    public TextSpan SpanIncludingLineBreak { get; }
}
