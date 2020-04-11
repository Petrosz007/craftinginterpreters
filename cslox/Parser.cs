using System;
using System.Collections.Generic;

namespace cslox
{
    public class Parser
    {
        private class ParseError : Exception {}

        private readonly List<Token> tokens;
        private int current;

        public Parser(List<Token> tokens) =>
            (this.tokens, current) = (tokens, 0);

        public Expr Parse()
        {
            try {
                return Expression();
            }
            catch(ParseError error) {
                return null;
            }
        }

        private Expr Expression() =>
            Equality();

        private Expr Equality() =>
            ParseBinomExpr(Comparison, TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL);
        // {
        //     Expr expr = Comparison();

        //     while(Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
        //     {
        //         Token op = Previous();
        //         Expr right = Comparison();
        //         expr = new Expr.Binary(expr, op, right);
        //     }

        //     return expr;
        // }

        private Expr Comparison() =>
            ParseBinomExpr(Addition, TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL);
        // {
        //     Expr expr = Addition();
            
        //     while(Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
        //     {
        //         Token op = Previous();
        //         Expr right = Addition();
        //         expr = new Expr.Binary(expr, op, right);
        //     }

        //     return expr;
        // }

        private Expr Addition() =>
            ParseBinomExpr(Multiplication, TokenType.MINUS, TokenType.PLUS);
        // {
        //     Expr expr = Multiplication();

        //     while(Match(TokenType.MINUS, TokenType.PLUS))
        //     {
        //         Token operator = Previous();
        //         Expr right = Multiplication();
        //         expr = new Expr.Binary(expr, op, right);
        //     }

        //     return expr;
        // }

        private Expr Multiplication() =>
            ParseBinomExpr(Unary, TokenType.SLASH, TokenType.STAR);

        private delegate Expr BinopExpr();

        private Expr ParseBinomExpr(BinopExpr f, params TokenType[] tokens)
        {
            Expr expr = f();

            while(Match(tokens))
            {
                Token op = Previous();
                Expr right = f();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Unary()
        {
            if(Match(TokenType.BANG, TokenType.MINUS))
            {
                Token op = Previous();
                Expr right = Unary();
                return new Expr.Unary(op, right);
            }

            return Primary();
        }

        private Expr Primary()
        {
            if(Match(TokenType.FALSE)) return new Expr.Literal(false);
            if(Match(TokenType.TRUE)) return new Expr.Literal(true);
            if(Match(TokenType.NIL)) return new Expr.Literal(null);

            if(Match(TokenType.NUMBER, TokenType.STRING))
            {
                return new Expr.Literal(Previous().Literal);
            }

            if(Match(TokenType.LEFT_PAREN))
            {
                Expr expr = Expression();
                Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw Error(Peek(), "Expected expression.");
        }

        private Token Consume(TokenType type, string message)
        {
            if(Check(type)) return Advance();

            throw Error(Peek(), message);
        }

        private ParseError Error(Token token, string message)
        {
            Lox.Error(token, message);
            return new ParseError();
        }

        private void Synchronize()
        {
            Advance();

            while(!IsAtEnd())
            {
                if(Previous().Type == TokenType.SEMICOLON) return;

                bool shouldReturn = Peek().Type switch {
                    TokenType.CLASS  => true,
                    TokenType.FUN    => true,
                    TokenType.VAR    => true,
                    TokenType.FOR    => true,
                    TokenType.IF     => true,
                    TokenType.WHILE  => true,
                    TokenType.PRINT  => true,
                    TokenType.RETURN => true,
                    _                => false,
                };
                if(shouldReturn) return;

                Advance();
            }
        }

        private bool Match(params TokenType[] types)
        {
            foreach(var token in types)
            {
                if(Check(token))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private bool Check(TokenType type) =>
            IsAtEnd() ? false : Peek().Type == type;

        private Token Advance()
        {
            if(!IsAtEnd()) current++;

            return Previous();
        }

        private bool IsAtEnd() =>
            Peek().Type == TokenType.EOF;

        private Token Peek() =>
            tokens[current];

        private Token Previous() =>
            tokens[current - 1];
    }
}