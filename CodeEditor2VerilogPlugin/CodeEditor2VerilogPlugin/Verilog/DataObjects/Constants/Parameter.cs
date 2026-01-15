using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Constants
{
    public class Parameter : Constants
    {
        public override DataObject Clone(string name)
        {
            return new Parameter { DefinedReference = DefinedReference, Expression = Expression, Name = name, Defined = Defined, DataType = DataType?.Clone() };
        }
    }
}
