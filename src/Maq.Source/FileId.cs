namespace Maq.Source;

/// <summary>
/// A compact, process-local identity for a source file.
/// Value 0 is reserved for an invalid/default identifier.
/// </summary>
public readonly record struct FileId
{
    public FileId(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "File ids must be positive.");

        Value = value;
    }

    public int Value { get; }

    public bool IsValid => Value > 0;

    public override string ToString() => $"file:{Value}";
}
