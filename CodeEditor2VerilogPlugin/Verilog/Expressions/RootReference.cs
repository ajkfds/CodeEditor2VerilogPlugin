using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    /// <summary>
    /// SystemVerilog hierarchical root reference: $root.a.b.c
    /// (IEEE 1800-2017 23.8 top-level scope / $root).
    /// The "root" scope is not a user name space in this parser, so the whole
    /// "$root[.identifier]*" path is consumed as an opaque reference to avoid
    /// "undefined" / "( required" errors when used as a function argument
    /// (e.g. $coverage_control(..., $root.top.unit1)).
    /// </summary>
    public class RootReference : Primary
    {
        protected RootReference() { }

        public List<string> Path = new List<string>();

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("$root", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));
            foreach (string p in Path)
            {
                label.AppendText("." + p, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            }
        }

        public override string CreateString()
        {
            string ret = "$root";
            foreach (string p in Path) ret += "." + p;
            return ret;
        }

        public static RootReference? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "$root") return null;

            RootReference reference = new RootReference();
            reference.Reference = word.GetReference();
            reference.Constant = false;

            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext(); // $root

            // ".identifier" hierarchical chain
            while (word.Text == ".")
            {
                word.MoveNext(); // .
                if (!General.IsIdentifier(word.Text)) break;
                reference.Path.Add(word.Text);
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            return reference;
        }
    }
}
