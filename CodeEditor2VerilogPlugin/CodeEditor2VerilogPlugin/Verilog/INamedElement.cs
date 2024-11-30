using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public interface INamedElement
    {
        public string Name { get; }
        public CodeDrawStyle.ColorType ColorType { get; }

        public NamedElements NamedElements { get; }

        

    }
}
