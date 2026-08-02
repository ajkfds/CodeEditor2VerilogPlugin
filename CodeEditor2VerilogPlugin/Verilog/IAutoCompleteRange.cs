using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public interface IAutoCompleteRange
    {
        public IndexReference BeginIndexReference { get; }
        public IndexReference? LastIndexReference { get;}

        public void AppendKeywordAutoCompleteItems(
            List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items,
            string candidate,
            int candidateStartIndex,
            int lineStartIndex,
            bool systemVerilog
            );

    }
}
