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
        public static void Update(IVerilogRelatedFile item)
        {
            // Update the Items member of this object according to the rootItem.ParsedDocument.
            Project project = item.Project;
            CodeEditor2.Data.Item? parent = item as CodeEditor2.Data.Item;

            // doesn't have appropriate pased data for this item
            if (
                parent == null ||
                item.VerilogParsedDocument == null ||
                item.VerilogParsedDocument.Root == null
                )
            {
                // dispose all sub items
                lock (item.Items)
                {
                    foreach (CodeEditor2.Data.Item subItem in item.Items.Values) subItem.Dispose();
                    item.Items.Clear();
                }
                return;
            }

            string? moduleName = null;
            VerilogModuleInstance? verilogModuleInstance = item as VerilogModuleInstance;
            moduleName = verilogModuleInstance?.ModuleName;

            lock (item.Items)
            {
                // create new items list
                Dictionary<string, CodeEditor2.Data.Item> newSubItems = new Dictionary<string, CodeEditor2.Data.Item>();

                // add vh instance
                foreach (VerilogHeaderInstance newVhInstance in item.VerilogParsedDocument.IncludeFiles.Values)
                {
                    {  // add new include item
                        string keyName = newVhInstance.Name;
                        { // If the names are duplicated, append a number to the end
                            int i = 0;
                            while (item.Items.ContainsKey(keyName + "_" + i.ToString()) || newSubItems.ContainsKey(keyName + "_" + i.ToString()))
                            {
                                i++;
                            }
                            keyName = keyName + "_" + i.ToString();
                        }
                        newSubItems.Add(keyName, newVhInstance);
                        newVhInstance.Parent = parent;
                    }
                }

                // add building block instance
                foreach (BuildingBlock newModule in item.VerilogParsedDocument.Root.BuldingBlocks.Values)
                {
                    if(moduleName != null && moduleName != newModule.Name)
                    {
                        // parsed document can have multiple modules, but update only matched module to this item
                        continue;
                    }

                    // get instanciations on this module
                    List<INamedElement> newInstantiations = newModule.NamedElements.Values.FindAll(x => x is IBuildingBlockInstantiation);

                    foreach (IBuildingBlockInstantiation instantiation in newInstantiations)
                    {
                        bool alreadyExist = false;

                        if (item.Items.ContainsKey(instantiation.Name))
                        {   // has same name item
                            CodeEditor2.Data.Item subItem = item.Items[instantiation.Name];

                            if (instantiation is ModuleInstantiation)
                            {
                                ModuleInstantiation moduleInstantiation = (ModuleInstantiation)instantiation;
                                VerilogModuleInstance? moduleInstance = subItem as VerilogModuleInstance;

                                if (
                                    moduleInstantiation != null &&
                                    moduleInstance != null &&
                                    moduleInstantiation.SourceName == moduleInstance.ModuleName &&
                                    moduleInstantiation.ParameterId == moduleInstance.ParameterId
                                    ) {
                                    alreadyExist = true;

                                    newSubItems.Add(subItem.Name, subItem);
                                    continue;
                                }
                            }
                            else if (instantiation is IBuildingBlockInstantiation)
                            {

                            }
                        }

                        if (alreadyExist)
                        {
                            newSubItems.Add(instantiation.Name, (CodeEditor2.Data.Item)instantiation);
                        }
                        else
                        {
                            if (instantiation is ModuleInstantiation)
                            {
                                VerilogModuleInstance? newVerilogModuleInstance = VerilogModuleInstance.Create((ModuleInstantiation)instantiation, project);
                                if (newSubItems.ContainsKey(instantiation.Name) || newVerilogModuleInstance == null) continue;
                                newVerilogModuleInstance.Parent = parent;
                                newSubItems.Add(instantiation.Name, newVerilogModuleInstance);
                            }
                            else if (instantiation is IBuildingBlockInstantiation)
                            {

                            }
                        }
                    }

                }

                item.Items.Clear();
                foreach (CodeEditor2.Data.Item i in newSubItems.Values)
                {
                    item.Items.Add(i.Name,i);
                }


                // remove unused item
                //List<CodeEditor2.Data.Item> removeItems = new List<CodeEditor2.Data.Item>();
                //foreach(CodeEditor2.Data.Item i in item.Items.Values)
                //{
                //    if (!newSubItems.Values.Contains(i))
                //    {
                //        removeItems.Add(i);
                //    }
                //}
                //foreach(CodeEditor2.Data.Item i in removeItems)
                //{
                //    item.Items.Remove(i.Name);
                //}

                // add new items




                //// keep old include files

                //List<CodeEditor2.Data.Item> keepItems = new List<CodeEditor2.Data.Item>();

                //// get new include files
                //Dictionary<string, VerilogHeaderInstance> oldIncludes = new Dictionary<string, VerilogHeaderInstance>();
                //foreach (CodeEditor2.Data.Item item in targetItem.Items.Values)
                //{
                //    VerilogHeaderInstance? oldVerilogHeaderInstance = item as VerilogHeaderInstance;
                //    if (oldVerilogHeaderInstance == null) continue;
                //    oldIncludes.Add(oldVerilogHeaderInstance.ID, oldVerilogHeaderInstance);
                //}

                //// include file
                //foreach (VerilogHeaderInstance newVhInstance in targetItem.VerilogParsedDocument.IncludeFiles.Values)
                //{
                //    if (oldIncludes.ContainsKey(newVhInstance.ID)) // already exist
                //    {
                //        oldIncludes[newVhInstance.ID].ReplaceBy(newVhInstance);
                //        keepItems.Add(oldIncludes[newVhInstance.ID]);
                //    }
                //    else
                //    {   // add new include item
                //        string keyName = newVhInstance.Name;
                //        { // If the names are duplicated, append a number to the end
                //            int i = 0;
                //            while (targetItem.Items.ContainsKey(keyName + "_" + i.ToString()) || newItems.ContainsKey(keyName + "_" + i.ToString()))
                //            {
                //                i++;
                //            }
                //            keyName = keyName + "_" + i.ToString();
                //        }
                //        newItems.Add(keyName, newVhInstance);
                //        keepItems.Add(newVhInstance);
                //        CodeEditor2.Data.Item? parent = targetItem as CodeEditor2.Data.Item;
                //        if (parent == null) throw new Exception();
                //        newVhInstance.Parent = parent;
                //    }
                //}

                //// module
                //if (targetItem.VerilogParsedDocument.Root != null)
                //{
                //    foreach (BuildingBlock module in targetItem.VerilogParsedDocument.Root.BuldingBlocks.Values)
                //    {
                //        if (moduleName == null || moduleName == module.Name) // matched module
                //        {
                //            System.Diagnostics.Debug.Print("### VerilogCommon.Updater UpdateInstance");
                //            UpdateInstance(module, project, targetItem, keepItems, newItems); // targetItem.VerilogParsedDocument eliminated here
                //        }
                //    }
                //}

                //{ // Items not included in the acceptItems are considered unnecessary and will be deleted.
                //    List<CodeEditor2.Data.Item> removeItems = new List<CodeEditor2.Data.Item>();
                //    foreach (CodeEditor2.Data.Item item in targetItem.Items.Values)
                //    {
                //        if (!keepItems.Contains(item)) removeItems.Add(item);
                //    }

                //    foreach (CodeEditor2.Data.Item item in removeItems)
                //    {
                //        System.Diagnostics.Debug.Print("### VerilogCommon.Updater RemoveItem");
                //        targetItem.Items.Remove(item.Name);
                //    }
                //}

                //foreach (CodeEditor2.Data.Item item in newItems.Values)
                //{
                //    if(!targetItem.Items.ContainsValue(item)) targetItem.Items.Add(item.Name, item);
                //}
            }
        }

        private static void UpdateInstance(BuildingBlock newModule,Project project, IVerilogRelatedFile targetItem, List<CodeEditor2.Data.Item> keepItems,Dictionary<string, CodeEditor2.Data.Item> newItems)
        {
            List<INamedElement> instantiations = newModule.NamedElements.Values.FindAll(x => x is IBuildingBlockInstantiation);

            foreach (IBuildingBlockInstantiation newInstantiation in instantiations)
            {
                if (targetItem.Items.ContainsKey(newInstantiation.Name))
                { // already exist item
                    System.Diagnostics.Debug.Print("### VerilogCommon.Updater UpdateInstance replace");
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
                        System.Diagnostics.Debug.Print("### VerilogCommon.Updater UpdateInstance regenerate");
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
                    System.Diagnostics.Debug.Print("### VerilogCommon.Updater UpdateInstance new");

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
