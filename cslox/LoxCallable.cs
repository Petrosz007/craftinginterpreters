using System;
using System.Collections.Generic;

namespace cslox
{
    public interface LoxCallable
    {
        int Arity { get; }
        Func<Interpreter, List<object>, object> Call { get; }
    }

    public class LoxPrimitive : LoxCallable
    {
        public int Arity { get; set; }
        public Func<Interpreter, List<object>, object> Call { get; set; }

        public override string ToString() =>
            "<native fn>";
    }
}