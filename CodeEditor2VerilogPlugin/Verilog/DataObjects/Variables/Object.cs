using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Object : Variable
    {
        protected Object() { }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        public required Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }
        public BuildingBlocks.Class? GetSourceClass() {

            ProjectProperty projectProperty = (ProjectProperty)Project.GetPluginProperty();
    
            Data.IVerilogRelatedFile? file = projectProperty.GetFileOfBuildingBlock(SourceName);
            if (file == null) return null;
            if (file is not Data.VerilogFile) return null;

            Data.VerilogFile source = (Data.VerilogFile)file;
            if (source == null) return null;

            string instanceKey = Verilog.ParsedDocument.KeyGenerator(file, SourceName, ParameterOverrides);

            CodeEditor2.CodeEditor.ParsedDocument? codeEditorParsedDocument = source.GetInstancedParsedDocument(instanceKey);
            if(codeEditorParsedDocument == null) // don't have module parse result
            {
                codeEditorParsedDocument = source.ParsedDocument;
            }


            if (codeEditorParsedDocument is not ParsedDocument) return null;
            ParsedDocument? parsedDocument = (ParsedDocument)codeEditorParsedDocument;
            if (parsedDocument == null) return null;
            if (parsedDocument.Root == null) return null;

            if(parsedDocument.Root.BuildingBlocks.TryGetValue(SourceName, out BuildingBlocks. BuildingBlock? buildingBlock))
            {
                return buildingBlock as BuildingBlocks.Class;
            }
            else
            {
                return null;
            }
        }
        public required string SourceName { get; init; }
        public required CodeEditor2.Data.Project Project { get; init; }

        public override NamedElements NamedElements { get {
                Class? @class = GetSourceClass();
                if(@class == null) return new NamedElements();
                return @class.NamedElements; 
            } }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name);
        }
        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            Class? @class = GetSourceClass();
            if(@class == null) return;
            label.AppendText(@class.Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }
        public static new Object Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Class);
            BuildingBlocks.Class? class_ = dataType as BuildingBlocks.Class;
            if (class_ == null) throw new Exception();

            Object val = new Object() { 
                Name = name, 
                ParameterOverrides = new Dictionary<string, Expressions.Expression>(), 
                Project = class_.Project,
                SourceName = class_.Name
            };

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
            Object val = new Object() { 
                Name = name, 
                Defined = Defined, 
                ParameterOverrides = new Dictionary<string, Expressions.Expression>(), 
                Project = Project, 
                SourceName = SourceName 
            };
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
            Class? @class = GetSourceClass();
            if (@class == null) return null;

            if (@class.NamedElements.ContainsKey(identifier) && @class.NamedElements[identifier] is Task_) return (Task_)@class.NamedElements[identifier];
            return null;
        }
        public override Function? GetFunction(string identifier)
        {
            Class? @class = GetSourceClass();
            if (@class == null) return null;

            if (@class.NamedElements.ContainsKey(identifier) && @class.NamedElements[identifier] is Function) return (Function)@class.NamedElements[identifier];
            return null;
        }
        public override DataObject? GetDataObject(string identifier)
        {
            Class? @class = GetSourceClass();
            if (@class == null) return null;

            if (!@class.NamedElements.ContainsKey(identifier)) return null;
            return @class.NamedElements[identifier] as DataObject;
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items)
        {
            Class? @class = GetSourceClass();
            if (@class == null) return;

            @class.AppendAutoCompleteItem(items);
        }


    }
}
