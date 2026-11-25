using pluginVerilog.Verilog.DataObjects;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.Expressions
{
    /// <summary>
    /// SystemVerilog 18.12 std::randomize() scope randomization.
    ///   std::randomize( [ variable_identifier_list ] ) [ with { constraint_block } ]
    /// The call returns int (1 on success). The argument identifiers are
    /// scope variables that already exist, so no extra name space binding is
    /// required; the "with" constraint block is consumed without resolution.
    /// </summary>
    public class StdRandomizeCall : Primary
    {
        protected StdRandomizeCall() { }

        public override void AppendLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText("std::randomize()", VerilogColorType.Identifier);
        }

        public override string CreateString()
        {
            return "std::randomize()";
        }

        public static StdRandomizeCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "std" || word.NextText != "::") return null;

            WordReference beginRef = word.GetReference();

            word.Color(VerilogColorType.Identifier);
            word.MoveNext(); // std
            word.MoveNext(); // ::
            word.Color(VerilogColorType.Identifier);
            word.MoveNext(); // randomize

            if (word.Text == "(")
            {
                word.MoveNext(); // (
                int parenDepth = 0;
                while (!word.Eof)
                {
                    if (word.Text == "(")
                    {
                        parenDepth++;
                    }
                    else if (word.Text == ")")
                    {
                        if (parenDepth == 0) { word.MoveNext(); break; }
                        parenDepth--;
                    }
                    else if (word.Text == "{" || word.Text == "}")
                    {
                        // constraint block inside an argument (unlikely) - keep balance
                    }
                    word.MoveNext();
                }
            }

            // [ with { constraint_block } ]
            if (word.Text == "with")
            {
                word.Color(VerilogColorType.Keyword);
                word.MoveNext(); // with
                if (word.Text == "{")
                {
                    int braceDepth = 0;
                    while (!word.Eof)
                    {
                        if (word.Text == "{")
                        {
                            braceDepth++;
                        }
                        else if (word.Text == "}")
                        {
                            braceDepth--;
                            word.MoveNext();
                            if (braceDepth == 0) break;
                            continue;
                        }
                        word.MoveNext();
                    }
                }
            }

            StdRandomizeCall call = new StdRandomizeCall();
            call.Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference());
            call.Constant = false;
            call.BitWidth = 32;
            return call;
        }
    }
}
