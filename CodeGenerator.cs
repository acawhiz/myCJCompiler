using System;
using System.IO;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

public class CodeGenerator
{
    int CodeGeneratorc_ount = 0;//keep track how many times code has been generated. if>0 means previous resul is stored in accumulator
    ushort result_address = MemoryMap.TEMPORARY_RESULT_STORAGE_RAM;


    public CodeGenerator()
	{
	}

    Node getTerminalNode(Node n)
    {
        if (n.left == null)
            return null;//terminal found
        else
            return n.left; //non terminal
    }
    SymbolTable symboltable = null;
    public void codegen(Node expression_tree, int scope, SymbolTable symboltable)
    {
        this.symboltable = symboltable;

        Node curr = null;
        //if (expression_tree.right == null)
        //    curr = expression_tree.left;
        //else
        curr = expression_tree;

        Node next = null;
        Node parent = null;
        Node parent_right = null;
        Stack<Node> nodestack = new Stack<Node>();


        if (expression_tree.getValue().StartsWith("mainlabel") || expression_tree.getValue().StartsWith("iflabel") || expression_tree.getValue().StartsWith("elselabel"))
        {
            string strasm = "";
            strasm = expression_tree.getValue() + "nop\r\n";
            Console.Write(strasm);
            return;
        }
        if (expression_tree.getValue().StartsWith("endmainlabel"))
        {
            string strasm = "";
            strasm = expression_tree.getValue() + "hlt\r\n"; //end of main program

            Console.Write(strasm);
            return;
        }

        if (expression_tree.getValue().StartsWith("endiflabel"))
        {
            if (expression_tree.getValue().StartsWith("endiflabelhaselse"))
            {
                string strasm = "";
                if (scope != 1)
                {
                    strasm = expression_tree.getValue().Replace("haselse", "") + "nop\r\n";
                    strasm = strasm + "jmp endelselabel" + expression_tree.codeblock_lookahead_else + "\r\n";
                }
                Console.Write(strasm);
                return;
            }
            else
            {
                string strasm = "";
                if (scope != 1)
                    strasm = expression_tree.getValue() + "nop\r\n";
                Console.Write(strasm);
                return;
            }
        }

        if (expression_tree.getValue().Equals("else"))
        {
            //string strasm = "";
            //strasm = "jmp endelselabel" + expression_tree.codeblock_assignment + "\r\n";
            //Console.Write(strasm);
            return;
        }
        if (expression_tree.getValue().StartsWith("endelselabel"))
        {
            string strasm = "";

            strasm = expression_tree.getValue() + "nop\r\n";

            Console.Write(strasm);
            return;
        }
        //if (expression_tree.getValue().Equals("else"))
        //{
        //    string strasm = "";
        //    strasm = "jmp endlabel" + expression_tree.codeblock_assignment + "\r\n";
        //    Console.Write(strasm);
        //    return;
        //}
        if (expression_tree.getValue().Equals("while"))
        {
            string strasm = "";
            strasm = "whilelabel" + expression_tree.codeblock_assignment + ":nop\r\n";
            Console.Write(strasm);
            //return;
        }

        if (expression_tree.getValue().StartsWith("whilelabel"))
        {
            string strasm = "";
            strasm = strasm + expression_tree.getValue().Replace("whilelabel", "whilelabeltrue") + "nop\r\n";
            Console.Write(strasm);
            return;
        }

        if (expression_tree.getValue().StartsWith("endwhilelabel"))
        {
            string strasm = "";
            
            strasm = strasm+"jmp whilelabel" + scope + "\r\n";
            strasm = strasm+ expression_tree.getValue() + "nop\r\n";
            Console.Write(strasm);
            return;
        }

        while (true)
        {
            nodestack.Push(curr);
            next = getTerminalNode(curr);
            if (next == null)
            {
                nodestack.Pop();//discard terminal node from stack to get to parent node of terminal node
                parent = nodestack.Pop();
                if (parent.right != null)
                {
                    parent_right = parent.right;
                    if (parent_right.left == null && parent_right.right == null)
                    {
                        CodeGeneratorc_ount++;
                        //generate code

                        string strasm = codegentwooperands(parent.left, parent.getValue(), parent.right, scope);//a=b+c
                        Console.Write(strasm);

                        // end genrate code
                        
                        parent.setValue("_RegA_");
                        parent.address = this.result_address++;//Results pointer

                        if (this.result_address == MemoryMap.TEMPORARY_RESULT_STORAGE_RAM_TOP)
                        {
                            Console.WriteLine("MemoryMap.TEMPORARY_RESULT_STORAGE_RAM_TOP reached");
                            System.Environment.Exit(-5000);
                        }
                        parent.left = null;
                        parent.right = null;
                        // current node is parent of parent
                        if (nodestack.Count == 0)//if 0 topnode processed
                            break;
                        else
                        {
                            curr = nodestack.Pop();
                            if(curr.getValue().Equals(""))//replace empty node generated during unary parse tree
                            {
                                curr.setValue(parent.getValue());
                                curr.address=parent.address;
                                curr.left = null;
                            }
                        }
                    }
                    else
                    {
                        nodestack.Push(parent);//push current parent before parent right node
                        curr = parent.right;
                    }
                }
                else//for most cases where root => x=a  and right node=null.. 
                {
                    string strasm = "";
                    //generate code

                        strasm = codegenassignmentsingle(parent, scope);
                    if (strasm.Length == 0||parent.getValue().Equals("Condition="))//if empty we are dealing with an if structure
                    {
                        
                        parent.setValue("__if_while_condition__:" + parent.left.getValue());
                        parent.address = parent.left.address;
                        parent.left = null;
                    }
                    else if (parent.getValue().Equals("if")|| parent.getValue().Equals("while"))
                    {
                        Console.Write(strasm);
                        break;
                    }
                    else
                    {
                        Console.Write(strasm);
                        parent.setValue(parent.getValue() + parent.left.getValue());
                        parent.left = null;
                        break;
                    }
                }


            }
            else
                curr = next;

        }

    }


    
    ///<summary>
    ///<paramref name="index"/>
    ///Generate code when expression tree in form x=identifier or x=numeric
    ///</summary>
    string codegenassignmentsingle(Node n,int scope)
    {
        string strasm = "";
        string var_left= n.getValue().Replace("=","");// remove = to get variable name
        string var_right = n.left.getValue();

        ushort var_left_address = 0;

        if (var_left.Equals("if"))
        {
            strasm = strasm + "cpi 01h;" + "\r\n";
            strasm = strasm + "jnz endiflabel" + n.codeblock_assignment + "\r\n";//if structure still belong to current scope while if body is scope +1
            return strasm;
        }

        if (var_left.Equals("while"))
        {
            strasm = strasm + "cpi 01h;" + "\r\n";
            strasm = strasm + "jnz endwhilelabel" + n.codeblock_assignment + "\r\n";//if structure still belong to current scope while if body is scope +1
            return strasm;
        }




        if (var_right.Equals("true"))
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_left) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having smae scope
                {
                    strasm = strasm + "mvi " + defines.BOOLEAN_TRUE.ToString("X2") + "h\r\n";//download final result and store in lhs
                    strasm = strasm + "sta " + symbol.address.ToString("X2") + "h\r\n";
                    break;
                }
            }
            return strasm;
        }
        else if (var_right.Equals("false"))
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_left) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having smae scope
                {
                    strasm = strasm + "mvi " + defines.BOOLEAN_FALSE.ToString("X2") + "h\r\n";//download final result and store in lhs
                    strasm = strasm + "sta " + symbol.address.ToString("X2") + "h\r\n";
                    break;
                }
            }
            return strasm;
        }

        if (var_right.Equals("null"))
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_left) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having smae scope
                {
                    strasm = strasm + "mvi " + defines.NULL.ToString("X2") + "h\r\n";//download final result and store in lhs
                    strasm = strasm + "sta " + symbol.address.ToString("X2") + "h\r\n";
                    break;
                }
            }
            return strasm;
        }


        if (var_right.Equals("_RegA_"))
        {

            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_left) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having smae scope
                {
                    strasm = strasm + "lda " + n.left.address.ToString("X2") + "h\r\n";//download final result and store in lhs
                    strasm = strasm + "sta " + symbol.address.ToString("X2") + "h\r\n";
                    break;
                }
            }

            return strasm;
        }


        foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
        {
            if (symbol.varname.Equals(var_left) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having same scope or declared in scope 1 and get address location
            {
                var_left_address = symbol.address;
                break;
            }
        }

        if (Regex.IsMatch(var_right, @"^(-?|\+?)[\d]+$"))//" check if numeric oprand
        {

            byte num = Byte.Parse(var_right);
            strasm = strasm + "mvi a," + num.ToString("X2") + "h\r\n";//set numeric in acc
            strasm = strasm + "sta " + var_left_address.ToString("X2") + "h\r\n";//store acc to memory location of left identifier
                    
            //Console.WriteLine();
            //Console.WriteLine(strasm);
            return strasm;
        }
        else //is a variable
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_right) && (symbol.scope == scope || symbol.scope == 1))  //find left variable variable in symbol table having same scope or declared in scope 1 and get address location
                {
                    strasm = strasm + "lda " + symbol.address.ToString("X2") + "h\r\n";
                    strasm = strasm + "sta " + var_left_address.ToString("X2") + "h\r\n";
                    //Console.WriteLine();
                    //Console.WriteLine(strasm);
                    break;
                }
            }
            //return strasm;
        }

        if(strasm.Length == 0)//if 0 mean then assign value to identifier
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_left) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having same scope or declared in scope 1 and get address location
                {
                    strasm = strasm + "lda " + n.left.address.ToString("X2") + "h\r\n";
                    strasm = strasm + "sta " + symbol.address.ToString("X2") + "h\r\n";
                    break;
                }
            }

        }

        return strasm;

    }


    ///<summary>
    ///<paramref name="var_left"/>
    ///<paramref name="op"/>
    ///<paramref name="var_right"/>
    ///<paramref name="scope"/>
    ///Generate code when expression tree in form x=(identifier|=numeric)(oper)(identifier|=numeric)
    ///<return>string</return>
    ///</summary>
    string codegentwooperands(Node var_left, string op, Node var_right, int scope)
    {
        //string strasm = "";
        //ushort var_right_address = 0;




        switch (op)
        {
            case "+":return operator_add(var_left, var_right, scope);
                
            case "-":return operator_sub(var_left, var_right, scope);
                
            case "*":return operator_mul(var_left, var_right, scope);
                
            case "/":return operator_div(var_left, var_right, scope);

            case "^": return operator_bitwise_xor(var_left, var_right, scope);

            case "~":return operator_bitwise_complement(var_right, scope);

            case "!":return operator_bitwise_NOT(var_right, scope);
            
            case "!=":return operator_not_equal(var_left, var_right, scope);
            
            case "==":return operator_equal(var_left, var_right, scope);
            
            case "<":return operator_lower(var_left, var_right, scope);
            
            case "<=":return operator_lower_or_equal(var_left, var_right, scope);
            
            case ">":return operator_bigger(var_left, var_right, scope);
            
            case ">=":return operator_bigger_or_equal(var_left, var_right, scope);
            
            case "&":return operator_bitwise_and(var_left, var_right, scope);
            
            case "&&":return operator_logical_and(var_left, var_right, scope);
            
            case "|":return operator_bitwise_or(var_left, var_right, scope);
            
            case "||":return operator_logical_or(var_left, var_right, scope);
            
            default: return "";
        }
        return "";
    }


    //!a or NOT a
    //a->acc or immediate 1 or 0
    //result in acc
    string operator_bitwise_NOT(Node var_right, int scope)
    {
        string strasm = "";

        if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "cma " + "\r\n";//no need to lda since acc is unchanged due to unary op.
            strasm = strasm + "ani 01h" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" check if numeric oprand
        {
            strasm = strasm + "mvi a," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";
            strasm = strasm + "cma " + "\r\n";
            strasm = strasm + "ani 01h" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;

        }
        else
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_right.getValue()) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having same scope anf get address location
                {
                    strasm = strasm + "lda " + symbol.address.ToString("X2") + "h\r\n";//download final result and store in lhs
                    strasm = strasm + "cma " + "\r\n";
                    strasm = strasm + "ani 01h" + "\r\n";
                    strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
                    return strasm;
                }
            }
        }
        return strasm;
    }

    //~a or complement a
    //a->acc or immediate 1 or 0
    //result in acc
    string operator_bitwise_complement( Node var_right, int scope)
    {
        string strasm = "";

        if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "cma " + "\r\n";//no need to lda since acc is unchanged due to unary op.
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" check if numeric oprand
        {
            strasm = strasm + "mvi a," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";
            strasm = strasm + "cma " + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;

        }
        else
        {
            foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
            {
                if (symbol.varname.Equals(var_right.getValue()) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having same scope anf get address location
                {
                    strasm = strasm + "lda " + symbol.address.ToString("X2") + "h\r\n";//download final result and store in lhs
                    strasm = strasm + "cma " + "\r\n";
                    strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
                    return strasm;
                }
            }
        }
        return strasm;
    }

        //a^b a xor b
        //a->acc  b->regb  or immediate
        //result in acc
        string operator_bitwise_xor(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "xra b" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }

        if (var_left.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "xra M" + "\r\n";
            }
            else
                strasm = strasm + "xri " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            {
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "xra M" + "\r\n";
            }
            else// is numeric
            {
                strasm = strasm + "xri " + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b
            }

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }



        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            strasm = strasm + "lda " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//left operand goes to accumulator

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "xra M" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=identifier 
        else
            strasm = strasm + "xra b" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric  

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }

    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a && b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_logical_or(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call logicalor" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call logicalor" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call logicalor" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call logicalor" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }




    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a && b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_logical_and(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call logicaland" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call logicaland" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call logicaland" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call logicaland" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    //a or b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_bitwise_or(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "ora b" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }

        if (var_left.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "add M" + "\r\n";
            }
            else
                strasm = strasm + "ori " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            {
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "ana M" + "\r\n";
            }
            else// is numeric
            {
                strasm = strasm + "ori " + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b
            }

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }



        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            strasm = strasm + "lda " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//left operand goes to accumulator

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "ora M" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=identifier 
        else
            strasm = strasm + "ora b" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric  

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a^b or a AND b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_bitwise_and(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "ana b" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }

        if (var_left.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "add M" + "\r\n";
            }
            else
                strasm = strasm + "ani " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            {
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "ana M" + "\r\n";
            }
            else// is numeric
            {
                strasm = strasm + "ani " + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b
            }

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }



        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            strasm = strasm + "lda " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//left operand goes to accumulator

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "ana M" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=identifier 
        else
            strasm = strasm + "ana b" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric  

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }

    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a>=b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_bigger_or_equal(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call isbiggerorequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call isbiggerorequal" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call isbiggerorequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call isbiggerorequal" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }

    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a<=b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_lower_or_equal(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call islowerorequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call islowerorequal" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call islowerorequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call islowerorequal" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a!=b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_not_equal(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call isnotequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call isnotequal" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call isnotequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call isnotequal" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a==b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_equal(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call isequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call isequal" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call isequal" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call isequal" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }



    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a<b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_bigger(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call isbigger" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call isbigger" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->regb\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call isbigger" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n";
            strasm = strasm = strasm + "mov b,a" + ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n";

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n";
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n";

        strasm = strasm + "call isbigger" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a<b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>

    string operator_lower(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call islower" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

             if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg b\r\n";// 
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg b\r\n";//right operand goes to reg b

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h;lhs->a\r\n";
            strasm = strasm + "call islower" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))// 
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + ";rhs->b\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg a\r\n";//left operand goes to accumulator
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;lhs->reg a\r\n";//right operand goes to reg b

            strasm = strasm + "call islower" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;rhs->reg a\r\n"; 
            strasm = strasm = strasm + "mov b,a"+ ";rhs->reg b\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;rhs->reg a\r\n"; 

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;lhs->reg b\r\n"; 
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";lhs->reg b\r\n"; 

        strasm = strasm + "call islower" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a/b
    ///a->acc
    ///b->reg b
    ///result = acc
    ///<return>string</return>
    ///</summary>
    string operator_div(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;


        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "call divide" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }


        if (var_left.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);




            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h ;multiplier location\r\n";//left operand goes to accumulator
                strasm = strasm + "mov b,m" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;multiplier\r\n";//right operand goes to reg b

            strasm = strasm + "call divide" + "\r\n";

            strasm = strasm + "sta " +this.result_address.ToString("X2")+"h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))//multiplier stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            strasm = strasm + "mov b,a" + ";divisor\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;dividend\r\n";
            else
                strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;dividend\r\n";//

            strasm = strasm + "call divide" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }




        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;dividend\r\n";//dividend
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";dividend\r\n";//dividend

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lxi h," + var_right_address.ToString("X2") + "h ;divisor\r\n";//divisor
            strasm = strasm + "mov b,m" + "\r\n";
        }
        else
            strasm = strasm + "mvi b, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;divisor\r\n";//divisor

        strasm = strasm + "call divide" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    ///<summary>
    ///<paramref name="operand"/>
    ///<paramref name="symboltable"/>
    ///<paramref name="scope"/>
    ///return address of an identifier
    ///<return>ushort address</return>
    ///</summary>
    ushort getAddress(string operand,SymbolTable symboltable,int scope)
    {
        ushort addr = 0;
        foreach (Symbol symbol in symboltable.symbols) //confirm type match from symbol
        {
            if (symbol.varname.Equals(operand) && (symbol.scope == scope || symbol.scope == 1)) //find left variable in symbol table having same scope anf get address location
            {
                addr = symbol.address;
                break;
            }
        }
        return addr;
    }


    ///<summary>
    ///<paramref operand="var_left"/>
    ///<paramref operand="var_right"/>
    ///<paramref scope="scope"/>
    ///a*b
    ///a->reg b
    ///b->reg c
    ///result = acc
    ///<return>string</return>
    ///</summary>
    string operator_mul(Node var_left, Node var_right, int scope)
    {
        string strasm_left = "";
        string strasm_right = "";
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;


        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov c,a" + "\r\n";
            strasm = strasm + "lda " +  var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "call multiplication" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }

        if (var_left.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

            strasm = strasm + "mov b,a" + ";multiplicand\r\n";
            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;multiplier\r\n";//left operand goes to accumulator
                strasm = strasm + "mov c,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi c," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;multiplier\r\n";//right operand goes to reg b

            strasm = strasm + "call multiplication" + "\r\n";

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))//multiplier stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
                        var_left_address = getAddress(var_left.getValue(), symboltable, scope);

            strasm = strasm + "mov c,a" + ";multiplier\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;muliplicant\r\n";//left operand goes to accumulator
                strasm = strasm + "mov b,a" + "\r\n";
            }
            else
                strasm = strasm + "mvi b," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h ;muliplicant\r\n";//right operand goes to reg b

            strasm = strasm + "call multiplication" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);

  

        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_left_address.ToString("X2") + "h ;multiplicant\r\n";//multiplicant
            strasm = strasm + "mov b,a" + "\r\n";
        }
        else
            strasm = strasm + "mvi b," + Convert.ToByte(var_left.getValue()).ToString("X2") + ";multiplicant\r\n";//multiplicant

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
        {
            strasm = strasm = strasm + "lda " + var_right_address.ToString("X2") + "h ;multiplier\r\n";//multiplier
            strasm = strasm + "mov c,a" + "\r\n";
        }
        else
            strasm = strasm + "mvi c, " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h ;multiplier\r\n";//multiplier

        strasm = strasm + "call multiplication" + "\r\n";

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }


    //a+b
    //a->acc  b->regb  or immediate
    //result in acc
    string operator_add(Node var_left, Node var_right, int scope)
    {
        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " +var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a"+ "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "add b" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }

        if (var_left.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            {
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);
                strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "add M" + "\r\n";
            }
            else
                strasm = strasm + "adi " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {

            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            {
                        var_left_address = getAddress(var_left.getValue(), symboltable, scope);
                        strasm = strasm + "lxi h, " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                        strasm = strasm + "add M" + "\r\n";
            }
            else// is numeric
            {
                strasm = strasm + "adi " + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b
            }

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }



        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
                    var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            strasm = strasm + "lda " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//left operand goes to accumulator

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b


        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm + "add M" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=identifier 
        else
            strasm = strasm + "add b" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric  

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

        return strasm;
    }
    //a-b
    //a->acc  b->regb   or immediate
    //result in acc
    string operator_sub(Node var_left, Node var_right, int scope)
    {

        string strasm = "";
        ushort var_left_address = 0;
        ushort var_right_address = 0;

        if (var_left.getValue().Equals("_RegA_") && var_right.getValue().Equals("_RegA_"))//multiplicant stored in accumulator
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";
            strasm = strasm + "mov b,a" + "\r\n";
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";
            strasm = strasm + "sub b" + "\r\n";
            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }

        if (var_left.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_left.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            {
                var_right_address = getAddress(var_right.getValue(), symboltable, scope);

                strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "sub M" + "\r\n";
            }

            else// is numeric
               strasm = strasm + "sui " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
            return strasm;
        }
        else if (var_right.getValue().Equals("_RegA_"))
        {
            strasm = strasm + "lda " + var_right.address.ToString("X2") + "h\r\n";

            if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            {

                var_left_address = getAddress(var_left.getValue(), symboltable, scope);
                strasm = strasm + "mov b,a " + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric
                strasm = strasm + "lda " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
                strasm = strasm + "sub b" + "\r\n";
            }
            else// is numeric
            {
                strasm = strasm + "mov b,a " + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric
                strasm = strasm + "mvi a, " + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric
                strasm = strasm + "sub b" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric

            }

            strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";

            return strasm;
        }


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
           var_left_address = getAddress(var_left.getValue(), symboltable, scope);

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
           var_right_address = getAddress(var_right.getValue(), symboltable, scope);


        if (!Regex.IsMatch(var_left.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric
            strasm = strasm + "lda " + var_left_address.ToString("X2") + "h\r\n";//left operand goes to accumulator
        else
            strasm = strasm + "mvi a," + Convert.ToByte(var_left.getValue()).ToString("X2") + "h\r\n";//left operand goes to accumulator

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))//" true if identifier false if numeric(do nothing yet)
            strasm = strasm + "lxi h, " + var_right_address.ToString("X2") + "h\r\n";//right operand goes to b
        else
            strasm = strasm + "mvi b," + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//right operand goes to reg b

        if (!Regex.IsMatch(var_right.getValue(), @"^(-?|\+?)[\d]+$"))
            strasm = strasm+"sub M" + "\r\n";//store addition result in accumulator //if (left=num or identifier) and right=identifier 
        else
            strasm = strasm + "sui " + Convert.ToByte(var_right.getValue()).ToString("X2") + "h\r\n";//store addition result in accumulator //if (left=num or identifier) and right=numeric

        strasm = strasm + "sta " + this.result_address.ToString("X2") + "h\r\n";
        return strasm;
    }
}
