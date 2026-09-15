using System.Text;

namespace Maq.Source;

/// <summary>
/// Immutable, validated UTF-8 source text.
/// All offsets exposed by this class are UTF-8 byte offsets.
/// </summary>
public class SourceText
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly byte[] _utf8;
    private int[]? _lineOffsets;

    private SourceText(byte[] utf8)
    {
        _utf8 = utf8;
    }

    public int Length => _utf8.Length;

    public int LineCount => GetLineOffsets().Length;

    public byte this[int offset] => _utf8[offset];

    public ReadOnlySpan<byte> Bytes => _utf8;

    public static SourceText From(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new SourceText(StrictUtf8.GetBytes(text));
    }

    public static SourceText FromUtf8(ReadOnlySpan<byte> utf8)
    {
        // NOTE(alex): Validate before taking our own immutable copy.
        _ = StrictUtf8.GetCharCount(utf8);
        return new SourceText(utf8.ToArray());
    }

    public ReadOnlySpan<byte> Slice(TextSpan span)
    {
        ValidateSpan(span);
        return _utf8.AsSpan(span.Start, span.Length);
    }

    public string GetText(TextSpan span)
    {
        ValidateSpan(span);
        return StrictUtf8.GetString(_utf8, span.Start, span.Length);
    }

    public SourceLine GetLine(int index)
    {
        var offsets = GetLineOffsets();
        if ((uint)index >= (uint)offsets.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var start = offsets[index];
        var endIncludingLineBreak = index + 1 < offsets.Length ? offsets[index + 1] : Length;
        var end = endIncludingLineBreak;

        if (end > start)
        {
            if (_utf8[end - 1] == (byte)'\n')
            {
                end -= 1;
                if (end > start && _utf8[end - 1] == (byte)'\r')
                    end -= 1;
            }
            else if (_utf8[end - 1] == (byte)'\r')
            {
                end -= 1;
            }
        }

        return new SourceLine(index, TextSpan.FromBounds(start, end), TextSpan.FromBounds(start, endIncludingLineBreak));
    }

    public int GetLineIndex(int offset)
    {
        if ((uint)offset > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        var offsets = GetLineOffsets();
        var index = Array.BinarySearch(offsets, offset);
        if (index >= 0)
            return index;

        return ~index - 1;
    }

    public SourceLine GetLineFromOffset(int offset) => GetLine(GetLineIndex(offset));

    public override string ToString() => StrictUtf8.GetString(_utf8);

    private int[] GetLineOffsets() => LazyInitializer.EnsureInitialized(ref _lineOffsets, FindLineOffsets);

    private int[] FindLineOffsets()
    {
        var offsets = new List<int> { 0 };  // TODO(alex): Use ArrayPool<T>.Shared?

        for (var i = 0; i < _utf8.Length; i += 1)
        {
            switch (_utf8[i])
            {
                case (byte)'\r':
                    if (i + 1 < _utf8.Length && _utf8[i + 1] == (byte)'\n')
                        i += 1;
                    offsets.Add(i + 1);
                    break;

                case (byte)'\n':
                    offsets.Add(i + 1);
                    break;
            }
        }

        return offsets.ToArray();
    }

    private void ValidateSpan(TextSpan span)
    {
        if (span.End > Length)
            throw new ArgumentOutOfRangeException(nameof(span), "Span extends beyond the source text.");
    }
}
