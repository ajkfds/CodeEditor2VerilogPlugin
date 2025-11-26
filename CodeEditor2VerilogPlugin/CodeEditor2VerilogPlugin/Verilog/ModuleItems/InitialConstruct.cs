using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public class InitialConstruct
    {
        protected InitialConstruct() { }
        public Statements.IStatement? Statement { get; protected set; }

        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            ModuleItems.InitialConstruct? initial = await ModuleItems.InitialConstruct.ParseCreate(word, nameSpace);
            return true;
        }

        public static async Task<InitialConstruct?> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "initial");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            InitialConstruct initial = new InitialConstruct();
            initial.Statement = await Statements.Statements.ParseCreateStatement(word, nameSpace);
            if (initial.Statement == null)
            {
                word.AddError("illegal initial construct");
                return null;
            }
            return initial;
        }
    }
}
