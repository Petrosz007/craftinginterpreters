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

        public List<Stmt> Parse()
        {
            List<Stmt> statements = new List<Stmt>();

            while(!IsAtEnd())
            {
                statements.Add(Declaration());
            }

            return statements;
        }

        private Stmt Declaration()
        {
            try {
                if(Match(TokenType.VAR)) return VarDeclaration();

                return Statement();
            }
            catch(ParseError error)
            {
                Synchronize();
                return null;
            }
        }

        private Stmt VarDeclaration()
        {
            Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

            Expr initialiser = null;
            if(Match(TokenType.EQUAL))
            {
                initialiser = Expression();
            }

            Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
            return new Stmt.Var(name, initialiser);
        }

        private Stmt Statement() 
        {
            if(Match(TokenType.PRINT)) return PrintStatement();
            if(Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

            return ExpressionStatement();
        }

        private List<Stmt> Block()
        {
            List<Stmt> statements = new List<Stmt>();

            while(!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
            {
                statements.Add(Declaration());
            }

            Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        private Stmt PrintStatement()
        {
            Expr value = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        private Stmt ExpressionStatement()
        {
            Expr expr = Expression();
            Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expression(expr);
        }

        private Expr Expression() =>
            Assignment();

        private Expr Assignment()
        {
            Expr expr = Equality();

            if(Match(TokenType.EQUAL))
            {
                Token equals = Previous();
                Expr value = Assignment();

                if(expr is Expr.Variable variable)
                {
                    return new Expr.Assign(variable.Name, value);
                }

                Error(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expr Equality() =>
            ParseBinomExpr(Comparison, TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL);

        private Expr Comparison() =>
            ParseBinomExpr(Addition, TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL);

        private Expr Addition() =>
            ParseBinomExpr(Multiplication, TokenType.MINUS, TokenType.PLUS);

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

            if(Match(TokenType.IDENTIFIER))
            {
                return new Expr.Variable(Previous());
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