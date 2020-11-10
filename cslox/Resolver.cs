using System.Collections.Generic;

namespace cslox
{
    public enum FunctionType
    {
        NONE,
        FUNCTION,
    }
    public class Resolver : Expr.Visitor<NoValue>, Stmt.Visitor<NoValue>
    {
        private Interpreter interpreter;
        private Stack<Dictionary<string,bool>> scopes = new Stack<Dictionary<string, bool>>();
        private FunctionType currentFunction = FunctionType.NONE;

        public Resolver(Interpreter interpreter) =>
            this.interpreter = interpreter;

        internal void Resolve(List<Stmt> statements)
        {
            foreach(var statement in statements)
                Resolve(statement);
        }

#nullable enable
        private void Resolve(Stmt? stmt) =>
            stmt?.accept(this);
        private void Resolve(Expr? expr) =>
            expr?.accept(this);
#nullable restore

        private void ResolveFunction(Stmt.Function function, FunctionType type)
        {
            FunctionType enclosingFunction = currentFunction;
            currentFunction = type;

            BeginScope();
            foreach(var param in function.Parameters)
            {
                Declare(param);
                Define(param);
            }
            Resolve(function.Body);
            EndScope();

            currentFunction = enclosingFunction;
        }

        private void BeginScope() =>
            scopes.Push(new Dictionary<string, bool>());

        private void EndScope() =>
            scopes.Pop();

        private void Declare(Token name)
        {
            if(scopes.Count == 0) return;

            var scope = scopes.Peek();

            if(scope.ContainsKey(name.Lexeme))
                Lox.Error(name, "Variable with this name already declared in this scope.");

            scope[name.Lexeme] = false;
        }

        private void Define(Token name)
        {
            if(scopes.Count == 0) return;
            scopes.Peek()[name.Lexeme] = true;
        }

        private void ResolveLocal(Expr expr, Token name)
        {
            int i = 0;
            foreach(var scope in scopes)
            {
                if(scope.ContainsKey(name.Lexeme))
                {
                    interpreter.Resolve(expr,i);
                    return;
                }
                ++i;
            }

            // Not found. Assume it is global.
        }
        
        public NoValue visitBlockStmt(Stmt.Block stmt)
        {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
            return null;
        }

        public NoValue visitClassStmt(Stmt.Class stmt)
        {
            Declare(stmt.Name);
            Define(stmt.Name);
            return null;
        }

        public NoValue visitExpressionStmt(Stmt.Expression stmt)
        {
            Resolve(stmt.Expr);
            return null;
        }

        public NoValue visitVarStmt(Stmt.Var stmt)
        {
            Declare(stmt.Name);
            Resolve(stmt.Initializer);
            Define(stmt.Name);
            return null;
        }

        public NoValue visitVariableExpr(Expr.Variable expr)
        {
            if(scopes.Count != 0 && scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool defined))
                if (!defined)
                    Lox.Error(expr.Name, "Cannot read local variable in its own initializer.");

            ResolveLocal(expr, expr.Name);
            return null;
        }

        public NoValue visitAssignExpr(Expr.Assign expr)
        {
            Resolve(expr.Value);
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public NoValue visitFunctionStmt(Stmt.Function stmt)
        {
            Declare(stmt.Name);
            Define(stmt.Name);

            ResolveFunction(stmt, FunctionType.FUNCTION);
            return null;
        }

        public NoValue visitIfStmt(Stmt.If stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.ThenBranch);
            Resolve(stmt.ElseBranch);
            return null;
        }

        public NoValue visitPrintStmt(Stmt.Print stmt)
        {
            Resolve(stmt.Expr);
            return null;
        }

        public NoValue visitReturnStmt(Stmt.Return stmt)
        {
            if(currentFunction == FunctionType.NONE)
                Lox.Error(stmt.Keyword, "Cannot return from top-level code.");

            Resolve(stmt.Value);
            return null;
        }

        public NoValue visitWhileStmt(Stmt.While stmt)
        {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
            return null;
        }

        public NoValue visitBinaryExpr(Expr.Binary expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public NoValue visitCallExpr(Expr.Call expr)
        {
            Resolve(expr.Callee);

            foreach(var arg in expr.Arguments)
                Resolve(arg);

            return null;
        }

        public NoValue visitGetExpr(Expr.Get expr)
        {
            Resolve(expr.Obj);
            return null;
        }

        public NoValue visitGroupingExpr(Expr.Grouping expr)
        {
            Resolve(expr.Expression);
            return null;
        }

        public NoValue visitLiteralExpr(Expr.Literal expr) => null;

        public NoValue visitLogicalExpr(Expr.Logical expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public NoValue visitSetExpr(Expr.Set expr)
        {
            Resolve(expr.Value);
            Resolve(expr.Obj);
            return null;
        }

        public NoValue visitUnaryExpr(Expr.Unary expr)
        {
            Resolve(expr.Right);
            return null;
        }
    }
}