using System;

namespace Compil
{
    public static class StructComp
    {
        public struct TextPosition
        {
            public uint LineNumber;
            public byte CharNumber;

            public TextPosition(uint ln = 0, byte c = 0)
            {
                LineNumber = ln;
                CharNumber = c;
            }

            public override string ToString()
            {
                return $"({LineNumber}, {CharNumber})";
            }
        }

        public struct Err
        {
            public TextPosition ErrorPosition;
            public byte ErrorCode;

            public Err(TextPosition position, byte code)
            {
                ErrorPosition = position;
                ErrorCode = code;
            }
        }
    }
}