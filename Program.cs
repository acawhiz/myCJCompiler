// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");



using myCJCompiler;
using System;
using System.ComponentModel;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

//string path = @"source.txt";
string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
string path = Path.Combine(projectRoot, "source.txt");


SourceFile sf = new SourceFile();
byte[] byte_source = sf.getSourceFile(path);
sf.ShowSourceFile(path);

Tokenizer tokenizer = new Tokenizer();

tokenizer.GenerateTokens(byte_source);

tokenizer.ShowTokens();

List<Tuple<string, string>> tokens = tokenizer.GetTokens();
Parser parser = new Parser();



int result = parser.advance_token_list(tokens, 0);
if (result == 0)
{
    Console.WriteLine("Parsing failed!!");
    Environment.Exit(-3);
}
else if (parser.curly_bracket_open == parser.curly_bracket_closed)
{
    Console.WriteLine("Parsing complete!!");
    parser.Show_variables();
}
else
{
    if (parser.curly_bracket_closed != parser.curly_bracket_open)
        Console.WriteLine("Block statment is unbalanced");
    Console.WriteLine("Parsing failed!!!");
    Environment.Exit(-3);
}


ProgramParseTreeGenerator programParseTreeGenerator = new ProgramParseTreeGenerator();

programParseTreeGenerator.advance_token_list(tokens, 0);

 
////////symbol table creation/////////////////
 SymbolTable symboltable= new SymbolTable();
foreach (var variable in parser.variables)
{
    int size=0;
    //System.Console.WriteLine(variable);
    if (variable.Item2.Equals("int")|| variable.Item2.Equals("bool"))
        size = 1;//1 byte for now
    else if (variable.Item2.Equals("string"))
        size = 32;//32 byte for now
    symboltable.Add(variable.Item1/*identifier name*/, "@@" + variable.Item2 + "_" + variable.Item1/*symbol name*/, size, variable.Item3/*scope*/, variable.Item2/*type*/);//item1=identifier name, item2=type,item3=scope(int)
}
////////end symbol table creation/////////////////
//symboltable.Show();

CodeGenerator codegenerator = new CodeGenerator();


///////////////load built in functions//////////
///
string strasm = "";

strasm = strasm + "multiplication: nop" + "\r\n";
strasm = strasm + "mov a, c" + ";c=multiplier\r\n";
strasm = strasm + "cpi 0" + "\r\n";
strasm = strasm + "jz multend" + "\r\n";
strasm = strasm + "mov a, b" + ";b=multiplicand\r\n";
strasm = strasm + "cpi 0" + "\r\n";
strasm = strasm + "jz multend" + "\r\n";
strasm = strasm + "mult: dcr c" + "\r\n";
strasm = strasm + "\t jz multend" + "\r\n";
strasm = strasm + "\t add b" + "\r\n";
strasm = strasm + "\t jmp mult" + "\r\n";
strasm = strasm + "multend: ret" + ";result in accumulator\r\n";

strasm = strasm + "\r\n";

strasm = strasm + "divide:nop" + ";a/b\r\n";
strasm = strasm + "cpi 0" + ";if dividend is 0 end\r\n";
strasm = strasm + "jz divend" + "\r\n";
strasm = strasm + "mov c,a" + ";save dividend in c\r\n";
strasm = strasm + "mov a,b" + "\r\n";
strasm = strasm + "cpi 0" + ";if divider is 0 end\r\n";
strasm = strasm + "jz divend" + "\r\n";
strasm = strasm + "mov a,c" + ";restore dividend\r\n";
strasm = strasm + "mvi c,0" + ";division counter\r\n";
strasm = strasm + "div: cmp b" + ";if a<=b end\r\n";
strasm = strasm + "\t jc divend; acc <= regb" + "\r\n";
strasm = strasm + "\t sub b" + "\r\n";
strasm = strasm + "\t inr c" + "\r\n";
strasm = strasm + "\t jmp div" + "\r\n";
strasm = strasm + "divend: mov a, c" + ";store result in accumulator\r\n";
strasm = strasm + "ret" + ";result in accumulator\r\n";

strasm = strasm + "\r\n";

strasm = strasm + "islower: nop" + "\r\n";//check if a<b
strasm = strasm + "cmp b" + "\r\n";
strasm = strasm + "jnc islowerfalse; set carry if a<b" + "\r\n";
strasm = strasm +"mvi a,01h" + "\r\n";
strasm = strasm +"ret" + "\r\n";
strasm = strasm + "islowerfalse:mvi a,00h" + "\r\n";
strasm = strasm +"ret" + "\r\n";

strasm = strasm + "\r\n";
strasm = strasm + "isbigger: nop" + "\r\n";//check if a>b
strasm = strasm + "cmp b" + "\r\n";
strasm = strasm + "jc isbiggerfalse;reset carry if not bigger" + "\r\n";
strasm = strasm + "mvi a,01h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "isbiggerfalse:mvi a,00h" + "\r\n";
strasm = strasm + "ret" + "\r\n";

strasm = strasm + "\r\n";//check if a==b
strasm = strasm + "isequal: nop" + "\r\n";
strasm = strasm + "cmp b" + "\r\n";
strasm = strasm + "jnz isequalfalse;" + "\r\n";
strasm = strasm + "mvi a,01h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "isequalfalse:mvi a,00h" + "\r\n";
strasm = strasm + "ret" + "\r\n";

