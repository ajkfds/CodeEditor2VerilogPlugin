using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class ValueVariable : Variable
    {


        public override void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));

            foreach (Range dimension in Dimensions)
            {
                if (dimension == null) continue;
                label.AppendText(" ");
                label.AppendLabel(dimension.GetLabel());
            }

            if (Comment != "")
            {
                label.AppendText(" ");
                label.AppendText(Comment.Trim(new char[] { '\r', '\n', '\t', ' ' }), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Comment));
            }

            label.AppendText("\r\n");
        }


    }
}
