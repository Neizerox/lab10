using System;

namespace Компилятор
{
    public class LexicalAnalyzer
    {
        public const byte Star = 21;
        public const byte Slash = 60;
        public const byte Equal = 16;
        public const byte Comma = 20;
        public const byte Semicolon = 14;
        public const byte Colon = 5;
        public const byte Point = 61;
        public const byte Arrow = 62;
        public const byte LeftPar = 9;
        public const byte RightPar = 4;
        public const byte LBracket = 11;
        public const byte RBracket = 12;
        public const byte FlPar = 63;
        public const byte FrPar = 64;
        public const byte Later = 65;
        public const byte Greater = 66;
        public const byte LaterEqual = 67;
        public const byte GreaterEqual = 68;
        public const byte LaterGreater = 69;
        public const byte Plus = 70;
        public const byte Minus = 71;
        public const byte LComment = 72;
        public const byte RComment = 73;
        public const byte Assign = 51;
        public const byte TwoPoints = 74;
        public const byte Ident = 2;
        public const byte FloatC = 82;
        public const byte IntC = 15;
        public const byte StringSy = 83;
        public const byte CaseSy = 31;
        public const byte ElseSy = 32;
        public const byte FileSy = 57;
        public const byte GotoSy = 33;
        public const byte ThenSy = 52;
        public const byte TypeSy = 34;
        public const byte UntilSy = 53;
        public const byte DoSy = 54;
        public const byte WithSy = 37;
        public const byte IfSy = 56;
        public const byte InSy = 100;
        public const byte OfSy = 101;
        public const byte OrSy = 102;
        public const byte ToSy = 103;
        public const byte EndSy = 104;
        public const byte VarSy = 105;
        public const byte DivSy = 106;
        public const byte AndSy = 107;
        public const byte NotSy = 108;
        public const byte ForSy = 109;
        public const byte ModSy = 110;
        public const byte NilSy = 111;
        public const byte SetSy = 112;
        public const byte BeginSy = 113;
        public const byte WhileSy = 114;
        public const byte ArraySy = 115;
        public const byte ConstSy = 116;
        public const byte LabelSy = 117;
        public const byte DownToSy = 118;
        public const byte PackedSy = 119;
        public const byte RecordSy = 120;
        public const byte RepeatSy = 121;
        public const byte ProgramSy = 122;
        public const byte FunctionSy = 123;
        public const byte ProcedureSy = 124;
        public const byte IntegerSy = 125;
        public const byte RealSy = 126;
        public const byte WritelnSy = 127;
        public const byte BooleanSy = 128;
        public const byte CharSy = 129;
        public const byte TrueSy = 130;
        public const byte FalseSy = 131;
        public const byte Error = 254;
        public const byte Eof = 255;

        private byte _symbol;
        private TextPosition _token;
        private string _addrName;
        private int _intValue;
        private float _floatValue;
        private string _stringValue;

        public byte Symbol
        {
            get
            {
                return _symbol;
            }
        }

        public TextPosition Token
        {
            get
            {
                return _token;
            }
        }

        public string AddrName
        {
            get
            {
                return _addrName;
            }
        }

        public int IntValue
        {
            get
            {
                return _intValue;
            }
        }

        public float FloatValue
        {
            get
            {
                return _floatValue;
            }
        }

        public string StringValue
        {
            get
            {
                return _stringValue;
            }
        }

        public LexicalAnalyzer()
        {
            _symbol = 0;
            _token = new TextPosition();
            _addrName = null;
            _intValue = 0;
            _floatValue = 0f;
            _stringValue = null;
        }

        public byte NextSym()
        {
            while (InputOutput.CurrentChar == ' ' ||
                   InputOutput.CurrentChar == '\t' ||
                   InputOutput.CurrentChar == '\r' ||
                   InputOutput.CurrentChar == '\n')
            {
                InputOutput.NextCh();
            }

            if (InputOutput.CurrentChar == '\0' || InputOutput.IsEof())
            {
                _symbol = Eof;
                return _symbol;
            }

            _token = new TextPosition(
                InputOutput.PositionNow.LineNumber,
                (byte)(InputOutput.PositionNow.CharNumber - 1)
            );

            char ch = InputOutput.CurrentChar;

            if (ch >= '0' && ch <= '9')
            {
                return ReadNumber();
            }

            if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_')
            {
                return ReadIdentifierOrKeyword();
            }

            if (ch == '\'')
            {
                return ReadString();
            }

            return ReadOperator();
        }

