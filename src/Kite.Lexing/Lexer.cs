using System.Globalization;
using System.Text;

namespace Kite.Lexing;

public sealed class Lexer
{
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.Ordinal)
    {
        ["func"] = TokenType.Func,
        ["struct"] = TokenType.Struct,
        ["var"] = TokenType.Var,
        ["int"] = TokenType.Int,
        ["float"] = TokenType.Float,
        ["bool"] = TokenType.Bool,
        ["string"] = TokenType.String,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["while"] = TokenType.While,
        ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue,
        ["return"] = TokenType.Return,
        ["new"] = TokenType.New,
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
    };

    private static readonly UTF8Encoding SourceEncoding = new(false, true);

    private readonly string _source;
    private int _position;
    private int _line = 1;
    private int _column = 1;

    public Lexer(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public static Lexer FromFile(string filePath)
    {
        ReadOnlySpan<byte> source = File.ReadAllBytes(filePath);
        ReadOnlySpan<byte> preamble = Encoding.UTF8.Preamble;
        if (source.StartsWith(preamble))
        {
            source = source[preamble.Length..];
        }

        return new Lexer(SourceEncoding.GetString(source));
    }

    public List<Token> Tokenize()
    {
        List<Token> tokens = [];
        while (!IsAtEnd())
        {
            SkipWhitespaceAndComments();
            if (IsAtEnd())
            {
                break;
            }

            tokens.Add(ReadNextToken());
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", _position, _line, _column));
        return tokens;
    }

    private bool IsAtEnd() => _position == _source.Length;

    private char Peek(int offset = 0)
    {
        int index = _position + offset;
        return index < _source.Length ? _source[index] : '\0';
    }

    private char Advance()
    {
        char current = _source[_position++];
        if (current == '\n')
        {
            ++_line;
            _column = 1;
        }
        else
        {
            ++_column;
        }

        return current;
    }

    private bool Match(char expected)
    {
        if (Peek() != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            switch (Peek())
            {
                case ' ' or '\t' or '\r' or '\n':
                    Advance();
                    break;
                case '/' when Peek(1) == '/':
                    SkipLineComment();
                    break;
                case '/' when Peek(1) == '*':
                    SkipBlockComment();
                    break;
                default:
                    return;
            }
        }
    }

    private void SkipLineComment()
    {
        Advance();
        Advance();
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
    }

    private void SkipBlockComment()
    {
        int startLine = _line;
        int startColumn = _column;
        Advance();
        Advance();

        while (!IsAtEnd())
        {
            if (Advance() == '*' && Match('/'))
            {
                return;
            }
        }

        throw new LexerException("Незакрытый многострочный комментарий", startLine, startColumn);
    }

    private Token ReadNextToken()
    {
        int startPosition = _position;
        int startLine = _line;
        int startColumn = _column;
        char current = Peek();
        TokenType type;
        string value;

        if (char.IsAsciiDigit(current))
        {
            type = ReadNumber(startPosition, startLine, startColumn);
            value = _source[startPosition.._position];
        }
        else if (IsIdentifierStart(current))
        {
            value = ReadIdentifier();
            type = Keywords.GetValueOrDefault(value, TokenType.Identifier);
        }
        else if (current == '"')
        {
            type = TokenType.StringLiteral;
            value = ReadString(startLine, startColumn);
        }
        else
        {
            type = ReadOperatorOrDelimiter(startLine, startColumn);
            value = _source[startPosition.._position];
        }

        return new Token(type, value, startPosition, startLine, startColumn);
    }

    private TokenType ReadNumber(int startPosition, int startLine, int startColumn)
    {
        SkipDigits();
        bool isFloat = Peek() == '.' && char.IsAsciiDigit(Peek(1));
        if (isFloat)
        {
            Advance();
            SkipDigits();
        }

        if (IsIdentifierStart(Peek()))
        {
            throw new LexerException("Буква или подчёркивание сразу после числа", _line, _column);
        }

        if (isFloat)
        {
            return TokenType.FloatLiteral;
        }

        ReadOnlySpan<char> digits = _source.AsSpan(startPosition, _position - startPosition);
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            throw new LexerException("Целый литерал превышает диапазон int", startLine, startColumn);
        }

        return TokenType.IntegerLiteral;
    }

    private void SkipDigits()
    {
        while (char.IsAsciiDigit(Peek()))
        {
            Advance();
        }
    }

    private static bool IsIdentifierStart(char character) => char.IsAsciiLetter(character) || character == '_';

    private string ReadIdentifier()
    {
        int startPosition = _position;
        do
        {
            Advance();
        }
        while (IsIdentifierStart(Peek()) || char.IsAsciiDigit(Peek()));

        return _source[startPosition.._position];
    }

    private string ReadString(int startLine, int startColumn)
    {
        Advance();
        StringBuilder value = new();
        while (!IsAtEnd() && Peek() is not ('\r' or '\n'))
        {
            char current = Advance();
            if (current == '"')
            {
                return value.ToString();
            }

            value.Append(current == '\\' ? ReadEscape(startLine, startColumn) : current);
        }

        throw new LexerException("Незакрытый строковый литерал", startLine, startColumn);
    }

    private char ReadEscape(int stringLine, int stringColumn)
    {
        if (IsAtEnd() || Peek() is '\r' or '\n')
        {
            throw new LexerException("Незакрытый строковый литерал", stringLine, stringColumn);
        }

        int escapeColumn = _column - 1;
        char escaped = Advance();
        return escaped switch
        {
            '"' => '"',
            '\\' => '\\',
            'n' => '\n',
            't' => '\t',
            _ => throw new LexerException($"Неизвестная управляющая последовательность \\{escaped}", _line, escapeColumn),
        };
    }

    private TokenType ReadOperatorOrDelimiter(int startLine, int startColumn)
    {
        char current = Advance();
        return current switch
        {
            '+' => TokenType.Plus,
            '-' => TokenType.Minus,
            '*' => TokenType.Multiply,
            '/' => TokenType.Divide,
            '%' => TokenType.Modulo,
            '=' => Match('=') ? TokenType.Equal : TokenType.Assign,
            '!' => Match('=') ? TokenType.NotEqual : TokenType.Not,
            '<' => Match('=') ? TokenType.LessEqual : TokenType.Less,
            '>' => Match('=') ? TokenType.GreaterEqual : TokenType.Greater,
            '&' when Match('&') => TokenType.And,
            '|' when Match('|') => TokenType.Or,
            '(' => TokenType.LeftParen,
            ')' => TokenType.RightParen,
            '{' => TokenType.LeftBrace,
            '}' => TokenType.RightBrace,
            '[' => TokenType.LeftBracket,
            ']' => TokenType.RightBracket,
            ',' => TokenType.Comma,
            ';' => TokenType.Semicolon,
            ':' => TokenType.Colon,
            '.' => TokenType.Dot,
            _ => throw new LexerException($"Недопустимый символ '{current}'", startLine, startColumn),
        };
    }
}