using SystemVerilogCore.Documents;
namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class ValueVariable : Variable
    {

        public override VerilogColorType ColorType { get { return VerilogColorType.Variable; } }

        public override void AppendLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name, VerilogColorType.Register);

            foreach (Arrays.VariableArray dimension in UnpackedArrays)
            {
                if (dimension == null) continue;
                label.AppendText(" ");
                label.AppendLabel(dimension.GetLabel());
            }

            if (Comment != "")
            {
                label.AppendText(" ");
                label.AppendText(Comment.Trim(new char[] { '\r', '\n', '\t', ' ' }), VerilogColorType.Comment);
            }

            label.AppendText("\r\n");

            SyncContext.AppendLabel(label);
        }


    }
}
