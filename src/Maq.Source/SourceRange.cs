namespace Maq.Source;

/// <summary>
/// A range in one source file, for mapping compiler output to user input.
/// </summary>
public readonly record struct SourceRange
{
    public SourceRange(FileId file, TextSpan span)
    {
        if (!file.IsValid)
            throw new ArgumentException("A source location requires a valid file.", nameof(file));

        File = file;
        Span = span;
    }

    public FileId File { get; }
    public TextSpan Span { get; }

    public SourceLocation Start => new(File, Span.Start);
    public SourceLocation End => new(File, Span.End);

    public bool Contains(SourceLocation location) => location.File == File && Span.Contains(location.Offset);

    public override string ToString() => $"{File}{Span}";
}
