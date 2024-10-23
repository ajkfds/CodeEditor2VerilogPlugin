using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
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
                    foreach (Item item in targetItem.Items.Values) item.Dispose();
                    targetItem.Items.Clear();
                    return;
                }

                List<Item> keepItems = new List<Item>();
                Dictionary<string, Item> newItems = new Dictionary<string, Item>();

                // get new include files
                Dictionary<string, VerilogHeaderInstance> oldIncludes = new Dictionary<string, VerilogHeaderInstance>();
                foreach (Item item in targetItem.Items.Values)
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
                        Item? parent = targetItem as Item;
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
                            UpdateModuleInstance(module, project, targetItem, keepItems, newItems); // targetItem.VerilogParsedDocument eliminated here
                        }
                    }
                }

                { // Items not included in the acceptItems are considered unnecessary and will be deleted.
                    List<Item> removeItems = new List<Item>();
                    foreach (CodeEditor2.Data.Item item in targetItem.Items.Values)
                    {
                        if (!keepItems.Contains(item)) removeItems.Add(item);
                    }

                    foreach (Item item in removeItems)
                    {
                        targetItem.Items.Remove(item.Name);
                    }
                }

                foreach (Item item in newItems.Values)
                {
                    if(!targetItem.Items.ContainsValue(item)) targetItem.Items.Add(item.Name, item);
                }
            }
        }

        private static void UpdateModuleInstance(BuildingBlock newModule,Project project, IVerilogRelatedFile targetItem, List<Item> keepItems,Dictionary<string, Item> newItems)
        {
            foreach (IInstantiation newInstantiation in newModule.Instantiations.Values)
            {
                if (targetItem.Items.ContainsKey(newInstantiation.Name))
                { // already exist item
                    Item oldItem = targetItem.Items[newInstantiation.Name];
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

                        Item? parent = targetItem as Item;
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

                    Item? parent = targetItem as Item;
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


                    //Item item = null;
                    //if (newInstantiation is ModuleInstantiation)
                    //{
                    //    item = Data.VerilogModuleInstance.Create(newInstantiation as ModuleInstantiation, project);
                    //}
                    //if (item != null & !newItems.ContainsKey(newInstantiation.Name))
                    //{
                    //    item.Parent = targetItem as Item;
                    //    newItems.Add(newInstantiation.Name, item);
                    //    keepItems.Add(item);
                    //    if (newInstantiation.ParameterOverrides.Count != 0)
                    //    {
                    //        Data.VerilogModuleInstance moduleInstance = item as Data.VerilogModuleInstance;

                            
                    //        if (moduleInstance.ParsedDocument == null)
                    //        {   // background reparse
                    //            project.AddReparseTarget(item);
                    //        }
                    //    }
                    //}
                }
            }
        }

    }
}
