namespace Kite.Lexing;

public sealed class Token(TokenType type, string value, int position, int line, int column)
{
    public TokenType Type { get; } = type;

    public string Value { get; } = value;

    public int Position { get; } = position;

    public int Line { get; } = line;

    public int Column { get; } = column;

    public override string ToString() => $"Token({Type}, \"{Value}\", {Line}:{Column})";
}