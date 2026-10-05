using Avalonia;
using CodeEditor2.Data;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Items;
using System;
using System. Collections. Generic;
using System.Globalization;
using System. Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog. Data
{
    public class SimulationSetup
    {
        protected SimulationSetup() { }

        public string TopName;
        public VerilogFile TopFile;
        public List<IVerilogRelatedFile> Files = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> IncludeFiles = new List<IVerilogRelatedFile>();
        public List<string> IncludePaths = new List<string>();

        public List<IVerilogRelatedFile> ImportFiles = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> ClassFiles = new List<IVerilogRelatedFile>();

        /// <summary>
        /// 循環参照 (相互参照) を持つクラスファイルに対して、
        /// シミュレーションコマンドに渡す前にファイル先頭に前置すべき
        /// "typedef class X;" 宣言のソーステキスト。
        /// key: 前方宣言が必要なファイル (IVerilogRelatedFile)
        /// value: ファイル先頭に前置するテキスト (改行区切り)
        ///
        /// シミュレーション backend (IcarusVerilogSimulation など) は、
        /// このマップを参照し、必要に応じてラッパー / prepend で
        /// typedef class 行を挿入すること。
        /// </summary>
        public Dictionary<IVerilogRelatedFile, string> RequiredForwardDeclarations = new Dictionary<IVerilogRelatedFile, string>();


        public List<string> ExternalLibraryPathList = new List<string>();
        public List<string> UnfoundModules = new List<string>();

        public CodeEditor2. Data. Project Project;

        public Dictionary<CodeEditor2. Data. Project, SimulationSetup> ExternalProjectReferences = new Dictionary<Project, SimulationSetup>();
        public Dictionary<string, CodeEditor2. Data. Project> ExternalProjectEntryInstance = new Dictionary<string, Project>();

        public static async Task<SimulationSetup?> CreateAsync(pluginVerilog.Data.VerilogFile verilogFile)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await Tool.ParseHierarchy.ParseAsync(verilogFile, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            return Create(verilogFile);
        }
        public static SimulationSetup? Create(pluginVerilog. Data. VerilogFile verilogFile)
        {
            SimulationSetup setup = new SimulationSetup();
            setup.TopFile = verilogFile;

            List<string> ids = new List<string>();

            if (verilogFile. VerilogParsedDocument == null) return null;
            if (verilogFile. VerilogParsedDocument. Root == null) return null;

            setup.Project = verilogFile. Project;

            // TopName selection rule: when the top file declares multiple
            // building blocks (e.g. DUT + testbench in one file), the FIRST
            // declared building block is used as the simulation top.
            // This is intentional (documented behavior); a top-module selector
            // UI is a possible future enhancement.
            BuildingBlock? buildingBlock = verilogFile. VerilogParsedDocument. Root. BuildingBlocks. Values. FirstOrDefault();
            if (buildingBlock == null) return null;

            setup.TopName = buildingBlock. Name;
            searchHier(verilogFile, setup. TopName, ids, setup, setup.TopName);

            List<IVerilogRelatedFile> targetClassFiles = setup.ClassFiles.ToList();

            // defensive iteration cap: normally the loop terminates when no new
            // class file is found; the cap guards against unexpected cycles
            int classSearchLoopCount = 0;
            while (true)
            {
                classSearchLoopCount++;
                if (classSearchLoopCount > 1000) break;

                List<IVerilogRelatedFile> newClassFiles = new List<IVerilogRelatedFile>();

                foreach (IVerilogRelatedFile classFile in targetClassFiles)
                {
                    Data.VerilogFile? file = classFile as Data.VerilogFile;
                    if (file == null) continue;
                    Verilog.ParsedDocument? parsedDocument = file.VerilogParsedDocument;
                    if (parsedDocument == null) continue;
                    ProjectProperty? projectProperty = parsedDocument.ProjectProperty;
                    if(projectProperty == null) continue;
                    foreach(string className in parsedDocument.ReferencedUnitNameSpace)
                    {
                        IVerilogRelatedFile? newfile = projectProperty.UnitNameSpace.GetFile(className);
                        // exclude files already collected (in this batch or in
                        // setup.ClassFiles). Without the setup.ClassFiles check,
                        // circular class references (A -> B -> A) alternate the
                        // batches forever and this while(true) loop never ends
                        // (UI freeze / deadlock when invoked from the menu thread).
                        if(newfile != null
                            && !newClassFiles.Contains(newfile)
                            && !setup.ClassFiles.Contains(newfile))
                        {
                            newClassFiles.Add(newfile);
                        }
                    }
                }
                if (newClassFiles.Count == 0) break;
                foreach(IVerilogRelatedFile file in newClassFiles)
                {
                    if(!setup.ClassFiles.Contains(file)) setup.ClassFiles.Add(file);
                }

                targetClassFiles = newClassFiles;
            }

            // Reorder setup.Files based on class dependency graph
            // (extends / implements / references) so that referenced classes
            // are compiled before the referencing files.
            ReorderFilesByClassDependencies(setup);


            if (setup. UnfoundModules. Count != 0)
            {
                foreach (var module in setup. UnfoundModules)
                {
                    CodeEditor2. Controller. AppendLog(verilogFile. Project. Name + ":" + module + " unfound", Avalonia. Media. Colors. Red);
                }
                return null;
            }
            return setup;
        }

        /// <summary>
        /// setup.Files のうち VerilogFile 同士の依存関係 (extends / implements / 参照) に基づいて
        /// コンパイル順を整列する。循環参照は typedef class 前方宣言で吸収する。
        /// </summary>
        private static void ReorderFilesByClassDependencies(SimulationSetup setup)
        {
            try
            {
                ClassFileOrderResolver.Result result = ClassFileOrderResolver.ResolveOrder(setup.Files, setup);
                if (result.OrderedFiles != null && result.OrderedFiles.Count > 0)
                {
                    setup.Files.Clear();
                    setup.Files.AddRange(result.OrderedFiles);
                }
                if (result.CyclicGroups != null)
                {
                    foreach (var group in result.CyclicGroups)
                    {
                        var names = new List<string>();
                        foreach (var f in group)
                        {
                            try { names.Add(f.RelativePath); } catch { names.Add(f.ID); }
                        }
                        CodeEditor2.Controller.AppendLog(
                            "class circular reference detected (typedef class will be inserted): " + string.Join(", ", names),
                            Avalonia.Media.Colors.Orange);
                    }
                }
                if (result.ForwardDeclarations != null && result.ForwardDeclarations.Count > 0)
                {
                    setup.RequiredForwardDeclarations = result.ForwardDeclarations;
                }
            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog("class file order resolver failed: " + ex.Message, Avalonia.Media.Colors.Orange);
            }
        }

        private static void searchHier(IVerilogRelatedFile file, string buildingBlockName, List<string> ids, SimulationSetup setup, string path)
        {
            if (ids. Contains(file. ID)) return;
            ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument == null) return;

            appendFile(file, setup, ids, path, buildingBlockName);
            
            foreach (string unfound in parsedDocument.UnfoundModules)
            {
                CodeEditor2. Controller. AppendLog("unfound instance on " + file. RelativePath);
                if (!setup.UnfoundModules.Contains(unfound)) setup.UnfoundModules.Add(unfound);

            }

            foreach (string external in parsedDocument. ExternalRefrenceModules)
            {
                if (file.ProjectProperty.ExtenralModuleLibraryPath.ContainsKey(external))
                {
                    string libPath = file. ProjectProperty. ExtenralModuleLibraryPath[external];
                    if (!setup. ExternalLibraryPathList. Contains(libPath)) setup. ExternalLibraryPathList. Add(libPath);
                }
                if (file.ProjectProperty.ExtenralPrimitiveLibraryPath.ContainsKey(external))
                {
                    string libPath = file.ProjectProperty.ExtenralPrimitiveLibraryPath[external];
                    if (!setup.ExternalLibraryPathList.Contains(libPath)) setup.ExternalLibraryPathList.Add(libPath);
                }
            }

            foreach (string className in parsedDocument.ReferencedUnitNameSpace)
            {
                Class? class_ = parsedDocument.ProjectProperty?.UnitNameSpace.GetFile(className) as Class;
                if (parsedDocument.Project == null) continue;
                if (class_ != null)
                {
                    appendClass(className, parsedDocument.Project, setup, ids);
                }
                else
                {
                    // referenced class definition is missing: report as file shortage
                    // so that simulation startup is refused later in Create()
                    if (!setup.UnfoundModules.Contains(className)) setup.UnfoundModules.Add(className);
                }
            }

            foreach (var ifile in parsedDocument. IncludeFiles. Values)
            {
                appendVerilogHeaderInstance(ifile, setup);
            }

            foreach (string cFile in parsedDocument.ReferencedUnitNameSpace)
            {
                appendClass(cFile, file.Project, setup, ids);
            }

            // bind directives / program / udp / interface references are registered
            // to ReferencedDefinitionNameSpace during parse. Collect their definition
            // files here; a missing definition is reported as file shortage.
            foreach (string definitionName in parsedDocument.ReferencedDefinitionNameSpace)
            {
                ProjectProperty? projectProperty = file.ProjectProperty;
                if (projectProperty == null) continue;
                // external library modules are provided by external file paths, not project files
                if (parsedDocument.ExternalRefrenceModules.Contains(definitionName)) continue;
                if (projectProperty.ExtenralModuleLibraryPath.ContainsKey(definitionName)) continue;
                if (projectProperty.ExtenralPrimitiveLibraryPath.ContainsKey(definitionName)) continue;
                IVerilogRelatedFile? defFile = projectProperty.DefinitionNameSpace.GetFile(definitionName);
                if (defFile != null)
                {
                    // recurse into the referenced definition file so that its own
                    // module / class / package dependencies are collected as well
                    searchHier(defFile, definitionName, ids, setup, path);
                }
                else if (!setup.UnfoundModules.Contains(definitionName))
                {
                    // referenced module/interface/program/udp definition file is missing
                    setup.UnfoundModules.Add(definitionName);
                }
            }

            // Process imported packages
            foreach (string packageName in parsedDocument. ImportedPackages)
            {
                appendImportedPackage(packageName, file. Project, setup, ids);
            }

            if(!parsedDocument. Root. BuildingBlocks. TryGetValue(buildingBlockName,out BuildingBlock? buildingBlock))
            {
                return;
            }

            searchNameSpace(file, ids, buildingBlock, setup, path);
        }

        private static void searchNameSpace(IVerilogRelatedFile file, List<string> ids, NameSpace nameSpace, SimulationSetup setup, string path)
        {
            foreach (INamedElement element in nameSpace.NamedElements.Values)
            {
                if (element is NameSpace ns && ns.IsVirtualScope)
                {
                    // @scope comment references are NOT real instances and must not be collected for simulation.
                    // The target file is already collected via its own ModuleInstantiation chain.
                    continue;
                }
                if (element is NameSpace)
                {
                    NameSpace subNameSpace = (NameSpace) element;
                    string newPath = path + "." + subNameSpace. Name;
                    searchNameSpace(file, ids, subNameSpace, setup, newPath);
                }
                else if (element is ModuleInstantiation)
                {
                    ModuleInstantiation moduleInstantiation = (ModuleInstantiation) element;
                    if (nameSpace. BuildingBlock. Project. Name != moduleInstantiation. SourceProjectName)
                    {
                        string newPath = path + "." + moduleInstantiation. Name;
                        if (!setup. ExternalProjectEntryInstance. ContainsKey(newPath))
                        {
                            setup. ExternalProjectEntryInstance. Add(
                                newPath,
                                CodeEditor2.Global.Projects[moduleInstantiation.SourceProjectName]
                                );
                        }
                    }
                    if (file. Items. TryGetValue(moduleInstantiation. Name, out CodeEditor2. Data. Item? item))
                    {
                        string newPath = moduleInstantiation. Name;
                        if (path != "") newPath = path + "." + newPath;

                        var subfile = item as IVerilogRelatedFile;
                        if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
                    }
                    else if (moduleInstantiation. InstanceRange != null)
                    {
                        // instance array: VerilogModuleInstance.CreateArray registers
                        // each element to file.Items as "name[i]" while the
                        // ModuleInstantiation itself is registered with the plain
                        // name. Look up each array element so that (especially for
                        // external project instances) the definition file and its
                        // own dependencies are collected into the sub-setup.
                        int count = moduleInstantiation. InstanceCount;
                        for (int i = 0; i < count; i++)
                        {
                            string elementName = moduleInstantiation. Name + "[" + i. ToString() + "]";
                            if (!file. Items. TryGetValue(elementName, out CodeEditor2. Data. Item? arrayItem)) continue;

                            string newPath = elementName;
                            if (path != "") newPath = path + "." + newPath;

                            var subfile = arrayItem as IVerilogRelatedFile;
                            if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
                        }
                    }
                }
                else if ((element is Verilog.Items.InterfaceInstance))
                {
                    Verilog.Items.InterfaceInstance moduleInstantiation = (Verilog.Items.InterfaceInstance)element;
                    if (file.Items.TryGetValue(moduleInstantiation.Name, out CodeEditor2.Data.Item? item))
                    {
                        string newPath = moduleInstantiation.Name;
                        if (path != "") newPath = path + "." + newPath;

                        var subfile = item as IVerilogRelatedFile;
                        if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
                    }
                }
                else if (element is Verilog.Items.ProgramInstantiation)
                {
                    // program instantiation: collect the program definition file
                    // (definition lookup failure is reported by searchHier via
                    //  ReferencedDefinitionNameSpace, so nothing to do here)
                }
                else if (element is Verilog.Items.UdpInstantiation)
                {
                    // udp instantiation: collect the udp (primitive) definition file
                    // (definition lookup failure is reported by searchHier via
                    //  ReferencedDefinitionNameSpace, so nothing to do here)
                }
                else if (element is DataObject)
                {
                    // Handle DataObject - check if it's a Class or InterfaceClass instance
                    DataObject dataObject = (DataObject)element;
                    // Variables.Object holds BuildingBlocks.Class itself as DataType (not ClassType).
                    // UserDefinedVariable holds UserDefinedType (typedef of class / interface class).
                    if (dataObject.DataType is ClassType || dataObject.DataType is Class || dataObject.DataType is UserDefinedType)
                    {
                        appendClassInstance(file, dataObject, setup);
                    }
                    else if (dataObject.DataType is InterfaceClass)
                    {
                        appendInterfaceClassInstance(file, dataObject, setup);
                    }
                    else if (dataObject is Verilog.DataObjects.Variables.VirtualInterface)
                    {
                        // virtual interface: collect the interface definition file
                        appendVirtualInterfaceInstance(file, (Verilog.DataObjects.Variables.VirtualInterface)dataObject, setup);
                    }
                }
            }
        }


        /// <summary>
        /// append the given file to the appropriate list of the setup (or the
        /// external project sub-setup), then recurse into external project files
        /// so that their own dependencies are collected into the sub-setup.
        /// </summary>
        private static void appendFile(IVerilogRelatedFile file, SimulationSetup setup, List<string> ids, string path, string buildingBlockName)
        {
            if (file is pluginVerilog. Data. VerilogFile || file is SystemVerilogFile)
            {
                if (setup. Files. Contains(file)) return;
                setup. Files. Add(file);
                return;
            }
            if (file is VerilogModuleInstance)
            {
                VerilogModuleInstance? instance = file as VerilogModuleInstance;
                if (instance == null) return;
                IVerilogRelatedFile? sourceFile = instance. SourceTextFile as IVerilogRelatedFile;
                if (sourceFile == null) return;

                if (sourceFile. Project == setup. Project)
                {
                    if (setup. Files. Contains(sourceFile)) return;
                    setup. Files. Add(sourceFile);
                }
                else
                {
                    CodeEditor2. Data. Project project = sourceFile. Project;
                    SimulationSetup pSetup;
                    if (!setup. ExternalProjectReferences. ContainsKey(project))
                    {
                        pSetup = new SimulationSetup() { Project = project };
                        setup. ExternalProjectReferences. Add(project, pSetup);
                        pSetup. TopFile = instance. SourceVerilogFile;
                        pSetup. TopName = instance. ModuleName;
                    }
                    else
                    {
                        pSetup = setup. ExternalProjectReferences[project];
                    }
                    if (pSetup. Files. Contains(sourceFile)) return;
                    pSetup. Files. Add(sourceFile);

                    // recurse into the external project file so that its own
                    // module / class / package dependencies are collected into the
                    // external project sub-setup as well
                    string newPath = instance.Name;
                    if (path != "") newPath = path + "." + newPath;
                    searchHier(sourceFile, instance.ModuleName, ids, pSetup, newPath);
                }
                return;
            }else if(file is InterfaceInstance)
            {
                InterfaceInstance? instance = file as InterfaceInstance;
                if (instance == null) return;
                IVerilogRelatedFile? sourceFile = instance.SourceTextFile as IVerilogRelatedFile;
                if (sourceFile == null) return;

                if (sourceFile.Project == setup.Project)
                {
                    if (setup.Files.Contains(sourceFile)) return;
                    setup.Files.Add(sourceFile);
                }
                else
                {
                    CodeEditor2.Data.Project project = sourceFile.Project;
                    SimulationSetup pSetup;
                    if (!setup.ExternalProjectReferences.ContainsKey(project))
                    {
                        pSetup = new SimulationSetup() { Project = project };
                        setup.ExternalProjectReferences.Add(project, pSetup);
                        pSetup.TopFile = instance.SourceVerilogFile;
                        pSetup.TopName = instance.ModuleName;
                    }
                    else
                    {
                        pSetup = setup.ExternalProjectReferences[project];
                    }
                    if (pSetup.Files.Contains(sourceFile)) return;
                    pSetup.Files.Add(sourceFile);

                    // recurse into the external project file so that its own
                    // module / class / package dependencies are collected into the
                    // external project sub-setup as well
                    string newPath = instance.Name;
                    if (path != "") newPath = path + "." + newPath;
                    searchHier(sourceFile, instance.ModuleName, ids, pSetup, newPath);
                }
                return;
            }
        }


        private static void appendClassInstance(IVerilogRelatedFile file, DataObject dataObject, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Handle Object (class instance) - has Class property
            if (dataObject is Verilog.DataObjects.Variables.Object objectInstance)
            {
                Class? class_ = objectInstance.GetSourceClass();
                if (class_ == null) return;

                IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(class_.Name);
                if (sourceFile == null) return;

                if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
                {
                    if (setup.Files.Contains(sourceFile)) return;
                    setup.Files.Add(sourceFile);
                    return;
                }
            }

            // Handle UserDefinedVariable with UserDefinedType
            if (dataObject is UserDefinedVariable userDefinedVariable)
            {
                if (userDefinedVariable.DataType is UserDefinedType userDefinedType)
                {
                    // typedef is registered to UnitNameSpace (compilation-unit scope), not DefinitionNameSpace
                    IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(userDefinedType.Typedef.Name);
                    if (sourceFile == null) return;

                    if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
                    {
                        if (setup.Files.Contains(sourceFile)) return;
                        setup.Files.Add(sourceFile);
                        return;
                    }
                }
            }
        }

        private static void appendInterfaceClassInstance(IVerilogRelatedFile file, DataObject dataObject, SimulationSetup setup)
        {
            // Handle InterfaceClass similarly - use UserDefinedType approach
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Handle UserDefinedVariable with UserDefinedType that references InterfaceClass
            if (dataObject is UserDefinedVariable userDefinedVariable)
            {
                if (userDefinedVariable.DataType is UserDefinedType userDefinedType)
                {
                    IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(userDefinedType.Typedef.Name);
                    if (sourceFile == null) return;

                    if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
                    {
                        if (setup.Files.Contains(sourceFile)) return;
                        setup.Files.Add(sourceFile);
                        return;
                    }
                }
            }
        }

        private static void appendVirtualInterfaceInstance(IVerilogRelatedFile file, Verilog.DataObjects.Variables.VirtualInterface virtualInterface, SimulationSetup setup)
        {
            // collect the interface definition file referenced by a virtual interface variable.
            // missing interface definition is reported as file shortage so that simulation
            // startup is refused later in Create()
            Verilog.BuildingBlocks.Interface? sourceInterface = virtualInterface.GetSourceInterface();
            if (sourceInterface == null)
            {
                Verilog.DataObjects.DataTypes.VirtualInterfaceType? dataType = virtualInterface.DataType as Verilog.DataObjects.DataTypes.VirtualInterfaceType;
                string? interfaceName = dataType?.InterfaceIdentifier;
                if (!string.IsNullOrEmpty(interfaceName) && !setup.UnfoundModules.Contains(interfaceName))
                {
                    setup.UnfoundModules.Add(interfaceName);
                }
                return;
            }

            ProjectProperty? declaringProjectProperty = file.ProjectProperty;
            if (declaringProjectProperty == null) return;

            IVerilogRelatedFile? interfaceFile = declaringProjectProperty.DefinitionNameSpace.GetFile(sourceInterface.Name);
            if (interfaceFile == null)
            {
                if (!setup.UnfoundModules.Contains(sourceInterface.Name)) setup.UnfoundModules.Add(sourceInterface.Name);
                return;
            }

            if (setup.Files.Contains(interfaceFile)) return;
            setup.Files.Add(interfaceFile);
        }

        private static void appendVerilogHeaderInstance(VerilogHeaderInstance file, SimulationSetup setup)
        {
            if (file. Project == setup. Project)
            {
                if (setup. IncludeFiles. Contains(file)) return;
                setup. IncludeFiles. Add(file);
                string? path = System. IO. Path. GetDirectoryName(file. Project. GetAbsolutePath(file. RelativePath));
                if (path == null) return;
                if (!setup. IncludePaths. Contains(path)) setup. IncludePaths. Add(path);
                return;
            }
            else
            {
                CodeEditor2. Data. Project project = file. Project;
                SimulationSetup pSetup;
                if (!setup. ExternalProjectReferences. ContainsKey(project))
                {
                    pSetup = new SimulationSetup() { Project = project };
                    setup. ExternalProjectReferences. Add(project, pSetup);
                }
                else
                {
                    pSetup = setup. ExternalProjectReferences[project];
                }
                if (pSetup. IncludeFiles. Contains(file)) return;
                pSetup. IncludeFiles. Add(file);
                string? path = System. IO. Path. GetDirectoryName(file. Project. GetAbsolutePath(file. RelativePath));
                if (path == null) return;
                if (!pSetup. IncludePaths. Contains(path)) pSetup. IncludePaths. Add(path);
                return;
            }

        }

        private static void appendImportedPackage(string packageName, CodeEditor2. Data. Project project, SimulationSetup setup, List<string> ids)
        {
            // Search for the package file in the project
            IVerilogRelatedFile? packageFile = findPackageFile(packageName, project, setup);
            if (packageFile == null) return;

            // Add to ImportFiles
            if (packageFile.Project == setup.Project)
            {
                if (setup.ImportFiles.Contains(packageFile)) return;
                setup.ImportFiles.Add(packageFile);
            }
            else
            {
                // Handle external project reference
                CodeEditor2.Data.Project extProject = packageFile.Project;
                SimulationSetup pSetup;
                if (!setup.ExternalProjectReferences.ContainsKey(extProject))
                {
                    pSetup = new SimulationSetup() { Project = extProject };
                    setup.ExternalProjectReferences.Add(extProject, pSetup);
                }
                else
                {
                    pSetup = setup.ExternalProjectReferences[extProject];
                }
                if (pSetup.ImportFiles.Contains(packageFile)) return;
                pSetup.ImportFiles.Add(packageFile);
            }

            // package file may itself reference other packages / classes: trace them
            // (also reports missing references in the package as file shortage)
            searchHier(packageFile, packageName, ids, setup, "");
        }
        private static void appendClass(string  className, CodeEditor2.Data.Project project, SimulationSetup setup, List<string> ids)
        {
            IVerilogRelatedFile? classFile = findClassFile(className, project, setup);
            if (classFile == null)
            {
                // referenced class definition file is missing: report as file shortage
                if (!setup.UnfoundModules.Contains(className)) setup.UnfoundModules.Add(className);
                return;
            }

            // Add to ImportFiles
            if (classFile.Project == setup.Project)
            {
                if (setup.ClassFiles.Contains(classFile)) return;
                setup.ClassFiles.Add(classFile);
            }
            else
            {
                // Handle external project reference
                CodeEditor2.Data.Project extProject = classFile.Project;
                SimulationSetup pSetup;
                if (!setup.ExternalProjectReferences.ContainsKey(extProject))
                {
                    pSetup = new SimulationSetup() { Project = extProject };
                    setup.ExternalProjectReferences.Add(extProject, pSetup);
                }
                else
                {
                    pSetup = setup.ExternalProjectReferences[extProject];
                }
                if (pSetup.ClassFiles.Contains(classFile)) return;
                pSetup.ClassFiles.Add(classFile);
            }

            // class file may itself reference other classes / packages: trace them
            // (also reports missing references in the class file as file shortage)
            searchHier(classFile, className, ids, setup, "");
        }

        private static IVerilogRelatedFile? findPackageFile(string packageName, CodeEditor2. Data. Project project, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return null;

//            Package? package = projectProperty.PackageNameSpace.Get(packageName) as Package;
            IVerilogRelatedFile? verilogRelatedFile = projectProperty.PackageNameSpace.GetFile(packageName);
            return verilogRelatedFile;
        }
        private static IVerilogRelatedFile? findClassFile(string className, CodeEditor2.Data.Project project, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return null;

//            Class? class_ = projectProperty.UnitNameSpace.Get(className) as Class;
            IVerilogRelatedFile? verilogRelatedFile = projectProperty.UnitNameSpace.GetFile(className);
            return verilogRelatedFile;
        }

        private static IVerilogRelatedFile? findPackageFileInProject(string packageName, CodeEditor2. Data. Project project)
        {
            foreach (var file in project.Items)
            {
                IVerilogRelatedFile? vFile = file as IVerilogRelatedFile;
                if (vFile == null) continue;

                ParsedDocument? parsedDoc = vFile. VerilogParsedDocument;
                if (parsedDoc == null) continue;
                if (parsedDoc. Root == null) continue;

                foreach (var bb in parsedDoc. Root. BuildingBlocks. Values)
                {
                    if (bb is Package)
                    {
                        Package pkg = (Package) bb;
                        if (pkg. Name == packageName)
                        {
                            return vFile;
                        }
                    }
                }
            }
            return null;
        }

    }
}
