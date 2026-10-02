using Maq.IR;
using Maq.Source;
using Maq.Syntax;

namespace Maq.Compiler;

/// <summary>
/// This is a simple implementation of a parser that returns Sea of Nodes IR.
/// Eventually, this parser will be merged with <see cref="Parser"/>.
/// </summary>
public class NodeParser
{
    private readonly Tokenizer _tokenizer;
    private SyntaxToken _current;
    private SyntaxToken _next;

    public NodeParser(SourceText source)
        : this(new Tokenizer(source))
    {
    }

    public NodeParser(Tokenizer tokenizer)
    {
        _tokenizer = tokenizer;
        _current = ReadToken();
        _next = ReadToken();
    }

    public Graph Parse()
    {
        var graph = new Graph();
        var start = graph.Add(NodeKind.Start, NodeType.Bottom);
        var result = ParseStatement(graph, start);
        return graph;
    }

    private NodeId ParseBinding(Graph graph, NodeId start)
    {
        var name = Expect(SyntaxKind.IdentifierToken);
        var equals = Expect(SyntaxKind.EqualsSignToken);
        var value = ParseExpression(graph, start);

        SyntaxToken? semicolon = null;
        if (Current.Kind == SyntaxKind.SemicolonToken)
        {
            semicolon = Advance();
        }

        if (_tokenizer.Source.GetText(name.Span) != "Result")
        {
            throw new NotImplementedException("Parsing a binding to a name other than 'Result'.");
        }

        return graph.Add(NodeKind.Return, NodeType.Bottom, start, value);
    }

    private NodeId ParseStatement(Graph graph, NodeId start)
    {
        if (Current.Kind == SyntaxKind.IdentifierToken &&
            Next.Kind == SyntaxKind.EqualsSignToken)
        {
            return ParseBinding(graph, start);
        }

        throw new NotImplementedException("Parsing any kind of statement other than a binding.");
    }

    private NodeId ParseExpression(Graph graph, NodeId start)
    {
        var expression = ParsePrimaryExpression(graph, start);
        return expression;
    }

    private NodeId ParsePrimaryExpression(Graph graph, NodeId start)
    {
        switch (Current.Kind)
        {
            case SyntaxKind.IntegerLiteralToken:
		var token = Advance();
		var text = _tokenizer.Source.GetText(token.Span);
		if (!ulong.TryParse(text, out var value))
		{
		    throw new SyntaxException($"Unable to parse integer {text}.");
		}

                return graph.Add(NodeKind.Constant, NodeType.Integer(unchecked((long)value)), start);

            default:
                throw new NotImplementedException("Parsing a primary expression other than an integer literal.");
        }
    }

    //
    // NOTE(alex): Everything below here is copied from the Parser class.
    //

    private SyntaxToken Current => _current;

    private SyntaxToken Next => _next;

    private SyntaxToken Advance()
    {
        var token = _current;
        _current = _next;
        _next = _tokenizer.Read();
        return token;
    }

    private SyntaxToken Expect(SyntaxKind expected)
    {
        if (Current.Kind != expected)
        {
            throw new SyntaxException($"Expected {expected}, found {Current.Kind}.");
        }

        return Advance();
    }

    private SyntaxToken ReadToken() => _tokenizer.Read();
}
