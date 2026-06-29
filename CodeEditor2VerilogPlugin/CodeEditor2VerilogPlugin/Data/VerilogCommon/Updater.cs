using Avalonia.Input;
using Avalonia.Threading;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Threading;

namespace pluginVerilog.Data.VerilogCommon
{
    public static class Updater
    {


        /// <summary>
        /// Update the Items member of this object according to the rootItem.ParsedDocument.
        /// </summary>
        /// <param name="item"></param>
        /// <summary>
        /// Lock for atomic items update to prevent partial updates during reads
        /// </summary>

        public static async System.Threading.Tasks.Task UpdateAsync(IVerilogRelatedFile item, SemaphoreSlim _semaphore)
        {
            // run on background thread to avoid blocking UI
            if (Dispatcher.UIThread.CheckAccess())
            {
                await System.Threading.Tasks.Task.Run(async ()=>await UpdateAsync(item, _semaphore));
                return;
            }

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
                // dispose all sub items - use atomic update to prevent partial state
                item.Items.Clear(out List<Item> clearedItems);
                // Dispose after clearing to avoid deadlock
                foreach (var subItem in clearedItems)
                {
                    subItem.Dispose();
                }
                return;
            }

            // create new items list
            Dictionary<string, CodeEditor2.Data.Item> newSubItems = new Dictionary<string, CodeEditor2.Data.Item>();

            // Build new sub-items dictionary first (outside the lock)
            // add vh instance
            foreach (VerilogHeaderInstance newVhInstance in item.VerilogParsedDocument.IncludeFiles.Values)
            {
                addVhInstance(newSubItems, item, newVhInstance);
            }

            if (item is VerilogModuleInstance)
            {
                string? moduleName = null;
                VerilogModuleInstance? verilogModuleInstance = item as VerilogModuleInstance;
                moduleName = verilogModuleInstance?.ModuleName;
                addSubItemsSingleBuldingBlock(item, moduleName, newSubItems, parent, project);
            }
            else if (item is VerilogFile)
            {
                // 複数のmoduleを含むroot itemは子として各moduleのinstanceを生成させる
                VerilogFile verilogFile = (VerilogFile)item;
                if (item.VerilogParsedDocument.Root.BuildingBlocks.Count == 1)
                {
                    addSubItemsSingleBuldingBlock(item, null, newSubItems, parent, project);
                }
                else
                {
                    addSubItemsMultiBuildingBlock(verilogFile, newSubItems, parent, project);
                }
            }

            // Atomic swap: dispose old items and replace with new ones in a single operation

            var oldItems = new List<CodeEditor2.Data.Item>();

            await _semaphore.WaitAsync();
            try
            {

                // Atomically swap items: clear old and add new in one locked operation
                // Capture old items for disposal
                oldItems = item.Items.GetSnapShot();
                foreach (Item newItem in newSubItems.Values)
                {
                    if (oldItems.Contains(newItem)) oldItems.Remove(newItem);
                }

                // Clear and populate in a single atomic operation
                item.Items.ReplaceTo(newSubItems);
                // Dispose old items after the atomic swap is complete
            }
            finally
            {
                _semaphore.Release();
            }

            foreach (var oldItem in oldItems)
            {
                oldItem.Dispose();
            }
        }

        private static void addVhInstance(Dictionary<string, CodeEditor2.Data.Item> newSubItems, IVerilogRelatedFile item, VerilogHeaderInstance newVhInstance)
        {
            string keyName = newVhInstance.Name;

            // update external project
            if (item is VerilogHeaderInstance && ((VerilogHeaderInstance)item).ExternalProject)
            {
                newVhInstance.ExternalProject = true;
            }
            VerilogHeaderInstance? oldInstanceTextFile = null;
            if (item.Items.TryGetValue(keyName, out CodeEditor2.Data.Item? gotItem))
            {
                if (gotItem == null) throw new Exception();
                oldInstanceTextFile = gotItem as VerilogHeaderInstance;
            }

            if (
                oldInstanceTextFile != null &&
                oldInstanceTextFile.ExternalProject == newVhInstance.ExternalProject &&
                oldInstanceTextFile.ID == newVhInstance.ID
                )
            {
                oldInstanceTextFile.ReplaceBy(newVhInstance);
                newSubItems.Add(keyName, oldInstanceTextFile);
                return;
            }

            // add new one
            newSubItems.Add(keyName, newVhInstance);
            newVhInstance.Parent = item as CodeEditor2.Data.Item;
        }

