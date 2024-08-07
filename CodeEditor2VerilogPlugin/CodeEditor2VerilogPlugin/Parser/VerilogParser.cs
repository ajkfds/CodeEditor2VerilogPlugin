﻿using CodeEditor2.CodeEditor.Parser;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Parser
{
    public class VerilogParser : DocumentParser
    {
        // create parser
        public VerilogParser(
            Data.IVerilogRelatedFile verilogRelatedFile,
            DocumentParser.ParseModeEnum parseMode
            )
        {
            this.ParseMode = parseMode;

            CodeEditor2.Data.TextFile? textFile = verilogRelatedFile as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            this.TextFile = textFile;

            File = verilogRelatedFile;
            parsedDocument = new Verilog.ParsedDocument(verilogRelatedFile,null, parseMode);
            parsedDocument.Version = verilogRelatedFile.CodeDocument.Version;

            // swap CodeDocument to new one
            CodeEditor.CodeDocument originalCodeDocument = parsedDocument.CodeDocument;

            parsedDocument.CodeDocument = new CodeEditor.CodeDocument(verilogRelatedFile); // use verilog codeDocument
            parsedDocument.CodeDocument.CopyTextOnlyFrom(originalCodeDocument);
            this.document = parsedDocument.CodeDocument;

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
        }

        // create parser with parameter override
        public VerilogParser(
            Data.IVerilogRelatedFile verilogRelatedFile,
            string moduleName,
            Dictionary<string, Verilog.Expressions.Expression> parameterOverrides,
            DocumentParser.ParseModeEnum parseMode
            ) : base(verilogRelatedFile as CodeEditor2.Data.TextFile, parseMode)
        {
            this.document = new CodeEditor.CodeDocument(verilogRelatedFile); // use verilog codeDocument
            this.document.CopyTextOnlyFrom(verilogRelatedFile.CodeDocument);

            this.ParseMode = parseMode;
            CodeEditor2.Data.TextFile? textFile = verilogRelatedFile as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            this.TextFile = textFile;

            File = verilogRelatedFile;
            parsedDocument = new Verilog.ParsedDocument(verilogRelatedFile,null, parseMode);
            if(
                (verilogRelatedFile is Data.VerilogFile && (verilogRelatedFile as Data.VerilogFile).SystemVerilog) ||
                (verilogRelatedFile is Data.VerilogModuleInstance && (verilogRelatedFile as Data.VerilogModuleInstance).SystemVerilog)
            )                
            {
                parsedDocument.SystemVerilog = true;
            }
            parsedDocument.Version = verilogRelatedFile.CodeDocument.Version;
            parsedDocument.Instance = true;
            parsedDocument.ParameterOverrides = parameterOverrides;
            parsedDocument.TargetBuildingBlockName = moduleName;
            word = new Verilog.WordScanner(VerilogDocument, parsedDocument, parsedDocument.SystemVerilog);
        }


        public Verilog.WordScanner word;

        public CodeEditor.CodeDocument VerilogDocument
        {
            get
            {
                return Document as CodeEditor.CodeDocument;
            }
        }

        private Verilog.ParsedDocument parsedDocument = null;
        public override CodeEditor2.CodeEditor.ParsedDocument ParsedDocument { get { return parsedDocument as CodeEditor2.CodeEditor.ParsedDocument; } }
        public virtual Verilog.ParsedDocument VerilogParsedDocument { get { return parsedDocument; } }

//        private Dictionary<string, Verilog.Expressions.Expression> parameterOverrides;
//        private string targetModuleName = null;

        private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
        public Data.IVerilogRelatedFile File
        {
            get
            {
                Data.IVerilogRelatedFile ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
            {
                fileRef = new WeakReference<Data.IVerilogRelatedFile>(value);
            }
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
            word.GetFirst();

            word.RootParsedDocument.LockedDocument.Add(word.Document);
            Root root = Root.ParseCreate(word,VerilogParsedDocument,File as Data.VerilogFile);

            word.RootParsedDocument.UnlockDocument();
//            word.Document.LockThreadToUI();
            word.Dispose();
            word = null;
        }
    }
}
