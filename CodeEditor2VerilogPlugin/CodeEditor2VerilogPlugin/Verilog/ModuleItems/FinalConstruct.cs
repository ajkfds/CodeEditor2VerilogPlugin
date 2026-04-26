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
            ModuleItems.FinalConstruct? initial = await ModuleItems.FinalConstruct.ParseCreate(word, nameSpace);
            return true;
        }

        public static async Task<FinalConstruct?> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "final");
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
