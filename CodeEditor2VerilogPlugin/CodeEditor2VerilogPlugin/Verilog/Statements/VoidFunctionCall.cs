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
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
        }

        public FunctionCall? FunctionCall { get; private set; } = null!; // Initialized in Create method
        public static VoidFunctionCall Create(FunctionCall functionCall)
        {
            VoidFunctionCall voidFunctionCall = new VoidFunctionCall();
            voidFunctionCall.FunctionCall = functionCall;
            return voidFunctionCall;
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
            voidFunctionCall.FunctionCall = func;

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
