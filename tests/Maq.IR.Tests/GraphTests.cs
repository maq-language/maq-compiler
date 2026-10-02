using Maq.IR;

namespace Maq.IR.Tests;

[TestClass]
public sealed class GraphTests
{
    [TestMethod]
    public void TestAddReturnsSequentialNodes()
    {
        var graph = new Graph();
        var start = graph.Add(NodeKind.Start, NodeType.Bottom);
        var constant = graph.Add(NodeKind.Constant, NodeType.Integer(5));
        Assert.AreEqual(new NodeId(0), start);
        Assert.AreEqual(new NodeId(1), constant);
        Assert.AreEqual(2, graph.Count);
    }

    [TestMethod]
    public void TestIndexOperatorReturnsAddedNode()
    {
        var graph = new Graph();
        var id = graph.Add(NodeKind.Constant, NodeType.Integer(5));
        var node = graph[id];
        Assert.AreEqual(NodeKind.Constant, node.Kind);
        Assert.AreEqual(NodeType.Integer(5), node.Type);
    }

    [TestMethod]
    public void TestInputsAreReturnedInOrder()
    {
        var graph = new Graph();
        var left = graph.Add(NodeKind.Constant, NodeType.Integer(6));
        var right = graph.Add(NodeKind.Constant, NodeType.Integer(7));
        var add = graph.Add(NodeKind.Add, NodeType.Integer(13), left, right);
        var inputs = graph.GetInputs(add);
        Assert.AreEqual(2, inputs.Length);
        Assert.AreEqual(left, inputs[0]);
        Assert.AreEqual(right, inputs[1]);
    }

    [TestMethod]
    public void TestInputsEmptyForNodeWithoutInputs()
    {
        var graph = new Graph();
        var start = graph.Add(NodeKind.Start, NodeType.Bottom);
        Assert.AreEqual(0, graph.GetInputs(start).Length);
    }
}
