using System.Text;

using Xunit;

namespace Kite.Lexing.UnitTests;

public class LexerTests
{
    public static TheoryData<string, TokenType> Keywords() => new()
    {
        { "func", TokenType.Func },
        { "struct", TokenType.Struct },
        { "var", TokenType.Var },
        { "int", TokenType.Int },
        { "float", TokenType.Float },
        { "bool", TokenType.Bool },
        { "string", TokenType.String },
        { "if", TokenType.If },
        { "else", TokenType.Else },
        { "while", TokenType.While },
        { "break", TokenType.Break },
        { "continue", TokenType.Continue },
        { "return", TokenType.Return },
        { "new", TokenType.New },
        { "true", TokenType.True },
        { "false", TokenType.False },
    };

    public static TheoryData<string, TokenType> OperatorsAndDelimiters() => new()
    {
        { "+", TokenType.Plus },
        { "-", TokenType.Minus },
        { "*", TokenType.Multiply },
        { "/", TokenType.Divide },
        { "%", TokenType.Modulo },
        { "=", TokenType.Assign },
        { "==", TokenType.Equal },
        { "!=", TokenType.NotEqual },
        { "<", TokenType.Less },
        { ">", TokenType.Greater },
        { "<=", TokenType.LessEqual },
        { ">=", TokenType.GreaterEqual },
        { "&&", TokenType.And },
        { "||", TokenType.Or },
        { "!", TokenType.Not },
        { "(", TokenType.LeftParen },
        { ")", TokenType.RightParen },
        { "{", TokenType.LeftBrace },
        { "}", TokenType.RightBrace },
        { "[", TokenType.LeftBracket },
        { "]", TokenType.RightBracket },
        { ",", TokenType.Comma },
        { ";", TokenType.Semicolon },
        { ":", TokenType.Colon },
        { ".", TokenType.Dot },
    };

    public static TheoryData<string> Identifiers() => new()
    {
        "_",
        "_tmp",
        "maxValue",
        "point2",
        "If",
        "IF",
        "if1",
        "varx",
        "for",
        "null",
        "main",
        new string('a', 1024),
    };

    public static TheoryData<string, TokenType> NumericLiterals() => new()
    {
        { "0", TokenType.IntegerLiteral },
        { "007", TokenType.IntegerLiteral },
        { "9223372036854775807", TokenType.IntegerLiteral },
        { new string('0', 128) + "9223372036854775807", TokenType.IntegerLiteral },
        { "3.14", TokenType.FloatLiteral },
        { "0.5", TokenType.FloatLiteral },
        { "10.0", TokenType.FloatLiteral },
    };

    public static TheoryData<string, string> StringLiterals() => new()
    {
        { "\"\"", "" },
        { "\"Привет, 世界!\"", "Привет, 世界!" },
        { "\"a\\nb\\tc\"", "a\nb\tc" },
        { "\"\\\"\\\\\"", "\"\\" },
        { "\"// текст /* ещё текст */\"", "// текст /* ещё текст */" },
        { "\"a\0b\"", "a\0b" },
    };

    public static TheoryData<string, TokenType[], string[]> AdjacentTokens() => new()
    {
        { "-5", [TokenType.Minus, TokenType.IntegerLiteral], ["-", "5"] },
        { ".5", [TokenType.Dot, TokenType.IntegerLiteral], [".", "5"] },
        { "5.", [TokenType.IntegerLiteral, TokenType.Dot], ["5", "."] },
        { "1..2", [TokenType.IntegerLiteral, TokenType.Dot, TokenType.Dot, TokenType.IntegerLiteral], ["1", ".", ".", "2"] },
        { "point.x", [TokenType.Identifier, TokenType.Dot, TokenType.Identifier], ["point", ".", "x"] },
        { "x=1", [TokenType.Identifier, TokenType.Assign, TokenType.IntegerLiteral], ["x", "=", "1"] },
        {
            "a++--b",
            [TokenType.Identifier, TokenType.Plus, TokenType.Plus, TokenType.Minus, TokenType.Minus, TokenType.Identifier],
            ["a", "+", "+", "-", "-", "b"]
        },
        { "a===b", [TokenType.Identifier, TokenType.Equal, TokenType.Assign, TokenType.Identifier], ["a", "==", "=", "b"] },
        { "a!==b", [TokenType.Identifier, TokenType.NotEqual, TokenType.Assign, TokenType.Identifier], ["a", "!=", "=", "b"] },
        { "a/**/b", [TokenType.Identifier, TokenType.Identifier], ["a", "b"] },
        { "/* a /* b */ c */", [TokenType.Identifier, TokenType.Multiply, TokenType.Divide], ["c", "*", "/"] },
    };

