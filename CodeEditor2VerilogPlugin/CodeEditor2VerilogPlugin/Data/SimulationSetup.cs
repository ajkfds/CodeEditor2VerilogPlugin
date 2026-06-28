using CodeEditor2.Data;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.ModuleItems;
using System. Collections. Generic;
using System. Linq;

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
        public List<string> ImportPaths = new List<string>();

        public List<string> ExternalLibraryPathList = new List<string>();
        public List<string> UnfoundModules = new List<string>();

        public CodeEditor2. Data. Project Project;

        public Dictionary<CodeEditor2. Data. Project, SimulationSetup> ExternalProjectReferences = new Dictionary<Project, SimulationSetup>();
        public Dictionary<string, CodeEditor2. Data. Project> ExternalProjectEntryInstance = new Dictionary<string, Project>();
        public static SimulationSetup? Create(pluginVerilog. Data. VerilogFile verilogFile)
        {
            SimulationSetup setup = new SimulationSetup();
            setup.TopFile = verilogFile;

            List<string> ids = new List<string>();

            if (verilogFile. VerilogParsedDocument == null) return null;
            if (verilogFile. VerilogParsedDocument. Root == null) return null;

            setup.Project = verilogFile. Project;
            BuildingBlock? buildingBlock = verilogFile. VerilogParsedDocument. Root. BuildingBlocks. Values. FirstOrDefault();
            if (buildingBlock == null) return null;

            setup.TopName = buildingBlock. Name;
            searchHier(verilogFile, setup. TopName, ids, setup, setup.TopName);

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

        private static void searchHier(IVerilogRelatedFile file, string buildingBlockName, List<string> ids, SimulationSetup setup, string path)
        {
            if (ids. Contains(file. ID)) return;
            ParsedDocument? parsedDocument = file. VerilogParsedDocument;
            if (parsedDocument == null) return;

            appendFile(file, setup);
            foreach (string unfound in parsedDocument. UnfoundModules)
            {
                CodeEditor2. Controller. AppendLog("unfound instance on " + file. RelativePath);
                if (!setup. UnfoundModules. Contains(unfound)) setup. UnfoundModules. Add(unfound);

            }
            foreach (string external in parsedDocument. ExternalRefrenceModules)
            {
                if (file. ProjectProperty. ExtenralLibraryPath. ContainsKey(external))
                {
                    string libPath = file. ProjectProperty. ExtenralLibraryPath[external];
                    if (!setup. ExternalLibraryPathList. Contains(libPath)) setup. ExternalLibraryPathList. Add(libPath);
                }
            }

            foreach (var ifile in parsedDocument. IncludeFiles. Values)
            {
                appendVerilogHeaderInstance(ifile, setup);
            }

            // Process imported packages
            foreach (string packageName in parsedDocument. ImportedPackages)
            {
                appendImportedPackage(packageName, file. Project, setup);
            }

            if(!parsedDocument. Root. BuildingBlocks. TryGetValue(buildingBlockName,out BuildingBlock? buildingBlock))
            {
                return;
            }

            searchNameSpace(file, ids, buildingBlock, setup, path);
            //foreach(var item in file. Items. Values)
            //{
            //    if(item is IVerilogRelatedFile)
            //    {
            //        searchHier((IVerilogRelatedFile)item, ids, setup);
            //    }
            //}
        }

        private static void searchNameSpace(IVerilogRelatedFile file, List<string> ids, NameSpace nameSpace, SimulationSetup setup, string path)
        {
            foreach (INamedElement element in nameSpace. NamedElements. Values)
            {
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
                        setup. ExternalProjectEntryInstance. Add(
                            newPath,
                            CodeEditor2. Global. Projects[moduleInstantiation. SourceProjectName]
                            );
                    }
                    if (file. Items. TryGetValue(moduleInstantiation. Name, out CodeEditor2. Data. Item? item))
                    {
                        string newPath = moduleInstantiation. Name;
                        if (path != "") newPath = path + "." + newPath;

                        var subfile = item as IVerilogRelatedFile;
                        if (subfile != null) searchHier(subfile, moduleInstantiation. SourceName, ids, setup, newPath);
                    }
                }
                else if (element is pluginVerilog.Verilog.DataObjects.InterfaceInstance)
                {
                    // Handle InterfaceInstance - add the source Interface file to Files
                    pluginVerilog.Verilog.DataObjects.InterfaceInstance interfaceInstance = (pluginVerilog.Verilog.DataObjects.InterfaceInstance)element;
                    appendInterfaceInstance(file, interfaceInstance, setup);
                }
                else if (element is DataObject)
                {
                    // Handle DataObject - check if it's a Class or InterfaceClass instance
                    DataObject dataObject = (DataObject)element;
                    if (dataObject.DataType is ClassType)
                    {
                        appendClassInstance(file, dataObject, setup);
                    }
                    else if (dataObject.DataType is InterfaceClass)
                    {
                        appendInterfaceClassInstance(file, dataObject, setup);
                    }
                }
            }
        }


        private static void appendFile(IVerilogRelatedFile file, SimulationSetup setup)
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
                }
                return;
            }
        }

        private static void appendInterfaceInstance(IVerilogRelatedFile file, pluginVerilog.Verilog.DataObjects.InterfaceInstance interfaceInstance, SimulationSetup setup)
        {
            // Get the source Interface file from ProjectProperty
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Use SourceName directly from InterfaceInstance
            IVerilogRelatedFile? sourceFile = projectProperty.GetFileOfBuildingBlock(interfaceInstance.SourceName);
            if (sourceFile == null) return;

            // Add to Files (same logic as appendFile for VerilogFile)
            if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
            {
                if (setup.Files.Contains(sourceFile)) return;
                setup.Files.Add(sourceFile);
                return;
            }
        }

        private static void appendClassInstance(IVerilogRelatedFile file, DataObject dataObject, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Handle Object (class instance) - has Class property
            if (dataObject is Object objectInstance)
            {
                Class class_ = objectInstance.Class;
                if (class_ == null) return;

                IVerilogRelatedFile? sourceFile = projectProperty.GetFileOfBuildingBlock(class_.Name);
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
                    IVerilogRelatedFile? sourceFile = projectProperty.GetFileOfBuildingBlock(userDefinedType.Typedef.Name);
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
                    IVerilogRelatedFile? sourceFile = projectProperty.GetFileOfBuildingBlock(userDefinedType.Typedef.Name);
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

        private static void appendImportedPackage(string packageName, CodeEditor2. Data. Project project, SimulationSetup setup)
        {
            // Search for the package file in the project
            IVerilogRelatedFile? packageFile = findPackageFile(packageName, project, setup);
            if (packageFile == null) return;

            // Add to ImportFiles
            if (packageFile. Project == setup. Project)
            {
                if (setup. ImportFiles. Contains(packageFile)) return;
                setup. ImportFiles. Add(packageFile);
                string? path = System. IO. Path. GetDirectoryName(packageFile. Project. GetAbsolutePath(packageFile. RelativePath));
                if (path == null) return;
                if (!setup. ImportPaths. Contains(path)) setup. ImportPaths. Add(path);
            }
            else
            {
                // Handle external project reference
                CodeEditor2. Data. Project extProject = packageFile. Project;
                SimulationSetup pSetup;
                if (!setup. ExternalProjectReferences. ContainsKey(extProject))
                {
                    pSetup = new SimulationSetup() { Project = extProject };
                    setup. ExternalProjectReferences. Add(extProject, pSetup);
                }
                else
                {
                    pSetup = setup. ExternalProjectReferences[extProject];
                }
                if (pSetup. ImportFiles. Contains(packageFile)) return;
                pSetup. ImportFiles. Add(packageFile);
                string? path = System. IO. Path. GetDirectoryName(packageFile. Project. GetAbsolutePath(packageFile. RelativePath));
                if (path == null) return;
                if (!pSetup. ImportPaths. Contains(path)) pSetup. ImportPaths. Add(path);
            }
        }

        private static IVerilogRelatedFile? findPackageFile(string packageName, CodeEditor2. Data. Project project, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return null;

            Package? package = projectProperty.GetBuildingBlock(packageName) as Package;
            IVerilogRelatedFile? verilogRelatedFile = projectProperty.GetFileOfBuildingBlock(packageName);
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
