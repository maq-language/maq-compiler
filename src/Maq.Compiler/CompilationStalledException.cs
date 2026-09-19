namespace Maq.Compiler;

public sealed class CompilationStalledException : Exception
{
    public CompilationStalledException()
        : base("Compilation cannot make progress because all remaining work is blocked.")
    {
    }
}
