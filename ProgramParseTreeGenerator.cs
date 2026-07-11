using System;
using System.Text.RegularExpressions;

public class ProgramParseTreeGenerator
{
    public List<BinaryTree> list_BinaryTree;
    TreeGenerator tree_generator;
    private int label_counter = 0;
    private int token_index = 0;

    private int code_block_counter = 0;// track code blocks. This is needed to check scope of declared variables
    private Stack<int> stack_scope = new Stack<int>(0);//track current code blocks/scope
    private int current_scope = 0;

    Stack<Node> codeblockstack = new Stack<Node>();//used to keep track of code block to identify jumps

    public ProgramParseTreeGenerator()
	{
        list_BinaryTree = new List<BinaryTree>();
        tree_generator = new TreeGenerator();
    }




    public int advance_token_list(List<Tuple<string, string>> tokens, int index)
    {
        for (int i = index; i < tokens.Count; i++)
        {
            token_index = i;
            
            switch (tokens[i].Item1)
            {
                
                //case "void":
                case "main":
                    codeblockstack.Push(new Node("main"));
                    list_BinaryTree.Add(new BinaryTree(new Node("main"), current_scope));//:jmp
                    break;
                case "{"://block of code
                         //
                         ////////////////////////////////7/16
                    string strlabel = "";
                    if (codeblockstack.Peek().getValue().Equals("main"))
                        strlabel = "mainlabel";
                    else if (codeblockstack.Peek().getValue().Equals("if"))
                        strlabel = "iflabel";
                    else if (codeblockstack.Peek().getValue().Equals("else"))
                        strlabel = "elselabel";
                    else if (codeblockstack.Peek().getValue().Equals("while"))
                        strlabel = "whilelabel";
                    else if (codeblockstack.Peek().getValue().Equals("for"))
                        strlabel = "forlabel";
                    //////////////////////////////////
                    codeblockstack.Push(new Node("{"));
                    Console.WriteLine("{");
                        stack_scope.Push(current_scope);
                        code_block_counter++;
                        current_scope = code_block_counter;

                        label_counter++;

                    list_BinaryTree.Add(new BinaryTree(new Node(strlabel + label_counter + ":"), current_scope));//:label
                    //list_BinaryTree.Add(new BinaryTree(new Node("label" + label_counter + ":"), current_scope));//:label
                    break;
                   
                case "}":// end of a block main ,while ,if, else etc

                    string lablelname = "";
                    Node lblnode = new Node("");
                    codeblockstack.Pop();//throw out "{" to get next structure
                    Node n = codeblockstack.Pop();
                    if (n.getValue().Equals("if"))
                    {
                        if (tokens[i + 1].Item1.Equals("else"))
                        {
                            lablelname = "endiflabelhaselse";//Need to know if else statment exist to make proper jump intruction
                            lblnode.codeblock_lookahead_else = code_block_counter + 1;
                        }
                        else
                            lablelname = "endiflabel"; 
                    }
                    else if (n.getValue().Equals("else"))
                        lablelname = "endelselabel";
                    else if (n.getValue().Equals("while"))
                        lablelname = "endwhilelabel";
                    else if (n.getValue().Equals("for"))
                        lablelname = "endwhilelabel";
                    else
                        lablelname = "endmainlabel";

                    lblnode.setValue(lablelname + current_scope + ":");

                    Console.WriteLine("}");
                    //list_BinaryTree.Add(new BinaryTree(new Node("endlabel" + current_scope + ":"), current_scope));//:label
                    list_BinaryTree.Add(new BinaryTree(lblnode, current_scope));//:label
                    current_scope = stack_scope.Pop();
                    break;
                case ";":// used later to count statements
                    break;
                case "bool":
                case "int":
                case "string":
                    skip_tokens(tokens);
                    i = token_index;
                    break;
                case "while":
                case "if":

                    string exprifwhile = get_if_while_statement(tokens);
                    Node ifwhileNode;
                    if (tokens[i].Item1.Equals("if"))
                        ifwhileNode = new Node("if");
                    else
                        ifwhileNode = new Node("while");

                    ifwhileNode.codeblock_assignment = code_block_counter + 1;//assume next code count block to follow
                    codeblockstack.Push(ifwhileNode);

                    ifwhileNode.left = tree_generator.genererateTree(exprifwhile).root;
                    tree_generator.Clear();
                    BinaryTree binaryTreeifwhile = new BinaryTree(ifwhileNode, current_scope);
                    list_BinaryTree.Add(binaryTreeifwhile);
                    i = token_index;
                    break;
                case "else":
                    Node elseNode = new Node("else");
                    elseNode.codeblock_assignment = code_block_counter + 1;//assume next code count block to follow
                    list_BinaryTree.Add(new BinaryTree(elseNode, current_scope));
                    codeblockstack.Push(elseNode);
                    break;
                case "for":
                    string[] exprfor=get_for_statement(tokens);
                    Node forNode = new Node("for");
                    forNode.left = tree_generator.genererateTree(exprfor[0]).root;//first part which declare and initialize loop v  e.g int i=0;
                    tree_generator.Clear();
                    forNode.left.right = tree_generator.genererateTree( exprfor[1]).root;//second part which check for loop condition e.g i<2;
                    tree_generator.Clear();
                    forNode.right = tree_generator.genererateTree(exprfor[2]).root;//third part assignement statement e.g i=i+1
                    tree_generator.Clear();
                    BinaryTree binaryTreefor = new BinaryTree(forNode, current_scope);
                    list_BinaryTree.Add(binaryTreefor);
                    i = token_index;
                    codeblockstack.Push(forNode);
                    break;
                case "(":
                case ")":
                    break;

                default:
                    {
                        string s = get_statement(tokens);
                        BinaryTree binaryTree = tree_generator.genererateTree(s);
                        binaryTree.scope = current_scope;
                        tree_generator.Clear();
                        list_BinaryTree.Add(binaryTree);
                        i = token_index;
                    }
                    break;
            }


        }

        return tokens.Count;

        Console.WriteLine("Parsing failed!");
        Environment.Exit(-3);
    }


