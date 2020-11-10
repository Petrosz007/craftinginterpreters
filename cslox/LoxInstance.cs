using System.Collections.Generic;
namespace cslox
{
    public class LoxInstance
    {
        private LoxClass klass;
        private Dictionary<string, object> fields = new Dictionary<string, object>();

        public LoxInstance(LoxClass klass) =>
            this.klass = klass;

        public override string ToString() =>
            $"{klass.Name} instance";

        public object Get(Token name)
        {
            object obj;
            if(fields.TryGetValue(name.Lexeme, out obj))
                return obj;

            throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
        }

        public void Set(Token name, object value) =>
            fields[name.Lexeme] = value;
    }
}