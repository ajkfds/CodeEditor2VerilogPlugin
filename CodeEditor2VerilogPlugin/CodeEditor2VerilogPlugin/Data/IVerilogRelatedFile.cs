using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public interface IVerilogRelatedFile : CodeEditor2.Data.ITextFile
    {

        Verilog.ParsedDocument VerilogParsedDocument { get; }

        ProjectProperty ProjectProperty { get; }

    }
}
