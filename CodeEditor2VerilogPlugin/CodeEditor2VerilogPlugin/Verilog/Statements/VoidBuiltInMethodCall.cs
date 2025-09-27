using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class VoidBuiltInMethodCall : IStatement
    {
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
        }

        public BuiltinMethodCall? BuiltinMethodCall { get; private set; } = null!; // Initialized in Create method
        public static VoidBuiltInMethodCall Create(BuiltinMethodCall builtInMethodCall)
        {
            VoidBuiltInMethodCall voidBuiltInMethodCall = new VoidBuiltInMethodCall();
            voidBuiltInMethodCall.BuiltinMethodCall = builtInMethodCall;
            return voidBuiltInMethodCall;
        }
        //public static VoidFunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        //{
        //    if (word.Text != "void") throw new Exception();
        //    VoidBuiltInMethodCall builtInMethodCall = new VoidBuiltInMethodCall();
        //    word.Color(CodeDrawStyle.ColorType.Identifier);
        //    word.MoveNext();
        //    if (word.Text != "'") throw new Exception();
        //    word.MoveNext();
        //    if (word.Eof || word.Text != "(")
        //    {
        //        word.AddError("illegal cast");
        //        return null;
        //    }
        //    word.MoveNext();

        //    BuiltinMethodCall? func = BuiltinMethodCall.ParseCreate(word, nameSpace, nameSpace);
        //    builtInMethodCall.BuiltinMethodCall = func;

        //    if (word.Eof || func == null || word.Text != ")")
        //    {
        //        word.AddError("illegal cast");
        //        return null;
        //    }
        //    word.MoveNext();

        //    if (word.Text == ";")
        //    {
        //        word.MoveNext();
        //    }
        //    else
        //    {
        //        word.AddError("; required");
        //    }

        //    return voidFunctionCall;
        //}
    }
}
