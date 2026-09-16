namespace Maq.Syntax;

[Flags]
public enum TokenFilter
{
    None = 0,

    Spacing = 1 << 0,
    EndOfLine = 1 << 1,
    Comment = 1 << 2,

    Trivia = Spacing | EndOfLine | Comment,
}
