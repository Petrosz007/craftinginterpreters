using System.IO;
using System;
using System.Collections.Generic;

namespace cslox
{
    class Lox
    {
        private static readonly Interpreter interpreter = new Interpreter();
        private static bool HadError { get; set; } = false;
        private static bool HadRuntimeError { get; set; } = false;
        static void Main(string[] args)
        {
            if(args.Length > 1)
            {
                Console.WriteLine("Usage: cslox [script]");
                System.Environment.Exit(64);
            }
            else if(args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
            }
        }

        private static void RunFile(string path)
        {
            string text = File.ReadAllText(path);
            Run(text);

            if(HadError)
            {
                System.Environment.Exit(65);
            }
            if(HadRuntimeError)
            {
                System.Environment.Exit(70);
            }
        }

        private static void RunPrompt()
        {
            while(true)
            {
                Console.Write("> ");
                Run(Console.ReadLine());
                HadError = false;
            }
        }

        private static void Run(string source)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.scanTokens();
            Parser parser = new Parser(tokens);
            List<Stmt> statements = parser.Parse();

            // Stop if there was a syntax error
            if(HadError) return;

            interpreter.Interpret(statements);
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        private static void Report(int line, string where, string message)
        {
            Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
            HadError = true;
        }

        public static void Error(Token token, string message)
        {
            if(token.Type == TokenType.EOF)
            {
                Report(token.Line, " at end", message);
            }
            else
            {
                Report(token.Line, $"at '{token.Lexeme}'", message);
            }
        }

        public static void RuntimeError(RuntimeError error)
        {
            Console.Error.WriteLine($"{error.Message}\n[line {error.Token.Line}]");
            HadRuntimeError = true;
        }
    }
}
