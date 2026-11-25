namespace Kite.Lexing;

#pragma warning disable RCS1194 // Лексическая ошибка всегда должна содержать координаты.
public sealed class LexerException(string message, int line, int column)
    : Exception($"{message} (строка {line}, столбец {column}).")
{
    public int Line { get; } = line;

    public int Column { get; } = column;
}
#pragma warning restore RCS1194