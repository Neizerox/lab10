using System;
using System.Collections.Generic;

namespace Компилятор
{
    //Описание переменных простых типов 
    //Описание процедур (с параметрами-значениями и var-параметрами)
    //Операторы: присваивания, вызов процедуры, составной (begin...end)
    public class Parser
    {
        private LexicalAnalyzer _lexer;
        private SymbolTable _symbols;
        private byte _current;

        // Символы, с которых может начинаться оператор
        private static readonly HashSet<byte> _statementStartSymbols;
        // Символы, на которых нейтрализация ошибок останавливается
        private static readonly HashSet<byte> _stopSymbols;

        static Parser()
        {
            _statementStartSymbols = new HashSet<byte>
            {
                LexicalAnalyzer.Ident,
                LexicalAnalyzer.BeginSy,
                LexicalAnalyzer.WritelnSy,
            };

            _stopSymbols = new HashSet<byte>
            {
                LexicalAnalyzer.Semicolon,
                LexicalAnalyzer.EndSy,
                LexicalAnalyzer.Eof,
                LexicalAnalyzer.Point,
            };
        }

        public Parser(LexicalAnalyzer lex)
        {
            _lexer = lex;
            _symbols = new SymbolTable();
            _current = 0;
            Advance();
        }

        // Утилиты 

        private void Advance()
        {
            _current = _lexer.NextSym();
        }

        private bool Match(byte expected)
        {
            if (_current == expected)
            {
                Advance();
                return true;
            }
            InputOutput.Error(104, _lexer.Token);
            return false;
        }

        private bool MatchColon()
        {
            if (_current == LexicalAnalyzer.Colon)
            {
                Advance();
                return true;
            }
            InputOutput.Error(108, _lexer.Token);
            return false;
        }

        private bool MatchAssign()
        {
            if (_current == LexicalAnalyzer.Assign)
            {
                Advance();
                return true;
            }
            InputOutput.Error(109, _lexer.Token);
            return false;
        }

        private void SkipTo(HashSet<byte> stop)
        {
            while (!stop.Contains(_current) && _current != LexicalAnalyzer.Eof)
                Advance();
        }

        private void SkipToStatementEnd()
        {
            SkipTo(_stopSymbols);
        }

        // Точка входа 

        public void Parse()
        {
            Program();
        }

        // Программа

        private void Program()
        {
            if (!Match(LexicalAnalyzer.ProgramSy))
            {
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.Ident,
                    LexicalAnalyzer.Semicolon,
                    LexicalAnalyzer.VarSy,
                    LexicalAnalyzer.ProcedureSy,
                    LexicalAnalyzer.BeginSy
                });
            }

            if (_current == LexicalAnalyzer.Ident)
                Advance();
            else
                InputOutput.Error(104, _lexer.Token);

