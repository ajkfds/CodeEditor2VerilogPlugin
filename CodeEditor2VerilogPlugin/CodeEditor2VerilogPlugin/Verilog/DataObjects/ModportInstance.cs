using AjkAvaloniaLibs.Controls;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    internal class ModportInstance : DataObject, INamedElement
    {
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public static ModportInstance Create(string identifier,Interface interface_, ModPort modPort)
        {
            ModportInstance modportInstance = new ModportInstance() { Name = identifier, ModPort = modPort, InterfaceName=interface_.Name, ModportName=modPort.Name };

            foreach (var element in modPort.NamedElements.Values)
            {
                modportInstance.NamedElements.Add(element.Name, element);
            }
            return modportInstance;
        }

        public required ModPort ModPort { init; get; }
        public required string InterfaceName { init; get; }
        public required string ModportName { init; get; }

        // substrの戻り値がStringを持つため、遅延評価される必要がある。
        private NamedElements? namedElements = null;
        public override NamedElements NamedElements
        {
            get
            {
                if (namedElements != null) return namedElements;
                namedElements = new NamedElements();

                foreach(var element in ModPort.NamedElements.Values)
                {
                    namedElements.Add(element.Name, element);
                }
                return namedElements;
            }
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(InterfaceName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(".");
            label.AppendText(ModportName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }
        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));

            foreach (Arrays.VariableArray dimension in UnpackedArrays)
            {
                if (dimension == null) continue;
                label.AppendText(" ");
                label.AppendLabel(dimension.GetLabel());
            }

            if (Comment != "")
            {
                label.AppendText(" ");
                label.AppendText(Comment.Trim(new char[] { '\r', '\n', '\t', ' ' }), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Comment));
            }
            label.AppendText("\r\n");

            SyncContext.AppendLabel(label);
        }
        public override DataObject Clone()
        {
            return Clone(Name);
        }
        public override DataObject Clone(string name)
        {
            ModportInstance modportInstance = new ModportInstance() { Name = name, InterfaceName = InterfaceName, ModPort = ModPort, ModportName = ModportName, Defined = Defined };
            return modportInstance;
        }
    }
}
