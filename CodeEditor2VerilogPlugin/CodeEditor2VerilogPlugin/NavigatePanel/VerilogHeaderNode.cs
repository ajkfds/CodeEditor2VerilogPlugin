using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Avalonia.Media;
using pluginVerilog.Data;
using Avalonia.Threading;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogHeaderNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogHeaderNode(Data.VerilogHeaderFile headerFile) : base(headerFile)
        {

        }
        public Data.IVerilogRelatedFile? VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }
        public CodeEditor2.Data.TextFile? TextFile
        {
            get { return Item as CodeEditor2.Data.TextFile; }
        }

        public override void Update()
        {
            UpdateVisual();
        }
        public override void UpdateVisual()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _updateVisual();
                }
                catch (Exception ex)
                {
                    CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                    throw;
                }
            });
        }
        public void _updateVisual()
        {
            if (FileItem == null)
            {
                Text = "?";
            }
            else
            {
                Text = FileItem.Name;
            }

            IVerilogRelatedFile? verilogRelatedFile = TextFile as IVerilogRelatedFile;
            if (verilogRelatedFile == null) return;
            Image = GetIcon(verilogRelatedFile);
        }
        public override void OnSelected()
        {
            if(TextFile == null) return;
            CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile);
        }

        public static IImage? GetIcon(IVerilogRelatedFile verilogRelatedFile)
        {
            // Icon badge will update only in UI thread
            if (System.Threading.Thread.CurrentThread.Name != "UI")
            {
                throw new Exception();
            }

            List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon> overrideIcons = new List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon>();

            if (verilogRelatedFile.CodeDocument != null && verilogRelatedFile.CodeDocument.IsDirty)
            {
                overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                {
                    SvgPath = "CodeEditor2/Assets/Icons/shine.svg",
                    Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 200),
                    OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.UpRight
                });
            }

            if (verilogRelatedFile != null && verilogRelatedFile.VerilogParsedDocument != null)
            {
                if (verilogRelatedFile.VerilogParsedDocument.ErrorCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = Avalonia.Media.Color.FromArgb(255, 255, 20, 20),
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
                else if (verilogRelatedFile.VerilogParsedDocument.WarningCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 20),
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
            }


            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                Avalonia.Media.Color.FromArgb(100, 200, 240, 240),
                overrideIcons
                );
        }

    }
}
