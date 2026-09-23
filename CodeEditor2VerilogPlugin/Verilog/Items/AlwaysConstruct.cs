using System.Threading.Tasks;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    public class AlwaysConstruct : IDocumentRegeion
    {
        protected AlwaysConstruct() { }
        public Statements.IStatement? Statement { get; protected set; }

        /* ## Verilog2001
            always_construct ::= always statement     */

        /* ## SystemVerilog
            always_construct ::= always_keyword statement 
            always_keyword ::= always | always_comb | always_latch | always_ff     
         */
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;

        public static AlwaysConstruct? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "always":
                    break;
                case "always_comb":
                case "always_latch":
                case "always_ff":
                    if (!word.SystemVerilog) word.AddError("Systemverilog Function");
                    break;
                default:
                    System.Diagnostics.Debug.Assert(true);
                    break;
            }


            //System.Diagnostics.Debug.Assert(word.Text == "always");
            IndexReference beginIndex = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            AlwaysConstruct always = new AlwaysConstruct() { BeginIndexReference = beginIndex };
            always.Statement = Statements.Statements.ParseCreateStatement(word, nameSpace);
            if (always.Statement == null)
            {
                word.AddError("illegal always construct");
                return null;
            }
            always.LastIndexReference = word.CreateIndexReferenceBefore();
            if (!word.Prototype) nameSpace.DocumentRegions.Add(always);
            return always;
        }
    }

}
