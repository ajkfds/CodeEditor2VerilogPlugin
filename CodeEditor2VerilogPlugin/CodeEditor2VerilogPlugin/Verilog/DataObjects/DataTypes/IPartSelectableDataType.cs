using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public interface IPartSelectableDataType
    {
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace);
    }
}