strasm = strasm + "\r\n";//check if a!=b
strasm = strasm + "isnotequal: nop" + "\r\n";
strasm = strasm + "cmp b" + "\r\n";
strasm = strasm + "jz isnotequalfalse;" + "\r\n";
strasm = strasm + "mvi a,01h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "isnotequalfalse:mvi a,00h" + "\r\n";
strasm = strasm + "ret" + "\r\n";

strasm = strasm + "\r\n";//check if a<=b
strasm = strasm + "islowerorequal: nop" + "\r\n";
strasm = strasm + "cmp b; set carry if a<b set zero flag if a==b" + "\r\n";
strasm = strasm + "jnc notlowercheckequal" + "\r\n";
strasm = strasm + "islowerorequaltrue:nop" + "\r\n";
strasm = strasm + "mvi a,01h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "notlowercheckequal:jz islowerorequaltrue" + "\r\n";
strasm = strasm + "mvi a,00h" + "\r\n";
strasm = strasm + "ret" + "\r\n";

strasm = strasm + "\r\n";//check if a>=b
strasm = strasm + "isbiggerorequal: nop" + "\r\n";
strasm = strasm + "cmp b; resetset carry if a>b set zero if a==b" + "\r\n";
strasm = strasm + "jc notbiggercheckequal" + "\r\n";
strasm = strasm + "isbiggerorequaltrue:nop" + "\r\n";
strasm = strasm + "mvi a,01h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "notbiggercheckequal:jz isbiggerorequaltrue" + "\r\n";
strasm = strasm + "mvi a,00h" + "\r\n";
strasm = strasm + "ret" + "\r\n";

strasm = strasm + "\r\n";
strasm = strasm + "logicaland: nop" + "\r\n";
strasm = strasm + "ana b" + "\r\n";
strasm = strasm + "rar" + "\r\n";
strasm = strasm + "jnc logicalandfalse" + "\r\n";
strasm = strasm + "mvi a,1h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "logicalandfalse:nop" + "\r\n";
strasm = strasm + "mvi a,0h" + "\r\n";
strasm = strasm + "ret" + "\r\n";

strasm = strasm + "\r\n";
strasm = strasm + "logicalor: nop" + "\r\n";
strasm = strasm + "ora b" + "\r\n";
strasm = strasm + "rar" + "\r\n";
strasm = strasm + "jnc logicalorfalse" + "\r\n";
strasm = strasm + "mvi a,1h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + "logicalorfalse:nop" + "\r\n";
strasm = strasm + "mvi a,0h" + "\r\n";
strasm = strasm + "ret" + "\r\n";
strasm = strasm + ";;;;;;;;;;;" + "\r\n";

Console.Write(strasm);


foreach (BinaryTree binarytree in programParseTreeGenerator.list_BinaryTree)
{
    if (binarytree.root.getValue().Contains("=")|| binarytree.root.getValue().Equals("while")
        || binarytree.root.getValue().Equals("if") || binarytree.root.getValue().Equals("else")
        || binarytree.root.getValue().StartsWith("mainlabel") || binarytree.root.getValue().StartsWith("endmainlabel")
        || binarytree.root.getValue().StartsWith("iflabel") || binarytree.root.getValue().StartsWith("endiflabel")
        || binarytree.root.getValue().StartsWith("elselabel") || binarytree.root.getValue().StartsWith("endelselabel")
        || binarytree.root.getValue().StartsWith("whilelabel") || binarytree.root.getValue().StartsWith("endwhilelabel")
        )
    {
        Console.WriteLine(";"+binarytree.root.getValue());
        codegenerator.codegen(binarytree.root, binarytree.scope, symboltable);
    }

}



////////code gen block/////////////////
{//https://www.tutorialspoint.com/compiler_design/compiler_design_phases_of_compiler.htm
 //https://www.sim8085.com/
 //create var names
 //create constants names

    //SymbolTable symboltable= new SymbolTable();
    //foreach(IThreadPoolWorkItem in EnvironmentVariableTarget lists){
    //    AddingNewEventArgs item in symbol table;
    //}

    // test tree
    //Node l = new Node("b");
    //Node r = new Node("c");
    //Node op = new Node("*");
    //op.left=l; op.right=r;

    //Node ll = new Node("a");
    //Node op2 = new Node("+");
    //op2.left = ll; op2.right = op;

    //Node op3 = new Node("+");
    //op3.left = op2;


    //Node l8 = new Node("5");
    //Node r8 = new Node("2");
    //Node op8 = new Node("-");
    //op8.left = l8; op8.right = r8;

    //Node r7 = new Node("5");
    //Node op7 = new Node("+");
    //op7.left = op8; op7.right = r7;

    //Node l6 = new Node("2");
    //Node op6 = new Node("+");
    //op6.left = l6; op6.right = op7;

    //Node l5 = new Node("10");
    //Node op5 = new Node("-");
    //op5.left = l5; op5.right = op6;

    //Node l4 = new Node("20");
    //Node op4 = new Node("+");
    //op4.left = l4; op4.right = op5;

    //op3.right = op4;//top node

    ////Stack<Node> nodestack = new Stack<Node>();

    ////CodeGenerator codegenerator = new CodeGenerator();



    //codegenerator.codegen(op4);

   // codegentwooperands("7", "/", "2",1);
    


    


}