        private byte ReadNumber()
        {
            const int MaxInt = 32767;
            _intValue = 0;
            bool overflow = false;

            while (InputOutput.CurrentChar >= '0' && InputOutput.CurrentChar <= '9')
            {
                int digit = InputOutput.CurrentChar - '0';
                if (!overflow)
                {
                    if (_intValue <= (MaxInt - digit) / 10)
                    {
                        _intValue = _intValue * 10 + digit;
                    }
                    else
                    {
                        InputOutput.Error(203, _token);
                        overflow = true;
                        _intValue = 0;
                    }
                }
                InputOutput.NextCh();
            }

            if (InputOutput.CurrentChar == '.')
            {
                if (InputOutput.PeekNext() == '.')
                {
                    _symbol = IntC;
                    return _symbol;
                }

                InputOutput.NextCh();
                float realPart = 0f;
                float divisor = 10f;
                while (InputOutput.CurrentChar >= '0' && InputOutput.CurrentChar <= '9')
                {
                    realPart += (InputOutput.CurrentChar - '0') / divisor;
                    divisor *= 10f;
                    InputOutput.NextCh();
                }
                _floatValue = _intValue + realPart;
                _symbol = FloatC;
                return _symbol;
            }

            _symbol = IntC;
            return _symbol;
        }

        private byte ReadIdentifierOrKeyword()
        {
            string name = "";
            while ((InputOutput.CurrentChar >= 'a' && InputOutput.CurrentChar <= 'z') ||
                   (InputOutput.CurrentChar >= 'A' && InputOutput.CurrentChar <= 'Z') ||
                   (InputOutput.CurrentChar >= '0' && InputOutput.CurrentChar <= '9') ||
                   InputOutput.CurrentChar == '_')
            {
                name += InputOutput.CurrentChar;
                InputOutput.NextCh();
            }

            switch (name.ToLower())
            {
                case "program":
                    {
                        _symbol = ProgramSy;
                        break;
                    }
                case "var":
                    {
                        _symbol = VarSy;
                        break;
                    }
                case "begin":
                    {
                        _symbol = BeginSy;
                        break;
                    }
                case "end":
                    {
                        _symbol = EndSy;
                        break;
                    }
                case "while":
                    {
                        _symbol = WhileSy;
                        break;
                    }
                case "do":
                    {
                        _symbol = DoSy;
                        break;
                    }
                case "repeat":
                    {
                        _symbol = RepeatSy;
                        break;
                    }
                case "until":
                    {
                        _symbol = UntilSy;
                        break;
                    }
                case "for":
                    {
                        _symbol = ForSy;
                        break;
                    }
                case "to":
                    {
                        _symbol = ToSy;
                        break;
                    }
                case "downto":
                    {
                        _symbol = DownToSy;
                        break;
                    }
                case "if":
                    {
                        _symbol = IfSy;
                        break;
                    }
                case "then":
                    {
                        _symbol = ThenSy;
                        break;
                    }
                case "else":
                    {
                        _symbol = ElseSy;
                        break;
                    }
                case "case":
                    {
                        _symbol = CaseSy;
                        break;
                    }
                case "of":
                    {
                        _symbol = OfSy;
                        break;
                    }
                case "and":
                    {
                        _symbol = AndSy;
                        break;
                    }
                case "or":
                    {
                        _symbol = OrSy;
                        break;
                    }
                case "not":
                    {
                        _symbol = NotSy;
                        break;
                    }
                case "div":
                    {
                        _symbol = DivSy;
                        break;
                    }
                case "mod":
                    {
                        _symbol = ModSy;
                        break;
                    }
                case "nil":
                    {
                        _symbol = NilSy;
                        break;
                    }
                case "in":
                    {
                        _symbol = InSy;
                        break;
                    }
                case "array":
                    {
                        _symbol = ArraySy;
                        break;
                    }
                case "record":
                    {
                        _symbol = RecordSy;
                        break;
                    }
                case "set":
                    {
                        _symbol = SetSy;
                        break;
                    }
                case "const":
                    {
                        _symbol = ConstSy;
                        break;
                    }
                case "type":
                    {
                        _symbol = TypeSy;
                        break;
                    }
                case "label":
                    {
                        _symbol = LabelSy;
                        break;
                    }
                case "goto":
                    {
                        _symbol = GotoSy;
                        break;
                    }
                case "function":
                    {
                        _symbol = FunctionSy;
                        break;
                    }
                case "procedure":
                    {
                        _symbol = ProcedureSy;
                        break;
                    }
                case "packed":
                    {
                        _symbol = PackedSy;
                        break;
                    }
                case "with":
                    {
                        _symbol = WithSy;
                        break;
                    }
                case "file":
                    {
                        _symbol = FileSy;
                        break;
                    }
                case "integer":
                    {
                        _symbol = IntegerSy;
                        break;
                    }
                case "real":
                    {
                        _symbol = RealSy;
                        break;
                    }
                case "boolean":
                    {
                        _symbol = BooleanSy;
                        break;
                    }
                case "char":
                    {
                        _symbol = CharSy;
                        break;
                    }
                case "true":
                    {
                        _symbol = TrueSy;
                        break;
                    }
                case "false":
                    {
                        _symbol = FalseSy;
                        break;
                    }
                case "writeln":
                    {
                        _symbol = WritelnSy;
                        break;
                    }
                default:
                    {
                        _symbol = Ident;
                        _addrName = name.ToLower();
                        break;
                    }
            }
            return _symbol;
        }

