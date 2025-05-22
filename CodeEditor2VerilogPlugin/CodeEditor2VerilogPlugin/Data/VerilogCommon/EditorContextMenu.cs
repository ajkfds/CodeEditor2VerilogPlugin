using Avalonia.Controls;
using Avalonia.Input;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using pluginVerilog.Verilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon
{
    public static class EditorContextMenu
    {
        public static void CustomizeEditorContextMenu(ContextMenu contextMenu)
        {
            MenuItem menuItem_GoToDefinition = CodeEditor2.Global.CreateMenuItem(
                "Go to Definition",
                "menuItem_GoToDefinition",
                "CodeEditor2VerilogPlugin/Assets/Icons/ArrowRightBend.svg",
                Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                );
            menuItem_GoToDefinition.Click += MenuItem_GoToDefinition_Click;
            contextMenu.Items.Add(menuItem_GoToDefinition);
        }

        private static void MenuItem_GoToDefinition_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CodeEditor2.Data.TextFile? textFile = CodeEditor2.Controller.CodeEditor.GetTextFile();
            IVerilogRelatedFile? verilogRelatedFile = textFile as IVerilogRelatedFile;
            if (verilogRelatedFile == null) return;
            int? index = CodeEditor2.Controller.CodeEditor.GetCaretPosition();
            if(index == null) return;
            if (verilogRelatedFile.CodeDocument == null) return;
            verilogRelatedFile.CodeDocument.GetWord((int)index, out int headindex, out int length);

            string word = verilogRelatedFile.CodeDocument.CreateString(headindex, length);

            Verilog.ParsedDocument parsedDocument = verilogRelatedFile.VerilogParsedDocument;
            pluginVerilog.CodeEditor.CodeDocument? codeDocument = verilogRelatedFile.CodeDocument as pluginVerilog.CodeEditor.CodeDocument;
            if (codeDocument == null) return;

            IndexReference iref = IndexReference.Create(parsedDocument, codeDocument, headindex);
            NameSpace? nameSpace = parsedDocument.GetNameSpace(iref);
            if (nameSpace == null) return;

            if (!nameSpace.NamedElements.ContainsDataObject(word)) return;
            INamedElement? namedElement = nameSpace.GetNamedElementUpward(word);
            if (namedElement == null) return;

            if(namedElement is Verilog.DataObjects.DataObject)
            {
                var dataObject = (Verilog.DataObjects.DataObject)namedElement;
                WordReference? wordRef = dataObject.DefinedReference;
                if (wordRef == null) return;

                if(wordRef.IndexReference == null) return;
                if (wordRef.IndexReference.ParsedDocument== null || wordRef.IndexReference.ParsedDocument.TextFile != textFile) return;
                if (wordRef.Length == 0) return;
                CodeEditor2.Controller.CodeEditor.SetSelection(wordRef.Index, wordRef.Index+wordRef.Length-1);
            }

            
        }
    }
}
