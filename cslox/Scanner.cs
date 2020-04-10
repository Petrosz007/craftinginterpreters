using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace cslox
{
    public class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens;
        private static Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
        {
            {"and",     TokenType.AND},
            {"class",   TokenType.CLASS},
            {"else",    TokenType.ELSE},
            {"false",   TokenType.FALSE},
            {"for",     TokenType.FOR},
            {"fun",     TokenType.FUN},
            {"if",      TokenType.IF},
            {"nil",     TokenType.NIL},
            {"or",      TokenType.OR},
            {"print",   TokenType.PRINT},
            {"return",  TokenType.RETURN},
            {"super",   TokenType.SUPER},
            {"this",    TokenType.THIS},
            {"true",    TokenType.TRUE},
            {"var",     TokenType.VAR},
            {"while",   TokenType.WHILE},
        };
        private int start;
        private int current;
        private int line;

        public Scanner(string source)
        {
            this.source = source;
            tokens = new List<Token>();

            start = 0;
            current = 0;
            line = 1;
        }

        public List<Token> scanTokens()
        {
            while(!IsAtEnd())
            {
                start = current;
                scanToken();
            }

            tokens.Add(new Token(TokenType.EOF, "", null, line));
            return tokens;
        }

        private bool IsAtEnd() =>
            current >= source.Length;

        private void scanToken()
        {
            char c = advance();
            switch(c) {
                // One character
                case '(': addToken(TokenType.LEFT_PAREN); break;
                case ')': addToken(TokenType.RIGHT_PAREN); break;
                case '{': addToken(TokenType.LEFT_BRACE); break;
                case '}': addToken(TokenType.RIGHT_BRACE); break;
                case ',': addToken(TokenType.COMMA); break;
                case '.': addToken(TokenType.DOT); break;
                case '-': addToken(TokenType.MINUS); break;
                case '+': addToken(TokenType.PLUS); break;
                case ';': addToken(TokenType.SEMICOLON); break;
                case '*': addToken(TokenType.STAR); break;

                // One or two characters
                case '!': addToken(Match('=') ? TokenType.BANG_EQUAL    : TokenType.BANG); break;
                case '=': addToken(Match('=') ? TokenType.EQUAL_EQUAL   : TokenType.EQUAL); break;
                case '<': addToken(Match('=') ? TokenType.LESS_EQUAL    : TokenType.LESS); break;
                case '>': addToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;
                case '/': 
                    if(Match('/'))
                        while(Peek() != '\n' && !IsAtEnd()) { advance(); }
                    else
                        addToken(TokenType.SLASH);
                break;

                // Whitespace
                case ' ': break;
                case '\r': break;
                case '\t': break;
                case '\n': line++; break;

                case '"': String(); break;

                default:
                    if(IsDigit(c))
                    {
                        Number();
                    }
                    else if(IsAlpha(c))
                    {
                        Identifier();
                    }
                    else
                    {
                        Lox.Error(line, "Unexpected Character.");
                    }
                break;
            }
        }

        private char advance()
        {
            current++;
            return source[current - 1];
        }

        private bool Match(char expected)
        {
            if(IsAtEnd()) return false;
            if(source[current] != expected) return false;

            current++;
            return true;
        }

        private char Peek() =>
            IsAtEnd() ? '\0' : source[current];

        private char PeekNext() =>
            (current + 1 >= source.Length) ? '\0' : source[current + 1];

        private void String()
        {
            while(Peek() != '"' && !IsAtEnd())
            {
                if(Peek() == '\n') line++;
                advance();
            }

            // Unterminated string
            if(IsAtEnd())
            {
                Lox.Error(line, "Unterminated string");
            }

            // Closing "
            advance();

            // Trim the sorrounding quotes
            string value = source.Substring(start + 1, current - 2 - start);
            addToken(TokenType.STRING, value);
        }

        private bool IsDigit(char c) =>
            c >= '0' && c <= '9';

        private bool IsAlpha(char c) =>
            (c >= 'a' && c <= 'z' ||
             c >= 'A' && c <= 'Z' ||
             c == '_');

        private bool IsAlphaNumeric(char c) =>
            IsAlpha(c) || IsDigit(c);

        private void Identifier()
        {
            while(IsAlphaNumeric(Peek())) advance();

            // See if the identifier is a reserved word
            string text = source.Substring(start, current - start);

            TokenType type;
            if(!keywords.TryGetValue(text, out type))
            {
                type = TokenType.IDENTIFIER;
            }

            addToken(type);
        }

        private void Number()
        {
            while(IsDigit(Peek())) advance();

            // Look for a fractional part.
            if(Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consume the '.'
                advance();

                while(IsDigit(Peek())) advance();
            }

            addToken(TokenType.NUMBER, Double.Parse(source.Substring(start, current - start), CultureInfo.InvariantCulture));
        }

        private void addToken(TokenType type) =>
            addToken(type, null);
        
        private void addToken(TokenType type, object literal)
        {
            string text = source.Substring(start, current - start);
            tokens.Add(new Token(type, text, literal, line));
        }
    }
}