using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public interface IItem
    {
        public IndexReference? BeginIndexReference { get; }
        public IndexReference? LastIndexReference { get; }
    }
}
