using Maq.Source;

namespace Maq.Syntax;

public readonly record struct SyntaxToken
{
    public SyntaxToken(SyntaxKind kind, TextSpan span)
    {
        Kind = kind;
        Span = span;
    }

    public SyntaxKind Kind { get; }

    public TextSpan Span { get; }

    public override string ToString() => $"{Kind} {Span}";
}
