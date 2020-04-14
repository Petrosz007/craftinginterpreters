using System.Collections.Generic;
namespace cslox
{
    public class Environment
    {
        private readonly Environment enclosing;
        private Dictionary<string, object> values;

        public Environment(Environment enclosing = null)
        {
            this.enclosing = enclosing;
            values = new Dictionary<string, object>();
        }

        public void Define(string name, object value) =>
            values[name] = value;

        public void Assign(Token name, object value)
        {
            if(values.ContainsKey(name.Lexeme))
            {
                values[name.Lexeme] = value;
                return;
            }

            if(enclosing != null)
            {
                enclosing.Assign(name, value);
                return;
            }

            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        }

        public object Get(Token name)
        {
            object value;
            if(values.TryGetValue(name.Lexeme, out value))
            {
                return value;
            }

            if(enclosing != null)
            {
                return enclosing.Get(name);
            }

            throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
        }
    }
}