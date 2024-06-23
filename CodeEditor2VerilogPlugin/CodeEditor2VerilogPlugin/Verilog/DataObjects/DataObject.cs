using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    public class DataObject
    {
        // #SystemVeriog 2017
        //	net												user-defined-size	4state	v
        //
        //	variable	+ integer_vector_type	+ bit 		user-defined-size	2state	sv
        //										+ logic		user-defined-size	4state  sv
        //										+ reg		user-defined-size	4state	v
        //
        //				+ integer_atom_type		+ byte		8bit signed			2state  sv
        //										+ shortint	16bit signed		2state  sv
        //										+ int		32bit signed		2state  sv
        //										+ longint	64bit signed		2state  sv
        //										+ integer	32bit signed		4state	v
        //										+ time		64bit unsigned		        v
        //
        //            	+ non_integer_type		+ shortreal	                            sv
        //										+ real		                            v
        //										+ realtime	                            v
        //              + struct/union
        //              + enum
        //              + string
        //              + chandle
        //              + virtual(interface)
        //              + class/package
        //              + event
        //              + pos_covergroup
        //              + type_reference
        // 


        public string Name { set; get; } = "";
        public string Comment { set; get; } = "";
        public WordReference DefinedReference { set; get; } = null;
        public DataTypeEnum DataType = DataTypeEnum.Reg;
        public List<Range> Dimensions { get; set; } = new List<Range>();

        public List<WordReference> UsedReferences { set; get; } = new List<WordReference>();
        public List<WordReference> AssignedReferences { set; get; } = new List<WordReference>();
        public int DisposedIndex = -1;

        public string CreateDataTypeString()
        {
            StringBuilder sb = new StringBuilder();
            switch (DataType)
            {
                case DataTypeEnum.Bit:
                    sb.Append("bit");
                    break;
                case DataTypeEnum.Byte:
                    sb.Append("byte");
                    break;
                case DataTypeEnum.Chandle:
                    sb.Append("chandle");
                    break;
                case DataTypeEnum.Class:
                    sb.Append("class");
                    break;
                case DataTypeEnum.CoverGroup:
                    sb.Append("covergroup");
                    break;
                case DataTypeEnum.Enum:
                    sb.Append("enum");
                    break;
                case DataTypeEnum.Logic:
                    sb.Append("logic");
                    break;
                case DataTypeEnum.Reg:
                    sb.Append("reg");
                    break;
                case DataTypeEnum.String:
                    sb.Append("string");
                    break;
            }
            return sb.ToString();
        }

        public virtual void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            label.AppendText(Name);
        }

        public virtual void AppendTypeLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {

        }

        public virtual DataObject Clone()
        {
            return null;
        }




    }
}
