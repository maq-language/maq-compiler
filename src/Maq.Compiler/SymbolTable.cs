using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Maq.Compiler;

public class SymbolTable
{
    private readonly ConcurrentDictionary<string, Symbol> _symbols = new();

    public Symbol GetOrCreate(string name)
    {
        return _symbols.GetOrAdd(name, static name => new Symbol(name));
    }

    public bool TryGet(string name, [NotNullWhen(true)] out Symbol? symbol) => _symbols.TryGetValue(name, out symbol);
}
