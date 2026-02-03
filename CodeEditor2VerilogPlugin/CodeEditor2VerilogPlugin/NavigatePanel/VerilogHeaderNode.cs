using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using pluginVerilog.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override Task UpdateAsync()
        {
            UpdateVisual();
            return Task.CompletedTask;
        }
        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

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
        public override async void OnSelected()
        {
            try
            {
                if (TextFile == null) return;
                await CodeEditor2.Controller.CodeEditor.SetTextFileAsync(TextFile);
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
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

            Avalonia.Media.Color color = Avalonia.Media.Color.FromArgb(100, 200, 240, 240);

            if (verilogRelatedFile != null && verilogRelatedFile is InstanceTextFile)
            {
                if (((InstanceTextFile)verilogRelatedFile).ExternalProject)
                {
                    color = Avalonia.Media.Color.FromArgb(100, 250, 200, 200);
                }
            }

            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                color,
                overrideIcons
                );
        }
        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;


    }
}
