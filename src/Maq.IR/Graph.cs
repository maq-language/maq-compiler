using System.Runtime.InteropServices;

namespace Maq.IR;

public class Graph
{
    private readonly List<Node> _nodes = []; // NOTE(alex): Nodes of the graph data structure.
    private readonly List<NodeId> _inputs = []; // NOTE(alex): Edges of the graph data structure.

    public int Count => _nodes.Count;

    public Node this[NodeId id] => _nodes[id.Value];

    /// <summary>
    /// Get all nodes that are inputs to the node <paramref name="id"/>.
    /// </summary>
    public ReadOnlySpan<NodeId> GetInputs(NodeId id)
    {
        var node = _nodes[id.Value];
        return CollectionsMarshal.AsSpan(_inputs).Slice(node.InputOffset, node.InputCount);
    }

    /// <summary>
    /// Add a node to the graph, adding edges for each node id in <paramref name="inputs"/>.
    /// </summary>
    public NodeId Add(NodeKind kind, NodeType type, params ReadOnlySpan<NodeId> inputs)
    {
        var inputOffset = _inputs.Count;
        _inputs.AddRange(inputs);

        var id = new NodeId(_nodes.Count);
        _nodes.Add(new Node(kind, type, inputOffset, checked((ushort)inputs.Length)));
        return id;
    }
}
