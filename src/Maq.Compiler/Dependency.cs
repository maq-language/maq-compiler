namespace Maq.Compiler;

public readonly record struct Dependency
{
    public Dependency(Symbol symbol, SymbolFacts required)
    {
        Symbol = symbol;
        RequiredFacts = required;
    }

    public Symbol Symbol { get; }

    public SymbolFacts RequiredFacts { get; }

    public bool IsSatisfied => Symbol.HasFacts(RequiredFacts);
}
