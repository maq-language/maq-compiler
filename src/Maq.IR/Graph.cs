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

    /// <summary>
    /// Add a node to the graph, adding edges for each node id in <paramref name="inputs"/>.
    /// </summary>
    public NodeId AddOrImprove(NodeKind kind, params ReadOnlySpan<NodeId> inputs)
    {
	// TODO(alex): We need to remove all inputs that are no longer referenced if we constant fold this node.

	switch (kind)
	{
	    case NodeKind.Add:
		var in0 = this[inputs[0]];
		var in1 = this[inputs[1]];
		if (in0.Type.Kind == NodeTypeKind.Integer &&
		    in1.Type.Kind == NodeTypeKind.Integer &&
		    in0.Type.Flags.HasFlag(NodeTypeFlags.IsConstant) &&
		    in1.Type.Flags.HasFlag(NodeTypeFlags.IsConstant))
		{
		    return Add(NodeKind.Constant, NodeType.Integer(in0.Type.Value + in1.Type.Value));
		}
		break;
	}

	return Add(kind, NodeType.Bottom, inputs);
    }

    public IEnumerable<NodeId> VisitPostOrder(NodeId root)
    {
        var visited = new HashSet<NodeId> { root };
        var stack = new Stack<(NodeId Id, int NextInput)>();
        var current = (Id: root, NextInput: 0);

        do
        {
            var node = _nodes[current.Id.Value];

            while (current.NextInput < node.InputCount)
            {
                var input = _inputs[node.InputOffset + current.NextInput++];
                if (visited.Add(input))
                {
                    stack.Push(current);
                    current = (input, 0);
                    node = _nodes[input.Value];
                }
            }

            // NOTE(alex): All inputs of this node have already been visited, so visit the node.
            yield return current.Id;
        }
        while (stack.TryPop(out current));
    }
}
