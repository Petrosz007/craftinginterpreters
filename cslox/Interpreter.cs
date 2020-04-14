using System;
using System.Collections.Generic;
using System.Globalization;

namespace cslox
{
    public class NoValue {}
    public class RuntimeError : Exception {
        public Token Token { get; }
        public RuntimeError(Token token, string message)
            : base(message) => 
            Token = token;
    }
    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<NoValue>
    {
        private Environment environment;
        public Interpreter() =>
            environment = new Environment();
        public void Interpret(List<Stmt> statements)
        {
            try {
                foreach(var statement in statements)
                {
                    Execute(statement);
                }
            }
            catch(RuntimeError error)
            {
                Lox.RuntimeError(error);
            }
        }

        private void Execute(Stmt stmt) 
        {
            stmt.accept(this);
        }

        private string Stringify(object obj)
        {
            if(obj == null) return "nil";

            if(obj is Double num)
            {
                string text = num.ToString(CultureInfo.InvariantCulture);
                if(text.EndsWith(".0"))
                {
                    text = text.Substring(0, text.Length - 2);
                }
                return text;
            }

            return obj.ToString();
        }
        
        public object visitLiteralExpr(Expr.Literal expr) =>
            expr.Value;

        public object visitGroupingExpr(Expr.Grouping expr) =>
            Evaluate(expr.Expression);
        private object Evaluate(Expr expr) =>
            expr.accept(this);

        public object visitUnaryExpr(Expr.Unary expr) 
        {
            object right = Evaluate(expr.Right);

            switch(expr.Op.Type) {
                case TokenType.MINUS:
                    CheckNumberOperand(expr.Op, right);
                    return - (double) right;

                case TokenType.BANG:
                    return !IsThruthy(right);
            }

            return null;
        }

        private void CheckNumberOperand(Token op, object operand)
        {
            if(operand is Double) return;

            throw new RuntimeError(op, "Operand must be a number.");
        }

        private void CheckNumberOperands(Token op, object left, object right)
        {
            if(left is Double && right is Double) return;

            throw new RuntimeError(op, "Operands must be a numbers.");
        }

        private bool IsThruthy(object obj)
        {
            if(obj == null) return false;

            if(obj is bool b) return b;

            return true;
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            object left  = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch(expr.Binop.Type) {
                case TokenType.GREATER:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left > (double) right;

                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left >= (double) right;

                case TokenType.LESS:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left < (double) right;

                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left <= (double) right;

                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
                    
                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);

                case TokenType.MINUS:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left - (double) right; 

                case TokenType.PLUS:
                    if(left is double leftDouble && right is double rightDouble)
                    {
                        return leftDouble + rightDouble;
                    }

                    if(left is string leftStr && right is string rightStr)
                    {
                        return leftStr + rightStr;
                    }
        
                    throw new RuntimeError(expr.Binop, "Operands must be two numbers or two strings.");

                case TokenType.SLASH:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left / (double) right;

                case TokenType.STAR:
                    CheckNumberOperands(expr.Binop, left, right);
                    return (double) left * (double) right;
            }

            return null;
        }

        private bool IsEqual(object a, object b)
        {
            if(a == null && b == null) return true;
            if(a == null) return false;

            return a.Equals(b);
        }

        public NoValue visitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.Expr);
            return null;
        }

        public NoValue visitPrintStmt(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.Expr);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public NoValue visitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if(stmt.Initializer != null)
            {
                value = Evaluate(stmt.Initializer);
            }

            environment.Define(stmt.Name.Lexeme, value);
            return null;
        }

        public object visitAssignExpr(Expr.Assign expr)
        {
            object value = Evaluate(expr.Value);

            environment.Assign(expr.Name, value);
            return value;
        }

        public object visitVariableExpr(Expr.Variable expr) =>
            environment.Get(expr.Name);

        public NoValue visitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.Statements, new Environment(environment));
            return null;
        }

        private void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            Environment previous = this.environment;

            try {
                this.environment = environment;

                foreach(Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.environment = previous;
            }
        }
    }
}