using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Parser
{
    public class VerilogParser : DocumentParser
    {
        // create parser
        [SetsRequiredMembers]
        public VerilogParser(
            Data.IVerilogRelatedFile verilogRelatedFile,
            DocumentParser.ParseModeEnum parseMode
            ) : base(verilogRelatedFile.ToTextFile(),parseMode)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            this.ParseMode = parseMode;

            CodeEditor2.Data.TextFile? textFile = verilogRelatedFile as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            this.TextFile = textFile;

            VerilogFile? verilogFile = verilogRelatedFile as VerilogFile;
            if(verilogFile == null) 
            {
                VerilogModuleInstance? verilogModuleInstance = verilogRelatedFile as VerilogModuleInstance;
                if (verilogModuleInstance != null) verilogFile = verilogModuleInstance.SourceVerilogFile;
            }
            if (verilogFile == null) throw new Exception();

            fileRef = new WeakReference<Data.VerilogFile>(verilogFile);

            parsedDocument = new Verilog.ParsedDocument(verilogRelatedFile,null, parseMode);
            parsedDocument.Version = verilogRelatedFile.CodeDocument.Version;

            // swap CodeDocument to new one
            CodeEditor.CodeDocument originalCodeDocument = parsedDocument.CodeDocument;

            parsedDocument.CodeDocument = new CodeEditor.CodeDocument(verilogRelatedFile); // use verilog codeDocument
            parsedDocument.CodeDocument.CopyTextOnlyFrom(originalCodeDocument);
            this.Document = parsedDocument.CodeDocument;
            
            if (verilogRelatedFile is Data.VerilogFile)
            {
                VerilogFile? file = verilogRelatedFile as Data.VerilogFile;
                if (file == null) throw new Exception();
                if(file.SystemVerilog) parsedDocument.SystemVerilog = true;
            }

            if (verilogRelatedFile is Data.VerilogModuleInstance)
            {
                VerilogModuleInstance? file = verilogRelatedFile as Data.VerilogModuleInstance;
                if (file == null) throw new Exception();
                if (file.SystemVerilog) parsedDocument.SystemVerilog = true;
            }

             word = new Verilog.WordScanner(VerilogDocument, parsedDocument, parsedDocument.SystemVerilog);

//            System.Diagnostics.Debug.Print("Parser Construct " + sw.ElapsedMilliseconds.ToString());
        }

        // create parser with parameter override
        [SetsRequiredMembers]
        public VerilogParser(
            Data.IVerilogRelatedFile verilogRelatedFile,
            string moduleName,
            Dictionary<string, Verilog.Expressions.Expression> parameterOverrides,
            DocumentParser.ParseModeEnum parseMode
            ) : base(verilogRelatedFile.ToTextFile(), parseMode)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (verilogRelatedFile == null) throw new Exception();
            if (verilogRelatedFile.CodeDocument == null) throw new Exception();

            this.ParseMode = parseMode;
            CodeEditor2.Data.TextFile? textFile = verilogRelatedFile as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            this.TextFile = textFile;

            VerilogFile? verilogFile = verilogRelatedFile as VerilogFile;
            Data.VerilogModuleInstance? verilogModuleInstance = verilogRelatedFile as Data.VerilogModuleInstance;
            if (verilogFile == null)
            {
                if (verilogModuleInstance != null) verilogFile = verilogModuleInstance.SourceVerilogFile;
            }
            if (verilogFile == null) throw new Exception();

            fileRef = new WeakReference<Data.VerilogFile>(verilogFile);
            
            parsedDocument = new Verilog.ParsedDocument(verilogRelatedFile,null, parseMode);

            // swap CodeDocument to new one
            CodeEditor.CodeDocument originalCodeDocument = parsedDocument.CodeDocument;
            parsedDocument.CodeDocument = new CodeEditor.CodeDocument(verilogRelatedFile); // use verilog codeDocument
            parsedDocument.CodeDocument.CopyTextOnlyFrom(originalCodeDocument);
            this.Document = parsedDocument.CodeDocument;

            if (
                (verilogFile != null && verilogFile.SystemVerilog) ||
                (verilogModuleInstance != null && verilogModuleInstance.SystemVerilog)
            )                
            {
                parsedDocument.SystemVerilog = true;
            }
            parsedDocument.Version = verilogRelatedFile.CodeDocument.Version;
            parsedDocument.Instance = true;
            parsedDocument.ParameterOverrides = parameterOverrides;
            parsedDocument.TargetBuildingBlockName = moduleName;
            word = new Verilog.WordScanner(VerilogDocument, parsedDocument, parsedDocument.SystemVerilog);

//            System.Diagnostics.Debug.Print("Parser Construct " + sw.ElapsedMilliseconds.ToString());
        }


        public Verilog.WordScanner word;

        public CodeEditor.CodeDocument VerilogDocument
        {
            get
            {
                CodeEditor.CodeDocument? document = Document as CodeEditor.CodeDocument;
                if (document == null) throw new Exception();
                return document;
            }
        }

        private Verilog.ParsedDocument parsedDocument;
        public override CodeEditor2.CodeEditor.ParsedDocument ParsedDocument {
            get {
                return parsedDocument as CodeEditor2.CodeEditor.ParsedDocument; 
            } 
        }
        public virtual Verilog.ParsedDocument VerilogParsedDocument {
            get {
                return parsedDocument; 
            } 
        }

        //        private Dictionary<string, Verilog.Expressions.Expression> parameterOverrides;
        //        private string targetModuleName = null;

        private System.WeakReference<VerilogFile> fileRef;
        public VerilogFile? File
        {
            get
            {
                VerilogFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            //protected set
            //{
            //    fileRef = new WeakReference<Data.IVerilogRelatedFile>(value);
            //}
        }


        /* Verilog 2001
            source_text ::= { description }
            description ::= module_declaration
                            | udp_declaration
            module_declaration ::=      { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                        [ list_of_ports ] ; { module_item }
                                        endmodule

                                        | { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                        [ list_of_port_declarations ] ; { non_port_module_item }
                                        endmodule
            module_keyword ::= module | macromodule
        */

        /* System Verilog 2012
        source_text ::= [ timeunits_declaration ] { description } 
        description ::= 
              module_declaration 
            | udp_declaration 
            | interface_declaration 
            | program_declaration         
            | package_declaration 
            | { attribute_instance } package_item 
            | { attribute_instance } bind_directive 
            | config_declaration 

        module_nonansi_header ::= 
            { attribute_instance } module_keyword [ lifetime ] module_identifier 
            { package_import_declaration } [ parameter_port_list ] list_of_ports ;

        module_ansi_header ::= 
            { attribute_instance } module_keyword [ lifetime ] module_identifier 
            { package_import_declaration }1 [ parameter_port_list ] [ list_of_port_declarations ] ;

        module_declaration ::= 
                module_nonansi_header [ timeunits_declaration ] { module_item } 
                endmodule [ : module_identifier ] 

                | module_ansi_header [ timeunits_declaration ] { non_port_module_item } 
                endmodule [ : module_identifier ] 

                | { attribute_instance } module_keyword [ lifetime ] module_identifier ( .* ) ;
                [ timeunits_declaration ] { module_item } endmodule [ : module_identifier ] 

                | extern module_nonansi_header 
                | extern module_ansi_header 
        module_keyword ::= module | macromodule

        */

        public override void Parse()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            word.GetFirst();

            word.RootParsedDocument.LockedDocument.Add(word.Document);
            if((File as Data.VerilogFile) == null)
            {
                System.Diagnostics.Debugger.Break();
            }
            Root root = Root.ParseCreate(word,VerilogParsedDocument, File as Data.VerilogFile);
            //Document = word.RootParsedDocument.CodeDocument;

            word.RootParsedDocument.UnlockDocument();
            word.Dispose();

//            System.Diagnostics.Debug.Print("Parse " + sw.ElapsedMilliseconds.ToString());
        }
    }
}
