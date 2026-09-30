using CodeEditor2.CodeEditor.CodeComplete;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class InitialConstruct : IDocumentRegeion
    {
        protected InitialConstruct() { }
        public Statements.IStatement? Statement { get; protected set; }

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            Items.InitialConstruct? initial = Items.InitialConstruct.ParseCreate(word, nameSpace);
            return true;
        }

        public static InitialConstruct? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "initial");
            IndexReference beginIndexReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            InitialConstruct initial = new InitialConstruct() { BeginIndexReference = beginIndexReference };
            initial.Statement = Statements.Statements.ParseCreateStatement(word, nameSpace, null, null);
            if (initial.Statement == null)
            {
                word.AddError("illegal initial construct");
                return null;
            }
            initial.LastIndexReference = word.CreateIndexReferenceBefore();
            // if the statement has its own region end (e.g. "end" of begin..end block),
            // use it (same rule as AlwaysConstruct)
            if (initial.Statement is Items.IDocumentRegeion statementRegion && statementRegion.LastIndexReference != null)
            {
                initial.LastIndexReference = statementRegion.LastIndexReference;
            }
            if (!word.Prototype && word.CompletionContext == null) nameSpace.DocumentRegions.Add(initial);
            return initial;
        }
    }
}
