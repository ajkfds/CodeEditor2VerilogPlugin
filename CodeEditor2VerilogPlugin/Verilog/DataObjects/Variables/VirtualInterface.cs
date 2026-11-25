using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class VirtualInterface : Variable
    {
        protected VirtualInterface() { }
        public override VerilogColorType ColorType { get { return VerilogColorType.Variable; } }

        public required Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }

        public BuildingBlocks.Interface? GetSourceInterface()
        {
            IVerilogParserSettings? projectProperty = Project.Settings as IVerilogParserSettings;
            if (projectProperty == null) throw new Exception();

            Data.IVerilogRelatedFile? file = projectProperty.DefinitionNameSpace.GetFile(SourceName);
            if (file == null) return null;

            // The instance lookup is part of the UI-free source file contract,
            // so no cast to the editor's concrete file type is needed.
            string instanceKey = Verilog.ParsedDocument.KeyGenerator(file, SourceName, ParameterOverrides);

            ParsedDocument? parsedDocument = file.GetInstancedParsedDocument(instanceKey) as ParsedDocument;
            if (parsedDocument == null) // don't have module parse result
            {
                parsedDocument = file.VerilogParsedDocument;
            }
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
        public required SystemVerilogCore.Projects.IVerilogProject Project { get; init; }


        public override NamedElements NamedElements {
            get {
                Interface? @interface = GetSourceInterface();
                if (@interface == null) return new NamedElements();
                return @interface.NamedElements;
            } 
        }

        public override void AppendLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name);
        }
        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            Interface? @interface = GetSourceInterface();
            if (@interface == null) return;
            label.AppendText(@interface.Name, VerilogColorType.Keyword);
            label.AppendText(" ");
        }
        public static new VirtualInterface Create(string name,　DataObjects.DataTypes.IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataObjects.DataTypes.DataTypeEnum.Interface);
            BuildingBlocks.Interface? interface_ = dataType as BuildingBlocks.Interface;
            if (interface_ == null) throw new Exception();

            VirtualInterface val = new VirtualInterface() {
                Name = name, Project = interface_.Project, SourceName = interface_.Name,ParameterOverrides = new Dictionary<string, Expressions.Expression>() };

            List<string> ids = new List<string>();
            defineElements(val,ids);

            val.DataType = dataType;
            return val;
        }

        private static void defineElements(INamedElement namedElement,List<string> ids)
        {
            foreach (INamedElement subElement in namedElement.NamedElements)
            {
                Variable? variable = subElement as Variable;
                if (variable != null) variable.Defined = true;
                string key = subElement.Name;
                if(subElement is DataObjects.Variables.VirtualInterface)
                {
                    var vi = (DataObjects.Variables.VirtualInterface) subElement;
                    key = vi.SourceName;
                }
                if (ids.Contains(key))
                {
                    continue;
                }
                else
                {
                    ids.Add(key);
                }

                defineElements(subElement,ids);
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



    }
}
