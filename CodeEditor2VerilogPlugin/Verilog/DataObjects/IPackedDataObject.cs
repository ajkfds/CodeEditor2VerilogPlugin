using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects
{
    internal interface IPackedDataObject
    {
        List<DataObjects.Arrays.PackedArray> PackedDimensions { get; }
    }
}
