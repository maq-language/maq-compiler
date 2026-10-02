namespace Maq.IR;

public record struct NodeType
{
    public NodeType(NodeTypeKind kind)
	: this(kind, NodeTypeFlags.None)
    {
    }

    public NodeType(NodeTypeKind kind, NodeTypeFlags flags)
    {
	Kind = kind;
	Flags = flags;
    }

    public NodeTypeKind Kind { get; }

    public NodeTypeFlags Flags { get; }

    public long Value { get; init; }

    public static readonly NodeType Bottom = new(NodeTypeKind.Bottom);

    public static NodeType Integer(long value) => new(NodeTypeKind.Integer) { Value = value };
}
