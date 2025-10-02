using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ClassType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Class;
            }
        }
        public int? BitWidth
        {
            get
            {
                return null;
            }
        }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();



        //public Class? Class
        //{
        //    get
        //    {

        //    }
        //}

        /*
        class_type ::=  ps_class_identifier [ parameter_value_assignment ] 
                        { "::" class_identifier [ parameter_value_assignment ] }

        parameter_value_assignment ::= # ( [ list_of_parameter_assignments ] ) 
        */

        public static ClassType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.NextText != ":" && word.NextText != "#") throw new Exception();
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal class name");
                return null;
            }

            ClassType dType = new ClassType();
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            return dType;
        }
        public bool IsVector { get { return false; } }

        public virtual string CreateString()
        {
            return "class";
        }

    }
}
