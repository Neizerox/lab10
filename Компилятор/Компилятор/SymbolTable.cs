using System.Collections.Generic;

namespace Компилятор
{
    public enum SymbolKind
    {
        Variable,
        Procedure
    }

    public class Symbol
    {
        public string Name 
        {
            get; 
        }
        public string Type 
        {
            get;    // тип переменной или "" для процедур
        }      
        public uint Line 
        {
            get;
        }
        public SymbolKind Kind 
        { 
            get;
        }
        public List<(string paramName, string paramType)> Params 
        {
            get;
        }

        public Symbol(string name, string type, uint line,
                      SymbolKind kind = SymbolKind.Variable,
                      List<(string, string)>? parameters = null)
        {
            Name = name;
            Type = type;
            Line = line;
            Kind = kind;
            Params = parameters ?? new List<(string, string)>();
        }
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, Symbol> _symbols = new();

        public virtual bool Add(string name, string type, uint line,
                                SymbolKind kind = SymbolKind.Variable,
                                List<(string, string)>? parameters = null)
        {
            string key = name.ToLower();
            if (_symbols.ContainsKey(key)) return false;
            _symbols[key] = new Symbol(name, type, line, kind, parameters);
            return true;
        }

        public virtual bool Exists(string name) =>
            _symbols.ContainsKey(name.ToLower());

        public virtual Symbol? Get(string name)
        {
            _symbols.TryGetValue(name.ToLower(), out Symbol? s);
            return s;
        }

        public virtual string GetType(string name) => Get(name)?.Type ?? "";

        public virtual SymbolKind GetKind(string name) =>
            Get(name)?.Kind ?? SymbolKind.Variable;
    }
}