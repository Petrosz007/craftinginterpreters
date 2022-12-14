using System.Diagnostics;
using System.Linq;
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

    public class Return : Exception
    {
        public object Value { get; }
        public Return(object value) : base() =>
            Value = value;
    }

    public class Interpreter : Expr.Visitor<object>, Stmt.Visitor<NoValue>
    {
        internal Environment globals = new Environment();
        private Environment environment;
        private Dictionary<Expr, int> locals = new Dictionary<Expr, int>();
        public Interpreter()
        {
            globals.Define("clock", new LoxPrimitive {
                Arity = 0,
                Call = (Interpreter x, List<object> y) => DateTime.Now.Ticks / TimeSpan.TicksPerSecond,
            });

            environment = globals;
        }
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

        internal void Resolve(Expr expr, int depth) =>
            locals[expr] = depth;

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

        public object visitLogicalExpr(Expr.Logical expr)
        {
            object left = Evaluate(expr.Left);

            if(expr.Op.Type == TokenType.OR)
            {
                if(IsTruthy(left)) return left;
            }
            else
            {
                if(!IsTruthy(left)) return left;
            }

            return Evaluate(expr.Right);
        }

        public object visitSetExpr(Expr.Set expr)
        {
            object obj = Evaluate(expr.Obj);

            if(!(obj is LoxInstance))
                throw new RuntimeError(expr.Name, "Only instances have fields.");

            object value = Evaluate(expr.Value);
            ((LoxInstance)obj).Set(expr.Name, expr.Value);
            return value;
        }

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
                    return !IsTruthy(right);
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

        private bool IsTruthy(object obj)
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

        public object visitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.Callee);

            var arguments = expr.Arguments.Select(arg => Evaluate(arg)).ToList();

            if(!(callee is LoxCallable function))
                throw new RuntimeError(expr.Paren, "Can only call functions and classes.");

            if(arguments.Count != function.Arity)
                throw new RuntimeError(expr.Paren, $"Expected {function.Arity} arguments but got {arguments.Count}.");

            return function.Call(this, arguments); 
        }

        public object visitGetExpr(Expr.Get expr)
        {
            object obj = Evaluate(expr.Obj);

            if(obj is LoxInstance instance)
                return instance.Get(expr.Name);

            throw new RuntimeError(expr.Name, "Only instances have properties.");
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

        public NoValue visitFunctionStmt(Stmt.Function stmt)
        {
            var function = new LoxFunction(stmt, environment);
            environment.Define(stmt.Name.Lexeme, function);
            return null;
        }

        public NoValue visitIfStmt(Stmt.If stmt)
        {
            if(IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.ThenBranch);
            }
            else if(stmt.ElseBranch != null)
            {
                Execute(stmt.ElseBranch);
            }

            return null;
        }

        public NoValue visitPrintStmt(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.Expr);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public NoValue visitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if(stmt.Value != null)
                value = Evaluate(stmt.Value);

            throw new Return(value);
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

        public NoValue visitWhileStmt(Stmt.While stmt)
        {
            while(IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }

            return null;
        }

        public object visitAssignExpr(Expr.Assign expr)
        {
            object value = Evaluate(expr.Value);

            if(locals.TryGetValue(expr, out int distance))
                environment.AssignAt(distance, expr.Name.Lexeme, value);
            else
                globals.Assign(expr.Name, value);

            return value;
        }

        public object visitVariableExpr(Expr.Variable expr) =>
            LookUpVariable(expr.Name, expr);

        private object LookUpVariable(Token name, Expr expr)
        {
            if(locals.TryGetValue(expr, out int distance))
                return environment.GetAt(distance, name.Lexeme);
            else
                return globals.Get(name);
        }

        public NoValue visitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.Statements, new Environment(environment));
            return null;
        }

        internal void ExecuteBlock(List<Stmt> statements, Environment environment)
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

        public NoValue visitClassStmt(Stmt.Class stmt)
        {
            environment.Define(stmt.Name.Lexeme, null);
            var klass = new LoxClass(stmt.Name.Lexeme);
            environment.Assign(stmt.Name, klass);

            return null;
        }
    }
}