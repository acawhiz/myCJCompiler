using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace myCJCompiler
{
    internal class Parser
    {
        public List<Tuple<string, string,int>> variables = new List<Tuple<string, string,int>>();//varname,type
        //private List<Tuple<string, string>> tokens = tokenizer.GetTokens();
        public int curly_bracket_open = 0;
        public int curly_bracket_closed = 0;

        private int code_block_counter=0;// track code blocks. This is needed to check scope of declared variables
        private Stack<int> stack_scope = new Stack<int>(0);//track current code blocks/scope
        private int current_scope = 0;                                                  //

        ///<summary>
        ///Check if variables used were declared
        ///</summary>
        ///<returns>
        ///true if match is found or false if no match
        ///</returns>
        private bool find_variable(string identifier)
        {
            foreach (var variable in variables)
            {
                if (variable.Item1.Equals(identifier))
                    return true;
            }
            return false;
        }

        ///<summary>
        ///Check if instance of variable is unique in the scope
        ///</summary>
        ///<returns>
        ///true if match is found or false if no match
        ///</returns>
        private bool check_variable_instance(string identifier,int scope)
        {
            foreach (var variable in variables)
            {
                if (variable.Item1.Contains(identifier))
                    if (variable.Item3 == scope)
                    {
                        Console.WriteLine("Variable '{0}' already declared in scope '{1}'!", variable.Item1, variable.Item3);
                        return true; 
                    }
            }
            return false;
        }

        ///<summary>
        ///show declared variables and data type
        ///</summary>
        public void Show_variables()
        {
            foreach (var variable in variables)
                Console.WriteLine("variable:{0}({1}) scope:{2}",variable.Item1, variable.Item2, variable.Item3);    
        }


        ///<summary>
        ///Wrapper for function find_variable()
        ///</summary>
        ///<returns>
        ///true if match is found or false if no match
        ///</returns>
        private bool check_identifier(string str)
        {

            foreach (Match match in Regex.Matches(str, @"[a-zA-Z]\w*"))//return identifiers in string 
            {
                if(match.Value.Equals("true") || match.Value.Equals("false"))//in case of boolean assignment
                    return true;

                if (!find_variable(match.Value))//check if identifier was declared
                {
                    Console.WriteLine("Variable '{0}' not declared !!", match.Value);

                        //Console.WriteLine("MemoryMap.TEMPORARY_RESULT_STORAGE_RAM_TOP reached");
                        System.Environment.Exit(-2);


                    return false;
                }

            }
            return true;
        }

        ///<summary>
        ///Recursive function that traverse the token list and check the grammar
        ///</summary>
        ///<returns>
        ///true if match is found or false if no match
        ///</returns>
        public int advance_token_list(List<Tuple<string, string>> tokens, int index)
    {

        for (int i = index; i < tokens.Count; i++)
        {
            int result;
            switch (tokens[i].Item1)
            {
                case "main":
                case "void":
                        result = handle_main(tokens, 0);
                        if (result == 0)
                                return 0;
                            else
                            i = result;
                        break;
                case "{"://block of code

                        stack_scope.Push(current_scope);
                        code_block_counter++;
                        current_scope = code_block_counter;
                        
                        curly_bracket_open++;
                        result = advance_token_list(tokens, i + 1);
                        if (result == 0)
                            return 0;
                        else
                            i = result;
                        break;

                case "}":// end of a block main ,while ,if, else etc

                        current_scope=stack_scope.Pop();

                        curly_bracket_closed++;
                        Console.WriteLine("}");
                        return i;
                        break;
                case ";":// used later to count statements
                        break;
                case "bool":
                case "int":
                case "string":
                        result = handle_declare(tokens, i);
                        if (result == 0)
                            return 0;//advance_token = false;
                        else
                        {
                            if (check_variable_instance(tokens[i + 1].Item1, current_scope)) {
                                return 0;                            
                            }
                            else
                            {
                                variables.Add(new Tuple<string, string, int>(tokens[i + 1].Item1/*variable name*/, tokens[i].Item1/*variable name*/, current_scope));
                                i = result;
                            }
                        }
                        break;
                case "while":
                case "if":
                        result = handle_while_if(tokens, i);
                        if (result == 0)
                            return 0;
                        else
                            i = result;
                        break;
                case "else":
                    result = handle_else(tokens, i);
                    if (result == 0)
                        return 0;
                    else
                        i = result;
                    break;
                case "for":
                        result = handle_for(tokens, i);
                        if (result == 0)
                            return 0;
                        else
                            i = result;
                        break;

                default:
                    result = handle_expr(tokens, i);
                    if (result == 0)
                        return 0;
                    else
                        i = result;

                    break;
            }


        }

        return tokens.Count;


    }



//}
//https://regex101.com/
    int handle_declare(List<Tuple<string, string>> tokens, int i)
    {
            string str = "";
            int token_index = i;
            while (!tokens[token_index].Item1.Equals(";"))
            {//build string of tokens from index i to ;
                str = str + tokens[token_index].Item1;
                token_index++;
            }
            str = str + ";";
            //(string""\D\w*"");"
            if (Regex.IsMatch(str, @"(int)\D\w*;|(string\D\w*);|(bool\D\w*);"))
            {
                Console.WriteLine(" accepted : " + str);
                return token_index - 1;
            }
            else
            {
                Console.WriteLine("Variable declaration error at token: " + i);
                Console.WriteLine(" Rejected : " + str);
                return 0;
            }

    }

    int handle_expr(List<Tuple<string, string>> tokens, int i)
    {
            string str = "";
            int token_index = i;
            while (!tokens[token_index].Item1.Equals(";") )
            {//build string of tokens from index i to ;
                str = str + tokens[token_index].Item1;
                token_index++;
                if (token_index == tokens.Count ) { return 0; }//prevent out of boundary scan
            }
            str = str + ";";


            //(string""\D\w+"");"
            if (Regex.IsMatch(str, @"^([a-zA-Z]\w*)=~*!*\(*(([a-zA-Z]\w*)|\d+)\)*((-|\+|\/|\*|\%|\^|\\|<|==|>|>=|<=|&|&&|!|!=|\^|\|\|*)~*\(*~*!*([a-zA-Z]\w*|\d+)\)*)*;"))//variable=numeric|variable opt( operator(numeric|variable)* );
            {
                if(!check_identifier(str))
                    return 0;
                Console.WriteLine(" accepted : " + str);
                return token_index - 1;
            }
            else if (Regex.IsMatch(str, @"^\D(\w*)=""(\w|\s)+"";"))
            {//match string assginment str="string constant";
                if (!find_variable(tokens[i].Item1))//check if string identifier is declared
                    return 0;
                Console.WriteLine(" accepted : " + str);
                return token_index - 1;
            }
            else
            {
                Console.WriteLine("Assignment error at token: " + i);
                Console.WriteLine(" Rejected : " + str);
                System.Environment.Exit(-2);
                return 0;
            }
    }

    int handle_main(List<Tuple<string, string>> tokens, int i)
    {
            string str = "";
            int token_index = i;

            while (!tokens[token_index].Item1.Equals("{"))
            {//build string of tokens from index i to ;
                //search for data in condition
                str = str + tokens[token_index].Item1;
                token_index++;
            }

            //str = str + "{";

            if (Regex.IsMatch(str, @"main\(\)"))
            {
                Console.WriteLine(" accepted : " + str + "{");
                //return advance_token_list(tokens, token_index);
                return token_index - 1;//backtrack one token )<-{
            }
            else
            {
                Console.WriteLine("Assignment error at token: " + i);
                Console.WriteLine(" Rejected : " + str);
                Console.WriteLine("Main entry point not detected");
                return 0;
            }
    }
    int handle_while_if(List<Tuple<string, string>> tokens, int i)
    {
            string str = "";
            int token_index = i;

            //List<string> conditional_expression = new List<string>();
            while (!tokens[token_index].Item1.Equals("{"))
            {//build string of tokens from index i to ;
                //search for data in condition
                str = str + tokens[token_index].Item1;
                token_index++;
            }

            str = str + "{";
            if (Regex.IsMatch(str, @"^(while|if)\(\(*(!*|\~*)\(*([a-zA-Z]\w*|\d+)((<|==|>|>=|<=|&|&&|!|\^|\|\*|-|\+|\/|\*|\%|\^|\\|\|\|*)\(*(!*|\~*)\(*([a-zA-Z]\w*|\d+)\)*)*\){"))
            {
                Console.WriteLine(" accepted : " + str);
                return token_index - 1;
            }
            else
            {
                Console.WriteLine("Assignment error at token: " + i);
                Console.WriteLine(" Rejected : " + str);
                return 0;
            }
    }

    int handle_else(List<Tuple<string, string>> tokens, int i)
    {
        string str = "";
        int token_index = i;

            str = tokens[token_index - 1].Item1;
            while (!tokens[token_index].Item1.Equals("{"))
            {//build string of tokens from index i to ;
                //search for data in condition
                str = str + tokens[token_index].Item1;
                token_index++;
            }

            str = str + "{";
            if (Regex.IsMatch(str, @"^}else{"))
            {
                Console.WriteLine(" accepted : else{" );
                return token_index - 1;
            }
            else
            {
                Console.WriteLine("Assignment error at token: " + i);
                Console.WriteLine(" Rejected : " + str);
                return 0;
            }
    }

        int handle_for(List<Tuple<string, string>> tokens, int i)
    {
            string str = "";
            int token_index = i;
            while (!tokens[token_index].Item1.Equals("{"))
            {//build string of tokens from index i to )
                //search for data in condition
                str = str + tokens[token_index].Item1;
                token_index++;
            }

            str = str + "{";
            if (Regex.IsMatch(str, @"^for\((int|long|byte)[a-zA-Z]\w*=(\d+|[a-zA-Z]\w*);(\d+|[a-zA-Z]\w*)(==|<=|>=|<|>)\(*(([a-zA-Z]\w*)|\d+)\)*((-|\+|\/|\*|\%|\^|\\|<|==|>|>=|<=|&|&&|!|\^|\|\|*)\(*([a-zA-Z]\w*|\d+)\)*)*;([a-zA-Z]\w*)=\(*(([a-zA-Z]\w*)|\d+)\)*((-|\+|\/|\*|\%|\^|\\|<|==|>|>=|<=|&|&&|!|\^|\|\|*)\(*([a-zA-Z]\w*|\d+)\)*)*\){"))
            {
                Console.WriteLine(" accepted : " + str);
                variables.Add(new Tuple<string, string, int>(tokens[i + 3].Item1, tokens[i + 2].Item1, code_block_counter + 1));// add declared variable to variabel list and increase scope to next exptected code block
                return token_index - 1;
            }
            else
            {
                Console.WriteLine("Assignment error at token: " + i);
                Console.WriteLine(" Rejected : " + str);
                return 0;
            }
    }


    }
}
