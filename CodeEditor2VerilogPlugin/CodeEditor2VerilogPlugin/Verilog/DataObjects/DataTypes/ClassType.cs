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

        /*
        class_type ::= 
            ps_class_identifier [ parameter_value_assignment ] 
            { "::" class_identifier [ parameter_value_assignment ] }

            parameter_value_assignment ::= # ( [ list_of_parameter_assignments ] ) 
        */

        public static ClassType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ClassType dType = new ClassType();
            if (word.Text != "class") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal class name");
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            return dType;
        }
        public virtual string CreateString()
        {
            return "chandle";
        }

    }
}
