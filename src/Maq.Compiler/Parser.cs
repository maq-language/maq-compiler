using Maq.Source;
using Maq.Syntax;

namespace Maq.Compiler;

public class Parser
{
    private readonly Tokenizer _tokenizer;
    private SyntaxToken _current;
    private SyntaxToken _next;

    public Parser(SourceText source)
        : this(new Tokenizer(source))
    {
    }

    public Parser(Tokenizer tokenizer)
    {
        _tokenizer = tokenizer;
        _current = ReadToken();
        _next = ReadToken();
    }

    /// <summary>
    /// Parses <c>CompilationUnit ::= Binding* EndOfFile</c>.
    /// </summary>
    public List<SyntaxToken> ParseCompilationUnit()
    {
        var bindings = new List<SyntaxToken>();

        while (Current.Kind != SyntaxKind.EndOfFileToken)
        {
            bindings.Add(ParseBinding());
        }

        return bindings;
    }

    /// <summary>
    /// Parses <c>Bindng ::= Identifier '=' Expression ';'?</c>.
    /// </summary>
    private SyntaxToken ParseBinding()
    {
        var name = Expect(SyntaxKind.IdentifierToken);
        var equals = Expect(SyntaxKind.EqualsSignToken);
        var value = ParseExpression();

        SyntaxToken? semicolon = null;
        if (Current.Kind == SyntaxKind.SemicolonToken)
        {
            semicolon = Advance();
        }

        return name;
    }

    /// <summary>
    /// Parses <c>Statement ::= Binding | Expression ';'?</c>.
    /// </summary>
    private SyntaxToken ParseStatement()
    {
        if (Current.Kind == SyntaxKind.IdentifierToken &&
            Next.Kind == SyntaxKind.EqualsSignToken)
        {
            return ParseBinding();
        }

        var expression = ParseExpression();

        SyntaxToken? semicolon = null;
        if (Current.Kind == SyntaxKind.SemicolonToken)
        {
            semicolon = Advance();
        }

        return expression;
    }

    /// <summary>
    /// Parses <c>Expression ::= Primary (CallSuffix | MemberAccessSuffix)*</c>.
    /// </summary>
    private SyntaxToken ParseExpression()
    {
        var expression = ParsePrimaryExpression();

        while (true)
        {
            switch (Current.Kind)
            {
                case SyntaxKind.LeftParenthesisToken:
                    expression = ParseCallExpression(expression);
                    break;

                case SyntaxKind.FullStopToken:
                    expression = ParseMemberAccessExpression(expression);
                    break;

                default:
                    return expression;
            }
        }
    }

    /// <summary>
    /// Parses <c>Primary ::= Identifier | Integer | String | Procedure | MacroProcedure | RecordType | CallerIdentifier</c>.
    /// </summary>
    private SyntaxToken ParsePrimaryExpression()
    {
        switch (Current.Kind)
        {
            case SyntaxKind.IdentifierToken:
                return Advance();

            case SyntaxKind.IntegerLiteralToken:
                return Advance();

            case SyntaxKind.StringLiteralToken:
                return Advance();

            case SyntaxKind.LeftParenthesisToken:
                return ParseProcedureExpression();

            case SyntaxKind.DollarSignToken:
                return ParseDollarExpression();

            case SyntaxKind.ReverseSolidusToken:
                return ParseRecordTypeExpression();

            default:
                throw new SyntaxException("Expected one of {Identifier, Integer, String, Procedure, MacroProcedure, RecordType, CallerIdentifier}");
        }
    }

    /// <summary>
    /// Parses <c>DollarExpression ::= '$' Identifier | '$' Procedure</c>.
    /// </summary>
    private SyntaxToken ParseDollarExpression()
    {
        var dollar = Expect(SyntaxKind.DollarSignToken);

        if (Current.Kind == SyntaxKind.LeftParenthesisToken)
        {
            return ParseProcedureExpression(dollar);
        }

        var identifier = Expect(SyntaxKind.IdentifierToken);

        return identifier;
    }