    public static TheoryData<string, int, int> EmptyInputs() => new()
    {
        { "", 1, 1 },
        { " \t", 1, 3 },
        { "\r", 1, 2 },
        { "\n", 2, 1 },
        { "\r\n", 2, 1 },
        { "// конец", 1, 9 },
        { "/**/", 1, 5 },
        { "/* **/", 1, 7 },
    };

    public static TheoryData<string, int, int> IgnoredPrefixes() => new()
    {
        { " \t", 1, 3 },
        { "\r", 1, 2 },
        { "\n", 2, 1 },
        { "\r\n", 2, 1 },
        { "// текст\n ", 2, 2 },
        { "// текст\r\n", 2, 1 },
        { "/* // текст\n */", 2, 4 },
    };

    public static TheoryData<string, int, int, string> InvalidSources() => new()
    {
        { "\r\n  @", 2, 3, "Недопустимый символ" },
        { "#", 1, 1, "Недопустимый символ" },
        { " &", 1, 2, "Недопустимый символ" },
        { " |", 1, 2, "Недопустимый символ" },
        { "имя", 1, 1, "Недопустимый символ" },
        { "xя", 1, 2, "Недопустимый символ" },
        { "١", 1, 1, "Недопустимый символ" },
        { "\u00a0", 1, 1, "Недопустимый символ" },
        { "\v", 1, 1, "Недопустимый символ" },
        { "\f", 1, 1, "Недопустимый символ" },
        { "\0", 1, 1, "Недопустимый символ" },
        { "\n  \"abc", 2, 3, "Незакрытый строковый литерал" },
        { "\"a\nb\"", 1, 1, "Незакрытый строковый литерал" },
        { "\"a\rb\"", 1, 1, "Незакрытый строковый литерал" },
        { "\"a\\", 1, 1, "Незакрытый строковый литерал" },
        { "\"a\\\nb\"", 1, 1, "Незакрытый строковый литерал" },
        { " \t\"a\\q\"", 1, 5, "Неизвестная управляющая последовательность" },
        { "\"\\r\"", 1, 2, "Неизвестная управляющая последовательность" },
        { "\n /* комментарий", 2, 2, "Незакрытый многострочный комментарий" },
        { "/* конец*", 1, 1, "Незакрытый многострочный комментарий" },
        { " 9223372036854775808", 1, 2, "Целый литерал превышает диапазон int" },
        { "12abc", 1, 3, "Буква или подчёркивание сразу после числа" },
        { "1.5f", 1, 4, "Буква или подчёркивание сразу после числа" },
        { "2_0", 1, 2, "Буква или подчёркивание сразу после числа" },
        { "1e3", 1, 2, "Буква или подчёркивание сразу после числа" },
    };

    public static TheoryData<bool> Utf8Preambles() => new() { false, true };

    [Theory]
    [MemberData(nameof(Keywords))]
    [MemberData(nameof(OperatorsAndDelimiters))]
    [MemberData(nameof(NumericLiterals))]
    public void TokenizeRecognizesLexeme(string source, TokenType expected)
    {
        List<Token> tokens = new Lexer(source).Tokenize();

        Assert.Equal([expected, TokenType.EndOfFile], tokens.Select(token => token.Type));
        Assert.Equal(source, tokens[0].Value);
    }

    [Theory]
    [MemberData(nameof(Identifiers))]
    public void TokenizeRecognizesIdentifier(string source)
    {
        List<Token> tokens = new Lexer(source).Tokenize();

        Assert.Equal([TokenType.Identifier, TokenType.EndOfFile], tokens.Select(token => token.Type));
        Assert.Equal(source, tokens[0].Value);
    }

    [Theory]
    [MemberData(nameof(StringLiterals))]
    public void TokenizeDecodesString(string source, string expected)
    {
        List<Token> tokens = new Lexer(source).Tokenize();

        Assert.Equal([TokenType.StringLiteral, TokenType.EndOfFile], tokens.Select(token => token.Type));
        Assert.Equal(expected, tokens[0].Value);
    }

