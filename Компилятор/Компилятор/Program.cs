using System;
using System.IO;

namespace Компилятор
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Pascal Compiler - Variant 7\n");

            string path;

            if (args.Length > 0)
            {
                path = args[0];
            }
            else
            {
                Console.Write("Введите путь к файлу: ");
                path = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Console.WriteLine($"Файл не найден: {path}");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Source:");
            Console.WriteLine(File.ReadAllText(path));
            Console.WriteLine();

            InputOutput.OpenFile(path);
            InputOutput.NextCh();

            LexicalAnalyzer lexer = new LexicalAnalyzer();
            Parser parser = new Parser(lexer);
            parser.Parse();

            InputOutput.CloseFile();

            Console.WriteLine("\nDone.");
            Console.ReadKey();
        }
    }
}