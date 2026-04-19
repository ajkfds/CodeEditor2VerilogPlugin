using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public class FinalConstruct
    {
        protected FinalConstruct() { }

        // final_construct ::= final function_statement
        // function_statement ::= statement
        public Statements.IStatement? Statement { get; protected set; }

        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            ModuleItems.InitialConstruct? initial = await ModuleItems.InitialConstruct.ParseCreate(word, nameSpace);
            return true;
        }

        public static async Task<FinalConstruct?> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "initial");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            FinalConstruct finalConstruct = new FinalConstruct();
            finalConstruct.Statement = await Statements.Statements.ParseCreateStatement(word, nameSpace);
            if (finalConstruct.Statement == null)
            {
                word.AddError("illegal initial construct");
                return null;
            }
            return finalConstruct;
        }
    }
}
