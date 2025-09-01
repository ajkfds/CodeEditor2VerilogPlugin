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
using static pluginVerilog.Verilog.DataObjects.DataTypes.Enum;

namespace pluginVerilog.Data.VerilogCommon
{
    public static class Updater
    {
        /// <summary>
        /// Update the Items member of this object according to the rootItem.ParsedDocument.
        /// </summary>
        /// <param name="item"></param>
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

                if(item is VerilogModuleInstance)
                {
                    string? moduleName = null;
                    VerilogModuleInstance? verilogModuleInstance = item as VerilogModuleInstance;
                    moduleName = verilogModuleInstance?.ModuleName;
                    addSubItemsSingleBuldingBlock(item, moduleName, newSubItems, parent, project);
                }
                else if( item is VerilogFile )
                {
                    if (item.VerilogParsedDocument.Root.BuildingBlocks.Count == 1)
                    {
                        addSubItemsSingleBuldingBlock(item, null, newSubItems, parent, project);
                    }
                    else
                    {
                        addSubItemsMultiBuildingBlock(item, newSubItems, parent, project);
                    }
                }

                item.Items.Clear();
                foreach (CodeEditor2.Data.Item i in newSubItems.Values)
                {
                    item.Items.Add(i.Name,i);
                }
            }
        }


        private static void addSubItemsMultiBuildingBlock(IVerilogRelatedFile item, Dictionary<string, CodeEditor2.Data.Item> newSubItems, CodeEditor2.Data.Item? parent, Project project)
        {
            if (item.VerilogParsedDocument?.Root == null) throw new Exception();

            // add building block instance
            foreach (BuildingBlock buldingBlock in item.VerilogParsedDocument.Root.BuildingBlocks.Values)
            {
                bool alreadyExist = false;

                if (item.Items.ContainsKey(buldingBlock.Name))
                {   // has same name item
                    CodeEditor2.Data.Item subItem = item.Items[buldingBlock.Name];

                    if (buldingBlock is Module)
                    {
                        VerilogModuleInstance? oldItem = subItem as VerilogModuleInstance;
                        Module module = (Module)buldingBlock;

                        if(oldItem != null && module != null && oldItem.Name == module.Name && oldItem.ModuleName == module.Name)
                        {
                            alreadyExist = true;
                            newSubItems.Add(oldItem.Name, oldItem);
                        }
                    }

                }

                if (!alreadyExist)
                {
                    if (buldingBlock is Module)
                    {
                        Module module = (Module)buldingBlock;

                        if (item.Items.ContainsKey(module.Name))
                        {

                        }
                        ModuleInstantiation moduleInstantiation = new ModuleInstantiation()
                        {
                            BeginIndexReference = module.BeginIndexReference,
                            DefinitionReference = module.DefinitionReference,
                            Name = module.Name,
                            ParameterOverrides = new Dictionary<string, Verilog.Expressions.Expression>(),
                            Project = project,
                            SourceName = module.Name
                        };

                        VerilogModuleInstance? instance = VerilogModuleInstance.Create(moduleInstantiation, project);
                        if (instance == null) throw new Exception();
                        instance.ModuleName = module.Name;
//                        instance.SourceTextFile = module.File;
                        newSubItems.Add(instance.Name, instance);

                    }
                    else if (buldingBlock is Interface)
                    {

                    }
                }

            }
        }

        private static void addSubItemsSingleBuldingBlock(IVerilogRelatedFile item,string? moduleName, Dictionary<string, CodeEditor2.Data.Item> newSubItems, CodeEditor2.Data.Item? parent,Project project)
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
                                moduleInstantiation.ParameterId == moduleInstance.InstanceId
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
        }


    }
}