    string[] get_for_statement(List<Tuple<string, string>> tokens)
    {
        //for(int i=0;i<a;i=i+1){
        string sforexpr1 = "";
        string sforexpr2 = "";
        string sforexpr3 = "";

        const int for_decl_offset = 3;//offset to the iterator variable. so skip for(int tokens
        while (!tokens[token_index + for_decl_offset].Item1.Equals(";"))
        {
            sforexpr1 += tokens[token_index + for_decl_offset].Item1;
            token_index++;
        }
        token_index+= for_decl_offset+1;

        //get 2nd(condition expression) part of for loop
        while (!tokens[token_index].Item1.Equals(";"))
        {
            sforexpr2 += tokens[token_index].Item1;
            token_index++;

        }
        token_index++;

        //get third part of for loop
        while (!tokens[token_index].Item1.Equals(")") || !tokens[token_index+1].Item1.Equals("{"))
        {
            sforexpr3 += tokens[token_index].Item1;
            token_index++;

        }

        return new string[] { sforexpr1+";", "Condition=" + sforexpr2 + ";", sforexpr3 + ";" };

    }


    string get_if_while_statement(List<Tuple<string, string>> tokens)
    {
        string str = "";
        while (!tokens[token_index+2].Item1.Equals(")") || !tokens[token_index + 3].Item1.Equals("{"))//+2 to skip if(
        {//build string of tokens x from if(x){
            str = str + tokens[token_index+2].Item1;//+2 to skip tokens  if(
            token_index++;
            //if (token_index == tokens.Count) { return 0; }//prevent out of boundary scan
        }
        str = "Condition=" + str + ";";//built trsing to be handeled by expression parser
        token_index+=2;//point to last )

        return str;

    }

    void skip_tokens(List<Tuple<string, string>> tokens)
    {
        while (!tokens[token_index].Item1.Equals(";"))
            token_index++;
    }

    string get_statement(List<Tuple<string, string>> tokens)
    {
        string str = "";
        while (!tokens[token_index].Item1.Equals(";"))
        {//build string of tokens from index i to ;
            str = str + tokens[token_index].Item1;
            token_index++;
            //if (token_index == tokens.Count) { return 0; }//prevent out of boundary scan
        }
        str = str + ";";

        return str;

    }

}