        private byte ReadString()
        {
            string value = "";
            InputOutput.NextCh();

            while (InputOutput.CurrentChar != '\'' &&
                   InputOutput.CurrentChar != '\0' &&
                   !InputOutput.IsEof())
            {
                value += InputOutput.CurrentChar;
                InputOutput.NextCh();
            }

            if (InputOutput.CurrentChar == '\'')
            {
                InputOutput.NextCh();
                _stringValue = value;
                _symbol = StringSy;
                return _symbol;
            }
            else
            {
                InputOutput.Error(103, _token);
                _symbol = Error;
                return _symbol;
            }
        }

        private byte ReadOperator()
        {
            char ch = InputOutput.CurrentChar;

            switch (ch)
            {
                case '+':
                    {
                        _symbol = Plus;
                        InputOutput.NextCh();
                        break;
                    }
                case '-':
                    {
                        _symbol = Minus;
                        InputOutput.NextCh();
                        break;
                    }
                case '*':
                    {
                        _symbol = Star;
                        InputOutput.NextCh();
                        break;
                    }
                case '/':
                    {
                        _symbol = Slash;
                        InputOutput.NextCh();
                        break;
                    }
                case '=':
                    {
                        _symbol = Equal;
                        InputOutput.NextCh();
                        break;
                    }
                case ',':
                    {
                        _symbol = Comma;
                        InputOutput.NextCh();
                        break;
                    }
                case ';':
                    {
                        _symbol = Semicolon;
                        InputOutput.NextCh();
                        break;
                    }
                case ')':
                    {
                        _symbol = RightPar;
                        InputOutput.NextCh();
                        break;
                    }
                case '[':
                    {
                        _symbol = LBracket;
                        InputOutput.NextCh();
                        break;
                    }
                case ']':
                    {
                        _symbol = RBracket;
                        InputOutput.NextCh();
                        break;
                    }
                case '^':
                    {
                        _symbol = Arrow;
                        InputOutput.NextCh();
                        break;
                    }
                case '(':
                    {
                        InputOutput.NextCh();
                        if (InputOutput.CurrentChar == '*')
                        {
                            InputOutput.NextCh();
                            while (!InputOutput.IsEof())
                            {
                                if (InputOutput.CurrentChar == '*' && InputOutput.PeekNext() == ')')
                                {
                                    InputOutput.NextCh();
                                    InputOutput.NextCh();
                                    break;
                                }
                                InputOutput.NextCh();
                            }
                            return NextSym();
                        }
                        _symbol = LeftPar;
                        break;
                    }
                case '{':
                    {
                        InputOutput.NextCh();
                        while (InputOutput.CurrentChar != '}' && !InputOutput.IsEof())
                        {
                            InputOutput.NextCh();
                        }
                        if (InputOutput.CurrentChar == '}')
                        {
                            InputOutput.NextCh();
                        }
                        return NextSym();
                    }
                case '}':
                    {
                        _symbol = FrPar;
                        InputOutput.NextCh();
                        break;
                    }
                case '>':
                    {
                        InputOutput.NextCh();
                        if (InputOutput.CurrentChar == '=')
                        {
                            _symbol = GreaterEqual;
                            InputOutput.NextCh();
                        }
                        else
                        {
                            _symbol = Greater;
                        }
                        break;
                    }
                case '<':
                    {
                        InputOutput.NextCh();
                        if (InputOutput.CurrentChar == '=')
                        {
                            _symbol = LaterEqual;
                            InputOutput.NextCh();
                        }
                        else if (InputOutput.CurrentChar == '>')
                        {
                            _symbol = LaterGreater;
                            InputOutput.NextCh();
                        }
                        else
                        {
                            _symbol = Later;
                        }
                        break;
                    }
                case ':':
                    {
                        InputOutput.NextCh();
                        if (InputOutput.CurrentChar == '=')
                        {
                            _symbol = Assign;
                            InputOutput.NextCh();
                        }
                        else
                        {
                            _symbol = Colon;
                        }
                        break;
                    }
                case '.':
                    {
                        InputOutput.NextCh();
                        if (InputOutput.CurrentChar == '.')
                        {
                            _symbol = TwoPoints;
                            InputOutput.NextCh();
                        }
                        else
                        {
                            _symbol = Point;
                        }
                        break;
                    }
                default:
                    {
                        InputOutput.Error(202, _token);
                        InputOutput.NextCh();
                        _symbol = Error;
                        break;
                    }
            }
            return _symbol;
        }
    }
}