using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Compil
{
    class InputOutput
    {
        public const byte ERRMAX = 9;

        public static char Ch;
        public static StructComp.TextPosition PositionNow;
        public static List<StructComp.Err> Err;
        public static bool IsEof;

        private static string _line;
        private static byte _lastInLine;
        private static StreamReader _file;
        private static uint _errCount;

        public static Dictionary<byte, string> ErrorTable = new Dictionary<byte, string>();

        static public void Init(string inputPath)
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"файл '{inputPath}' не найден.");
                return;
            }

            IsEof = false;
            _errCount = 0;
            PositionNow = new StructComp.TextPosition();
            Err = new List<StructComp.Err>();
            _file = new StreamReader(inputPath);

            if (!_file.EndOfStream)
            {
                _line = _file.ReadLine() + " ";
                _lastInLine = (byte)(_line.Length - 1);
                PositionNow = new StructComp.TextPosition(1, 0);
                Ch = _line[0];
            }
            else
            {
                _line = " ";
                _lastInLine = 0;
                Ch = (char)0;
                IsEof = true;
            }
        }

        static public void NextCh()
        {
            if (IsEof)
                return;

            if (PositionNow.CharNumber == _lastInLine)
            {
                ListThisLine();
                if (Err.Count > 0)
                    ListErrors();

                ReadNextLine();

                if (IsEof)
                    return;

                PositionNow = new StructComp.TextPosition(PositionNow.LineNumber + 1, 0);
            }
            else
            {
                PositionNow = new StructComp.TextPosition(PositionNow.LineNumber, (byte)(PositionNow.CharNumber + 1));
            }

            Ch = _line[PositionNow.CharNumber];
        }

        private static void ListThisLine()
        {
            string displayLine = "      " + _line.TrimEnd();
            Console.WriteLine(displayLine);
        }

        private static void ReadNextLine()
        {
            if (!_file.EndOfStream)
            {
                _line = _file.ReadLine() + " ";
                _lastInLine = (byte)(_line.Length - 1);
                Err = new List<StructComp.Err>();
            }
            else
            {
                End();
            }
        }

        private static void End()
        {
            Ch = (char)0;
            IsEof = true;
            _file?.Close();
            Console.WriteLine($"\n=== Компиляция завершена: ошибок — {_errCount} ===");
        }

        private static void ListErrors()
        {
            foreach (StructComp.Err item in Err)
            {
                _errCount++;
                string errNum = _errCount < 10 ? $"0{_errCount}" : $"{_errCount}";
                string s = $"**{errNum}**";

                int caretPos = (int)item.ErrorPosition.CharNumber + 6;
                s = s.PadRight(caretPos) + "^";

                s += $" ошибка {item.ErrorCode}";

                if (ErrorTable.ContainsKey(item.ErrorCode))
                    s += $" — {ErrorTable[item.ErrorCode]}";

                Console.WriteLine(s);
            }
        }

        static public void Error(StructComp.TextPosition position, byte errorCode)
        {
            if (Err == null)
                Err = new List<StructComp.Err>();

            if (Err.Count <= ERRMAX)
                Err.Add(new StructComp.Err(position, errorCode));
        }

        static public bool LoadErrorTable(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    CreateDefaultErrorTable(filename);
                }

                ErrorTable.Clear();
                string[] lines = File.ReadAllLines(filename, Encoding.UTF8);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;

                    int pipeIndex = line.IndexOf('|');
                    if (pipeIndex > 0)
                    {
                        string codeStr = line.Substring(0, pipeIndex).Trim();
                        string message = line.Substring(pipeIndex + 1).Trim();

                        if (byte.TryParse(codeStr, out byte code))
                            ErrorTable[code] = message;
                    }
                }

                Console.WriteLine($"Загружено {ErrorTable.Count} кодов ошибок");
                return ErrorTable.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки таблицы ошибок: {ex.Message}");
                return false;
            }
        }

        private static void CreateDefaultErrorTable(string filename)
        {
            string[] defaultErrors = {
                "1|Недопустимый символ",
                "2|Ожидался идентификатор",
                "3|Ожидался оператор",
                "4|Числовая константа вне диапазона",
                "5|Незакрытый комментарий"
            };
            File.WriteAllLines(filename, defaultErrors, Encoding.UTF8);
        }

        // Проверка символа
        static public byte CheckChar(char ch)
        {
            // Допустимые символы
            if (char.IsLetterOrDigit(ch))
                return 0;

            // Пробельные символы
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
                return 0;

            // Допустимые спецсимволы Паскаля
            string valid = "+-*/=<>(){}[];:,.^#$'\"";
            if (valid.Contains(ch))
                return 0;

            // Недопустимый символ
            return 1;
        }
    }
}