    /// <summary>
    /// Parses <c>Procedure ::= '(' Parameters? ')' '-&gt;' Block</c>, optionally preceded by <c>'$'</c> for a macro procedure.
    /// </summary>
    private SyntaxToken ParseProcedureExpression(SyntaxToken? dollar = null)
    {
        var leftParenthesis = Expect(SyntaxKind.LeftParenthesisToken);
        var parameters = ParseParameters();
        var rightParenthesis = Expect(SyntaxKind.RightParenthesisToken);

        var arrow = Expect(SyntaxKind.HyphenMinusGreaterThanToken);

        var body = ParseBlock();

        return leftParenthesis;
    }

    /// <summary>
    /// Parses <c>RecordType ::= '\' '(' Parameters? ')'</c>.
    /// </summary>
    private SyntaxToken ParseRecordTypeExpression()
    {
        var reverseSolidus = Expect(SyntaxKind.ReverseSolidusToken);
        var leftParenthesis = Expect(SyntaxKind.LeftParenthesisToken);
        var fields = ParseParameters();
        var rightParenthesis = Expect(SyntaxKind.RightParenthesisToken);
        return reverseSolidus;
    }

    /// <summary>
    /// Parses <c>Parameters ::= Parameter (',' Parameter)*</c>.
    /// </summary>
    private IReadOnlyList<SyntaxToken> ParseParameters()
    {
        var parameters = new List<SyntaxToken>();

        while (Current.Kind != SyntaxKind.RightParenthesisToken)
        {
            parameters.Add(ParseParameter());

            if (Current.Kind == SyntaxKind.CommaToken)
            {
                Advance();
            }
            else
            {
                break;
            }
        }

        return parameters;
    }

    /// <summary>
    /// Parses <c>Parameter ::= Identifier Identifier</c>.
    /// </summary>
    private SyntaxToken ParseParameter()
    {
        var name = Expect(SyntaxKind.IdentifierToken);
        var type = Expect(SyntaxKind.IdentifierToken);

        return name;
    }

    /// <summary>
    /// Parses <c>Block ::= '{' Statement* '}'</c>.
    /// </summary>
    private IReadOnlyList<SyntaxToken> ParseBlock()
    {
        var leftCurlyBracket = Expect(SyntaxKind.LeftCurlyBracketToken);
        var statements = new List<SyntaxToken>();

        while (Current.Kind != SyntaxKind.RightCurlyBracketToken &&
               Current.Kind != SyntaxKind.EndOfFileToken)
        {
            statements.Add(ParseStatement());
        }

        var rightCurlyBracket = Expect(SyntaxKind.RightCurlyBracketToken);

        return statements;
    }

    /// <summary>
    /// Parses <c>CallSuffix ::= '(' Arguments? ')'</c>.
    /// </summary>
    private SyntaxToken ParseCallExpression(SyntaxToken expression)
    {
        var leftParenthesis = Expect(SyntaxKind.LeftParenthesisToken);

        var arguments = new List<SyntaxToken>();

        while (Current.Kind != SyntaxKind.RightParenthesisToken)
        {
            arguments.Add(ParseExpression());

            if (Current.Kind == SyntaxKind.CommaToken)
            {
                Advance();
            }
            else
            {
                break;
            }
        }

        var rightParenthesis = Expect(SyntaxKind.RightParenthesisToken);

        return leftParenthesis;
    }

    /// <summary>
    /// Parses <c>MemberAccessSuffix ::= '.' Identifier</c>.
    /// </summary>
    private SyntaxToken ParseMemberAccessExpression(SyntaxToken expression)
    {
        var fullStop = Expect(SyntaxKind.FullStopToken);
        var name = Expect(SyntaxKind.IdentifierToken);

        return name;
    }

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
