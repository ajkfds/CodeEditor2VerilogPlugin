﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AjkAvaloniaLibs.Libs.Json;
using Avalonia.Controls.Platform;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;
using Avalonia.OpenGL;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog
{
    public class ProjectProperty : CodeEditor2.Data.ProjectProperty
    {
        //public ProjectProperty(CodeEditor2.Data.Project project)
        //{
        //    this.project = project;
        //}

        public ProjectProperty(Project project,ProjectProperty.Setup setup) : base(project,setup)
        {

        }
        public ProjectProperty(Project project) : base(project)
        {

        }

        private CodeEditor2.Data.Project project;

        public Verilog.AutoComplete.Setup SnippetSetup = new Verilog.AutoComplete.Setup();

        public override Setup CreateSetup()
        {
            return new Setup(this);
        }

        public override CodeEditor2.Data.ProjectProperty.Setup? CreateSetup(JsonElement jsonElement, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(jsonElement, typeof(Setup), options) as CodeEditor2.Data.ProjectProperty.Setup;
        }
        public static ProjectProperty.Setup? DeserializeSetup(JsonElement jsonElement, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(jsonElement, typeof(ProjectProperty.Setup), options) as ProjectProperty.Setup;
        }
        public new class Setup : CodeEditor2.Data.ProjectProperty.Setup
        {
            public Setup() { }
            public Setup(ProjectProperty projectProperty)
            {

            }
            public override string ID { get; set; } = Plugin.StaticID;
            public string test { get; set; } = "verilogTest";
            public override void Write(
                Utf8JsonWriter writer,
                JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, this, typeof(Setup), options);
            }


        }

        //public new class Setup : CodeEditor2.Data.ProjectProperty.Setup
        //{
        //    public Setup(CodeEditor2.Data.ProjectProperty projectProperty) : base(projectProperty)
        //    {

        //    }

        //    public override string ID { get => Plugin.StaticID;  }

        //    [JsonInclude]
        //    string Name = "VerilogPulgin";


        //}

        //public override void AddJsonConverter(JsonSerializerOptions options)
        //{
        //    options.Converters.Add(new ProjectPropertyJsonConverter());
        //}
        //public class ProjectPropertyJsonConverter : JsonConverter<CodeEditor2.Data.ProjectPropertySetup>
        //{
        //    public override ProjectPropertySetup Read(
        //        ref Utf8JsonReader reader,
        //        Type typeToConvert,
        //        JsonSerializerOptions options)
        //    {
        //        return (ProjectPropertySetup)JsonSerializer.Deserialize(ref reader, typeof(ProjectPropertySetup), options);
        //    }

        //    public override void Write(
        //        Utf8JsonWriter writer,
        //        ProjectPropertySetup value,
        //        JsonSerializerOptions options)
        //    {
        //        ProjectPropertySetup val = value as ProjectPropertySetup;
        //        JsonSerializer.Serialize(writer, value as ProjectPropertySetup, typeof(pluginVerilog.ProjectProperty.Setup), options);
        //    }
        //}

        //public override void SaveSetup(JsonWriter writer)
        //{
        //    using (var macroWriter = writer.GetObjectWriter("Macros"))
        //    {
        //        foreach (var kvp in Macros)
        //        {
        //            macroWriter.writeKeyValue(kvp.Key, kvp.Value.MacroText);
        //        }
        //    }
        //}

        // VerilogModuleParsedData
        //public override void LoadSetup(JsonReader jsonReader)
        //{
        //    Macros.Clear();
        //    using(var reader = jsonReader.GetNextObjectReader())
        //    {
        //        while (true)
        //        {
        //            string key = reader.GetNextKey();
        //            if (key == null) break;

        //            switch (key)
        //            {
        //                case "Macros":
        //                    loadMacros(reader);
        //                    break;
        //                default:
        //                    reader.SkipValue();
        //                    break;
        //            }
        //        }
        //    }
        //}

        public BuildingBlock? GetInstancedBuildingBlock(IBuildingBlockInstantiation instantiation)
        {
            if (instantiation.ParameterOverrides.Count == 0)
            {
                return GetBuildingBlock(instantiation.SourceName);
            }
            else
            {
                Data.VerilogFile? file = GetFileOfBuildingBlock(instantiation.SourceName) as Data.VerilogFile;
                if (file == null) return null;
                Verilog.ParsedDocument? parsedDocument = file.GetInstancedParsedDocument(instantiation.SourceName + ":" + instantiation.OverrideParameterID) as Verilog.ParsedDocument;
                if (parsedDocument == null) return null;
                if (parsedDocument.Root == null) return null;
                if (!parsedDocument.Root.BuldingBlocks.ContainsKey(instantiation.SourceName)) return null;
                return parsedDocument.Root.BuldingBlocks[instantiation.SourceName];
            }
        }

        public void loadMacros(JsonReader jsonReader)
        {
            using(var reader = jsonReader.GetNextObjectReader())
            {
                while (true)
                {
                    string macroIdentifier = reader.GetNextKey();
                    if (macroIdentifier == null) break;
                    if (Macros.ContainsKey(macroIdentifier))
                    {
                        reader.SkipValue();
                    }
                    else
                    {
                        string macroText = reader.GetNextStringValue();
                        Macros.Add(macroIdentifier, Verilog.Macro.Create(macroIdentifier, macroText));
                    }
                }
            }

        }

        private Dictionary<string, CodeEditor2.CodeEditor.ParsedDocument> pdocs = new Dictionary<string, CodeEditor2.CodeEditor.ParsedDocument>();
        public IReadOnlyDictionary<string, CodeEditor2.CodeEditor.ParsedDocument> VerilogModuleInstanceParsedDocuments
        {
            get
            {
                return pdocs;
            }
        }
        public void RegisterParsedDocument(string ID, CodeEditor2.CodeEditor.ParsedDocument parsedDocument)
        {
            if (pdocs.ContainsKey(ID))
            {
                pdocs.Remove(ID);
            }
            pdocs.Add(ID, parsedDocument);
        }

        public void RemoveRegisteredParsedDocument(string ID, CodeEditor2.CodeEditor.ParsedDocument parsedDocument)
        {
            if (pdocs.ContainsKey(ID))
            {
                pdocs.Remove(ID);
            }
            else
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public bool IsRegisteredParsedDocument(string ID)
        {
            if (pdocs.ContainsKey(ID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public CodeEditor2.CodeEditor.ParsedDocument GetParsedDocument(string id)
        {
            if (id == null) System.Diagnostics.Debugger.Break();

            if (pdocs.ContainsKey(id))
            {
                return pdocs[id];
            }
            else
            {
                return null;
            }
        }


        // BuildingBlock -> File Table
        private WeakReferenceDictionary<string, Data.IVerilogRelatedFile> buildingBlockFileTable = new WeakReferenceDictionary<string, Data.IVerilogRelatedFile>();
        private WeakReferenceDictionary<string, BuildingBlock> buildingBlockTable = new WeakReferenceDictionary<string, BuildingBlock>();

        public void RegisterBuildingBlock(string buildingBlockName,BuildingBlock buildingBlock, Data.IVerilogRelatedFile file)
        {
            buildingBlockTable.Register(buildingBlockName, buildingBlock);
            buildingBlockFileTable.Register(buildingBlockName, file);
        }

        public bool RemoveBuildingBlock(string moduleName)
        {
            buildingBlockTable.Remove(moduleName);
            return buildingBlockFileTable.Remove(moduleName);
        }

        public bool HasRegisteredBuildingBlock(string moduleName)
        {
            buildingBlockTable.HasItem(moduleName);
            return buildingBlockFileTable.HasItem(moduleName);
        }

        public Data.IVerilogRelatedFile? GetFileOfBuildingBlock(string buildingBlockName)
        {
            return buildingBlockFileTable.GetItem(buildingBlockName);
        }

        public List<string> GetBuildingBlockNameList()
        {
            buildingBlockTable.CleanDictionary();
            return buildingBlockTable.KeyList();
        }

        public List<string> GetModuleNameList()
        {
            return buildingBlockTable.GetMatchedKeyList((x) => { return (x is Module); });
        }

        public List<string> GetObjectsNameList()
        {
            return buildingBlockTable.GetMatchedKeyList(
                (x) => { return (x is Object)|| (x is Interface)||(x is Program); 
                });
        }

        public BuildingBlock? GetBuildingBlock(string buildingBlockName)
        {
            Data.IVerilogRelatedFile? file = GetFileOfBuildingBlock(buildingBlockName);
            if (file == null) return null;
            if (file.VerilogParsedDocument == null) return null;
            if (file.VerilogParsedDocument.Root == null) return null;
            if (!file.VerilogParsedDocument.Root.BuldingBlocks.ContainsKey(buildingBlockName)) return null;
            return file.VerilogParsedDocument.Root.BuldingBlocks[buildingBlockName] as BuildingBlock;
        }

        // inline comment
        public Dictionary<string, Action<Verilog.ParsedDocument>> InLineCommentCommands = new Dictionary<string, Action<Verilog.ParsedDocument>>();

        // macros
        public Dictionary<string, Verilog.Macro> Macros = new Dictionary<string, Verilog.Macro>();

        // system tasks
        public Dictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>> SystemTaskParsers = new Dictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>>
        {
            // Display task
            {"$display",null },
            {"$strobe",null },
            {"$displayb",null },
            {"$strobeb",null },
            {"$displayh",null },
            {"$strobeh",null },
            {"$displayo",null },
            {"$strobeo", null },
            {"$monitor", null },
            {"$write", null },
            {"$monitorb", null },
            {"$writeb", null },
            {"$monitorh", null },
            {"$writeh", null },
            {"$monitoro", null },
            {"$writeo", null },
            {"$monitoroff", null },
            {"$monitoron", null },
            // File I/O tasks
            {"$fclose", null },
            {"$fdisplay", null },
            {"$fstrobe", null } ,
            {"$fdisplayb", null },
            {"$fstrobeb", null },
            {"$fdisplayh", null },
            {"$fstrobeh", null },
            {"$fdisplayo", null },
            {"$fstrobeo", null },
            {"$ungetc", null },
            {"$fflush", null },
            {"$fmonitor", null },
            {"$fwrite", null },
            {"$fmonitorb", null },
            {"$fwriteb", null },
            {"$fmonitorh", null },
            {"$fwriteh", null },
            {"$fmonitoro", null },
            {"$fwriteo", null },
            {"$readmemb", null },
            {"$readmemh", null },
            {"$swrite", null },
            {"$swriteb", null },
            {"$swriteo", null },
            {"$swriteh", null } ,
            {"$sdf_annotate", null } ,
            // Timescale tasks
            {"$printtimescale", null },
            {"$timeformat", null },
            // Simulation control tasks
            {"$finish", null },
            {"$stop", null },
            // PLA modeling tasks
            {"$async$and$array", null },
            {"$async$and$plane", null },
            {"$async$nand$array", null },
            {"$async$nand$plane", null },
            {"$async$or$array", null },
            {"$async$or$plane", null },
            {"$async$nor$array", null },
            {"$async$nor$plane", null },
            {"$sync$and$array", null },
            {"$sync$and$plane", null },
            {"$sync$nand$array", null },
            {"$sync$nand$plane", null },
            {"$sync$or$array", null },
            {"$sync$or$plane", null },
            {"$sync$nor$array", null },
            {"$sync$nor$plane", null },
            // Stochastic analysis tasks
            {"$q_initialize", null },
            {"$q_add", null },
            {"$q_remove", null },
            {"$q_full", null },
            {"$q_exam", null },

            // Dump
            {"$dumpfile", (word,nameSpace) =>{ return Verilog.Statements.SystemTask.SystemTask.ParseCreate(word,nameSpace); } },
            {"$dumpall",null },
            {"$dumpoff",null },
            {"$dumpon",null },
            {"$dumpvars", (word,nameSpace) =>{ return Verilog.Statements.SystemTask.SkipArguments.ParseCreate(word,nameSpace); }  },
            {"$dumpflush",null },
            {"$dumplimit",null },
        };

        public Dictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>> SystemFunctions = new Dictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>>
        {
            {"$sformat", null },
            {"$ferror", null },
            {"$rewind", null },
            {"$fseek", null },
            {"$fread", null },
            {"$ftell", null } ,
            {"$sscanf", null } ,
            {"$fscanf", null },
            {"$fgetc", null },
            {"$fgets", null },
            {"$fopen", null },

            // Simulation time functions
            {"$realtime",null },
            {"$stime",null },
            {"$time",null },

            // Conversion functions
            {"$bitstoreal",null },
            {"$realtobits",null },
            {"$itor",null },
            {"$rtoi",null },
            {"$signed",null },
            {"$unsigned",null },

            // Probabilistic distribution functions
            {"$dist_chi_square",null },
            {"$dist_erlang",null },
            {"$dist_exponential",null },
            {"$dist_normal",null },
            {"$dist_poisson",null },
            {"$dist_t",null },
            {"$dist_uniform",null },
            {"$random",null },

            // Command line input
            {"$test$plusargs",null },
            {"$value$plusargs",null },

            // systemverilog
            {"$clog2",null },

        };

        public Dictionary<string, Action<Verilog.WordScanner>> InCommentTags = new Dictionary<string, Action<Verilog.WordScanner>>
        {
            { "@section",null }
        };

    }
}
