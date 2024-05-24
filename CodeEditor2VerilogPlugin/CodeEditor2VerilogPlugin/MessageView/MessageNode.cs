using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls;
using ExCSS;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.Controller;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pluginVerilog.MessageView
{
    public class MessageNode : CodeEditor2.MessageView.MessageNode
    {
        public MessageNode(Data.IVerilogRelatedFile file, Verilog.ParsedDocument.Message message)
        {
            fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
            Text = "[" + message.LineNo.ToString() + "]" + message.Text;
            this.messageType = message.Type;
            this.index = message.Index;
            this.length = message.Length;
            this.project = message.Project;
            this.lineNo = message.LineNo;
            Update();
        }


        private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
        public Data.IVerilogRelatedFile File
        {
            get
            {
                Data.IVerilogRelatedFile file;
                if (!fileRef.TryGetTarget(out file)) return null;
                return file;
            }
        }


        Verilog.ParsedDocument.Message.MessageType messageType;
        int index;
        int length;
        int lineNo;
        CodeEditor2.Data.Project project;

        public override void OnSelected()
        {
            if (File != null && File.CodeDocument != null)
            {
//                File.CodeDocument.SelectionStart = index;
//                File.CodeDocument.SelectionLast = index + length;
                CodeEditor2.Controller.CodeEditor.SetCaretPosition(index);
                CodeEditor2.Controller.CodeEditor.SetSelection(index, index + length-1);
            }
            CodeEditor2.Controller.CodeEditor.ScrollToCaret();
        }

        public override void Update()
        {
            if (textBlock.Inlines == null) return;
            textBlock.Inlines.Clear();

            Avalonia.Media.IImage? iimage;
            switch (messageType)
            {
                case Verilog.ParsedDocument.Message.MessageType.Error:
                    iimage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 255, 150, 150)
                            );
                    break;
                case Verilog.ParsedDocument.Message.MessageType.Warning:
                    iimage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 255, 255, 150)
                            );
                    //                    graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Orange), new Point(x, y));
                    break;
                case Verilog.ParsedDocument.Message.MessageType.Notice:
                    iimage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 150, 255, 150)
                            );
                    //                    graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Green), new Point(x, y));
                    break;
                case Verilog.ParsedDocument.Message.MessageType.Hint:
                    iimage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 150, 150, 255)
                            );
                    //                    graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Blue), new Point(x, y));
                    break;
                default:
                    throw new Exception();
            }


            Avalonia.Controls.Image image = new Avalonia.Controls.Image();
            image.Source = iimage;
            image.Width = 12;
            image.Height = 12;
            image.Margin = new Avalonia.Thickness(0, 0, 4, 0);
            image.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            {
                InlineUIContainer uiContainer = new InlineUIContainer();
                uiContainer.BaselineAlignment = Avalonia.Media.BaselineAlignment.Baseline;
                uiContainer.Child = image;
                textBlock.Inlines.Add(uiContainer);
            }


            Avalonia.Controls.Documents.Run run = new Avalonia.Controls.Documents.Run(Text);
            textBlock.Inlines.Add(run);
        }
        //private static ajkControls.Primitive.IconImage icon = new ajkControls.Primitive.IconImage(Properties.Resources.exclamationBox);
        //public override void DrawNode(Graphics graphics, int x, int y, Font font, Color color, Color backgroundColor, Color selectedColor, int lineHeight, bool selected)
        //{
        //    switch (messageType)
        //    {
        //        case Verilog.ParsedDocument.Message.MessageType.Error:
        //            graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Red), new Point(x, y));
        //            break;
        //        case Verilog.ParsedDocument.Message.MessageType.Warning:
        //            graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Orange), new Point(x, y));
        //            break;
        //        case Verilog.ParsedDocument.Message.MessageType.Notice:
        //            graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Green), new Point(x, y));
        //            break;
        //        case Verilog.ParsedDocument.Message.MessageType.Hint:
        //            graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Blue), new Point(x, y));
        //            break;
        //    }
        //    Color bgColor = backgroundColor;
        //    if (selected) bgColor = selectedColor;
        //    System.Windows.Forms.TextRenderer.DrawText(
        //        graphics,
        //        Text,
        //        font,
        //        new Point(x + lineHeight + (lineHeight >> 2), y),
        //        color,
        //        bgColor,
        //        System.Windows.Forms.TextFormatFlags.NoPadding
        //        );

        //    //            if (VerilogFile != null && VerilogFile.ParsedDocument != null && VerilogFile.ParsedDocument.Messages.Count != 0)
        //    //            {
        //    //                graphics.DrawImage(Style.ExclamationIcon.GetImage(lineHeight, ajkControls.Icon.ColorStyle.Red), new Point(x, y));
        //    //           }
        //}

    }
}
