namespace Maq.IR;

/// <summary>
/// Unique identifier for a <see cref="Node"/>.
/// </summary>
public readonly record struct NodeId
{
    public NodeId(int value)
    {
        Value = value;
    }

    internal int Value { get; }

    public static NodeId Invalid => new(-1);

    public bool IsValid => Value >= 0;
}
