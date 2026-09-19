namespace Maq.Compiler;

public sealed class SyntaxException : Exception
{
    // TODO(alex): Include a SourceLocation here for proper diagnostics reporting.
    public SyntaxException(string message)
        : base(message)
    {
    }
}
