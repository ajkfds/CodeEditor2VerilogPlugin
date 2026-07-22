using pluginVerilog.Verilog.BuildingBlocks;
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

        public required Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }

        public BuildingBlocks.Interface? GetSourceInterface()
        {
            ProjectProperty projectProperty = (ProjectProperty)Project.GetPluginProperty();

            Data.IVerilogRelatedFile? file = projectProperty.GetFileOfBuildingBlock(SourceName);
            if (file == null) return null;
            if (file is not Data.VerilogFile) return null;

            Data.VerilogFile source = (Data.VerilogFile)file;
            if (source == null) return null;

            string instanceKey = Verilog.ParsedDocument.KeyGenerator(file, SourceName, ParameterOverrides);

            CodeEditor2.CodeEditor.ParsedDocument? codeEditorParsedDocument = source.GetInstancedParsedDocument(instanceKey);
            if (codeEditorParsedDocument == null) // don't have module parse result
            {
                codeEditorParsedDocument = source.ParsedDocument;
            }


            if (codeEditorParsedDocument is not ParsedDocument) return null;
            ParsedDocument? parsedDocument = (ParsedDocument)codeEditorParsedDocument;
            if (parsedDocument == null) return null;
            if (parsedDocument.Root == null) return null;

            if (parsedDocument.Root.BuildingBlocks.TryGetValue(SourceName, out BuildingBlocks.BuildingBlock? buildingBlock))
            {
                return buildingBlock as BuildingBlocks.Interface;
            }
            else
            {
                return null;
            }
        }

        public required string SourceName { get; init; }
        public required CodeEditor2.Data.Project Project { get; init; }


        public override NamedElements NamedElements {
            get {
                Interface? @interface = GetSourceInterface();
                if (@interface == null) return new NamedElements();
                return @interface.NamedElements;
            } 
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name);
        }
        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            Interface? @interface = GetSourceInterface();
            if (@interface == null) return;
            label.AppendText(@interface.Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }
        public static new VirtualInterface Create(string name,　DataObjects.DataTypes.IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataObjects.DataTypes.DataTypeEnum.Interface);
            BuildingBlocks.Interface? interface_ = dataType as BuildingBlocks.Interface;
            if (interface_ == null) throw new Exception();

            VirtualInterface val = new VirtualInterface() {
                Name = name, Project = interface_.Project, SourceName = interface_.Name,ParameterOverrides = new Dictionary<string, Expressions.Expression>() };

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
            VirtualInterface val = new VirtualInterface() {
                Name = name, Defined = Defined, Project = Project, SourceName = SourceName,ParameterOverrides = ParameterOverrides };
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
            Interface? @interface = GetSourceInterface();
            if (@interface == null) return null;

            if (@interface.NamedElements.ContainsKey(identifier) && @interface.NamedElements[identifier] is Task_) return (Task_)@interface.NamedElements[identifier];
            return null;
        }
        public override Function? GetFunction(string identifier)
        {
            Interface? @interface = GetSourceInterface();
            if (@interface == null) return null;
 
            if (@interface.NamedElements.ContainsKey(identifier) && @interface.NamedElements[identifier] is Function) return (Function)@interface.NamedElements[identifier];
            return null;
        }
        public override DataObject? GetDataObject(string identifier)
        {
            Interface? @interface = GetSourceInterface();
            if (@interface == null) return null;

            if (!@interface.NamedElements.ContainsKey(identifier)) return null;
            return @interface.NamedElements[identifier] as DataObject;
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items)
        {
            Interface? @interface = GetSourceInterface();
            if (@interface == null) return;

            @interface.AppendAutoCompleteItem(items);
        }


    }
}
