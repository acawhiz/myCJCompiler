using System;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Linq;
//https://www.youtube.com/watch?v=Hb0ROzKywW4

///<summary>
///Class:BinaryTree used class Node
///Generate arithmetic binary tree
///</summary>
public class TreeGenerator
{//https://www.bytehide.com/blog/dispose-method-csharp
    //string expr = "x=1+(2*3)+4;";
    //string expr = "x=(1*2)+(3*4);";
    //string expr = "x=1+((2*3)/4);";
    //string expr = "x=(1*2);";
    //string expr = "x=10;";
    private string expr;// = "x=1+((2*3)/4);";
    private BinaryTree tree;// = new BinaryTree();
    private int master_index;//global position of experession array
    private string var_num = "";
    private string lhs = "";//left handside of expression
    private int token_count = 0;

       public TreeGenerator()
    {
        tree = new BinaryTree();
    }
    public void Clear()//will come back to this to ensure resources are released
    {
        tree = new BinaryTree();
    }

    ///<summary>
    ///<paramref name="index"/>
    ///Generates token from an expression string
    ///<return>string token</return>
    ///</summary>
    private string next_token(int index)
    {
        string str = "";
        for (int j = index; j < expr.Length; j++)
        {
            master_index = j;
            switch (expr[j].ToString())
            {

                case ">":
                case "<":
                    if (str.Length == 0)//check for bitwise operator > >> < << >= <=
                    {
                        if (expr[j + 1].ToString().Equals("=") || expr[j + 1].ToString().Equals(">") || expr[j + 1].ToString().Equals("<"))
                        {
                            master_index = j + 2;//move index to point to next operator symbol
                            return expr[j].ToString() + expr[j + 1].ToString();
                        }
                        else
                        {
                            master_index = j + 1;
                            return expr[j].ToString();
                        }
                    }
                    else
                        return str;

                case "!":
                case "~":
                    if (str.Length == 0)//check for bitwise operator  ! != ~
                    {
                        if (expr[j + 1].ToString().Equals("="))
                        {
                            master_index = j + 2;//move index to point to next operator symbol
                            return expr[j].ToString() + expr[j + 1].ToString();
                        }
                        else
                        {
                            master_index = j + 1;
                            return expr[j].ToString();
                        }
                    }
                    else
                        return str;

                case "=":
                case "&":
                case "|":
                    if (str.Length == 0)//check for bitwise operator = ==  & && | ||
                    {
                        if (expr[j + 1].ToString().Equals("=") || expr[j + 1].ToString().Equals("&") || expr[j + 1].ToString().Equals("|"))
                        {
                            master_index = j + 2;//move index to point to next operator symbol
                            return expr[j].ToString() + expr[j + 1].ToString();
                        }
                        else
                        {
                            master_index = j + 1;
                            return expr[j].ToString();
                        }
                    }
                    else
                        return str;
                case "^":
                case "+":
                case "-":
                case "*":
                case "/":
                case ";":
                case "(":
                case ")":

                    if (str.Length == 0)
                    {
                        master_index = j + 1;
                        return expr[j].ToString();
                        break;
                    }
                    else
                        return str;
                    break;
                default:
                    str = str + expr[j];
                    break;
            }

        }
        if (master_index >= expr.Length - 1)
            master_index = -1;
        return str;
    }


    ///<summary>
    ///<paramref name="expr"/>
    ///Generates arithmetic expression parsing tree
    ///<return>BinaryTree</return>
    ///</summary>
    public BinaryTree genererateTree(string expr)
    {
        this.expr = expr;
        lhs = "";//left handside of expression
        token_count = 0;
        master_index = 0;

        while (master_index != -1)
        {


            string token = next_token(master_index);
            Console.Write(token);

            switch (token)
            {

                case "":
                    break;
                case ";":
                    if (token.Equals(";"))
                        tree.addNode(lhs);
                    break;
                case "(":
                    tree.open_par();
                    break;

                case ")":
                    tree.close_par();
                    break;
                case "!":
                case "~":
                    tree.addNode("N/A");// add blank node to be left of root
                    tree.addNode(token);// then operator as root
                    break;

                default:
                    if (token_count <= 1)//assume first two tokens are left hand assignment (x=)
                        lhs = lhs + token;
                    else
                        tree.addNode(token);
                    break;
            }
            token_count++;
        }

        return tree;
    }


}
