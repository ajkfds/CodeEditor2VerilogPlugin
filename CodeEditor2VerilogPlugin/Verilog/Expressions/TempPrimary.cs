namespace pluginVerilog.Verilog.Expressions
{
    public class TempPrimary : Primary
    {
        public TempPrimary(string text)
        {
            this.text = text;
        }

        private string text;
        public override SystemVerilogCore.Documents.VerilogLabel GetLabel()
        {
            SystemVerilogCore.Documents.VerilogLabel label = new SystemVerilogCore.Documents.VerilogLabel();
            AppendLabel(label);
            return label;
        }

        public override string CreateString()
        {
            return GetLabel().CreateString();
        }

        public override void AppendLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText(text);
        }

    }
}
