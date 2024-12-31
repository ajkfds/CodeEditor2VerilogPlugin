using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog.Verilog.Snippets
{
    public class PortConnectionCreateSnippet : ToolItem
    {
        public PortConnectionCreateSnippet() : base("portConnectionCreate")
        {
        }

        public override void Apply()
        {
            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            CodeEditor2.CodeEditor.CodeDocument codeDocument = file.CodeDocument;

            CodeEditor2.Data.ITextFile? iText = CodeEditor2.Controller.CodeEditor.GetTextFile();

            if (!(iText is Data.IVerilogRelatedFile)) return;
            Data.IVerilogRelatedFile? vFile = iText as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            ParsedDocument parsedDocument = vFile.VerilogParsedDocument;
            if (parsedDocument == null) return;

            int index = codeDocument.CaretIndex;
            IndexReference iref = IndexReference.Create(parsedDocument.IndexReference, index);

            BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(index);
            if (buildingBlock == null) return;

            foreach (var instance in
                buildingBlock.NamedElements.Values)
            {
                ModuleInstantiation? moduleInstantiation = instance as ModuleInstantiation;
                if (moduleInstantiation == null) continue;

                if (iref.IsSmallerThan(moduleInstantiation.BeginIndexReference)) continue;
                if (moduleInstantiation.LastIndexReference==null || iref.IsGreaterThan(moduleInstantiation.LastIndexReference)) continue;

                writeModuleInstance(codeDocument, index, moduleInstantiation);
                return;
            }
        }

        private void writeModuleInstance(CodeDocument codeDocument, int index, ModuleItems.ModuleInstantiation moduleInstantiation)
        {
            CodeEditor.CodeDocument? vCodeDocument = codeDocument as CodeEditor.CodeDocument;
            if (vCodeDocument == null) return;
            if (moduleInstantiation.LastIndexReference == null) return;
            CodeEditor2.Data.ITextFile? iText = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (iText == null) return;
            ProjectProperty? projectProperty = iText.Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return;

            string indent = vCodeDocument.GetIndentString(index);

            CodeEditor2.Controller.CodeEditor.SetCaretPosition(moduleInstantiation.BeginIndexReference.Indexes.Last());
            codeDocument.Replace(
                moduleInstantiation.BeginIndexReference.Indexes.Last(),
                moduleInstantiation.LastIndexReference.Indexes.Last() - moduleInstantiation.BeginIndexReference.Indexes.Last() + 1,
                0,
                CreateString(moduleInstantiation,"\t",projectProperty)
                );
            CodeEditor2.Controller.CodeEditor.SetSelection(codeDocument.CaretIndex, codeDocument.CaretIndex);
            CodeEditor2.Controller.CodeEditor.RequestReparse();
        }

        private string CreateString(ModuleInstantiation moduleInstantiation,string indent, ProjectProperty projectProperty)
        {
            Module? instancedModule = projectProperty.GetBuildingBlock(moduleInstantiation.SourceName) as Module;
            if (instancedModule == null) return "";
            StringBuilder sbDefine = new StringBuilder();


            StringBuilder sb = new StringBuilder();
            bool first;

            sb.Append(moduleInstantiation.SourceName);
            sb.Append(" ");

            if (instancedModule.PortParameterNameList.Count != 0)
            {
                sb.Append("#(\r\n");

                first = true;
                foreach (var paramName in instancedModule.PortParameterNameList)
                {
                    if (!first) sb.Append(",\r\n");
                    sb.Append(indent);
                    sb.Append(".");
                    sb.Append(paramName);
                    sb.Append("\t( ");
                    if (moduleInstantiation.ParameterOverrides.ContainsKey(paramName))
                    {
                        sb.Append(moduleInstantiation.ParameterOverrides[paramName].CreateString());
                    }
                    else
                    {
                        if (
                            instancedModule.NamedElements.ContainsKey(paramName) &&
                            instancedModule.NamedElements[paramName] is DataObjects.Constants.Constants &&
                            ((DataObjects.Constants.Constants)instancedModule.NamedElements[paramName]).Expression != null
                            )
                        {
                            sb.Append(((DataObjects.Constants.Constants)instancedModule.NamedElements[paramName]).Expression.CreateString());
                        }
                    }
                    sb.Append(" )");
                    first = false;
                }
                sb.Append("\r\n) ");
            }

            sb.Append(moduleInstantiation.Name);
            sb.Append(" (\r\n");

            first = true;
            string? sectionName = null;
            foreach (var port in instancedModule.Ports.Values)
            {
                if (!first) sb.Append(",\r\n");

                if (port.SectionName != sectionName)
                {
                    sectionName = port.SectionName;
                    sb.Append("// ");
                    sb.Append(sectionName);
                    sb.Append("\r\n");
                }
                sb.Append(indent);
                sb.Append(".");
                sb.Append(port.Name);
                sb.Append("\t");
                sb.Append("( ");
                if (moduleInstantiation.PortConnection.ContainsKey(port.Name))
                {
                    sb.Append(moduleInstantiation.PortConnection[port.Name].CreateString());
                }
                else
                {
                    string valueName = port.Name.ToLower();
                    sb.Append(valueName);

                    BuildingBlock? buildingBlock = moduleInstantiation.GetInstancedBuildingBlock();
                    if(buildingBlock != null && !buildingBlock.NamedElements.ContainsKey(valueName)){


                        if(port.DataObject != null)
                        {
                            if(port.DataObject is DataObjects.Nets.Net)
                            {
                                DataObjects.Nets.Net net = (DataObjects.Nets.Net)port.DataObject;
                                DataObjects.Nets.Net newNet = new DataObjects.Nets.Net() { Name = valueName };
                            }
                            else if(port.DataObject is DataObjects.Variables.IntegerVectorValueVariable)
                            {
                                DataObjects.Variables.IntegerVectorValueVariable vector = (DataObjects.Variables.IntegerVectorValueVariable)port.DataObject;
                                DataObjects.Nets.Net newNet = new DataObjects.Nets.Net() { Name = valueName };

                                sb.Append("wire");
                            }
                            else
                            {
                                sbDefine.Append(port.DataObject.CreateTypeString());
                            }


                            sbDefine.Append("\t" + valueName);
                            foreach (var dimension in port.DataObject.Dimensions)
                            {
                                sbDefine.Append(dimension.CreateString());
                            }
                            sbDefine.Append(";\n");
                        }
                    }
                }
                sb.Append(" )");
                first = false;
            }
            sb.Append("\r\n);");

            sbDefine.Append(sb.ToString());
            return sbDefine.ToString();
        }
    }
}