        private static void addSubItemsMultiBuildingBlock(VerilogFile verilogFile, Dictionary<string, CodeEditor2.Data.Item> newSubItems, CodeEditor2.Data.Item? parent, Project project)
        {
            if (verilogFile.VerilogParsedDocument?.Root == null) throw new Exception();

            // add building block instance
            foreach (var buldingBlockKvp in verilogFile.VerilogParsedDocument.Root.BuildingBlocks)
            {
                bool alreadyExist = false;

                if (verilogFile.Items.TryGetValue(buldingBlockKvp.Value.Name, out CodeEditor2.Data.Item? gotItem))
                {   // has same name item
                    if (gotItem == null) throw new Exception();
                    CodeEditor2.Data.Item subItem = gotItem;

                    if (buldingBlockKvp.Value is Module)
                    {
                        VerilogModuleInstance? oldItem = subItem as VerilogModuleInstance;
                        Module module = (Module)buldingBlockKvp.Value;

                        if (oldItem != null && module != null && oldItem.Name == module.Name && oldItem.ModuleName == module.Name)
                        {
                            alreadyExist = true;
                            newSubItems.Add(oldItem.Name, oldItem);
                        }
                    }

                }

                if (!alreadyExist)
                {
                    if (buldingBlockKvp.Value is Module)
                    {
                        Module module = (Module)buldingBlockKvp.Value;

                        ModuleInstantiation moduleInstantiation = new ModuleInstantiation()
                        {
                            BeginIndexReference = module.BeginIndexReference,
                            DefinitionReference = module.DefinitionReference,
                            Name = module.Name,
                            ParameterOverrides = new Dictionary<string, Verilog.Expressions.Expression>(),
                            Project = project,
                            SourceName = module.Name,
                            SourceProjectName = module.Project.Name
                        };

                        VerilogModuleInstance? instance = VerilogModuleInstance.Create(moduleInstantiation);
                        if (instance == null) throw new Exception();
                        instance.ModuleName = module.Name;

                        // Handle instance arrays
                        if (moduleInstantiation.InstanceRange != null)
                        {
                            var arrayInstances = VerilogModuleInstance.CreateArray(moduleInstantiation);
                            if (arrayInstances != null)
                            {
                                foreach (var arrInstance in arrayInstances)
                                {
                                    arrInstance.Parent = parent;
                                    newSubItems.Add(arrInstance.Name, arrInstance);
                                }
                            }
                        }
                        //                        instance.SourceTextFile = module.File;
                        newSubItems.Add(instance.Name, instance);

                    }
                    else if (buldingBlockKvp.Value is Interface)
                    {

                    }
                }

            }
        }

