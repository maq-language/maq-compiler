namespace Maq.Compiler;

[Flags]
public enum SymbolFacts
{
    None           = 0,
    Declared       = 1 << 0,
    SignatureKnown = 1 << 1,
    BodyTyped      = 1 << 2,
    CodeGenerated  = 1 << 3
}
