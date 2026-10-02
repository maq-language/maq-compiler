namespace Maq.IR;

[Flags]
public enum NodeTypeFlags : ushort
{
    None = 0,
    IsConstant = 1<<0,
}
