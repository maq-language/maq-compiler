using Maq.IR;

namespace Maq.IR.Tests;

[TestClass]
public sealed class GraphTests
{
    [TestMethod]
    public void TestAddReturnsSequentialNodes()
    {
        var graph = new Graph();
        var start = graph.Add(NodeKind.Start, NodeType.Control);
        var constant = graph.Add(NodeKind.Constant, NodeType.Integer);
        Assert.AreEqual(new NodeId(0), start);
        Assert.AreEqual(new NodeId(1), constant);
        Assert.AreEqual(2, graph.Count);
    }

    [TestMethod]
    public void TestIndexOperatorReturnsAddedNode()
    {
        var graph = new Graph();
        var id = graph.Add(NodeKind.Constant, NodeType.Integer);
        var node = graph[id];
        Assert.AreEqual(NodeKind.Constant, node.Kind);
        Assert.AreEqual(NodeType.Integer, node.Type);
    }

    [TestMethod]
    public void TestInputsAreReturnedInOrder()
    {
        var graph = new Graph();
        var left = graph.Add(NodeKind.Constant, NodeType.Integer);
        var right = graph.Add(NodeKind.Constant, NodeType.Integer);
        var add = graph.Add(NodeKind.Add, NodeType.Integer, left, right);
        var inputs = graph.GetInputs(add);
        Assert.AreEqual(2, inputs.Length);
        Assert.AreEqual(left, inputs[0]);
        Assert.AreEqual(right, inputs[1]);
    }

    [TestMethod]
    public void TestInputsEmptyForNodeWithoutInputs()
    {
        var graph = new Graph();
        var start = graph.Add(NodeKind.Start, NodeType.Control);
        Assert.AreEqual(0, graph.GetInputs(start).Length);
    }
}
