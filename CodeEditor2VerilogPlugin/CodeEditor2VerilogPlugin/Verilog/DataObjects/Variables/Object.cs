using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Object : Variable
    {
        protected Object() { }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        public required BuildingBlocks.Class Class { get; init; }

        public override NamedElements NamedElements { get { return Class.NamedElements; } }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name);
        }
        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(Class.Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }
        public static new Object Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Class);
            BuildingBlocks.Class? class_ = dataType as BuildingBlocks.Class;
            if (class_ == null) throw new Exception();

            Object val = new Object() { Class = class_, Name = name };
            val.DataType = dataType;
            return val;
        }

        public override Variable Clone()
        {
            Object val = new Object() { Class = Class, Name = Name };
            val.DataType = DataType;
            return val;
        }

        // IInstance
        public override Task? GetTask(string identifier)
        {
            if (Class.NamedElements.ContainsKey(identifier) && Class.NamedElements[identifier] is Task) return (Task)Class.NamedElements[identifier];
            return null;
        }
        public override Function? GetFunction(string identifier)
        {
            if (Class.NamedElements.ContainsKey(identifier) && Class.NamedElements[identifier] is Function) return (Function)Class.NamedElements[identifier];
            return null;
        }
        public override DataObject? GetDataObject(string identifier)
        {
            if (!Class.NamedElements.ContainsKey(identifier)) return null;
            return Class.NamedElements[identifier] as DataObject;
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items)
        {
            Class.AppendAutoCompleteItem(items);
        }


    }
}
