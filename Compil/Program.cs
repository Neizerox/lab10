using System;

namespace Compil
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== МОДУЛЬ ВВОДА-ВЫВОДА (Задание 0) ===\n");

            // Загрузка таблицы ошибок
            InputOutput.LoadErrorTable("ErrorCodes.txt");
            Console.WriteLine();

            // Путь к тестовому файлу
            string inputFile = "test.pas";
            InputOutput.Init(inputFile);

            if (InputOutput.IsEof)
            {
                Console.WriteLine("Файл пуст или не найден!");
                return;
            }

            Console.WriteLine("Анализ файла (поиск недопустимых символов):\n");

            // Построчный анализ символов
            while (!InputOutput.IsEof && InputOutput.Ch != '\0')
            {
                byte errorCode = InputOutput.CheckChar(InputOutput.Ch);
                if (errorCode != 0)
                {
                    InputOutput.Error(InputOutput.PositionNow, errorCode);
                }
                InputOutput.NextCh();
            }

            Console.WriteLine("\nАнализ завершён.");
        }
    }
}