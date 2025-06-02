using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class HierarchyConnection
    {
        public HierarchyConnection(pluginVerilog.Verilog.DataObjects.DataObject dataObject)
        {
            if (dataObject.DefinedReference == null) return;
            pluginVerilog.Verilog.ParsedDocument? parsedDocument = dataObject.DefinedReference.ParsedDocument as pluginVerilog.Verilog.ParsedDocument;
            if (parsedDocument == null) return;

            pluginVerilog.Data.IVerilogRelatedFile? verilogRelatedFile = parsedDocument.File as pluginVerilog.Data.IVerilogRelatedFile;
        }

        private pluginVerilog.Verilog.ParsedDocument parsedDocument;

        private void searchInputPort()
        {

        }

    }
}
