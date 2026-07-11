using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/*   
    https://www.coursera.org/learn/nand2tetris2/lecture/QM0lZ/unit-4-2-lexical-analysis
    https://stackoverflow.com/questions/894263/identify-if-a-string-is-a-number  Regex.IsMatch(input, @"^\d+$") Regex.IsMatch(input, @"\d")
    https://en.wikipedia.org/wiki/Lexical_analysis
    https://byjus.com/gate/lexical-analysis/#:~:text=Lexical%20analysis%20is%20the%20starting,whitespace%20in%20the%20source%20code.
    */


namespace myCJCompiler
{
    internal class Tokenizer
    {
    
        private char[] symbols = { ' ', '!', '#', '%', '&', '(', ')', '*', '+', '-', '.', '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '`', '{', '|', '}', '~' };
        private string[] keywords = { "main", "class", "constructor", "function", "method", "field", "static", "var", "int", "char", "string", "bool", "void", "true", "false", "null", "this", "for", "let", "do", "if", "else", "while", "return" };
        //integerConstant decimal from 0 to inf
        //StringConstant "string"
        //identifier string
        //private string[] token_type = { "keywords", "symbols", "identifier", "integer", "float", "double", "string" };

        private List<Tuple<string, string>> tokens = new List<Tuple<string, string>>();
        
        
        //byte[] b;
        ///<summary>
        ///<paramref name="c"/>
        ///Check if character is an operator character
        ///</summary>
        ///<returns>
        ///True if character is an operator
        ///False if character is not an operator
        ///</returns>
        bool has_operator(char c)
        {
            foreach (char op in symbols)
            {
                if (c == op) return true;
            }
            return false;
        }


        ///<summary>
        ///Derive type of token
        ///</summary>
        ///<returns>
        ///Symbol,Keyword,Numeric,String or Identifier 
        ///</returns>
        private string getType(string s)//order is very important here
        {
            string sType;
/*
            sType = "Unary";
            if (Regex.IsMatch(s, @"(--)|[+][+]|(!=)"))
                return sType;

            sType = "Relational";
            if (Regex.IsMatch(s, @"[<]|[<][=]|[>]|[>][=]|[=][=]|[!][=]"))//no code yet
                return sType;

            sType = "Assigment";
            if (Regex.IsMatch(s, @"[=]|[+][=]|[-][=]|[*][=]|[/][=]|[%]]"))//no code yet
                return sType;

            sType = "Logical";
            if (Regex.IsMatch(s, @"[&][&]|[|][|]|[!]"))//no code yet
                return sType;

            sType = "Bitwise";
            if (Regex.IsMatch(s, @"[&]|[|]|[<][<]|[>][>]|[~]|['^']"))//no code yet
                return sType;

            sType = "Arithmetic";
            if (Regex.IsMatch(s, @"[+]|[-]|[*]|[/]|[%]"))
                return sType;
*/

            sType = "symbol";
            foreach (char op in symbols)
            {
                if (s.Equals(op.ToString()))
                    return sType;
            }

            sType = "keyword";
            foreach (string keyword in keywords)
            {
                if (s.Equals(keyword))
                    return sType;
            }

            sType = "Numeric Constant";
            if (Regex.IsMatch(s, @"^(-?|\+?)[\d]+$"))
                return sType;


            sType = "String Constant";
            if (s.StartsWith('"'))
                return sType;


            sType = "Identifier";// if not any of those then assume is Identifier
            return sType;


            //int.TryParse(s, out _);

        }


        public void GenerateTokens(byte[] byte_source)
        {
            string str = "";

            for (int index = 0; index < byte_source.Length; index++)//X1...Xi e L(R)
            {
                if (((char)byte_source[index] >= 32 & (char)byte_source[index] <= 126))///<summary>if character is set of text character process token or identifer else skip character</summary>
                {
                    if (!has_operator((char)byte_source[index]) )///<summary>if character is not an operator then assume is part of an identifier or keyword</summary>
                    {
                        if ((char)byte_source[index] == '"')//if quote detected assume start for string and loop till ; or end of line
                        {
                            while ((char)byte_source[index] != ';' & (char)byte_source[index] != '\r')//loop till ; or end of line
                            {
                                str = str + (char)byte_source[index];///<summary>build identifier or keyword string</summary>
                                index++;

                            }
                            index--;
                        }
                        else
                            str = str + (char)byte_source[index];///<summary>build identifier or keyword string</summary>
                    }
                    else//process symbols as tokens
                    {
                        if (str.Length != 0)//save string before handling symbol
                        {
                            //tokens.Add(str);
                            tokens.Add(new Tuple<string, string>(str, getType(str)));
                            str = "";
                        }


                        if ((char)byte_source[index] == '/' & (char)byte_source[index+1] == '/')//detect comment line
                        {
                            while ( (char)byte_source[index] != '\r')//loop till ; or end of line
                                index++;

                        }
                        else if ((char)byte_source[index] == '/' & (char)byte_source[index + 1] == '*')//detect comment multi line
                        {
                            while ((char)byte_source[index] != '*' | (char)byte_source[index + 1] != '/')//loop till */
                                index++;
                            index++;
                        }
                        else if ((char)byte_source[index] != ' ')///<summary>Ignore blank spaces</summary>
                        {
                                str = str + (char)byte_source[index];///<summary>build operator and store in token list</summary>
                                tokens.Add(new Tuple<string, string>(str, getType(str)));
                                str = "";

                            
                        }

                    }

                }
            }
        }

        public List<Tuple<string, string>> GetTokens()
        {
            return tokens;
        }

        public void ShowTokens()
        {
            int i = 0;
            foreach (var token in tokens)
            {
                //var name = token.Item1;
                //var value = token.Item2;
                Console.WriteLine(i+" : "+token);
                i++;
            }

        }

    }
}
