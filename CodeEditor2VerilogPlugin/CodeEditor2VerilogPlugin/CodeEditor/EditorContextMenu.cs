using Avalonia.Controls;
using Avalonia.Input;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using ExCSS;
using pluginVerilog.Data;
using pluginVerilog.Verilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.CodeEditor
{
    public static class EditorContextMenu
    {
        public static void CustomizeEditorContextMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.FirstOrDefault(x =>
            {
                if (x is MenuItem menuItem && menuItem.Name == "menuItem_GoToDefinition") return true;
                return false;
            }) == null)
            {
                MenuItem menuItem_GoToDefinition = CodeEditor2.Global.CreateMenuItem(
                    "Go to Definition",
                    "menuItem_GoToDefinition",
                    "CodeEditor2VerilogPlugin/Assets/Icons/ArrowRightBend.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                    );
                menuItem_GoToDefinition.Click += MenuItem_GoToDefinition_Click;
                contextMenu.Items.Add(menuItem_GoToDefinition);
                if (!TryGetGotoDefinition(out _,out _, out _))
                {
                    menuItem_GoToDefinition.IsEnabled = false;
                }
            }
            if (contextMenu.Items.FirstOrDefault(x =>
            {
                if (x is MenuItem menuItem && menuItem.Name == "menuItem_GoToDriver") return true;
                return false;
            }) == null)
            {
                MenuItem menuItem_GoToDriver = CodeEditor2.Global.CreateMenuItem(
                    "Go to Driver",
                    "menuItem_GoToDriver",
                    "CodeEditor2VerilogPlugin/Assets/Icons/ArrowRightBend.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                    );
                menuItem_GoToDriver.Click += MenuItem_GoToDriver_Click;
                contextMenu.Items.Add(menuItem_GoToDriver);
                if (!TryGetGotoDriver(out _,out _, out _))
                {
                    menuItem_GoToDriver.IsEnabled = false;
                }
            }
        }

        private static Verilog.DataObjects.DataObject? getDataObject()
        {
            TextFile? textFile = CodeEditor2.Controller.CodeEditor.GetTextFile();
            IVerilogRelatedFile? verilogRelatedFile = textFile as IVerilogRelatedFile;
            if (verilogRelatedFile == null) return null;
            int? index = CodeEditor2.Controller.CodeEditor.GetCaretPosition();
            if (index == null) return null;
            if (verilogRelatedFile.CodeDocument == null) return null;
            verilogRelatedFile.CodeDocument.GetWord((int)index, out int headindex, out int length);

            string word = verilogRelatedFile.CodeDocument.CreateString(headindex, length);

            Verilog.ParsedDocument? parsedDocument = verilogRelatedFile.VerilogParsedDocument;
            if(parsedDocument == null) return null;
            CodeDocument? codeDocument = verilogRelatedFile.CodeDocument as CodeDocument;
            if (codeDocument == null) return null;

            IndexReference iref = IndexReference.Create(parsedDocument, codeDocument, headindex);
            NameSpace? nameSpace = parsedDocument.GetNameSpace(iref);
            if (nameSpace == null) return null;

            if (!nameSpace.NamedElements.ContainsDataObject(word)) return null;
            INamedElement? namedElement = nameSpace.GetNamedElementUpward(word);
            if (namedElement == null) return null;

            if (namedElement is not Verilog.DataObjects.DataObject) return null;
            return (Verilog.DataObjects.DataObject)namedElement;
        }

        private static bool TryGetGotoDefinition(out IVerilogRelatedFile? file, out int start, out int length)
        {
            start = 0;
            length = 0;
            file = null;
            TextFile? textFile = CodeEditor2.Controller.CodeEditor.GetTextFile();
            Verilog.DataObjects.DataObject? dataObject = getDataObject();
            if (dataObject == null) return false;

            WordReference? wordRef = dataObject.DefinedReference;
            if (wordRef == null) return false;

            if (wordRef.IndexReference == null) return false;
            if (wordRef.IndexReference.ParsedDocument == null) return false;

            file = wordRef.IndexReference.ParsedDocument.File;
            if (file == null) return false;

            if (wordRef.Length == 0) return false;
            start = wordRef.Index;
            length = wordRef.Length;

            return true;
        }
        private static bool TryGetGotoDriver(out IVerilogRelatedFile? file, out int start, out int length)
        {
            start = 0;
            length = 0;
            file = null;
            TextFile? textFile = CodeEditor2.Controller.CodeEditor.GetTextFile();
            Verilog.DataObjects.DataObject? dataObject = getDataObject();
            if (dataObject == null) return false;

            WordReference? wordRef = dataObject.AssignedReferences.FirstOrDefault();
            if (wordRef == null) return false;

            if (wordRef.IndexReference == null) return false;
            if (wordRef.IndexReference.ParsedDocument == null) return false;

            file = wordRef.IndexReference.ParsedDocument.File;
            if (file == null) return false;
            if (wordRef.Length == 0) return false;
            start = wordRef.Index;
            length = wordRef.Length;
            return true;
        }

        private static void MenuItem_GoToDefinition_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!TryGetGotoDefinition(out IVerilogRelatedFile? file, out int start, out int length)) return;
            if (file == null) throw new Exception();

            CodeEditor2.Data.TextReference textRef = new TextReference(file, start, length);
            CodeEditor2.Controller.SelectText(textRef);
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(start);
            CodeEditor2.Controller.CodeEditor.ScrollToCaret();
            CodeEditor2.Controller.AddSelectHistory(textRef);
        }
        private static void MenuItem_GoToDriver_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!TryGetGotoDriver(out IVerilogRelatedFile? file, out int start, out int length)) return;
            if (file == null) throw new Exception();

            CodeEditor2.Data.TextReference textRef = new TextReference(file, start, length);
            CodeEditor2.Controller.SelectText(textRef);
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(start);
            CodeEditor2.Controller.CodeEditor.ScrollToCaret();
            CodeEditor2.Controller.AddSelectHistory(textRef);
        }
    }
}
