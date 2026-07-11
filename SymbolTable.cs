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

    public void Show()
    {
        Console.WriteLine();
        Console.WriteLine("----------- Symbol Table -----------");
        Console.WriteLine("{0,-12} {1,-12} {2,-8} {3,-8} {4,-10} {5,-8}",
            "Variable", "Symbol", "Address", "Size", "Type", "Scope");

        foreach (Symbol symbol in symbols)
        {
            Console.WriteLine("{0,-12} {1,-12} 0x{2:X4} {3,-8} {4,-10} {5,-8}",
                symbol.varname,
                symbol.symbolname,
                symbol.address,
                symbol.size,
                symbol.type,
                symbol.scope);
        }

        Console.WriteLine("------------------------------------");
    }
}
