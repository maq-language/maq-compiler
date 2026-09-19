namespace Maq.IR;

/// <summary>
/// A single node in a <see cref="Graph"/>.
/// </summary>
public readonly struct Node
{
    internal Node(NodeKind kind, NodeType type, int inputOffset, ushort inputCount)
    {
        Kind = kind;
        Type = type;
        InputOffset = inputOffset;
        InputCount = inputCount;
    }

    public NodeKind Kind { get; }

    public NodeType Type { get; }

    internal int InputOffset { get; }

    internal ushort InputCount { get; }
}
