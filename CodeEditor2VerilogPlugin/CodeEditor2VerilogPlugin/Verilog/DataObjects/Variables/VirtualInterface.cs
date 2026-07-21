using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class VirtualInterface : Variable
    {
        protected VirtualInterface() { }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        public required BuildingBlocks.Interface Interface { get; init; }

        public override NamedElements NamedElements { get { return Interface.NamedElements; } }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name);
        }
        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(Interface.Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }
        public static new VirtualInterface Create(string name,　DataObjects.DataTypes.IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataObjects.DataTypes.DataTypeEnum.Interface);
            BuildingBlocks.Interface? interface_ = dataType as BuildingBlocks.Interface;
            if (interface_ == null) throw new Exception();

            VirtualInterface val = new VirtualInterface() { Interface = interface_, Name = name };

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
            VirtualInterface val = new VirtualInterface() { Interface = Interface, Name = name, Defined = Defined };
            val.DataType = DataType;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }

        // IInstance
        public override Task_? GetTask(string identifier)
        {
            if (Interface.NamedElements.ContainsKey(identifier) && Interface.NamedElements[identifier] is Task_) return (Task_)Interface.NamedElements[identifier];
            return null;
        }
        public override Function? GetFunction(string identifier)
        {
            if (Interface.NamedElements.ContainsKey(identifier) && Interface.NamedElements[identifier] is Function) return (Function)Interface.NamedElements[identifier];
            return null;
        }
        public override DataObject? GetDataObject(string identifier)
        {
            if (!Interface.NamedElements.ContainsKey(identifier)) return null;
            return Interface.NamedElements[identifier] as DataObject;
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items)
        {
            Interface.AppendAutoCompleteItem(items);
        }


    }
}
