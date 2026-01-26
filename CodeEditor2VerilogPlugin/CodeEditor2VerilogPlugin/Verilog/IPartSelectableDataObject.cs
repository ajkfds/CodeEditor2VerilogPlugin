using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public interface IPartSelectableDataObject
    {
        public bool PartSelectable { get; }
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace);
    }
}
