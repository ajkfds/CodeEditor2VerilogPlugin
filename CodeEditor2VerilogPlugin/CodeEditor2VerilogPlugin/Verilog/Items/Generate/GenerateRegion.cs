using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items.Generate
{
    public class GenerateRegion
    {
        // generate_region ::=
        //      "generate" { generate_item } "endgenerate"

        public static async System.Threading.Tasks.Task ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "generate") throw new System.Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (!await GenerateItem.ParseAsync(word, nameSpace)) break;
            }

            if (word.Text == "endgenerate")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                word.AddError("endgenerate missing");
            }

            return;
        }
    }
}
