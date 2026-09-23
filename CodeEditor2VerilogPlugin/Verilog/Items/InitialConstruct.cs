using CodeEditor2.CodeEditor.CodeComplete;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class InitialConstruct
    {
        protected InitialConstruct() { }
        public Statements.IStatement? Statement { get; protected set; }

        public static bool Parse(WordScanner word, NameSpace nameSpace, CompletionContext? completionContext = null)
        {
            Items.InitialConstruct? initial = Items.InitialConstruct.ParseCreate(word, nameSpace, completionContext);
            return true;
        }

        public static InitialConstruct? ParseCreate(WordScanner word, NameSpace nameSpace, CompletionContext? completionContext = null)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "initial");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            InitialConstruct initial = new InitialConstruct();
            initial.Statement = Statements.Statements.ParseCreateStatement(word, nameSpace, null, null, completionContext);
            if (initial.Statement == null)
            {
                word.AddError("illegal initial construct");
                return null;
            }
            return initial;
        }
    }
}
