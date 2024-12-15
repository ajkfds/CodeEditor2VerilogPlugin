using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class VoidFunctionCall : IStatement
    {
        public void DisposeSubReference()
        {
        }

        public static VoidFunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "void") throw new Exception();
            VoidFunctionCall voidFunctionCall = new VoidFunctionCall();
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Eof || word.Text != "(")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            FunctionCall? func = FunctionCall.ParseCreate(word, nameSpace, nameSpace);

            if (word.Eof || func == null || word.Text != ")")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            if(word.Text == ";")
            {
                word.MoveNext();
            }else{
                word.AddError("; required");
            }

            return voidFunctionCall;
        }
    }
}
