using CodeEditor2.Data;
using pluginVerilog.Data;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog.BuildingBlocks;
using Svg.FilterEffects;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public CodeEditor2.Data.Project Project;

        public static SimulationSetup? Create(pluginVerilog.Data.VerilogFile verilogFile)
        {
            SimulationSetup setup = new SimulationSetup();
            setup.TopFile = verilogFile;

            List<string> ids = new List<string>();

            if (verilogFile.VerilogParsedDocument == null) return null;
            if (verilogFile.VerilogParsedDocument.Root == null) return null;

            setup.Project = verilogFile.Project;
            BuildingBlock? buildingBlock = verilogFile.VerilogParsedDocument.Root.BuildingBlocks.Values.FirstOrDefault();

            setup.TopName = buildingBlock?.Name;
            searchHier(verilogFile,ids,setup);

            return setup;
        }

        private static void searchHier(IVerilogRelatedFile file,List<string> ids,SimulationSetup setup)
        {
            if (ids.Contains(file.ID)) return;

            appendFile(file,setup);
            foreach(var item in file.Items.Values)
            {
                if(item is IVerilogRelatedFile)
                {
                    searchHier((IVerilogRelatedFile)item, ids, setup);
                }
            }
        }


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
                if (setup.Files.Contains(sourceFile)) return;
                setup.Files.Add(sourceFile);
                return;
            }
            if (file is VerilogHeaderInstance)
            {
                if (setup.IncludeFiles.Contains(file)) return;
                setup.IncludeFiles.Add(file);
                string? path = System.IO.Path.GetDirectoryName(file.Project.GetAbsolutePath(file.RelativePath));
                if (path == null) return;
                if (!setup.IncludePaths.Contains(path)) setup.IncludePaths.Add(path);
                return;
            }
        }

    }
}
