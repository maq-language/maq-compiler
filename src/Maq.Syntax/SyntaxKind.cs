namespace Maq.Syntax;

/// <summary>
/// Lexical token kinds.
/// </summary>
public enum SyntaxKind : ushort
{
    BadToken = 0,
    EndOfFileToken,

    EndOfLineToken,
    SpacingToken,
    CommentToken,

    IdentifierToken,
    IntegerLiteralToken,
    StringLiteralToken,

    ExclamationMarkToken,        // !
    ExclamationMarkEqualsToken,  // !=

    DollarSignToken,             // $
    PercentSignToken,            // %

    AmpersandToken,              // &
    AmpersandAmpersandToken,     // &&

    LeftParenthesisToken,        // (
    RightParenthesisToken,       // )

    AsteriskToken,               // *
    PlusSignToken,               // +
    CommaToken,                  // ,
    HyphenMinusToken,            // -
    HyphenMinusGreaterThanToken, // ->
    FullStopToken,               // .
    SolidusToken,                // /

    ColonToken,                  // :
    ColonColonToken,             // ::
    SemicolonToken,              // ;

    LessThanSignToken,           // <
    LessThanEqualsToken,         // <=
    LessThanLessThanToken,       // <<

    EqualsSignToken,             // =
    EqualsEqualsToken,           // ==
    EqualsGreaterThanToken,      // =>

    GreaterThanSignToken,        // >
    GreaterThanEqualsToken,      // >=
    GreaterThanGreaterThanToken, // >>

    QuestionMarkToken,           // ?

    LeftSquareBracketToken,      // [
    ReverseSolidusToken,         // \
    RightSquareBracketToken,     // ]

    CircumflexAccentToken,       // ^

    LeftCurlyBracketToken,       // {
    VerticalLineToken,           // |
    VerticalLineVerticalLineToken, // ||
    RightCurlyBracketToken,      // }

    TildeToken,                  // ~
}
