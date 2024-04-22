using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Avalonia.Media;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogHeaderNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogHeaderNode(Data.VerilogHeaderFile headerFile) : base(headerFile)
        {

        }

        public Data.IVerilogRelatedFile VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }
        public CodeEditor2.Data.TextFile TextFile
        {
            get { return Item as CodeEditor2.Data.TextFile; }
        }

        public override string Text
        {
            get { return FileItem.Name; }
        }

        public override void Update()
        {
            UpdateVisual();
        }
        public override void UpdateVisual()
        {
            if (TextFile.CodeDocument.IsDirty)
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 255, 255, 255),
                    "CodeEditor2/Assets/Icons/shine.svg",
                    Avalonia.Media.Color.FromArgb(255, 255, 255, 200)
                    );
            }
            else
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 255, 255, 255)
                    );
            }
        }
        public override void OnSelected()
        {
            CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile);
        }
    }
}