        private static void addSubItemsSingleBuldingBlock(IVerilogRelatedFile item, string? moduleName, Dictionary<string, CodeEditor2.Data.Item> newSubItems, CodeEditor2.Data.Item? parent, Project project)
        {
            if (item.VerilogParsedDocument?.Root == null) throw new Exception();

            // add building block instance
            foreach (BuildingBlock newModule in item.VerilogParsedDocument.Root.BuildingBlocks.Values)
            {
                if (moduleName != null && moduleName != newModule.Name)
                {
                    // parsed document can have multiple modules, but update only matched module to this item
                    continue;
                }

                // get instanciations on this module
                List<IBuildingBlockInstantiation> newInstantiations = newModule.GetBuildingBlockInstantiations();
                //List<INamedElement> newInstantiations = newModule.NamedElements.Values.FindAll(x => x is IBuildingBlockInstantiation);

                foreach (IBuildingBlockInstantiation instantiation in newInstantiations)
                {
                    bool alreadyExist = false;

                    if (item.Items.TryGetValue(instantiation.Name, out CodeEditor2.Data.Item? gotItem))
                    {   // has same name item
                        if (gotItem == null) throw new Exception();
                        CodeEditor2.Data.Item subItem = gotItem;

                        if (instantiation is ModuleInstantiation)
                        {
                            ModuleInstantiation moduleInstantiation = (ModuleInstantiation)instantiation;
                            Project sourceProject = moduleInstantiation.GetInstancedBuildingBlockProject();

                            VerilogModuleInstance? moduleInstance = subItem as VerilogModuleInstance;

                            ProjectProperty? projectProperty = sourceProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                            if (projectProperty == null) throw new Exception();

                            Data.IVerilogRelatedFile? ivFile = projectProperty.GetFileOfBuildingBlock(moduleInstantiation.SourceName);
                            if (ivFile != null)
                            {
                                string instanceKey = Verilog.ParsedDocument.KeyGenerator(ivFile, moduleInstantiation.SourceName, moduleInstantiation.ParameterOverrides);

                                if (
                                    moduleInstantiation != null &&
                                    moduleInstance != null &&
                                    moduleInstantiation.SourceName == moduleInstance.ModuleName &&
                                    instanceKey == moduleInstance.Key
                                    )
                                {
                                    alreadyExist = true;

                                    if (!newSubItems.ContainsKey(subItem.Name))
                                    {
                                        newSubItems.Add(subItem.Name, subItem);
                                    }
                                    else
                                    {
                                        //System.Diagnostics.Debugger.Break();
                                    }
                                    continue;
                                }
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
                            ModuleInstantiation moduleInstantiation = (ModuleInstantiation)instantiation;

                            // Handle instance arrays
                            if (moduleInstantiation.InstanceRange != null)
                            {
                                var arrayInstances = VerilogModuleInstance.CreateArray(moduleInstantiation);
                                if (arrayInstances != null)
                                {
                                    foreach (var arrInstance in arrayInstances)
                                    {
                                        arrInstance.Parent = parent;
                                        if (parent is VerilogModuleInstance && ((VerilogModuleInstance)parent).ExternalProject)
                                        {
                                            arrInstance.ExternalProject = true;
                                        }
                                        newSubItems.Add(arrInstance.Name, arrInstance);
                                    }
                                }
                            }
                            else
                            {
                                VerilogModuleInstance? newVerilogModuleInstance = VerilogModuleInstance.Create(moduleInstantiation);
                                if (newSubItems.ContainsKey(instantiation.Name) || newVerilogModuleInstance == null) continue;
                                newVerilogModuleInstance.Parent = parent;
                                if (parent is VerilogModuleInstance && ((VerilogModuleInstance)parent).ExternalProject)
                                {
                                    newVerilogModuleInstance.ExternalProject = true;
                                }
                                newSubItems.Add(instantiation.Name, newVerilogModuleInstance);
                            }
                        }
                        else if(instantiation is Verilog.DataObjects.InterfaceInstance)
                        {
                            Verilog.DataObjects.InterfaceInstance moduleInstantiation = (Verilog.DataObjects.InterfaceInstance)instantiation;

                            InterfaceInstance? newVerilogModuleInstance = InterfaceInstance.Create(moduleInstantiation,project);
                            if (newSubItems.ContainsKey(instantiation.Name) || newVerilogModuleInstance == null) continue;
                            newVerilogModuleInstance.Parent = parent;
                            if (parent is InterfaceInstance && ((InterfaceInstance)parent).ExternalProject)
                            {
                                newVerilogModuleInstance.ExternalProject = true;
                            }
                            newSubItems.Add(instantiation.Name, newVerilogModuleInstance);
                        }
                        else if (instantiation is IBuildingBlockInstantiation)
                        {

                        }
                    }
                }
            }
        }


    }
}
