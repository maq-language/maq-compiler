using Maq.Compiler;
using Maq.IR;
using Maq.Source;
using Maq.Syntax;

namespace Maq.Compiler.Tests;

[TestClass]
public sealed class NodeParserTests
{
    [TestMethod]
    public void TestParseReturnStatement()
    {
        var source = SourceText.From("Result = 5");

        var parser = new NodeParser(source);
        var graph = parser.Parse();

        var id0 = new NodeId(0);
        var id1 = new NodeId(1);
        var id2 = new NodeId(2);

        Assert.AreEqual(3, graph.Count); // NOTE(alex): Start -> Constant -> Return

        Assert.AreEqual(NodeKind.Start, graph[id0].Kind);
        Assert.AreEqual(NodeKind.Constant, graph[id1].Kind);
        Assert.AreEqual(NodeKind.Return, graph[id2].Kind);

        Assert.AreEqual(NodeType.Bottom, graph[id0].Type);
        Assert.AreEqual(NodeType.Integer(5), graph[id1].Type); // NOTE(alex): The type of the expression is the integer "5" - it is a compile-time known constant.
        Assert.AreEqual(NodeType.Bottom, graph[id2].Type);

        var inputs0 = graph.GetInputs(id0);
        Assert.AreEqual(0, inputs0.Length); // NOTE(alex): Start depends on nothing

        var inputs1 = graph.GetInputs(id1);
        Assert.AreEqual(1, inputs1.Length); // NOTE(alex): Constant depends on Start (for now)
        Assert.AreEqual(id0, inputs1[0]);

        var inputs2 = graph.GetInputs(id2);
        Assert.AreEqual(2, inputs2.Length); // NOTE(alex): Return depends on Constant and Start
        Assert.AreEqual(id0, inputs2[0]);
        Assert.AreEqual(id1, inputs2[1]);
    }
}
