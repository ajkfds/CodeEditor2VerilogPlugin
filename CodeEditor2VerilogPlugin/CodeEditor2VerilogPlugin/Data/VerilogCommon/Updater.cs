using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog.Data.VerilogCommon
{
    public static class Updater
    {
        public static void Update(IVerilogRelatedFile targetItem)
        {
            // Update the Items member of this object according to the rootItem.ParsedDocument.

            Project project = targetItem.Project;

            string? moduleName = null;
            VerilogModuleInstance? newVerilogModuleInstance = targetItem as VerilogModuleInstance;
            moduleName = newVerilogModuleInstance?.ModuleName;


            lock (targetItem.Items)
            {
                if (targetItem.VerilogParsedDocument == null)
                {
                    // dispose all sub items
                    foreach (CodeEditor2.Data.Item item in targetItem.Items.Values) item.Dispose();
                    targetItem.Items.Clear();
                    return;
                }

                List<CodeEditor2.Data.Item> keepItems = new List<CodeEditor2.Data.Item>();
                Dictionary<string, CodeEditor2.Data.Item> newItems = new Dictionary<string, CodeEditor2.Data.Item>();

                // get new include files
                Dictionary<string, VerilogHeaderInstance> oldIncludes = new Dictionary<string, VerilogHeaderInstance>();
                foreach (CodeEditor2.Data.Item item in targetItem.Items.Values)
                {
                    VerilogHeaderInstance? oldVerilogHeaderInstance = item as VerilogHeaderInstance;
                    if (oldVerilogHeaderInstance == null) continue;
                    oldIncludes.Add(oldVerilogHeaderInstance.ID, oldVerilogHeaderInstance);
                }

                // include file
                foreach (VerilogHeaderInstance newVhInstance in targetItem.VerilogParsedDocument.IncludeFiles.Values)
                {
                    if (oldIncludes.ContainsKey(newVhInstance.ID)) // already exist
                    {
                        oldIncludes[newVhInstance.ID].ReplaceBy(newVhInstance);
                        keepItems.Add(oldIncludes[newVhInstance.ID]);
                    }
                    else
                    {   // add new include item
                        string keyName = newVhInstance.Name;
                        { // If the names are duplicated, append a number to the end
                            int i = 0;
                            while (targetItem.Items.ContainsKey(keyName + "_" + i.ToString()) || newItems.ContainsKey(keyName + "_" + i.ToString()))
                            {
                                i++;
                            }
                            keyName = keyName + "_" + i.ToString();
                        }
                        newItems.Add(keyName, newVhInstance);
                        keepItems.Add(newVhInstance);
                        CodeEditor2.Data.Item? parent = targetItem as CodeEditor2.Data.Item;
                        if (parent == null) throw new Exception();
                        newVhInstance.Parent = parent;
                    }
                }

                // module
                if (targetItem.VerilogParsedDocument.Root != null)
                {
                    foreach (BuildingBlock module in targetItem.VerilogParsedDocument.Root.BuldingBlocks.Values)
                    {
                        if (moduleName == null || moduleName == module.Name) // matched module
                        {
                            UpdateInstance(module, project, targetItem, keepItems, newItems); // targetItem.VerilogParsedDocument eliminated here
                        }
                    }
                }

                { // Items not included in the acceptItems are considered unnecessary and will be deleted.
                    List<CodeEditor2.Data.Item> removeItems = new List<CodeEditor2.Data.Item>();
                    foreach (CodeEditor2.Data.Item item in targetItem.Items.Values)
                    {
                        if (!keepItems.Contains(item)) removeItems.Add(item);
                    }

                    foreach (CodeEditor2.Data.Item item in removeItems)
                    {
                        targetItem.Items.Remove(item.Name);
                    }
                }

                foreach (CodeEditor2.Data.Item item in newItems.Values)
                {
                    if(!targetItem.Items.ContainsValue(item)) targetItem.Items.Add(item.Name, item);
                }
            }
        }

        private static void UpdateInstance(BuildingBlock newModule,Project project, IVerilogRelatedFile targetItem, List<CodeEditor2.Data.Item> keepItems,Dictionary<string, CodeEditor2.Data.Item> newItems)
        {
            List<INamedElement> instantiations = newModule.NamedElements.Values.FindAll(x => x is IBuildingBlockInstantiation);

            foreach (IBuildingBlockInstantiation newInstantiation in instantiations)
            {
                if (targetItem.Items.ContainsKey(newInstantiation.Name))
                { // already exist item
                    CodeEditor2.Data.Item oldItem = targetItem.Items[newInstantiation.Name];
                    VerilogModuleInstance? oldVerilogModuleInstance = oldItem as VerilogModuleInstance;
                    if(oldVerilogModuleInstance != null)
                    {
                        ModuleInstantiation? newModuleInstantiation = newInstantiation as ModuleInstantiation;
                        if (newModuleInstantiation != null)
                        {
                            if (oldVerilogModuleInstance.ReplaceBy(newModuleInstantiation, project))
                            {
                                keepItems.Add(oldItem);
                                continue;
                            }
                        }
                    }

                    // re-generate (same module instance name, but different file or module name or parameter
                    {
                        // re-generate module instantiation
                        ModuleInstantiation? newModuleInstantiation = newInstantiation as ModuleInstantiation;
                        if (newModuleInstantiation == null) continue;
                        VerilogModuleInstance? newVerilogModuleInstance = VerilogModuleInstance.Create(newModuleInstantiation, project);
                        if (newVerilogModuleInstance == null) continue;
                        if (newItems.ContainsKey(newInstantiation.Name)) continue;

                        CodeEditor2.Data.Item? parent = targetItem as CodeEditor2.Data.Item;
                        if (parent == null) throw new Exception();
                        newVerilogModuleInstance.Parent = parent;
                        newItems.Add(newInstantiation.Name, newVerilogModuleInstance);
                        keepItems.Add(newVerilogModuleInstance);
                        if (newInstantiation.ParameterOverrides.Count != 0)
                        {
                            if(newVerilogModuleInstance.ParsedDocument == null)
                            {
                                project.AddReparseTarget(newVerilogModuleInstance);
                            }
                        }
                    }
                }
                else
                { // new item
                    ModuleInstantiation? newModuleInstantiation = newInstantiation as ModuleInstantiation;
                    if (newModuleInstantiation == null) continue;
                    VerilogModuleInstance? newVerilogModuleInstance = VerilogModuleInstance.Create(newModuleInstantiation, project);
                    if (newVerilogModuleInstance == null) continue;
                    if (newItems.ContainsKey(newInstantiation.Name)) continue;

                    CodeEditor2.Data.Item? parent = targetItem as CodeEditor2.Data.Item;
                    if (parent == null) throw new Exception();
                    newVerilogModuleInstance.Parent = parent;
                    newItems.Add(newInstantiation.Name, newVerilogModuleInstance);
                    keepItems.Add(newVerilogModuleInstance);
                    if (newInstantiation.ParameterOverrides.Count != 0)
                    {
                        if (newVerilogModuleInstance.ParsedDocument == null)
                        {
                            project.AddReparseTarget(newVerilogModuleInstance);
                        }
                    }
                }
            }
        }

    }
}
