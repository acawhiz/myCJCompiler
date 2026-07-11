using System;

public class Symbol
{

    public string varname;
    public string symbolname;
    public int size;
    public int scope;
    public string type;
    public ushort address;
    //int pointer_value;

    public Symbol(string varname,
    string symbolname,
    int size,
    int scope,
    string type,
    ushort address/*,int pointer_value*/)
    {
        this.varname = varname;
        this.symbolname = symbolname;
        this.size = size;//in bytes
        this.scope = scope;
        this.type = type;
        this.address = address;
        /*this.pointer_value = pointer_value;*/
    }
}
