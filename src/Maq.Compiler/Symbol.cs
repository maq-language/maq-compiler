namespace Maq.Compiler;

public class Symbol
{
    private int _facts;

    public Symbol(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public SymbolFacts Facts => (SymbolFacts)Volatile.Read(ref _facts);

    public bool HasFacts(SymbolFacts facts) => (Facts & facts) == facts;

    internal bool Publish(SymbolFacts published)
    {
        var value = (int)published;

        var previous = Interlocked.Or(ref _facts, value);

        // NOTE(alex): Return value tells you if anything changed.
        return (previous & value) != value;
    }

    public override string ToString() => Name;
}
