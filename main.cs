using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

class main
{
    public enum TokenType
    {
        IDENTIFIER,
        NUMBER,
        STRING,
        WHITESPACE,
        SEMICOLON,
        COLON,
        MINUS,
        PLUS,
        STAR,
        SLASH,
        LEFTPAREN,
        RIGHTPAREN,
        EQUALS,
        DEFUN,
        PRINT,
        LET,
        INT64,
        IF,
        ELSE,
        ENUM,
        STRUCT,
        RIGHTARROW,
        LEFTARROW,
        UNKNOWN
    }

    public sealed class Token
    {
        public TokenType Type { get; private set; }
        public string Lexeme { get; private set; }

        public Token(TokenType type, string lexeme)
        {
            Type = type;
            Lexeme = lexeme;
        }

        public override string ToString()
        {
            return Type + ": " + DisplayLexeme(Lexeme);
        }

        private static string DisplayLexeme(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }

    public abstract class AstNode
    {
    }

    public sealed class ProgramNode : AstNode
    {
        public List<AstNode> Statements = new List<AstNode>();
    }

    public sealed class PrintNode : AstNode
    {
        public AstNode Expression;

        public PrintNode(AstNode expression)
        {
            Expression = expression;
        }
    }

    public sealed class NumberNode : AstNode
    {
        public long Value;

        public NumberNode(long value)
        {
            Value = value;
        }
    }

    public sealed class StringNode : AstNode
    {
        public string Value;

        public StringNode(string value)
        {
            Value = value;
        }
    }

    public sealed class IdentifierNode : AstNode
    {
        public string Name;

        public IdentifierNode(string name)
        {
            Name = name;
        }
    }

    public sealed class BinaryNode : AstNode
    {
        public TokenType Operator;
        public AstNode Left;
        public AstNode Right;

        public BinaryNode(TokenType operatorType, AstNode left, AstNode right)
        {
            Left = left;
            Operator = operatorType;
            Right = right;
        }
    }

    public sealed class LetNode : AstNode
    {
        public string Name;
        public AstNode Value;

        public LetNode(string name, AstNode value)
        {
            Name = name;
            Value = value;
        }
    }

    public sealed class IfNode : AstNode
    {
        public AstNode Condition;
        public AstNode ThenBranch;
        public AstNode ElseBranch;

