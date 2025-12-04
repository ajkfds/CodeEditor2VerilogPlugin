using AjkAvaloniaLibs.Libs.Json;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.OpenGL;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class ProjectProperty : CodeEditor2.Data.ProjectProperty
    {
        //public ProjectProperty(CodeEditor2.Data.Project project)
        //{
        //    this.project = project;
        //}
        public ProjectProperty(Project project, ProjectProperty.Setup setup) : base(project,setup)
        {
            CompileOption = setup.CompileOption;
        }
        public RuleSet RuleSet = new RuleSet();
        public string CompileOption { get; set; } = "";

        public Verilog.AutoComplete.Setup SnippetSetup = new Verilog.AutoComplete.Setup();

        public override void InitializePropertyForm(ItemPropertyForm form, CodeEditor2.NavigatePanel.NavigatePanelNode node,Project project)
        {
            base.InitializePropertyForm(form, node,project);
            if (node is CodeEditor2.NavigatePanel.ProjectNode)
            {
                Views.ProjectPropertyTab tab = new Views.ProjectPropertyTab(this, form, node, project);
            }
        }

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
                this.CompileOption = projectProperty.CompileOption;
            }
            public override string ID { get; set; } = Plugin.StaticID;
            public string test { get; set; } = "verilogTest";

            public string CompileOption { get; set; } = "";
            public override void Write(
                Utf8JsonWriter writer,
                JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, this, typeof(Setup), options);
            }


        }

        public AnnotationCommandsClass AnnotationCommands { get; set; } = new AnnotationCommandsClass();
        public string AnnotationKeyValueDelimiter = ":";
        public string AnnotationSeparator = ",";
        public class AnnotationCommandsClass
        {
            public string Synchronized = "@sync";
            public string Clock = "@clock";
            public string Reset = "@reset";
            public string PortGroup = "@portgroup";
            public string Discard = "@discard";
            public string Unused = "@unused";
            public string RefInstance = "@ref_instance";
            public string Markdown = "@markdown";
            public string ToolOption = "@tool_option";
        }

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
                string InstanceKey = Verilog.ParsedDocument.KeyGenerator(file,instantiation.SourceName,instantiation.ParameterOverrides);

                Verilog.ParsedDocument? parsedDocument = file.GetInstancedParsedDocument(InstanceKey) as Verilog.ParsedDocument;
                if (parsedDocument == null) return null;
                if (parsedDocument.Root == null) return null;
                if (!parsedDocument.Root.BuildingBlocks.ContainsKey(instantiation.SourceName)) return null;
                return parsedDocument.Root.BuildingBlocks[instantiation.SourceName];
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

        public CodeEditor2.CodeEditor.ParsedDocument? GetParsedDocument(string id)
        {
            if (id == null) throw new Exception();
            
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

        public void RegisterBuildingBlock(string buildingBlockName,BuildingBlock buildingBlock, VerilogFile file)
        {
            if(file == null)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }
            buildingBlockTable.Register(buildingBlockName, buildingBlock);
            buildingBlockFileTable.Register(buildingBlockName, file);
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        //public bool RemoveBuildingBlock(string moduleName)
        //{
        //    buildingBlockTable.Remove(moduleName);
        //    return buildingBlockFileTable.Remove(moduleName);
        //}

        public bool HasRegisteredBuildingBlock(string moduleName)
        {
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }
            bool ret1 = false;
            bool ret2 = false;
            ret1 = buildingBlockTable.HasItem(moduleName);
            ret2 = buildingBlockFileTable.HasItem(moduleName);
            if (ret1!=ret2)
            {
                System.Diagnostics.Debugger.Break();
            }
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }
            return ret2;
        }

        public Data.IVerilogRelatedFile? GetFileOfBuildingBlock(string buildingBlockName)
        {
            return buildingBlockFileTable.GetItem(buildingBlockName);
        }

        public List<string> GetBuildingBlockNameList()
        {
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }

            buildingBlockTable.CleanDictionary();
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }
            List<string> ret = buildingBlockTable.KeyList();
            if (System.Diagnostics.Debugger.IsAttached && buildingBlockTable.Count != buildingBlockFileTable.Count)
            {
                System.Diagnostics.Debugger.Break();
            }
            return ret;
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
            if (!file.VerilogParsedDocument.Root.BuildingBlocks.ContainsKey(buildingBlockName)) return null;
            return file.VerilogParsedDocument.Root.BuildingBlocks[buildingBlockName] as BuildingBlock;
        }



        // inline comment
        public Dictionary<string, Action<Verilog.ParsedDocument>> InLineCommentCommands = new Dictionary<string, Action<Verilog.ParsedDocument>>();

        // macros
        public Dictionary<string, Verilog.Macro> Macros = new Dictionary<string, Verilog.Macro>();

        // system tasks
        public Dictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>?> SystemTaskParsers = new Dictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>?>
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

            // Elaboration system tasks
            {"$fatal",null },
            {"$error",null },
            {"$warning",null },
            {"$info",null },

            // SystemVerilog
            //Simulation control tasks (20.2)
            //{"$finish",null },
            //{"$stop",null },
            {"$exit",null },
            //Timescale tasks (20.4)
            //{"$printtimescale",null },
            //{"$timeformat",null },
            //Severity tasks (20.10)
            //{"$fatal",null },
            //{"$error",null },
            //{"$warning",null },
            //{"$info",null },
            //Elaboration tasks (20.11)
            //{"$fatal",null },
            //{"$error",null },
            //{"$warning",null },
            //{"$info",null },
            //Assertion control tasks (20.12)
            {"$asserton",null },
            {"$assertoff",null },
            {"$assertkill",null },
            {"$assertcontrol",null },
            {"$assertpasson",null },
            {"$assertpassoff",null },
            {"$assertfailon",null },
            {"$assertfailoff",null },
            {"$assertnonvacuouson",null },
            {"$assertvacuousoff",null },
            //Stochastic analysis tasks (20.16)
            //{"$q_add",null },
            //{"$q_remove",null },
            //{"$q_initialize",null },
            //{"$q_exam",null },
            //PLA modeling tasks (20.17)
            //{"$async$and$array",null },
            //{"$async$and$plane",null },
            //{"$async$nand$array",null },
            //{"$async$nand$plane",null },
            //{"$async$or$array",null },
            //{"$async$or$plane",null },
            //{"$async$nor$array",null },
            //{"$async$nor$plane",null },
            //{"$sync$and$array",null },
            //{"$sync$and$plane",null },
            //{"$sync$nand$array",null },
            //{"$sync$nand$plane",null },
            //{"$sync$or$array",null },
            //{"$sync$or$plane",null },
            //{"$sync$nor$array",null },
            //{"$sync$nor$plane",null },

            // Command line input
            {"$test$plusargs",null },
            {"$value$plusargs",null },

            //Miscellaneous tasks and functions (20.18)
            {"$system",null },

            //Formatting data to a string (21.3.3)
            {"$sformat",null },

            // undefined in systemverilog standard
        };
        /*

         
         */

        public Dictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>?> SystemFunctions = new Dictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>?>
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


            //SystemVerilog
            // 18.13 Random number system functions and methods
            {"$urandom",null },
            {"$urandom_range",null },

            //Simulation time functions (20.3)
            //{"$realtime",null },
            //{"$stime",null },
            //{"$time",null },
            //Conversion functions (20.5)
            //{"$bitstoreal",null },
            //{"$realtobits",null },
            {"$bitstoshortreal",null },
            {"$shortrealtobits",null },
            //{"$itor",null },
            //{"$rtoi",null },
            //{"$signed",null },
            //{"$unsigned",null },
            {"$cast",null },
            //Data query functions (20.6)
            {"$bits",null },
            {"$isunbounded",null },
            {"$typename",null },
            //Array query functions (20.7)
            {"$unpacked_dimensions",null },
            {"$dimensions",null },
            {"$left",null },
            {"$right",null },
            {"$low",null },
            {"$high",null },
            {"$increment",null },
            {"$size",null },
            //Math functions (20.8)
            {"$clog2",null },
            {"$asin",null },
            {"$ln",null },
            {"$acos",null },
            {"$log10",null },
            {"$atan",null },
            {"$exp",null },
            {"$atan2",null },
            {"$sqrt",null },
            {"$hypot",null },
            {"$pow",null },
            {"$sinh",null },
            {"$floor",null },
            {"$cosh",null },
            {"$ceil",null },
            {"$tanh",null },
            {"$sin",null },
            {"$asinh",null },
            {"$cos",null },
            {"$acosh",null },
            {"$tan",null },
            {"$atanh",null },
            //Bit vector system functions (20.9)
            {"$countbits",null },
            {"$countones",null },
            {"$onehot",null },
            {"$onehot0",null },
            {"$isunknown",null },

            //Sampled value system functions (20.13)
            {"$sampled",null },
            {"$rose",null },
            {"$fell",null },
            {"$stable",null },
            {"$changed",null },
            {"$past",null },
            {"$past_gclk",null },
            {"$rose_gclk",null },
            {"$fell_gclk",null },
            {"$stable_gclk",null },
            {"$changed_gclk",null },
            {"$future_gclk",null },
            {"$rising_gclk",null },
            {"$falling_gclk",null },
            {"$steady_gclk",null },
            {"$changing_gclk",null },
            //Coverage control functions (20.14)
            {"$coverage_control",null },
            {"$coverage_get_max",null },
            {"$coverage_get",null },
            {"$coverage_merge",null },
            {"$coverage_save",null },
            {"$get_coverage",null },
            {"$set_coverage_db_name",null },
            {"$load_coverage_db",null },
            //Probabilistic distribution functions (20.15)
            //{"$random",null },
            //{"$dist_chi_square",null },
            //{"$dist_erlang",null },
            //{"$dist_exponential",null },
            //{"$dist_normal",null },
            //{"$dist_poisson",null },
            //{"$dist_t",null },
            //{"$dist_uniform",null },
            //Stochastic analysis functions (20.16)
            //{"$q_full",null },
            //Formatting data to a string (21.3.3)
            {"$sformatf",null },
            // I/O error status(21.3.7)
            {"$feof",null },

        };


    }
}
