using System;
using System.Collections.Generic;

namespace cslox
{
    public class LoxClass : LoxCallable
    {
        public string Name { get; }

        public int Arity => 0;
        public Func<Interpreter, List<object>, object> Call => CallClass;

        public LoxClass(string name) =>
            Name = name;

        public override string ToString() => Name;

        private object CallClass(Interpreter interpreter, List<object> arguments)
        {
            var instance = new LoxInstance(this);
            return instance;
        } 
    }
}
