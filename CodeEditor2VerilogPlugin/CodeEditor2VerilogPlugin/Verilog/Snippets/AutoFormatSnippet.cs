using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog.Verilog.Snippets
{
    public class AutoFormatSnippet : CodeEditor2.CodeEditor.ToolItem
    {
        public AutoFormatSnippet() : base("autoFormat")
        {
        }

        public override void Apply(CodeDocument codeDocument)
        {
            CodeEditor2.Data.ITextFile itext = CodeEditor2.Controller.CodeEditor.GetTextFile();

            if (!(itext is Data.IVerilogRelatedFile)) return;
            var vfile = itext as Data.IVerilogRelatedFile;
            ParsedDocument parsedDocument = vfile.VerilogParsedDocument;
            if (parsedDocument == null) return;

            int index = codeDocument.CaretIndex;
            IndexReference iref = IndexReference.Create(parsedDocument.IndexReference, index);

            BuildingBlock buildingBlock = parsedDocument.GetBuildingBlockAt(index);

            foreach (var inst in
                buildingBlock.Instantiations.Values)
            {
                ModuleInstantiation moduleInstantiation = inst as ModuleInstantiation;
                if (moduleInstantiation == null) continue;

                if (iref.IsSmallerThan(moduleInstantiation.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(moduleInstantiation.LastIndexReference)) continue;

                writeModuleInstance(codeDocument, index, moduleInstantiation);
                return;
            }
        }

        private void writeModuleInstance(CodeDocument codeDocument, int index, ModuleItems.ModuleInstantiation moduleInstantiation)
        {
            string indent = (codeDocument as CodeEditor.CodeDocument).GetIndentString(index);

            codeDocument.CaretIndex = moduleInstantiation.BeginIndexReference.Indexs.Last();
            codeDocument.Replace(
                moduleInstantiation.BeginIndexReference.Indexs.Last(),
                moduleInstantiation.LastIndexReference.Indexs.Last() - moduleInstantiation.BeginIndexReference.Indexs.Last() + 1,
                0,
                moduleInstantiation.CreateSrting("\t")
                );
            CodeEditor2.Controller.CodeEditor.SetSelection(codeDocument.CaretIndex, codeDocument.CaretIndex);
        }
    }
}

