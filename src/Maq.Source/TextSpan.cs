namespace Maq.Source;

/// <summary>
/// A half-open range [Start, End) in UTF-8 byte offsets.
/// </summary>
public readonly record struct TextSpan
{
    public TextSpan(int start, int length)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (start > int.MaxValue - length)
            throw new ArgumentOutOfRangeException(nameof(length), "Span end exceeds Int32.MaxValue.");

        Start = start;
        Length = length;
    }

    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;
    public bool IsEmpty => Length == 0;

    public static TextSpan FromBounds(int start, int end)
    {
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), "End must be greater than or equal to start.");

        return new TextSpan(start, end - start);
    }

    public bool Contains(int offset) => offset >= Start && offset < End;

    public bool Contains(TextSpan other) => Start >= other.Start && other.End <= End;

    public bool Overlaps(TextSpan other) => Start < other.End && other.Start < End;

    public TextSpan? Intersection(TextSpan other)
    {
        var start = Math.Max(Start, other.Start);
        var end = Math.Min(End, other.End);
        return start < end ? FromBounds(start, end) : null;
    }

    public override string ToString() => $"[{Start}..{End})";
}

