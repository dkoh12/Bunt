using System;
using System.IO;

namespace bunt
{
    public class Bunt
    {
        private static Interpreter interpreter = new Interpreter();

        static bool hadError = false;
        static bool hadRuntimeError = false;

        public static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Pass in a single file or no arguments to bunt.");
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                runFile(args[0]);
            }
            else
            {
                runPrompt();
            }
        }

        private static void runFile(string path)
        {
            // read a file and run

            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);

                run(content);

                if (hadError) Environment.Exit(65);

                if (hadRuntimeError) Environment.Exit(70);
            }
            else
            {
                Environment.Exit(65);
            }
        }

        // REPL
        private static void runPrompt()
        {
            // read a stream from user

            for (; ; )
            {
                Console.Write("> ");
                string? line = Console.ReadLine();
                if (line == null)
                {
                    break;
                }
                run(line);
                hadError = false;
            }
        }

        private static void run(String source)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.scanTokens();

            Parser parser = new Parser(tokens);
                        
            List<Stmt> statements = parser.parse();

            if (hadError) return;
                        
            Resolver resolver = new Resolver(interpreter);
            resolver.resolve(statements);

            // could add a type checker here if Bunt had static types

            if (hadError) return;

            interpreter.interpret(statements);
            
        }


        #region Error Handling

        static void error(int line, string message)
        {
            report(line, "", message);
        }

        public static void error(Token token, string message)
        {
            if (token.type == TokenType.EOF)
            {
                report(token.line, " at end", message);
            } 
            else
            {
                report(token.line, " at '" + token.lexeme + "'", message);
            }
        }

        public static void runtimeError(RuntimeError error)
        {
            Console.Write(error.Message + "\n[line " + error.token.line + "]");
            hadRuntimeError = true;
        }

        static void report(int line, string where, string message)
        {
            Console.WriteLine("[line " + line + "] Error" + where + ": " + message);
            hadError = true;
        }

        #endregion
    }

}