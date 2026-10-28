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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class SimulationSetup
    {
        protected SimulationSetup() { }

        public string TopName;
        public IVerilogRelatedFile TopFile;
        public List<IVerilogRelatedFile> Files = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> IncludeFiles = new List<IVerilogRelatedFile>();
        public List<string> IncludePaths = new List<string>();

        public List<IVerilogRelatedFile> ImportFiles = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> ClassFiles = new List<IVerilogRelatedFile>();

        public bool Failed = false;

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

        public CodeEditor2.Data.Project Project;

//        public Dictionary<CodeEditor2.Data.Project, SimulationSetup> ExternalProjectReferences = new Dictionary<Project, SimulationSetup>();
//        public Dictionary<string, CodeEditor2.Data.Project> ExternalProjectEntryInstance = new Dictionary<string, Project>();

        public List<ExternalProjectReference> ExternalProjectReferences = new List<ExternalProjectReference>();


        public class ExternalProjectReference
        {
            public CodeEditor2.Data.Project Project;
            public SimulationSetup Setup;
            public string InstancePath;

        }



        /// <summary>
        /// ファイルが属するプロジェクトの絶対パスを返す。
        /// Files はプロジェクトを跨ぐため、トップの Project.GetAbsolutePath では
        /// 外部プロジェクトのファイルパスを解決できない。
        /// </summary>
        //public static string? GetAbsolutePath(IVerilogRelatedFile file)
        //{
        //    CodeEditor2.Data.Project? owner = null;
        //    try { owner = file.Project; } catch { owner = null; }
        //    if (owner == null) return null;
        //    try { return owner.GetAbsolutePath(file.RelativePath); } catch { return null; }
        //}

        public static async Task<SimulationSetup?> CreateAsync(pluginVerilog.Data.VerilogFile verilogFile)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await Tool.ParseHierarchy.ParseAsync(verilogFile, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            SimulationSetup? setup = Create(verilogFile);
            return setup;
        }
        public static SimulationSetup? Create(pluginVerilog.Data.VerilogFile verilogFile)
        {
            SimulationSetup setup = new SimulationSetup();
            setup.TopFile = verilogFile;

            List<string> ids = new List<string>();

            if (verilogFile.VerilogParsedDocument == null) return null;
            if (verilogFile.VerilogParsedDocument.Root == null) return null;

            setup.Project = verilogFile.Project;

            // get target top block
            BuildingBlock? buildingBlock = verilogFile.VerilogParsedDocument.Root.BuildingBlocks.Values.FirstOrDefault(
                    block => !(block is Class) && !(block is InterfaceClass) && !(block is Package));
            if (buildingBlock == null) return null;

            setup.TopName = buildingBlock.Name;
            searchHier(verilogFile, setup.TopName, ids, setup, setup.TopName);

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
                    if (projectProperty == null) continue;
                    foreach (string className in parsedDocument.ReferencedUnitNameSpace)
                    {
                        IVerilogRelatedFile? newfile = projectProperty.UnitNameSpace.GetFile(className);
                        // exclude files already collected (in this batch or in
                        // setup.ClassFiles). Without the setup.ClassFiles check,
                        // circular class references (A -> B -> A) alternate the
                        // batches forever and this while(true) loop never ends
                        // (UI freeze / deadlock when invoked from the menu thread).
                        if (newfile != null
                            && !newClassFiles.Contains(newfile)
                            && !setup.ClassFiles.Contains(newfile))
                        {
                            newClassFiles.Add(newfile);
                        }
                    }
                }
                if (newClassFiles.Count == 0) break;
                foreach (IVerilogRelatedFile file in newClassFiles)
                {
                    if (!setup.ClassFiles.Contains(file))
                    {
                        setup.ClassFiles.Add(file);
                    }
                }

                targetClassFiles = newClassFiles;
            }

            // Reorder setup.Files based on class dependency graph
            // (extends / implements / references) so that referenced classes
            // are compiled before the referencing files.
            ReorderFilesByClassDependencies(setup);


            if (setup.Failed) return null;
            return setup;
        }


        /// <summary>
        /// setup.Files のうち VerilogFile 同士の依存関係 (extends / implements / 参照) に基づいて
        /// コンパイル順を整列する。循環参照は typedef class 前方宣言で吸収する。
        /// </summary>
        private static void ReorderFilesByClassDependencies(SimulationSetup setup)
        {
            ReorderFilesByClassDependencies(setup, new List<SimulationSetup>(), "");
        }

        private static void ReorderFilesByClassDependencies(SimulationSetup setup, List<SimulationSetup> visited, string projectPath)
        {
            if (visited.Contains(setup)) return;
            visited.Add(setup);

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
            string visitedKey = file.Project.Name + ":" + file.ID + ":" + buildingBlockName;
            if (ids.Contains(visitedKey)) return;
            ids.Add(visitedKey);

            ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument == null) return;
            if (parsedDocument.Project == null) return;

