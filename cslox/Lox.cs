using System.IO;
using System;
using System.Collections.Generic;

namespace cslox
{
    class Lox
    {
        private static bool HadError { get; set; }
        static void Main(string[] args)
        {
            if(args.Length > 1)
            {
                Console.WriteLine("Usage: cslox [script]");
                Environment.Exit(64);
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
                Environment.Exit(65);
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

            foreach(var token in tokens)
            {
                Console.WriteLine(token);
            } 
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
    }
}
