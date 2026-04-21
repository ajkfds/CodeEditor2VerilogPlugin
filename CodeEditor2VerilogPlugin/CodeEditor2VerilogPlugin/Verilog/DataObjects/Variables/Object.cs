using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;

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
        public static new Object Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Class);
            BuildingBlocks.Class? class_ = dataType as BuildingBlocks.Class;
            if (class_ == null) throw new Exception();

            Object val = new Object() { Class = class_, Name = name };

            defineElements(val);

            val.DataType = dataType;
            return val;
        }

        private static void defineElements(INamedElement namedElement)
        {
            foreach (INamedElement subElement in namedElement.NamedElements)
            {
                Variable? variable = subElement as Variable;
                if (variable != null) variable.Defined = true;

                defineElements(subElement);
            }
        }


        public override Variable Clone()
        {
            return Clone(Name);
        }

        public override Variable Clone(string name)
        {
            Object val = new Object() { Class = Class, Name = name, Defined = Defined };
            val.DataType = DataType;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
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