//            appendFile(file, ref setup, ids, path, buildingBlockName);

            foreach (string unfound in parsedDocument.UnfoundModules)
            {
                CodeEditor2.Controller.AppendLog("unfound " + unfound, Avalonia.Media.Colors.Red);
                setup.Failed = false;
                return;
            }

            foreach (string external in parsedDocument.ExternalRefrenceModules)
            {
                if (file.ProjectProperty.ExtenralModuleLibraryPath.ContainsKey(external))
                {
                    string libPath = file.ProjectProperty.ExtenralModuleLibraryPath[external];
                    if (!setup.ExternalLibraryPathList.Contains(libPath)) setup.ExternalLibraryPathList.Add(libPath);
                }
                if (file.ProjectProperty.ExtenralPrimitiveLibraryPath.ContainsKey(external))
                {
                    string libPath = file.ProjectProperty.ExtenralPrimitiveLibraryPath[external];
                    if (!setup.ExternalLibraryPathList.Contains(libPath)) setup.ExternalLibraryPathList.Add(libPath);
                }
            }

            foreach (string className in parsedDocument.ReferencedUnitNameSpace)
            {
                // UnitNameSpace.GetFile() returns the FILE that declares the class
                // (not the Class building block itself), so resolve the Class from
                // the file's parsed document (same rule as RegisterNameSpace.Get).
                // Casting the file to Class always yielded null and every class
                // reference (e.g. "Packet") was wrongly reported as unfound.
                IVerilogRelatedFile? classFile = parsedDocument.ProjectProperty?.UnitNameSpace.GetFile(className);
                Class? class_ = null;
                if (classFile != null
                    && classFile.VerilogParsedDocument != null
                    && classFile.VerilogParsedDocument.Root != null
                    && classFile.VerilogParsedDocument.Root.NamedElements.TryGetValue(className, out INamedElement? element))
                {
                    class_ = element as Class;
                }
                if (class_ != null)
                {
                    appendClass(file, className, parsedDocument.Project, setup, ids);
                }
                else
                {
                    CodeEditor2.Controller.AppendLog("unfound " + className, Avalonia.Media.Colors.Red);
                    setup.Failed= true;
                    return;
                }
            }

            foreach (var ifile in parsedDocument.IncludeFiles.Values)
            {
                appendVerilogHeaderInstance(ifile, setup);
            }

            foreach (string cFile in parsedDocument.ReferencedUnitNameSpace)
            {
                appendClass(file, cFile, file.Project, setup, ids);
            }

            // module / interface / program / udp references are registered to
            // ReferencedDefinitionNameSpaceProjects during parse. Collect their definition
            // files here; a missing definition is reported as file shortage.

            foreach ((string,string) definitionNameAndProject in parsedDocument.ReferencedDefinitionNameSpaceAndProjects )
            {
                string definitionName = definitionNameAndProject.Item1;
                string projectName = definitionNameAndProject.Item2;

                if (!CodeEditor2.Global.Projects.ContainsKey(projectName))
                {
                    setup.Failed = true;
                    continue;
                }
                if (projectName != setup.Project.Name) continue;

                CodeEditor2.Data.Project? localProject = CodeEditor2.Global.Projects[projectName];
                ProjectProperty? localProperty = localProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (localProperty == null) continue;

                if (localProperty.ExtenralModuleLibraryPath.ContainsKey(definitionName)) continue;
                if (localProperty.ExtenralPrimitiveLibraryPath.ContainsKey(definitionName)) continue;
                IVerilogRelatedFile? defFile = localProperty.DefinitionNameSpace.GetFile(definitionName);
                if (defFile != null && !setup.Files.Contains(defFile))
                {
                    setup.Files.Add(defFile);
                }
            }

            // Process imported packages
            foreach (string packageName in parsedDocument.ImportedPackages)
            {
                appendImportedPackage(packageName, file.Project, setup, ids);
            }

            if (!parsedDocument.Root.BuildingBlocks.TryGetValue(buildingBlockName, out BuildingBlock? buildingBlock))
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
                    NameSpace subNameSpace = (NameSpace)element;
                    string newPath = path + "." + subNameSpace.Name;
                    searchNameSpace(file, ids, subNameSpace, setup, newPath);
                }
                else if (element is ModuleInstantiation)
                {
                    ModuleInstantiation moduleInstantiation = (ModuleInstantiation)element;
                    if (nameSpace.BuildingBlock.Project.Name != moduleInstantiation.SourceProjectName)
                    {
                        string sourceProjectName = moduleInstantiation.SourceProjectName;
                        string newPath = path + "." + moduleInstantiation.Name;

                        if (!CodeEditor2.Global.Projects.ContainsKey(sourceProjectName))
                        {
                            CodeEditor2.Controller.AppendLog("external project not found: " + sourceProjectName, Avalonia.Media.Colors.Red);
                            setup.Failed = true;
                            return;
                        }
                        CodeEditor2.Data.Project newProject = CodeEditor2.Global.Projects[sourceProjectName];
                        ProjectProperty? newProperty = newProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                        VerilogFile? newFile = newProperty?.DefinitionNameSpace.GetFile(moduleInstantiation.SourceName) as VerilogFile;
                        if(newFile != null)
                        {
                            SimulationSetup? newSimulationSetup = SimulationSetup.Create(newFile as VerilogFile);
                            if(newSimulationSetup != null)
                            {
                                setup.ExternalProjectReferences.Add(new ExternalProjectReference()
                                {
                                    Project = newProject,
                                    Setup = newSimulationSetup,
                                    InstancePath = newPath
                                });
                            }
                        }
                    }
                    else
                    {
                        if (file.Items.TryGetValue(moduleInstantiation.Name, out CodeEditor2.Data.Item? item))
                        {
                            string newPath = moduleInstantiation.Name;
                            if (path != "") newPath = path + "." + newPath;

                            var subfile = item as IVerilogRelatedFile;
                            VerilogModuleInstance? instance = subfile as VerilogModuleInstance;
                            VerilogFile? sourceFile = instance?.SourceVerilogFile;
                            if (sourceFile != null)
                            {
                                if (!setup.Files.Contains(sourceFile)) setup.Files.Add(sourceFile);
                                searchHier(sourceFile, moduleInstantiation.SourceName, ids, setup, newPath);
                            }
                        }
                        else if (moduleInstantiation.InstanceRange != null)
                        {
                            // instance array: VerilogModuleInstance.CreateArray registers
                            // each element to file.Items as "name[i]" while the
                            // ModuleInstantiation itself is registered with the plain
                            // name. Look up each array element so that (especially for
                            // external project instances) the definition file and its
                            // own dependencies are collected into the sub-setup.
                            int count = moduleInstantiation.InstanceCount;
                            for (int i = 0; i < count; i++)
                            {
                                string elementName = moduleInstantiation.Name + "[" + i.ToString() + "]";
                                if (!file.Items.TryGetValue(elementName, out CodeEditor2.Data.Item? arrayItem)) continue;

                                string newPath = elementName;
                                if (path != "") newPath = path + "." + newPath;

                                var subfile = arrayItem as IVerilogRelatedFile;
                                if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
                            }
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
                    /*
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
                    */
                }
            }
        }


        /// <summary>
        /// append the given file to the appropriate list of the setup (or the
        /// external project sub-setup), then recurse into external project files
        /// so that their own dependencies are collected into the sub-setup.
        /// </summary>
        /// 
        /*
        private static void appendFile(IVerilogRelatedFile file, ref SimulationSetup setup, List<string> ids, string path, string buildingBlockName)
        {
            if (file is pluginVerilog.Data.VerilogFile || file is SystemVerilogFile)
            {
                if (setup.Files.Contains(file)) return;
                setup.Files.Add(file);
                return;
            }
            if (file is VerilogModuleInstance)
            {
                VerilogModuleInstance? instance = file as VerilogModuleInstance;
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
                        pSetup.TopFile = instance.SourceVerilogFile as VerilogFile;
                        setup.ExternalProjectReferences.Add(project, pSetup);
                        pSetup.TopFile = instance.SourceVerilogFile;
                        pSetup.TopName = instance.ModuleName;
                    }
                    else
                    {
                        pSetup = setup.ExternalProjectReferences[project];
                    }
                    if (!pSetup.Files.Contains(sourceFile)) pSetup.Files.Add(sourceFile);

                    // recurse into the external project instance so that its own
                    // module / class / package dependencies are collected into the
                    // external project sub-setup as well.
                    // Note: do NOT return when the source file was already added.
                    // The same external file may be instantiated several times
                    // with different parameter overrides, and each instance has
                    // its own parsed document (hence its own dependencies).
                    appendExternalProjectDependencies(instance, sourceFile, pSetup, ids, path);

                    setup = pSetup;
                }
                return;
            }
            else if (file is InterfaceInstance)
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
                    if (!pSetup.Files.Contains(sourceFile)) pSetup.Files.Add(sourceFile);

                    // recurse into the external project instance (same reasoning
                    // as the module instance above: several parameterized
                    // instances of one file have different dependencies).
                    appendExternalProjectDependencies(instance, sourceFile, pSetup, ids, path);
                }
                return;
            }
        }

        /// <summary>
        /// Walk the dependencies of an instance that belongs to another project
        /// and collect them into that project's sub-setup.
        ///
        /// The INSTANCE (not its source file) is traversed: an instance holds
        /// the parsed document produced with the parameter overrides of this
        /// particular instantiation (VerilogModuleInstance.ParsedDocument looks
        /// the instanced parsed document up by key, and its Items were built
        /// from that same document). Walking the source file instead used the
        /// base parse (default parameter values), so a parameterized external
        /// instance such as "@project X" + "#(.MODE(1))" collected the modules
        /// selected by the *default* value of MODE and missed the ones actually
        /// instantiated (e.g. inside "generate if (MODE == 1)").
        ///
        /// The source file is used only as a fallback when the instanced parsed
        /// document is not available yet (the instance has not been parsed),
        /// because then only the base parse exists.
        /// </summary>
        private static void appendExternalProjectDependencies(
            CodeEditor2.Data.Item instance,
            IVerilogRelatedFile sourceFile,
            SimulationSetup subSetup,
            List<string> ids,
            string path)
        {
            string buildingBlockName = getInstantiatedBuildingBlockName(instance, sourceFile);

            IVerilogRelatedFile? instanceFile = instance as IVerilogRelatedFile;
            if (instanceFile != null && instanceFile.VerilogParsedDocument != null)
            {
                string instancePath = instance.Name;
                if (path != "") instancePath = path + "." + instancePath;
                searchHier(instanceFile, buildingBlockName, ids, subSetup, instancePath);
                return;
            }

            // the instance has not been parsed yet: fall back to the base parse
            // of the source file so that at least its own dependencies are found
            searchHier(sourceFile, buildingBlockName, ids, subSetup, path);
        }

        /// <summary>
        /// Name of the building block (module / interface) an instance refers to.
        /// </summary>
        private static string getInstantiatedBuildingBlockName(CodeEditor2.Data.Item instance, IVerilogRelatedFile sourceFile)
        {
            if (instance is VerilogModuleInstance moduleInstance) return moduleInstance.ModuleName;
            if (instance is InterfaceInstance interfaceInstance) return interfaceInstance.ModuleName;
            return instance.Name ?? sourceFile.Name;
        }
        */
        /*
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
                if (!string.IsNullOrEmpty(interfaceName))
                {
                    addUnfoundModule(setup, file, interfaceName);
                }
                return;
            }

            ProjectProperty? declaringProjectProperty = file.ProjectProperty;
            if (declaringProjectProperty == null) return;

            IVerilogRelatedFile? interfaceFile = declaringProjectProperty.DefinitionNameSpace.GetFile(sourceInterface.Name);
            if (interfaceFile == null)
            {
                addUnfoundModule(setup, file, sourceInterface.Name);
                return;
            }

            if (setup.Files.Contains(interfaceFile)) return;
            setup.Files.Add(interfaceFile);
        }
        */
        private static void appendVerilogHeaderInstance(VerilogHeaderInstance file, SimulationSetup setup)
        {
            if (file.Project == setup.Project)
            {
                if (setup.IncludeFiles.Contains(file)) return;
                setup.IncludeFiles.Add(file);
                string? path = System.IO.Path.GetDirectoryName(file.Project.GetAbsolutePath(file.RelativePath));
                if (path == null) return;
                if (!setup.IncludePaths.Contains(path)) setup.IncludePaths.Add(path);
                return;
            }
            else
            {
                // TODO
            }

        }
        
        private static void appendImportedPackage(string packageName, CodeEditor2.Data.Project project, SimulationSetup setup, List<string> ids)
        {
            // Search for the package file in the project
            IVerilogRelatedFile? packageFile = findPackageFile(packageName, project, setup);
            if (packageFile == null) return;

            // Add to ImportFiles
            SimulationSetup ownerSetup;
            if (packageFile.Project == setup.Project)
            {
                if (setup.ImportFiles.Contains(packageFile)) return;
                setup.ImportFiles.Add(packageFile);
                ownerSetup = setup;
                searchHier(packageFile, packageName, ids, ownerSetup, "");
            }
            else
            {
                // TODO
            }

            // package file may itself reference other packages / classes: trace them
            // (also reports missing references in the package as file shortage)
        }
        
        private static void appendClass(IVerilogRelatedFile referrer, string className, CodeEditor2.Data.Project project, SimulationSetup setup, List<string> ids)
        {
            IVerilogRelatedFile? classFile = findClassFile(className, project, setup);
            if (classFile == null)
            {
                // referenced class definition file is missing: report as file shortage
                setup.Failed = true;
                return;
            }

            // Add to ClassFiles
            SimulationSetup ownerSetup;
            if (classFile.Project == setup.Project)
            {
                if (setup.ClassFiles.Contains(classFile)) return;
                setup.ClassFiles.Add(classFile);
                ownerSetup = setup;
                searchHier(classFile, className, ids, ownerSetup, "");
            }
            else
            {
                // TODO
            }

            // class file may itself reference other classes / packages: trace them
            // (also reports missing references in the class file as file shortage)
        }
        /*

        /// <summary>
        /// True when the given file has an instance item for the given definition
        /// name (i.e. an instance of the module / interface was resolved and
        /// registered to file.Items).
        ///
        /// Such an instance is traversed by the instance tree (searchNameSpace ->
        /// appendExternalProjectDependencies), which collects the dependencies of
        /// the PARAMETER-OVERRIDDEN parse. Walking the definition's base parse as
        /// well would collect the modules of the default (non-selected) generate
        /// branch and report them as unfound.
        /// </summary>
        private static bool hasInstantiationItem(IVerilogRelatedFile file, string definitionName)
        {
            try
            {
                foreach (CodeEditor2.Data.Item item in file.Items)
                {
                    if (item is VerilogModuleInstance moduleInstance && moduleInstance.ModuleName == definitionName) return true;
                    if (item is InterfaceInstance interfaceInstance && interfaceInstance.ModuleName == definitionName) return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// True when the given name (recorded as unfound by some parse pass) is a
        /// cross-project reference whose definition file resolves in the project
        /// that owns it. Such a name is not a missing module file.
        /// </summary>
        private static bool isResolvableCrossProjectReference(ParsedDocument parsedDocument, string definitionName)
        {
            CodeEditor2.Data.Project? ownProject = parsedDocument.Project;
            foreach (CodeEditor2.Data.Project crossProject in getReferencedDefinitionProjects(parsedDocument, definitionName))
            {
                if (crossProject == ownProject) continue;
                ProjectProperty? crossProperty = crossProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (crossProperty == null) continue;
                if (crossProperty.DefinitionNameSpace.GetFile(definitionName) != null) return true;
            }
            return false;
        }

        /// <summary>
        /// Projects that own the given referenced definition name, as recorded by
        /// the parse for cross-project ("// @project <name>") references.
        /// </summary>
        private static List<CodeEditor2.Data.Project> getReferencedDefinitionProjects(ParsedDocument parsedDocument, string definitionName)
        {
            List<CodeEditor2.Data.Project> results = new List<CodeEditor2.Data.Project>();
            foreach (string projectName in parsedDocument.GetReferencedDefinitionNameSpaceProjects(definitionName))
            {
                if (CodeEditor2.Global.Projects.TryGetValue(projectName, out CodeEditor2.Data.Project? project)
                    && project != null)
                {
                    if (!results.Contains(project)) results.Add(project);
                }
            }
            return results;
        }

        /// <summary>
        /// Project name used as part of the visited key of searchHier.
        /// </summary>
        private static string getProjectName(IVerilogRelatedFile file)
        {
            try
            {
                return file.Project?.Name ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// register an unfound reference as "RelativePath:Name" so that the
        /// log shows where the missing definition/instance is referenced from.
        /// </summary>
        private static void addUnfoundModule(SimulationSetup setup, IVerilogRelatedFile file, string name)
        {
            string entry;
            try { entry = file.RelativePath + ":" + name; }
            catch { entry = file.ID + ":" + name; }
            if (!setup.UnfoundModules.Contains(entry)) setup.UnfoundModules.Add(entry);
        }
        */
        private static IVerilogRelatedFile? findPackageFile(string packageName, CodeEditor2.Data.Project project, SimulationSetup setup)
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

    }
}
