using pluginVerilog.Verilog;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.Items;
using System.Collections.Generic;
using System.Linq;

namespace pluginVerilog.Data
{
    /// <summary>
    /// Walks the driver / receiver relationships of a data object through the
    /// module instances that surround it.
    ///
    /// <para>
    /// This is an editor feature (it feeds the connection view), and it is built
    /// entirely on the data layer's concrete instance type
    /// (<see cref="VerilogModuleInstance"/>), so it lives here rather than in the
    /// parser. It used to sit under <c>pluginVerilog.Verilog</c>, which is what
    /// kept that folder bound to the data layer.
    /// </para>
    /// </summary>
    public class HierarchyConnection
    {
        public HierarchyConnection(IVerilogRelatedFile file, DataObject dataObject)
        {
            Node = new ConnectionNode(file, dataObject);
        }

        public ConnectionNode? Node = null;

        public class ConnectionNode
        {
            public ConnectionNode(IVerilogRelatedFile file, DataObject dataObject)
            {
                this.DataObject = dataObject;
                this.File = file;

                VerilogModuleInstance? moduleInstance = file as VerilogModuleInstance;
                if (moduleInstance != null)
                {
                    searchModuleInstance(moduleInstance, dataObject);
                    searchModulePort(moduleInstance, dataObject);
                }
            }

            public List<ConnectionNode> Driver = new List<ConnectionNode>();
            public List<ConnectionNode> Receiver = new List<ConnectionNode>();

            private void searchModulePort(VerilogModuleInstance moduleInstance, DataObject dataObject)
            {
                if (moduleInstance.Module == null) return;
                if (!moduleInstance.Module.Ports.TryGetValue(dataObject.Name, out Port? port)) return;

                IVerilogRelatedFile? parentFile = File.Parent as IVerilogRelatedFile;
                if (parentFile == null) return;
                Verilog.ParsedDocument? parentParsedDocument = parentFile.VerilogParsedDocument;
                if (parentParsedDocument == null) return;
                if (parentParsedDocument.Root == null) return;

                if (parentParsedDocument.Root.BuildingBlocks.Count != 1) return;
                Verilog.BuildingBlocks.Module? module =
                    parentParsedDocument.Root.BuildingBlocks.First().Value as Verilog.BuildingBlocks.Module;
                if (module == null) return;

                if (!module.NamedElements.ContainsKey(moduleInstance.Name)) return;
                ModuleInstantiation? moduleInstantiation =
                    module.NamedElements[moduleInstance.Name] as ModuleInstantiation;
                if (moduleInstantiation == null) return;

                if (!moduleInstantiation.PortConnection.ContainsKey(port.Name)) return;
                Expression connextionExpression = moduleInstantiation.PortConnection[port.Name];

                if (port.Direction == Port.DirectionEnum.Input)
                {
                    if (connextionExpression is DataObjectReference)
                    {
                        DataObjectReference variableReference = (DataObjectReference)connextionExpression;
                        DataObject? connectionDataObject = variableReference.TargetDataObject;
                        if (connectionDataObject is null) return;
                        foreach (var assign in connectionDataObject.AssignedReferences)
                        {
                        }
                    }
                }
                else if (port.Direction == Port.DirectionEnum.Output)
                {
                }
            }

            private void searchModuleInstance(VerilogModuleInstance moduleInstance, DataObject dataObject)
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

            DataObject DataObject;
            IVerilogRelatedFile File;
            public ConnectionNode? Source;
            public List<ConnectionNode>? Destination;
        }

        // NOTE: getDriver(IVerilogRelatedFile, DataObject) and
        // getParentModuleInstance(CodeEditor2.Data.File) used to live here. Both
        // were dead code (no callers outside their own declarations), and the
        // second carried the editor's file type purely as a parameter. They were
        // removed rather than re-typed.
    }
}
