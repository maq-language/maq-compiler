using Maq.Compiler;
using Maq.Source;
using Maq.Syntax;

namespace Maq.Compiler.Tests;

[TestClass]
public sealed class ParserTests
{
    [TestMethod]
    public void TestParseRecordBinding()
    {
        var source = SourceText.From("Person = \\(Name string)");

        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();

        Assert.AreEqual(1, unit.Count);

        var binding = unit[0];
        Assert.AreEqual(SyntaxKind.IdentifierToken, binding.Kind);
        Assert.AreEqual("Person", source.GetText(binding.Span));
    }

    [TestMethod]
    public void TestParseProcedureBinding()
    {
        var source = SourceText.From(
        """
        Serialize = (P Person) ->
        {
            Result = NewString(P.Name);
        }
        """);

        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();

        Assert.AreEqual(1, unit.Count);

        var binding = unit[0];
        Assert.AreEqual(SyntaxKind.IdentifierToken, binding.Kind);
        Assert.AreEqual("Serialize", source.GetText(binding.Span));

        // TODO(alex): Once we are returning a real tree, test the rest of the properties.
    }

    [TestMethod]
    public void TestParseMacroProcedureAndCallerIdentifier()
    {
        var source = SourceText.From(
        """
        Increment = $(Value u32) ->
        {
            $Value;
        }
        """);

        var parser = new Parser(source);
        var unit = parser.ParseCompilationUnit();

        Assert.AreEqual(1, unit.Count);

        var binding = unit[0];
        Assert.AreEqual(SyntaxKind.IdentifierToken, binding.Kind);
        Assert.AreEqual("Increment", source.GetText(binding.Span));

        // TODO(alex): Once we are returning a real tree, test the rest of the properties.
    }
}
