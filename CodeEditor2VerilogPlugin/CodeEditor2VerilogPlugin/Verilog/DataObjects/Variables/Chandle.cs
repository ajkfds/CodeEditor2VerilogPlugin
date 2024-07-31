using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Chandle : Variable
    {
        protected Chandle() { }

        public List<Range> PackedDimensions { get; set; } = new List<Range>();


        public Range Range
        {
            get
            {
                if (PackedDimensions.Count < 1) return null;
                return PackedDimensions[0];
            }
        }


        //public override void AppendLabel(ColorLabel label)
        //{
        //    AppendTypeLabel(label);
        //    label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));

        //    foreach (Range dimension in Dimensions)
        //    {
        //        label.AppendText(" ");
        //        label.AppendLabel(dimension.GetLabel());
        //    }

        //    if (Comment != "")
        //    {
        //        label.AppendText(" ");
        //        label.AppendText(Comment.Trim(new char[] { '\r', '\n', '\t', ' ' }), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Comment));
        //    }

        //    label.AppendText("\r\n");
        //}

        //public override void AppendTypeLabel(ColorLabel label)
        //{
        //}

        public static new Chandle Create(DataTypes.IDataType dataType)
        {
            DataTypes.Chandle? dType = dataType as DataTypes.Chandle;
            if (dType == null) throw new Exception();

            Chandle val = new Chandle();
            val.DataType = dType.Type;
            return val;
        }


    }
}
