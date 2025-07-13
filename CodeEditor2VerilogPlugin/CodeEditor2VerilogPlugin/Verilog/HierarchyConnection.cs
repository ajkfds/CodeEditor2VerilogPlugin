using pluginVerilog.Data;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.ModuleItems;
using pluginVerilog.Verilog.Statements;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace pluginVerilog.Verilog
{
    public class HierarchyConnection
    {
        public HierarchyConnection(pluginVerilog.Data.IVerilogRelatedFile file,pluginVerilog.Verilog.DataObjects.DataObject dataObject)
        {
            Node = new ConnectionNode(file, dataObject);

        }

        public ConnectionNode? Node = null;
        public class ConnectionNode
        {
            public ConnectionNode(pluginVerilog.Data.IVerilogRelatedFile file,DataObjects.DataObject dataObject)
            {
                this.DataObject = dataObject;
                this.File = file;

                Data.VerilogModuleInstance? moduleInstance = file as Data.VerilogModuleInstance;
                if (moduleInstance != null)
                {
                    searchModuleInstance(moduleInstance, dataObject);
                    searchModulePort(moduleInstance, dataObject);
                }

            }

            public List<ConnectionNode> Driver = new List<ConnectionNode>();
            public List<ConnectionNode> Receiver = new List<ConnectionNode>();
            private void searchModulePort(Data.VerilogModuleInstance moduleInstance,DataObject dataObject)
            {
                if (moduleInstance.Module == null) return;
                if (!moduleInstance.Module.Ports.TryGetValue(dataObject.Name, out Port? port)) return;
                
                IVerilogRelatedFile? parentFile = File.Parent as IVerilogRelatedFile;
                if (parentFile == null) return;
                Verilog.ParsedDocument? parentParsedDocument = parentFile.VerilogParsedDocument;
                if (parentParsedDocument == null) return;
                if (parentParsedDocument.Root == null) return;

                if (parentParsedDocument.Root.BuldingBlocks.Count != 1) return;
                BuildingBlocks.Module? module = parentParsedDocument.Root.BuldingBlocks.First().Value as BuildingBlocks.Module;
                if (module == null) return;

                if (!module.NamedElements.ContainsKey(moduleInstance.Name)) return;
                ModuleInstantiation? moduleInstantiation = module.NamedElements[moduleInstance.Name] as ModuleInstantiation;
                if (moduleInstantiation == null) return;

                if (!moduleInstantiation.PortConnection.ContainsKey(port.Name)) return;
                Expressions.Expression connextionExpression = moduleInstantiation.PortConnection[port.Name];

                if (port.Direction == Port.DirectionEnum.Input)
                {
                    if(connextionExpression is VariableReference)
                    {
                        VariableReference variableReference = (VariableReference)connextionExpression;
                        DataObject? connectionDataObject = variableReference.Variable;
                        if(connectionDataObject is null) return;
                        foreach( var assign in connectionDataObject.AssignedReferences)
                        {

                        }
                    }
                    
                }else if(port.Direction == Port.DirectionEnum.Output)
                {
                    
                }
            }
            private void searchModuleInstance(Data.VerilogModuleInstance moduleInstance, DataObject dataObject)
            {
                if (moduleInstance.Module == null) return;
                if (!moduleInstance.Module.Ports.TryGetValue(dataObject.Name, out Port? port)) return;

                if (port.Direction == Port.DirectionEnum.Input)
                {
//                    moduleInstance.Module.NamedElements
                }
                else if (port.Direction == Port.DirectionEnum.Output)
                {

                }
            }



            DataObjects.DataObject DataObject;
            pluginVerilog.Data.IVerilogRelatedFile File;
            public ConnectionNode? Source;
            public List<ConnectionNode>? Destination;

            

        }


        //private (pluginVerilog.Data.IVerilogRelatedFile? file,pluginVerilog.Verilog.DataObjects.DataObject?) getDriver(pluginVerilog.Data.IVerilogRelatedFile file,pluginVerilog.Verilog.DataObjects.DataObject dataObject)
        //{
        //    if (dataObject.DefinedReference == null) return (null,null);
        //    pluginVerilog.Verilog.ParsedDocument? parsedDocument = dataObject.DefinedReference.ParsedDocument as pluginVerilog.Verilog.ParsedDocument;
        //    if (parsedDocument == null) return (null, null);
        //    if (parsedDocument.Root == null) return (null, null);

        //    if (!parsedDocument.Root.NamedElements.ContainsKey(dataObject.Name)) return (null, null);
        //    INamedElement? element = parsedDocument.Root.NamedElements[dataObject.Name];

        //    DataObjects.Port? port = element as Verilog.DataObjects.Port;
        //    if (port == null) return (null, null);
        //    if (port.Direction != DataObjects.Port.DirectionEnum.Input) return (null, null);

        //    if (parsedDocument.File is not Data.VerilogModuleInstance) return dataObject;
        //    Data.VerilogModuleInstance moduleInstanceFile = (Data.VerilogModuleInstance)parsedDocument.File;
        //    string instanceName = moduleInstanceFile.Name;

        //    ModuleItems.ModuleInstantiation? instanceElement = moduleInstanceFile.GetParentInstanciatiation();
        //    if (instanceElement == null) return null;
        //    if(!instanceElement.PortConnection.ContainsKey(port.Name)) return null;

        //    Expression expression = instanceElement.PortConnection[port.Name];
        //    if(expression is VariableReference variableReference)
        //    {
                
        //    }


        //    return null;
        //}

        private DataObject? getModuleInstancePortConnection(ModuleItems.ModuleInstantiation moduleInst,string portName)
        {
            if(!moduleInst.PortConnection.ContainsKey(portName)) return null;
            Expressions.Expression expression = moduleInst.PortConnection[portName];

            return null;
        }

        private ModuleItems.ModuleInstantiation? getParentModuleInstance(CodeEditor2.Data.File file)
        {
            Data.VerilogModuleInstance? moduleInstance = file as Data.VerilogModuleInstance;
            if(moduleInstance == null) return null;
            string instanceName = moduleInstance.Name;

            Data.IVerilogRelatedFile? parentFile = file.Parent as Data.IVerilogRelatedFile;
            if(parentFile?.VerilogParsedDocument?.Root?.NamedElements.ContainsKey(instanceName) != true) return null;
            INamedElement namedElement = parentFile.VerilogParsedDocument.Root.NamedElements[instanceName];
            Verilog.ModuleItems.ModuleInstantiation? moduleInst = namedElement as Verilog.ModuleItems.ModuleInstantiation;
            if(moduleInst == null) return null;
            return moduleInst;
        }


    }
}
