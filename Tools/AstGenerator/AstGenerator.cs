using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.IO;
using System;
using System.Collections.Generic;

namespace cslox.Tools.AstGenerator
{
    public static class StringExtensions 
    {
        public static string Capitalise(this string input) =>
            input switch {
                null => throw new ArgumentException(nameof(input)),
                ""   => "",
                _    => input.First().ToString().ToUpper() + input.Substring(1),
    };
    class AstGenerator
    {
        }
        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.Error.WriteLine("Usage: ast-generator <output directory>");
                Environment.Exit(1);
            }

            string outputDir =  args[0];

            DefineAst(outputDir, "Expr", new List<string>{
                "Assign   : Token name, Expr value",
                "Binary   : Expr left, Token binop, Expr right",
                "Call     : Expr callee, Token paren, List<Expr> arguments",
                "Get      : Expr obj, Token name",
                "Grouping : Expr expression",
                "Literal  : object value",
                "Logical  : Expr left, Token op, Expr right",
                "Set      : Expr obj, Token name, Expr value",
                "Unary    : Token op, Expr right",
                "Variable : Token name",
            });

            DefineAst(outputDir, "Stmt", new List<string>{
                "Block      : List<Stmt> statements",
                "Class      : Token name, List<Stmt.Function> methods",
                "If         : Expr condition, Stmt thenBranch, Stmt elseBranch",
                "Expression : Expr expr",
                "Function   : Token name, List<Token> parameters, List<Stmt> body",
                "Print      : Expr expr",
                "Return     : Token keyword, Expr value",
                "Var        : Token name, Expr initializer",
                "While      : Expr condition, Stmt body",
            });
        }

        private static void DefineAst(string outputDir, string baseName, List<string> types)
        {
            var path = Path.Combine(outputDir, $"{baseName}.cs");

            using StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8);
            
            writer.WriteLine("// Auto generated with Tools/AstGenerator");
            writer.WriteLine("");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("");
            writer.WriteLine("namespace cslox {");
            writer.WriteLine($"\tpublic abstract class {baseName} {{");

            DefineVisitor(writer, baseName, types);

            foreach(var type in types)
            {
                string className = type.Split(':')[0].Trim();
                string fields = type.Split(':')[1].Trim();
                DefineType(writer, baseName, className, fields);
            }

            writer.WriteLine("");
            writer.WriteLine("\t\tpublic abstract R accept<R>(Visitor<R> visitor);");

            writer.WriteLine("\t}");
            writer.WriteLine("}");
        }

        private static void DefineVisitor(StreamWriter writer, string baseName, List<string> types)
        {
            writer.WriteLine("\t\tpublic interface Visitor<R> {");

            foreach(var type in types)
            {
                string typeName = type.Split(":")[0].Trim();
                writer.WriteLine($"\t\t\tR visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
            }

            writer.WriteLine("\t\t}");
        }

        private static void DefineType(StreamWriter writer, string baseName, string className, string fieldList)
        {
            writer.WriteLine($"\t\tpublic class {className} : {baseName} {{");

            var fields = fieldList.Split(", ");
            foreach(var field in fields)
            {
                var split = field.Split(" ");
                writer.WriteLine($"\t\t\tpublic {split[0]} {split[1].Capitalise()} {{ get; }}");
            }

            writer.WriteLine($"\t\t\tpublic {className}({fieldList}) {{");

            foreach(var field in fields)
            {
                string name = field.Split(" ")[1];
                writer.WriteLine($"\t\t\t\t{name.Capitalise()} = {name};");
            }
            
            writer.WriteLine("\t\t\t}");

            writer.WriteLine("");
            writer.WriteLine("\t\t\tpublic override R accept<R>(Visitor<R> visitor) =>");
            writer.WriteLine($"\t\t\t\tvisitor.visit{className}{baseName}(this);");

            writer.WriteLine("\t\t\t");
            writer.WriteLine("\t\t}");
        }
    }
}
