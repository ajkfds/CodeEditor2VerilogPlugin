using SystemVerilogCore.Documents;
namespace pluginVerilog.Verilog.Expressions
{
    public class ConstantString : Primary
    {
        protected ConstantString() { }
        public static new ConstantString ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            word.Color(VerilogColorType.Number);
            ConstantString str = new ConstantString();
            str.text = word.Text;

            str.Reference = word.GetReference();
            str.BitWidth = (word.Text.Length - 2) * 8;
            str.Constant = true;
            if (str.BitWidth <= 16)
            {
                int value = 0;
                for (int i = 1; i < word.Text.Length - 1; i++)
                {
                    value = value << 8;
                    value = value + word.Text[i];
                }
                str.Value = value;
            }

            word.MoveNext();
            return str;
        }

        protected string text;
        public string Text
        {
            get { return text; }
        }
        public override string CreateString()
        {
            return Text;
        }
        //public override void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        //{
        //    label.AppendText(Text, VerilogColorType.Variable);
        //}

        //public override void AppendString(StringBuilder stringBuilder)
        //{
        //    stringBuilder.Append(Text);
        //}


    }
}