            if (!Match(LexicalAnalyzer.Semicolon))
            {
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.VarSy,
                    LexicalAnalyzer.ProcedureSy,
                    LexicalAnalyzer.BeginSy,
                    LexicalAnalyzer.Eof
                });
            }

            if (_current == LexicalAnalyzer.VarSy)
                VarDeclarations(_symbols);

            while (_current == LexicalAnalyzer.ProcedureSy)
                ProcedureDeclaration();

            if (!Match(LexicalAnalyzer.BeginSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> { LexicalAnalyzer.BeginSy, LexicalAnalyzer.Eof });
                if (_current == LexicalAnalyzer.BeginSy)
                    Advance();
            }

            StatementSequence(_symbols);

            if (!Match(LexicalAnalyzer.EndSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> { LexicalAnalyzer.Point, LexicalAnalyzer.Eof });
            }

            Match(LexicalAnalyzer.Point);
        }

        // Объявление переменных 

        private void VarDeclarations(SymbolTable table)
        {
            Match(LexicalAnalyzer.VarSy);

            while (_current == LexicalAnalyzer.Ident)
            {
                TextPosition declPos = _lexer.Token;
                List<string> names = IdentifierList();

                if (!MatchColon())
                {
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.IntegerSy, LexicalAnalyzer.RealSy,
                        LexicalAnalyzer.BooleanSy, LexicalAnalyzer.CharSy,
                        LexicalAnalyzer.Semicolon, LexicalAnalyzer.BeginSy,
                        LexicalAnalyzer.ProcedureSy
                    });
                }

                string type = GetCurrentType();
                if (type == "unknown")
                {
                    InputOutput.Error(105, _lexer.Token);
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.Semicolon, LexicalAnalyzer.BeginSy,
                        LexicalAnalyzer.ProcedureSy, LexicalAnalyzer.Eof
                    });
                }
                else
                {
                    Advance();
                }

                if (!Match(LexicalAnalyzer.Semicolon))
                {
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.Ident, LexicalAnalyzer.BeginSy,
                        LexicalAnalyzer.ProcedureSy, LexicalAnalyzer.Eof
                    });
                }

                if (type != "unknown")
                {
                    foreach (string name in names)
                    {
                        if (!table.Add(name, type, declPos.LineNumber, SymbolKind.Variable))
                            InputOutput.Error(204, declPos);
                    }
                }
            }
        }

        // Объявление процедуры 

        private void ProcedureDeclaration()
        {
            Match(LexicalAnalyzer.ProcedureSy);

            string procName = "";
            TextPosition procPos = _lexer.Token;

            if (_current == LexicalAnalyzer.Ident)
            {
                procName = _lexer.AddrName;
                Advance();
            }
            else
            {
                InputOutput.Error(104, _lexer.Token);
            }

            SymbolTable localTable = new SymbolTable();
            var paramList = new List<(string, string)>();

            if (_current == LexicalAnalyzer.LeftPar)
            {
                Advance(); // (

                while (_current == LexicalAnalyzer.Ident || _current == LexicalAnalyzer.VarSy)
                {
                    if (_current == LexicalAnalyzer.VarSy)
                        Advance(); // пропускаем var (параметр по ссылке — принимаем синтаксически)

                    TextPosition paramPos = _lexer.Token;
                    List<string> paramNames = IdentifierList();
                    MatchColon();

                    string paramType = GetCurrentType();
                    if (paramType == "unknown")
                    {
                        InputOutput.Error(105, _lexer.Token);
                        SkipTo(new HashSet<byte> {
                            LexicalAnalyzer.Semicolon, LexicalAnalyzer.RightPar,
                            LexicalAnalyzer.BeginSy, LexicalAnalyzer.Eof
                        });
                    }
                    else
                    {
                        Advance();
                    }

                    if (paramType != "unknown")
                    {
                        foreach (string pn in paramNames)
                        {
                            if (!localTable.Add(pn, paramType, paramPos.LineNumber, SymbolKind.Variable))
                                InputOutput.Error(204, paramPos);
                            paramList.Add((pn, paramType));
                        }
                    }

                    if (_current == LexicalAnalyzer.Semicolon)
                        Advance();
                    else
                        break;
                }

                if (!Match(LexicalAnalyzer.RightPar))
                {
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.Semicolon, LexicalAnalyzer.BeginSy,
                        LexicalAnalyzer.VarSy, LexicalAnalyzer.Eof
                    });
                }
            }

            // Регистрируем процедуру в глобальной таблице
            if (procName != "")
            {
                if (!_symbols.Add(procName, "", procPos.LineNumber, SymbolKind.Procedure, paramList))
                    InputOutput.Error(204, procPos);
            }

            if (!Match(LexicalAnalyzer.Semicolon))
            {
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.VarSy, LexicalAnalyzer.BeginSy, LexicalAnalyzer.Eof
                });
            }

            if (_current == LexicalAnalyzer.VarSy)
                VarDeclarations(localTable);

            if (!Match(LexicalAnalyzer.BeginSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> { LexicalAnalyzer.BeginSy, LexicalAnalyzer.Eof });
                if (_current == LexicalAnalyzer.BeginSy)
                    Advance();
            }

            StatementSequence(new CompositeSymbolTable(_symbols, localTable));

            if (!Match(LexicalAnalyzer.EndSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.Semicolon, LexicalAnalyzer.ProcedureSy,
                    LexicalAnalyzer.BeginSy, LexicalAnalyzer.Eof
                });
            }

            // ; после end процедуры
            if (_current == LexicalAnalyzer.Semicolon)
                Advance();
        }

        // Последовательность операторов 

        private void StatementSequence(SymbolTable table)
        {
            Statement(table);

            
            while (_current != LexicalAnalyzer.EndSy &&
                   _current != LexicalAnalyzer.Point &&
                   _current != LexicalAnalyzer.Eof)
            {
                if (_current == LexicalAnalyzer.Semicolon)
                {
                    Advance(); 

                    
                    if (_current == LexicalAnalyzer.EndSy)
                        break;

                    Statement(table);
                }
                else
                {
                    
                    InputOutput.Error(104, _lexer.Token);

                    // Нейтрализация: если это начало оператора, продолжаем парсинг;
                    // иначе пропускаем до ближайшего разделителя.
                    if (_statementStartSymbols.Contains(_current))
                    {
                        Statement(table);
                    }
                    else
                    {
                        SkipToStatementEnd();
                        if (_current == LexicalAnalyzer.Semicolon)
                            Advance();
                    }
                }
            }
        }

        // Оператор

        private void Statement(SymbolTable table)
        {
            switch (_current)
            {
                case LexicalAnalyzer.Ident:
                    {
                        string name = _lexer.AddrName;
                        if (table.Exists(name) && table.GetKind(name) == SymbolKind.Procedure)
                            ProcedureCall(table);
                        else
                            Assignment(table);
                        break;
                    }
                case LexicalAnalyzer.BeginSy:
                    CompoundStatement(table);
                    break;

                case LexicalAnalyzer.WritelnSy:
                    Writeln(table);
                    break;

                default:
                    if (!_stopSymbols.Contains(_current) &&
                        !_statementStartSymbols.Contains(_current))
                    {
                        InputOutput.Error(107, _lexer.Token);
                        SkipToStatementEnd();
                    }
                    break;
            }
        }

        // Составной оператор 

        private void CompoundStatement(SymbolTable table)
        {
            Match(LexicalAnalyzer.BeginSy);
            StatementSequence(table);

            if (!Match(LexicalAnalyzer.EndSy))
            {
                InputOutput.Error(104, _lexer.Token);
                SkipTo(new HashSet<byte> {
                    LexicalAnalyzer.EndSy, LexicalAnalyzer.Semicolon,
                    LexicalAnalyzer.Point, LexicalAnalyzer.Eof
                });
                if (_current == LexicalAnalyzer.EndSy)
                    Advance();
            }
        }

        //Оператор присваивания

        private void Assignment(SymbolTable table)
        {
            string name = _lexer.AddrName;
            TextPosition pos = _lexer.Token;

            if (!table.Exists(name))
                InputOutput.Error(201, pos);

            Advance();

            if (!MatchAssign())
            {
                SkipToStatementEnd();
                return;
            }

            string exprType = Expression(table);

            if (table.Exists(name))
                CheckTypeCompatibility(table.GetType(name), exprType, pos);
        }

        // Вызов процедуры

        private void ProcedureCall(SymbolTable table)
        {
            string name = _lexer.AddrName;
            TextPosition pos = _lexer.Token;
            Symbol? procSym = table.Get(name);

            Advance();

            var argTypes = new List<string>();

            if (_current == LexicalAnalyzer.LeftPar)
            {
                Advance();

                if (_current != LexicalAnalyzer.RightPar)
                {
                    argTypes.Add(Expression(table));
                    while (_current == LexicalAnalyzer.Comma)
                    {
                        Advance();
                        argTypes.Add(Expression(table));
                    }
                }

                if (!Match(LexicalAnalyzer.RightPar))
                {
                    InputOutput.Error(104, _lexer.Token);
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.RightPar, LexicalAnalyzer.Semicolon,
                        LexicalAnalyzer.EndSy, LexicalAnalyzer.Eof
                    });
                    if (_current == LexicalAnalyzer.RightPar)
                        Advance();
                }
            }

            if (procSym != null)
            {
                var fp = procSym.Params;
                if (argTypes.Count != fp.Count)
                {
                    InputOutput.Error(107, pos); // несоответствие числа аргументов
                }
                else
                {
                    for (int i = 0; i < fp.Count; i++)
                        CheckTypeCompatibility(fp[i].paramType, argTypes[i], pos);
                }
            }
        }

        //  writeln 

        private void Writeln(SymbolTable table)
        {
            Match(LexicalAnalyzer.WritelnSy);

            if (_current == LexicalAnalyzer.LeftPar)
            {
                Advance();
                if (_current != LexicalAnalyzer.RightPar)
                {
                    Expression(table);
                    while (_current == LexicalAnalyzer.Comma)
                    {
                        Advance();
                        Expression(table);
                    }
                }
                if (!Match(LexicalAnalyzer.RightPar))
                {
                    InputOutput.Error(104, _lexer.Token);
                    SkipToStatementEnd();
                }
            }
        }

        // Выражение


        private string Expression(SymbolTable table)
        {
            string t = Term(table);
            while (_current == LexicalAnalyzer.Plus ||
                   _current == LexicalAnalyzer.Minus ||
                   _current == LexicalAnalyzer.OrSy)
            {
                Advance();
                string t2 = Term(table);
                t = MergeTypes(t, t2);
            }
            return t;
        }

        private string Term(SymbolTable table)
        {
            string t = Factor(table);
            while (_current == LexicalAnalyzer.Star ||
                   _current == LexicalAnalyzer.Slash ||
                   _current == LexicalAnalyzer.DivSy ||
                   _current == LexicalAnalyzer.ModSy ||
                   _current == LexicalAnalyzer.AndSy)
            {
                Advance();
                string t2 = Factor(table);
                t = MergeTypes(t, t2);
            }
            return t;
        }

        private string Factor(SymbolTable table)
        {
            if (_current == LexicalAnalyzer.Minus)
            {
                Advance();
                return Factor(table);
            }

            if (_current == LexicalAnalyzer.NotSy)
            {
                Advance();
                string inner = Factor(table);
                if (inner != "boolean")
                    InputOutput.Error(206, _lexer.Token);
                return "boolean";
            }

            if (_current == LexicalAnalyzer.Ident)
            {
                string name = _lexer.AddrName;
                TextPosition pos = _lexer.Token;
                if (!table.Exists(name))
                    InputOutput.Error(201, pos);
                Advance();
                return table.GetType(name);
            }

            if (_current == LexicalAnalyzer.IntC) { Advance(); return "integer"; }
            if (_current == LexicalAnalyzer.FloatC) { Advance(); return "real"; }
            if (_current == LexicalAnalyzer.TrueSy ||
                _current == LexicalAnalyzer.FalseSy) { Advance(); return "boolean"; }

            if (_current == LexicalAnalyzer.StringSy)
            {
                string? sv = _lexer.StringValue;
                Advance();
                return (sv != null && sv.Length == 1) ? "char" : "string";
            }

            if (_current == LexicalAnalyzer.LeftPar)
            {
                Advance();
                string t = Expression(table);
                if (!Match(LexicalAnalyzer.RightPar))
                {
                    InputOutput.Error(104, _lexer.Token);
                    SkipTo(new HashSet<byte> {
                        LexicalAnalyzer.RightPar, LexicalAnalyzer.Semicolon,
                        LexicalAnalyzer.EndSy, LexicalAnalyzer.Eof
                    });
                    if (_current == LexicalAnalyzer.RightPar)
                        Advance();
                }
                return t;
            }

            InputOutput.Error(104, _lexer.Token);
            SkipToStatementEnd();
            return "";
        }

        //  Вспомогательные 

        private List<string> IdentifierList()
        {
            var list = new List<string>();
            if (_current == LexicalAnalyzer.Ident)
            {
                list.Add(_lexer.AddrName);
                Advance();
            }
            else
            {
                InputOutput.Error(104, _lexer.Token);
            }

            while (_current == LexicalAnalyzer.Comma)
            {
                Advance();
                if (_current == LexicalAnalyzer.Ident)
                {
                    list.Add(_lexer.AddrName);
                    Advance();
                }
                else
                {
                    InputOutput.Error(104, _lexer.Token);
                }
            }
            return list;
        }

        private string GetCurrentType()
        {
            if (_current == LexicalAnalyzer.IntegerSy) return "integer";
            if (_current == LexicalAnalyzer.RealSy) return "real";
            if (_current == LexicalAnalyzer.BooleanSy) return "boolean";
            if (_current == LexicalAnalyzer.CharSy) return "char";
            return "unknown";
        }

        private static string MergeTypes(string a, string b)
        {
            if (a == "real" || b == "real") return "real";
            return a;
        }

        private static void CheckTypeCompatibility(string varType, string exprType, TextPosition pos)
        {
            if (exprType == "" || varType == "") return;
            if (varType == "real" && exprType == "integer") return; // неявное расширение
            if (varType == exprType) return;
            InputOutput.Error(205, pos);
        }
    }

    //Объединённая таблица символов

    internal class CompositeSymbolTable : SymbolTable
    {
        private readonly SymbolTable _global;
        private readonly SymbolTable _local;

        public CompositeSymbolTable(SymbolTable global, SymbolTable local)
        {
            _global = global;
            _local = local;
        }

        public override bool Exists(string name) =>
            _local.Exists(name) || _global.Exists(name);

        public override string GetType(string name) =>
            _local.Exists(name) ? _local.GetType(name) : _global.GetType(name);

        public override SymbolKind GetKind(string name) =>
            _local.Exists(name) ? _local.GetKind(name) : _global.GetKind(name);

        public override Symbol? Get(string name) =>
            _local.Exists(name) ? _local.Get(name) : _global.Get(name);

        public override bool Add(string name, string type, uint line,
                                 SymbolKind kind = SymbolKind.Variable,
                                 List<(string, string)>? parameters = null) =>
            _local.Add(name, type, line, kind, parameters);
    }
}