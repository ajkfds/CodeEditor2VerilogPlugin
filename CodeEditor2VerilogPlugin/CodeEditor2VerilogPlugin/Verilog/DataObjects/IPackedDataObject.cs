using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    internal interface IPackedDataObject
    {
        List<DataObjects.Arrays.PackedArray> PackedDimensions { get; }
    }
}