        public IfNode(AstNode condition, AstNode thenBranch, AstNode elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    public sealed class ElseNode : AstNode
    {
        public AstNode Body;

        public ElseNode(AstNode body)
        {
            Body = body;
        }
    }

    public sealed class StructNode : AstNode
    {
        public string Name;
        public List<string> Members;

        public StructNode(string name, List<string> members)
        {
            Name = name;
            Members = members;
        }
    }

    public sealed class EnumNode : AstNode
    {
        public string Name;
        public List<string> Members;

        public EnumNode(string name, List<string> members)
        {
            Name = name;
            Members = members;
        }
    }

    public sealed class DefunNode : AstNode
    {
        public string Name;
        public List<string> Parameters;
        public AstNode Body;

        public DefunNode(string name, List<string> parameters, AstNode body)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }

    public static string sourceCode = @"./test.5th";

    public static void Main()
    {
        string[] sourceLines = File.ReadAllLines(sourceCode);
        List<List<Token>> tokenLines = Lex(sourceLines);

        Console.WriteLine("--- Source by line ---");

        int displayedLine = 0;
        for (int lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++)
        {
            if (tokenLines[lineIndex].Count > 0)
                Console.WriteLine("Line {0:D2}: {1}", displayedLine++, sourceLines[lineIndex]);
        }

        Console.WriteLine("--- Tokens by line ---");

        displayedLine = 0;
        for (int lineIndex = 0; lineIndex < tokenLines.Count; lineIndex++)
        {
            if (tokenLines[lineIndex].Count == 0)
                continue;

            Console.WriteLine("Line {0:D2}:", displayedLine++);
            foreach (Token token in tokenLines[lineIndex])
                Console.WriteLine("  " + token);
        }

        
        Console.WriteLine("--- Abstract Syntax Tree ---");

        List<Token> allTokens = new List<Token>();

        foreach (List<Token> line in tokenLines)
        {
            allTokens.AddRange(line);
        }

        Parser parser = new Parser(allTokens);
        ProgramNode ast = parser.ParseProgram();

        PrintAst(ast, "");

    }

    public static List<List<Token>> Lex(string fileName)
    {
        return Lex(File.ReadAllLines(fileName));
    }

    private static List<List<Token>> Lex(string[] lines)
    {
        List<List<Token>> tokenLines = new List<List<Token>>();
        bool inComment = false;

        foreach (string line in lines)
        {
            tokenLines.Add(LexLine(line, ref inComment));
        }

        if (inComment)
            throw new InvalidDataException("Unterminated comment in source file.");

        return tokenLines;
    }

    private static List<Token> LexLine(string line, ref bool inComment)
    {
        List<Token> tokens = new List<Token>();
        int position = 0;

        while (position < line.Length)
        {
            if (inComment)
            {
                int end = line.IndexOf("-;", position);
                position = end < 0 ? line.Length : end + 2;
                inComment = end < 0;
                continue;
            }

            if (line[position] == ';' && position + 1 < line.Length &&
                line[position + 1] == '-')
            {
                inComment = true;
                position += 2;
            }
            else if (char.IsWhiteSpace(line[position]))
            {
                position++;
            }
            else if (line[position] == '"')
            {
                tokens.Add(ReadStringToken(line, ref position));
            }
            else if (char.IsLetter(line[position]) || line[position] == '_')
            {
                tokens.Add(ReadIdentifierToken(line, ref position));
            }
            else if (char.IsDigit(line[position]))
            {
                tokens.Add(ReadNumberToken(line, ref position));
            }
            else
            {
                tokens.Add(ReadPunctuationToken(line, ref position));
            }
        }

        return tokens;
    }

    private static Token ReadStringToken(string line, ref int position)
    {
        int start = position;
        StringBuilder value = new StringBuilder();
        position++;

        while (position < line.Length)
        {
            char current = line[position++];

            if (current == '"')
                return new Token(TokenType.STRING, value.ToString());

            if (current == '\\' && position < line.Length)
            {
                char escaped = line[position++];
                value.Append(escaped == 'n' ? '\n' : escaped);
            }
            else
            {
                value.Append(current);
            }
        }

        throw new InvalidDataException("Unterminated string starting at position " + start + ".");
    }

    private static Token ReadIdentifierToken(string line, ref int position)
    {
        int start = position;

        while (position < line.Length &&
            (char.IsLetterOrDigit(line[position]) || line[position] == '_'))
        {
            position++;
        }

        string lexeme = line.Substring(start, position - start);
        return new Token(GetKeywordType(lexeme), lexeme);
    }

    private static Token ReadNumberToken(string line, ref int position)
    {
        int start = position;

        while (position < line.Length && char.IsDigit(line[position]))
            position++;

        return new Token(TokenType.NUMBER, line.Substring(start, position - start));
    }

    private static Token ReadPunctuationToken(string line, ref int position)
    {
        char current = line[position++];
        return new Token(PunctuationType(current), current.ToString());
    }

    private static TokenType PunctuationType(char value)
    {
        switch (value)
        {
            case ';': return TokenType.SEMICOLON;
            case ':': return TokenType.COLON;
            case '-': return TokenType.MINUS;
            case '+': return TokenType.PLUS;
            case '*': return TokenType.STAR;
            case '/': return TokenType.SLASH;
            case '(': return TokenType.LEFTPAREN;
            case ')': return TokenType.RIGHTPAREN;
            case '>': return TokenType.RIGHTARROW;
            case '<': return TokenType.LEFTARROW;
            case '=': return TokenType.EQUALS;
            default: return TokenType.UNKNOWN;
        }
    }

    private static TokenType GetKeywordType(string lexeme)
    {
        switch (lexeme)
        {
            case "defun": return TokenType.DEFUN;
            case "print": return TokenType.PRINT;
            case "let": return TokenType.LET;
            case "int64": return TokenType.INT64;
            case "if": return TokenType.IF;
            case "else": return TokenType.ELSE;
            case "enum": return TokenType.ENUM;
            case "struct": return TokenType.STRUCT;
            default: return TokenType.IDENTIFIER;
        }
    }


    public sealed class Parser
    {
        private readonly List<Token> tokens;
        private int position;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
            position = 0;
        }

        private Token Current
        {
            get
            {
                if (position >= tokens.Count)
                    return null;

                return tokens[position];
            }
        }

        private Token Advance()
        {
            Token token = Current;

            if (token != null)
                position++;

            return token;
        }

        private bool Match(TokenType type)
        {
            if (Current == null || Current.Type != type)
                return false;

            Advance();
            return true;
        }

        private Token Expect(TokenType type)
        {
            if (Current == null || Current.Type != type)
            {
                string actual = Current == null
                    ? "end of input"
                    : Current.Type + " '" + Current.Lexeme + "'";

                throw new InvalidDataException(
                    "Expected " + type + ", got " + actual
                );
            }

            return Advance();
        }

        public ProgramNode ParseProgram()
        {
            ProgramNode program = new ProgramNode();

            while (Current != null)
            {
                program.Statements.Add(ParseExpression());
            }

            return program;
        }

        private AstNode ParseExpression()
        {
            if (Current == null)
                throw new InvalidDataException(
                    "Unexpected end of input."
                );

            if (Match(TokenType.STRING))
                return new StringNode(tokens[position - 1].Lexeme);

            if (Match(TokenType.NUMBER))
            {
                long value;

                if (!long.TryParse(
                    tokens[position - 1].Lexeme,
                    out value))
                {
                    throw new InvalidDataException(
                        "Invalid integer literal."
                    );
                }

                return new NumberNode(value);
            }

            if (Current.Type == TokenType.IDENTIFIER)
                return new IdentifierNode(Advance().Lexeme);

            if (Match(TokenType.LEFTPAREN))
            {
                if (Current == null)
                    throw new InvalidDataException(
                        "Unclosed expression."
                    );

                if (Current.Type == TokenType.STRING ||
                    Current.Type == TokenType.NUMBER ||
                    Current.Type == TokenType.IDENTIFIER)
                {
                    AstNode expression = ParseExpression();
                    Expect(TokenType.RIGHTPAREN);
                    return expression;
                }

                Token operation = Advance();

                if (operation.Type == TokenType.PRINT)
                {
                    AstNode argument = ParseExpression();

                    Expect(TokenType.RIGHTPAREN);
                    Expect(TokenType.RIGHTPAREN);

                    return new PrintNode(argument);
                }

                if (operation.Type == TokenType.LET)
                {
                    Token nameToken = Expect(TokenType.IDENTIFIER);
                    AstNode value = ParseExpression();

                    Expect(TokenType.RIGHTPAREN);

                    return new LetNode(nameToken.Lexeme, value);
                }

                if (operation.Type == TokenType.IF)
                {

                    Expect(TokenType.LEFTPAREN);

                    AstNode condition = ParseExpression();
                    AstNode thenBranch = ParseExpression();
                    AstNode elseBranch = null;

                    if (Current != null && Current.Type == TokenType.ELSE)
                    {
                        Advance(); // Consume the 'else' token
                        elseBranch = ParseExpression();
                        Expect(TokenType.LEFTPAREN);
                    }

                    Expect(TokenType.RIGHTPAREN);

                    return new IfNode(condition, thenBranch, elseBranch);
                }

                if (operation.Type == TokenType.PLUS ||
                    operation.Type == TokenType.MINUS ||
                    operation.Type == TokenType.STAR ||
                    operation.Type == TokenType.SLASH ||
                    operation.Type == TokenType.EQUALS ||
                    operation.Type == TokenType.RIGHTARROW ||
                    operation.Type == TokenType.LEFTARROW)
                {
                    AstNode left = ParseExpression();
                    AstNode right = ParseExpression();

                    Expect(TokenType.RIGHTPAREN);

                    return new BinaryNode(
                        operation.Type,
                        left,
                        right
                    );
                }

                throw new InvalidDataException(
                    "Unsupported expression: " + operation.Lexeme
                );
            }

            throw new InvalidDataException(
                "Unexpected token: " + Current.Lexeme
            );
        }
    }
    
    
    public static void PrintAst(AstNode node, string indent)
    {
        ProgramNode program = node as ProgramNode;

        if (program != null)
        {
            Console.WriteLine(indent + "Program");

            foreach (AstNode statement in program.Statements)
                PrintAst(statement, indent + "  ");

            return;
        }

        PrintNode print = node as PrintNode;

        if (print != null)
        {
            Console.WriteLine(indent + "Print");
            PrintAst(print.Expression, indent + "  ");
            return;
        }

        StringNode str = node as StringNode;

        if (str != null)
        {
            Console.WriteLine(
                indent + "String: \"" + str.Value + "\""
            );
            return;
        }

        NumberNode number = node as NumberNode;

        if (number != null)
        {
            Console.WriteLine(
                indent + "Number: " + number.Value
            );
            return;
        }

        IdentifierNode identifier = node as IdentifierNode;

        if (identifier != null)
        {
            Console.WriteLine(
                indent + "Identifier: " + identifier.Name
            );
            return;
        }

        BinaryNode binary = node as BinaryNode;

        if (binary != null)
        {
            Console.WriteLine(
                indent + "Binary: " + binary.Operator
            );

            PrintAst(binary.Left, indent + "  ");
            PrintAst(binary.Right, indent + "  ");
            return;
        }

        throw new InvalidOperationException(
            "Unknown AST node: " + node.GetType().Name
        );
    }


}
