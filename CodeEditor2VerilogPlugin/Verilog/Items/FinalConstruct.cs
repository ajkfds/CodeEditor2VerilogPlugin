using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class FinalConstruct : IDocumentRegeion
    {
        protected FinalConstruct() { }

        // final_construct ::= final function_statement
        // function_statement ::= statement
        public Statements.IStatement? Statement { get; protected set; }

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            Items.FinalConstruct? initial = Items.FinalConstruct.ParseCreate(word, nameSpace);
            return true;
        }

        public static FinalConstruct? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //    initial_construct   ::= initial statement
            System.Diagnostics.Debug.Assert(word.Text == "final");
            IndexReference beginIndexReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            FinalConstruct finalConstruct = new FinalConstruct() { BeginIndexReference = beginIndexReference };
            finalConstruct.Statement = Statements.Statements.ParseCreateStatement(word, nameSpace);
            if (finalConstruct.Statement == null)
            {
                word.AddError("illegal initial construct");
                return null;
            }
            finalConstruct.LastIndexReference = word.CreateIndexReferenceBefore();
            // if the statement has its own region end (e.g. "end" of begin..end block),
            // use it (same rule as AlwaysConstruct)
            if (finalConstruct.Statement is Items.IDocumentRegeion statementRegion && statementRegion.LastIndexReference != null)
            {
                finalConstruct.LastIndexReference = statementRegion.LastIndexReference;
            }
            if (!word.Prototype && word.CompletionContext == null) nameSpace.DocumentRegions.Add(finalConstruct);
            return finalConstruct;
        }
    }
}
