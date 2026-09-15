namespace Maq.Source;

/// <summary>
/// A position in a source file, expressed as a UTF-8 byte offset.
/// </summary>
public readonly record struct SourceLocation
{
    public SourceLocation(FileId file, int offset)
    {
        if (!file.IsValid)
            throw new ArgumentException("A source location requires a valid file.", nameof(file));
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        File = file;
        Offset = offset;
    }

    public FileId File { get; }
    public int Offset { get; }

    public override string ToString() => $"{File}+{Offset}";
}
