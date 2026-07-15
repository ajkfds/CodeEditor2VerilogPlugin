using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class InitialConstruct
    {
        protected InitialConstruct() { }
        public Statements.IStatement? Statement { get; protected set; }

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            Items.InitialConstruct? initial = Items.InitialConstruct.ParseCreate(word, nameSpace);
            return true;
        }

        public static InitialConstruct? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "initial");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            InitialConstruct initial = new InitialConstruct();
            initial.Statement = Statements.Statements.ParseCreateStatement(word, nameSpace);
            if (initial.Statement == null)
            {
                word.AddError("illegal initial construct");
                return null;
            }
            return initial;
        }
    }
}
