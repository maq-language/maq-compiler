namespace Maq.IR;

/// <summary>
/// Classification of a <see cref="Node"/>.
/// </summary>
public enum NodeKind : ushort
{
    Start,
    End,

    Region,
    Phi,
    Projection,

    Parameter, // TODO(alex): Is this different from Projection?
    Constant,

    Add,
    Subtract,
    Multiply,
    Divide,

    Equal,
    LessThan,

    If,

    Load,
    Store,

    Call,
    Return,
}
