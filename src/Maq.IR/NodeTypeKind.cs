namespace Maq.IR;

public enum NodeTypeKind : ushort
{
    Top, // NOTE(alex): Top of the type lattice - represents ANY type
    Integer,
    Bottom, // NOTE(alex): Bottom of the type lattice - represents ALL types
}
