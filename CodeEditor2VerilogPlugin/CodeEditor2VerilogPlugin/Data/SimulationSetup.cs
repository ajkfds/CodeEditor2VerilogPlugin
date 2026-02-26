using CodeEditor2;
using CodeEditor2.Data;
using pluginVerilog.Data;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using Svg.FilterEffects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class SimulationSetup
    {
        protected SimulationSetup() { }

        public string TopName;
        public VerilogFile TopFile;
        public List<IVerilogRelatedFile> Files = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> IncludeFiles = new List<IVerilogRelatedFile>();
        public List<string> IncludePaths = new List<string>();

        public List<string> ExternalLibraryPathList = new List<string>();
        public List<string> UnfoundModules = new List<string>();

        public CodeEditor2.Data.Project Project;

        public Dictionary<CodeEditor2.Data.Project, SimulationSetup> ExternalProjectReferences = new Dictionary<Project, SimulationSetup>();
        public Dictionary<string, CodeEditor2.Data.Project> ExternalProjectEntryInstance = new Dictionary<string, Project>();
        public static SimulationSetup? Create(pluginVerilog.Data.VerilogFile verilogFile)
        {
            SimulationSetup setup = new SimulationSetup();
            setup.TopFile = verilogFile;

            List<string> ids = new List<string>();

            if (verilogFile.VerilogParsedDocument == null) return null;
            if (verilogFile.VerilogParsedDocument.Root == null) return null;

            setup.Project = verilogFile.Project;
            BuildingBlock? buildingBlock = verilogFile.VerilogParsedDocument.Root.BuildingBlocks.Values.FirstOrDefault();
            if (buildingBlock == null) return null;

            setup.TopName = buildingBlock.Name;
            searchHier(verilogFile,setup.TopName,ids,setup,setup.TopName);

            if (setup.UnfoundModules.Count != 0)
            {
                foreach (var module in setup.UnfoundModules)
                {
                    CodeEditor2.Controller.AppendLog(module + " unfound", Avalonia.Media.Colors.Red);
                }
                return null;
            }
            return setup;
        }

        private static void searchHier(IVerilogRelatedFile file,string buildingBlockName,List<string> ids,SimulationSetup setup,string path)
        {
            if (ids.Contains(file.ID)) return;
            ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument == null) return;

            appendFile(file,setup);
            foreach(string unfound in parsedDocument.UnfoundModules)
            {
                if (!setup.UnfoundModules.Contains(unfound)) setup.UnfoundModules.Add(unfound);

            }
            foreach (string external in parsedDocument.ExternalRefrenceModules)
            {
                if (file.ProjectProperty.ExtenralLibraryPath.ContainsKey(external))
                {
                    string libPath = file.ProjectProperty.ExtenralLibraryPath[external];
                    if(!setup.ExternalLibraryPathList.Contains(libPath)) setup.ExternalLibraryPathList.Add(libPath);
                }
            }

            foreach (var ifile in parsedDocument.IncludeFiles.Values)
            {
                appendVerilogHeaderInstance(ifile, setup);
            }
            if (!parsedDocument.Root.BuildingBlocks.ContainsKey(buildingBlockName)) return;
            BuildingBlock buildingBlock = parsedDocument.Root.BuildingBlocks[buildingBlockName];

            searchNameSpace(file, ids, buildingBlock, setup,path);
            //foreach(var item in file.Items.Values)
            //{
            //    if(item is IVerilogRelatedFile)
            //    {
            //        searchHier((IVerilogRelatedFile)item, ids, setup);
            //    }
            //}
        }

        private static void searchNameSpace(IVerilogRelatedFile file, List<string> ids, NameSpace nameSpace, SimulationSetup setup,string path)
        {
            foreach(INamedElement element in nameSpace.NamedElements.Values)
            {
                if(element is NameSpace)
                {
                    NameSpace subNameSpace = (NameSpace)element;
                    string newPath = path + "." + subNameSpace.Name;
                    searchNameSpace(file,ids,subNameSpace, setup, newPath);
                }
                else if(element is ModuleInstantiation)
                {
                    ModuleInstantiation moduleInstantiation = (ModuleInstantiation)element;
                    if(nameSpace.BuildingBlock.Project.Name != moduleInstantiation.SourceProjectName)
                    {
                        string newPath = path + "." + moduleInstantiation.Name;
                        setup.ExternalProjectEntryInstance.Add(
                            newPath,
                            CodeEditor2.Global.Projects[moduleInstantiation.SourceProjectName]
                            );
                    }
                    if (file.Items.ContainsKey(moduleInstantiation.Name))
                    {
                        string newPath = moduleInstantiation.Name;
                        if (path != "") newPath = path + "." + newPath;

                        var subfile = file.Items[moduleInstantiation.Name] as IVerilogRelatedFile;
                        if(subfile != null) searchHier(subfile,moduleInstantiation.SourceName, ids, setup, newPath);
                    }
                }
            }
        }

        //private static string getHierName(VerilogModuleInstance instance)
        //{
        //    List<string> hierNames = new List<string>();
        //    searchHierUpward(hierNames,instance);
        //    StringBuilder sb = new StringBuilder();

        //}

        //private static string searchHierUpward(List<string> hierNames, VerilogModuleInstance instance)
        //{

        //}

        private static void appendFile(IVerilogRelatedFile file, SimulationSetup setup)
        {
            if(file is pluginVerilog.Data.VerilogFile || file is SystemVerilogFile)
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
                } else
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
                }
                return;
            }
        }

        private static void appendVerilogHeaderInstance(VerilogHeaderInstance file,SimulationSetup setup)
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
                CodeEditor2.Data.Project project = file.Project;
                SimulationSetup pSetup;
                if (!setup.ExternalProjectReferences.ContainsKey(project))
                {
                    pSetup = new SimulationSetup() { Project = project };
                    setup.ExternalProjectReferences.Add(project, pSetup);
                }
                else
                {
                    pSetup = setup.ExternalProjectReferences[project];
                }
                if (pSetup.IncludeFiles.Contains(file)) return;
                pSetup.IncludeFiles.Add(file);
                string? path = System.IO.Path.GetDirectoryName(file.Project.GetAbsolutePath(file.RelativePath));
                if (path == null) return;
                if (!pSetup.IncludePaths.Contains(path)) pSetup.IncludePaths.Add(path);
                return;
            }

        }

    }
}
