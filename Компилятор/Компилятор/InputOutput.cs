using System;
using System.Collections.Generic;
using System.IO;

namespace Компилятор
{
    public struct TextPosition
    {
        private uint _lineNumber;
        private byte _charNumber;

        public uint LineNumber
        {
            get
            {
                return _lineNumber;
            }
            set
            {
                _lineNumber = value;
            }
        }

        public byte CharNumber
        {
            get
            {
                return _charNumber;
            }
            set
            {
                _charNumber = value;
            }
        }

        public TextPosition(uint ln = 0, byte c = 0)
        {
            _lineNumber = ln;
            _charNumber = c;
        }
    }

    public struct Err
    {
        private TextPosition _errorPosition;
        private byte _errorCode;

        public TextPosition ErrorPosition
        {
            get
            {
                return _errorPosition;
            }
            set
            {
                _errorPosition = value;
            }
        }

        public byte ErrorCode
        {
            get
            {
                return _errorCode;
            }
            set
            {
                _errorCode = value;
            }
        }

        public Err(TextPosition errorPosition, byte errorCode)
        {
            _errorPosition = errorPosition;
            _errorCode = errorCode;
        }
    }

    public class InputOutput
    {
        private const byte ErrMax = 9;

        private static char _currentChar;
        private static TextPosition _positionNow;
        private static string _line;
        private static int _lastInLine;
        private static int _charIndex;
        private static StreamReader _file;
        private static uint _errorCount;
        private static Dictionary<uint, List<Err>> _errorsByLine;

        public static char CurrentChar
        {
            get
            {
                return _currentChar;
            }
        }

        public static TextPosition PositionNow
        {
            get
            {
                return _positionNow;
            }
        }

        static InputOutput()
        {
            _currentChar = ' ';
            _positionNow = new TextPosition();
            _line = null;
            _lastInLine = -1;
            _charIndex = 0;
            _file = null;
            _errorCount = 0;
            _errorsByLine = null;
        }

        public static void OpenFile(string path)
        {
            _file = new StreamReader(path);
            _errorsByLine = new Dictionary<uint, List<Err>>();
            _positionNow = new TextPosition(1, 0);
            _line = null;
            _lastInLine = -1;
            _charIndex = 0;
            _errorCount = 0;
            _currentChar = ' ';
        }

        public static void NextCh()
        {
            if (_line == null)
            {
                ReadNextLine();
                _positionNow.LineNumber = 1;
                _charIndex = 0;
                _positionNow.CharNumber = 0;
                if (_line == null)
                {
                    _currentChar = '\0';
                    return;
                }
            }

            if (_charIndex > _lastInLine)
            {
                ListThisLine();
                FlushErrorsForLine(_positionNow.LineNumber);

                ReadNextLine();
                _positionNow.LineNumber++;
                _charIndex = 0;
                _positionNow.CharNumber = 0;

                if (_line == null)
                {
                    _currentChar = '\0';
                    return;
                }

                if (_lastInLine < 0)
                {
                    _currentChar = ' ';
                    return;
                }
            }

            _currentChar = _line[_charIndex];
            _charIndex++;
            _positionNow.CharNumber = _charIndex <= 255 ? (byte)_charIndex : (byte)255;
        }

        private static void ListThisLine()
        {
            if (_line != null)
            {
                Console.WriteLine($"{_positionNow.LineNumber,4}: {_line}");
            }
        }

        private static void FlushErrorsForLine(uint lineNumber)
        {
            if (!_errorsByLine.TryGetValue(lineNumber, out List<Err> errs))
            {
                return;
            }
            foreach (Err item in errs)
            {
                PrintError(item);
            }
            _errorsByLine.Remove(lineNumber);
        }

        private static void ReadNextLine()
        {
            if (_file != null && !_file.EndOfStream)
            {
                _line = _file.ReadLine();
                _lastInLine = (_line != null && _line.Length > 0) ? _line.Length - 1 : -1;
            }
            else
            {
                End();
                _line = null;
                _lastInLine = -1;
            }
        }

        private static void End()
        {
            if (_line != null)
            {
                ListThisLine();
                FlushErrorsForLine(_positionNow.LineNumber);
            }

            if (_errorsByLine != null)
            {
                foreach (var kv in _errorsByLine)
                {
                    foreach (Err item in kv.Value)
                    {
                        PrintError(item);
                    }
                }
                _errorsByLine.Clear();
            }

            Console.WriteLine($"\nКомпиляция завершена: ошибок — {_errorCount}!");
        }

        public static string GetErrorMessage(byte code)
        {
            switch (code)
            {
                case 103:
                    {
                        return "незакрытая строковая константа";
                    }
                case 104:
                    {
                        return "ожидался другой символ";
                    }
                case 105:
                    {
                        return "ожидался тип переменной (integer, real, boolean или char)";
                    }
                case 106:
                    {
                        return "ожидался TO или DOWNTO";
                    }
                case 107:
                    {
                        return "ожидался оператор";
                    }
                case 108:
                    {
                        return "ожидалось двоеточие ':'";
                    }
                case 109:
                    {
                        return "ожидалось ':=' (оператор присваивания)";
                    }
                case 201:
                    {
                        return "необъявленная переменная";
                    }
                case 202:
                    {
                        return "недопустимый символ";
                    }
                case 203:
                    {
                        return "число вне допустимого диапазона (max = 32767)";
                    }
                case 204:
                    {
                        return "повторное объявление переменной";
                    }
                case 205:
                    {
                        return "несовместимые типы";
                    }
                case 206:
                    {
                        return "операция недопустима для данного типа";
                    }
                default:
                    {
                        return "неизвестная ошибка";
                    }
            }
        }

        public static void Error(byte errorCode, TextPosition position)
        {
            if (_errorsByLine == null)
            {
                _errorsByLine = new Dictionary<uint, List<Err>>();
            }

            uint errLine = position.LineNumber;

            if (errLine < _positionNow.LineNumber)
            {
                PrintError(new Err(position, errorCode));
                return;
            }

            if (!_errorsByLine.TryGetValue(errLine, out List<Err> list))
            {
                list = new List<Err>();
                _errorsByLine[errLine] = list;
            }

            if (list.Count <= ErrMax)
            {
                list.Add(new Err(position, errorCode));
            }
        }

        private static void PrintError(Err item)
        {
            _errorCount++;
            string counter = _errorCount < 10 ? $"**0{_errorCount}**" : $"**{_errorCount}**";
            string indent = new string(' ', 6 + item.ErrorPosition.CharNumber);
            Console.WriteLine($"{counter}{indent}^ ошибка код {item.ErrorCode}: {GetErrorMessage(item.ErrorCode)}");
        }

        public static char PeekNext()
        {
            if (_line != null && _charIndex <= _lastInLine)
            {
                return _line[_charIndex];
            }
            return '\0';
        }

        public static bool IsEof()
        {
            return _line == null && (_file == null || _file.EndOfStream);
        }

        public static void CloseFile()
        {
            if (_file != null)
            {
                _file.Close();
                _file = null;
            }
        }
    }
}