    [Theory]
    [MemberData(nameof(AdjacentTokens))]
    public void TokenizeSplitsAdjacentLexemes(string source, TokenType[] expectedTypes, string[] expectedValues)
    {
        List<Token> tokens = new Lexer(source).Tokenize();

        Assert.Equal(expectedTypes.Append(TokenType.EndOfFile), tokens.Select(token => token.Type));
        Assert.Equal(expectedValues.Append(""), tokens.Select(token => token.Value));
    }

    [Theory]
    [MemberData(nameof(EmptyInputs))]
    public void TokenizeEndsAtFinalPosition(string source, int expectedLine, int expectedColumn)
    {
        List<Token> tokens = new Lexer(source).Tokenize();

        Token end = Assert.Single(tokens);
        Assert.Equal(TokenType.EndOfFile, end.Type);
        Assert.Empty(end.Value);
        Assert.Equal(source.Length, end.Position);
        Assert.Equal(expectedLine, end.Line);
        Assert.Equal(expectedColumn, end.Column);
    }

    [Theory]
    [MemberData(nameof(IgnoredPrefixes))]
    public void TokenizeTracksPositionAfterWhitespaceAndComments(string prefix, int expectedLine, int expectedColumn)
    {
        List<Token> tokens = new Lexer(prefix + "==").Tokenize();

        Assert.Equal([TokenType.Equal, TokenType.EndOfFile], tokens.Select(token => token.Type));
        Assert.Equal(prefix.Length, tokens[0].Position);
        Assert.Equal(expectedLine, tokens[0].Line);
        Assert.Equal(expectedColumn, tokens[0].Column);
    }

    [Theory]
    [MemberData(nameof(InvalidSources))]
    public void TokenizeReportsErrorAtSourceLocation(string source, int line, int column, string message)
    {
        Lexer lexer = new(source);

        LexerException error = Assert.Throws<LexerException>(() => lexer.Tokenize());

        Assert.Equal(line, error.Line);
        Assert.Equal(column, error.Column);
        Assert.StartsWith(message, error.Message);
        Assert.EndsWith($"(строка {line}, столбец {column}).", error.Message);
    }

    [Theory]
    [MemberData(nameof(Utf8Preambles))]
    public void FromFileReadsUtf8WithOptionalBom(bool emitBom)
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "var x: string = \"Привет\";", new UTF8Encoding(emitBom));

            List<Token> tokens = Lexer.FromFile(path).Tokenize();

            Assert.Equal(TokenType.Var, tokens[0].Type);
            Assert.Equal(1, tokens[0].Column);
            Assert.Equal(TokenType.StringLiteral, tokens[5].Type);
            Assert.Equal("Привет", tokens[5].Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromFileRejectsInvalidUtf8()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [0xff]);

            Assert.Throws<DecoderFallbackException>(() => Lexer.FromFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ConstructorRejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>("source", () => new Lexer(null!));
    }

    [Fact]
    public void TokenToStringShowsTypeValueAndLocation()
    {
        Token token = new(TokenType.Identifier, "name", 2, 1, 3);

        string description = token.ToString();

        Assert.Equal("Token(Identifier, \"name\", 1:3)", description);
    }

    [Fact]
    public void TokenizeReadsMaxExample()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "max.kite");

        List<Token> tokens = Lexer.FromFile(path).Tokenize();

        Assert.Equal(
            [TokenType.Func, TokenType.Identifier, TokenType.LeftParen, TokenType.Identifier, TokenType.Colon,
                TokenType.Int, TokenType.LeftBracket, TokenType.RightBracket],
            tokens.Take(8).Select(token => token.Type));
        Assert.Equal("max", tokens[1].Value);
        Assert.Contains(tokens, token => token.Type == TokenType.While);
        Assert.Contains(tokens, token => token.Type == TokenType.If);
        Assert.Contains(tokens, token => token.Type == TokenType.New);
        Assert.Contains(tokens, token => token.Type == TokenType.Identifier && token.Value == "main");
        Assert.Contains(tokens, token => token.Type == TokenType.Identifier && token.Value == "print");
        Assert.Equal(TokenType.RightBrace, tokens[^2].Type);
        Assert.Equal(TokenType.EndOfFile, tokens[^1].Type);
    }
}