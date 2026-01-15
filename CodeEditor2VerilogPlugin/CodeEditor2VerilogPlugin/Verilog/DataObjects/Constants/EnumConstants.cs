using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Constants
{
    public class EnumConstants : Constants
    {
        public static EnumConstants Create(string name,DataTypes.IDataType dataType,WordReference definitionReference, Expressions.Expression expression)
        {
            EnumConstants constants = new EnumConstants() { Name = name, DefinedReference = definitionReference, Expression = expression };
            constants.DataType = dataType;
            constants.ConstantType = ConstantTypeEnum.enum_;
            return constants;
        }
        public override DataObject Clone(string name)
        {
            return new EnumConstants { DefinedReference = DefinedReference, Expression = Expression, Name = name, Defined = Defined, DataType = DataType?.Clone() };
        }
    }
}
