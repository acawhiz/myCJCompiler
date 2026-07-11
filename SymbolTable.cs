using System;
using System.Runtime.Intrinsics.Arm;

public class SymbolTable
{

    ushort last_address = 0;

    public List<Symbol> symbols;

    public void Add(string varname,string symbolname,int size,int scope,string type)
    {
        Symbol symbol=new Symbol(varname, symbolname, size, scope,type, last_address);
        symbols.Add(symbol);
        last_address += (ushort)size;//create to next avalable space
    }

	public SymbolTable()
	{
        symbols = new List<Symbol>();
        last_address = MemoryMap.RAM_BASE;
    }
}
