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
                if(Match(TokenType.FUN)) return Function("function");
                if(Match(TokenType.VAR)) return VarDeclaration();

                return Statement();
            }
            catch(ParseError)
            {
                Synchronize();
                return null;
            }
        }

        private Stmt Function(string kind)
        {
            Token name = Consume(TokenType.IDENTIFIER, $"Expect {kind} name.");

            Consume(TokenType.LEFT_PAREN, $"Expect '(' after {kind} name.");
            var parameters = new List<Token>();

            if(!Check(TokenType.RIGHT_PAREN))
                do {
                    if(parameters.Count >= 255)
                        Error(Peek(), "Cannot have more than 255 parameters.");
                    
                    parameters.Add(Consume(TokenType.IDENTIFIER, "Expect parameter name."));
                } while(Match(TokenType.COMMA));

            Consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");

            Consume(TokenType.LEFT_BRACE, $"Expect '{{' before {kind} body.");
            List<Stmt> body = Block();

            return new Stmt.Function(name, parameters, body);
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

        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after 'while'.");
            Stmt body = Statement();

            return new Stmt.While(condition, body);
        }

        private Stmt Statement() 
        {
            if(Match(TokenType.FOR)) return ForStatement();
            if(Match(TokenType.IF)) return IfStatement();
            if(Match(TokenType.PRINT)) return PrintStatement();
            if(Match(TokenType.RETURN)) return ReturnStatement();
            if(Match(TokenType.WHILE)) return WhileStatement();
            if(Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

            return ExpressionStatement();
        }

        private Stmt ForStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect 'c' after 'for'.");

            Stmt initialiser = null;
            if(Match(TokenType.SEMICOLON))
            {
                initialiser = null;
            }
            else if(Match(TokenType.VAR))
            {
                initialiser = VarDeclaration();
            }
            else
            {
                initialiser = ExpressionStatement();
            }

            Expr condition = null;
            if(!Check(TokenType.SEMICOLON))
            {
                condition = Expression();
            }
            Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if(!Check(TokenType.RIGHT_PAREN))
            {
                increment = Expression();
            }
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

            Stmt body = Statement();

            if(increment != null)
            {
                body = new Stmt.Block(new List<Stmt>{
                    body,
                    new Stmt.Expression(increment),
                });
            }

            condition ??= new Expr.Literal(true);
            body = new Stmt.While(condition, body);

            if(initialiser != null)
            {
                body = new Stmt.Block(new List<Stmt>{
                    initialiser,
                    body,
                });
            }

            return body;
        }

        private Stmt IfStatement()
        {
            Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt thanBranch = Statement();
            Stmt elseBranch = null;
            if(Match(TokenType.ELSE))
            {
                elseBranch = Statement();
            }

            return new Stmt.If(condition, thanBranch, elseBranch);
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

        private Stmt ReturnStatement()
        {
            Token keyword = Previous();
            Expr value = null;
            if(!Check(TokenType.SEMICOLON))
                value = Expression();

            Consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
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
            Expr expr = Or();

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

        private Expr Or() =>
            ParseLogicalExpr(And, TokenType.OR);

        private Expr And() =>
            ParseLogicalExpr(Equality, TokenType.AND);

        private Expr Equality() =>
            ParseBinomExpr(Comparison, TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL);

        private Expr Comparison() =>
            ParseBinomExpr(Addition, TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL);

        private Expr Addition() =>
            ParseBinomExpr(Multiplication, TokenType.MINUS, TokenType.PLUS);

        private Expr Multiplication() =>
            ParseBinomExpr(Unary, TokenType.SLASH, TokenType.STAR);

        private delegate Expr BinopExpr();

        private Expr ParseLogicalExpr(BinopExpr f, params TokenType[] tokens)
        {
            Expr expr = f();

            while(Match(tokens))
            {
                Token op = Previous();
                Expr right = f();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

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

            return Call();
        }

        private Expr Call()
        {
            Expr expr = Primary();

            while(true)
            {
                if(Match(TokenType.LEFT_PAREN))
                    expr = FinishCall(expr);
                else
                    break;
            }

            return expr;
        }

        private Expr FinishCall(Expr callee)
        {
            var arguments = new List<Expr>();

            if(!Check(TokenType.RIGHT_PAREN))
                do {
                    if(arguments.Count >= 255)
                        Error(Peek(), "Cannot have more than 255 arguments.");

                    arguments.Add(Expression());
                } while(Match(TokenType.COMMA));

            Token paren = Consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
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