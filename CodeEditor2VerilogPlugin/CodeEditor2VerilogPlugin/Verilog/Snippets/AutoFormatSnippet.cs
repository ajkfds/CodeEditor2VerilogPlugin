﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.PopupMenu;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog.Verilog.Snippets
{
    public class AutoFormatSnippet : ToolItem
    {
        public AutoFormatSnippet() : base("autoFormat")
        {
        }

        public override void Apply()
        {
            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            CodeDocument codeDocument = file.CodeDocument;

            Data.IVerilogRelatedFile? vFile = file as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            ParsedDocument parsedDocument = vFile.VerilogParsedDocument;
            if (parsedDocument == null) return;

            int index = codeDocument.CaretIndex;
            IndexReference iref = IndexReference.Create(parsedDocument.IndexReference, index);

            BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(index);
            if (buildingBlock == null) return;

            foreach (var instance in
                buildingBlock.Instantiations.Values)
            {
                ModuleInstantiation? moduleInstantiation = instance as ModuleInstantiation;
                if (moduleInstantiation == null) continue;

                if (iref.IsSmallerThan(moduleInstantiation.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(moduleInstantiation.LastIndexReference)) continue;

                writeModuleInstance(codeDocument, index, moduleInstantiation);

                CodeEditor2.Controller.CodeEditor.RequestReparse();
                return;
            }
        }

        private void writeModuleInstance(CodeDocument codeDocument, int index, ModuleItems.ModuleInstantiation moduleInstantiation)
        {
            CodeEditor.CodeDocument? vCodeDocument = codeDocument as CodeEditor.CodeDocument;
            if (vCodeDocument == null) return;

            string indent = vCodeDocument.GetIndentString(index);

            CodeEditor2.Controller.CodeEditor.SetCaretPosition(moduleInstantiation.BeginIndexReference.Indexes.Last());
            codeDocument.Replace(
                moduleInstantiation.BeginIndexReference.Indexes.Last(),
                moduleInstantiation.LastIndexReference.Indexes.Last() - moduleInstantiation.BeginIndexReference.Indexes.Last() + 1,
                0,
                moduleInstantiation.CreateString("\t")
                );
            CodeEditor2.Controller.CodeEditor.SetSelection(codeDocument.CaretIndex, codeDocument.CaretIndex);
        }
    }
}

