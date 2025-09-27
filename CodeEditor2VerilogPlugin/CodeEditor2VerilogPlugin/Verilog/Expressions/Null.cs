using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class Null : Primary
    {
        protected Null() { }
        public static new Null ParseCreate(WordScanner word,NameSpace nameSpace)
        {
            word.Color(CodeDrawStyle.ColorType.Variable);
            Null null_ = new Null();
            null_.Constant = true;
            null_.Reference = word.GetReference();
            word.MoveNext();
            return null_;
        }

        public override string CreateString()
        {
            return "null";
        }
    }
}
