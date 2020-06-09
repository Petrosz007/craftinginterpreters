using System.Linq;
using System;
using System.Collections.Generic;

namespace cslox
{
    public class LoxFunction : LoxCallable
    {
        private Stmt.Function declaration;
        private Environment closure;

        public int Arity => declaration.Parameters.Count;
        public Func<Interpreter, List<object>, object> Call => CallFunction;

        public LoxFunction(Stmt.Function declaration, Environment closure) =>
            (this.declaration, this.closure) = (declaration, closure);

        private object CallFunction(Interpreter interpreter, List<object> arguments)
        {
            Environment environment = new Environment(closure);

            for(int i = 0; i < declaration.Parameters.Count; ++i)
                environment.Define(declaration.Parameters[i].Lexeme, arguments[i]);

            try {

                interpreter.ExecuteBlock(declaration.Body, environment);
            }
            catch(Return returnValue) 
            {
                return returnValue.Value;
            }

            return null;
        }

        public override string ToString() => 
            $"<fn {declaration.Name.Lexeme}>";
    }
}