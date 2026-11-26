using CodeEditor2.CodeEditor.Parser;
using pluginVerilog.Data;
using pluginVerilog.Data.VerilogCommon;
using SystemVerilogCore.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace pluginVerilog.Parser
{
    public class VerilogParser : DocumentParser
    {
        /// <summary>
        /// Original Verilog File Parser Generator
        /// </summary>
        /// <param name="verilogRelatedFile"></param>
        /// <param name="parseMode"></param>
        /// <param name="token"></param>
        /// <exception cref="Exception"></exception>
        [SetsRequiredMembers]
        public VerilogParser(
            Data.IVerilogRelatedFile verilogRelatedFile,
            DocumentParser.ParseModeEnum parseMode,
            System.Threading.CancellationToken? token
            ) : base(verilogRelatedFile.ToTextFile(), parseMode, token)
        {
            this.ParseMode = parseMode;

            CodeEditor2.Data.TextFile? textFile = verilogRelatedFile as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            this.TextFile = textFile;

            VerilogFile? verilogFile = verilogRelatedFile as VerilogFile;
            if (verilogFile == null)
            {
                VerilogModuleInstance? verilogModuleInstance = verilogRelatedFile as VerilogModuleInstance;
                if (verilogModuleInstance != null) verilogFile = verilogModuleInstance.SourceVerilogFile;
            }
            if (verilogFile == null) throw new Exception();

            // WeakReference<T> is invariant, so it has to be created for the
            // contract type the property exposes.
            fileRef = new WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile>(verilogFile);

            string key = Verilog.ParsedDocument.KeyGenerator(verilogRelatedFile, null, null);
            parsedDocument = new Verilog.ParsedDocument(
                verilogRelatedFile, key, null, DocumentParser.ToVerilogParseMode(parseMode));
            parsedDocument.Version = verilogRelatedFile.CodeDocument.Version;

            // swap the buffer to a fresh one owned by this file
            //
            // The parse result only exposes the UI-free document contract, so the
            // editor document is built here and published through it. This used
            // to read/write a concrete `CodeDocument` property on the parse
            // result, which kept the parser bound to the editor assembly.
            CodeEditor.CodeDocument? originalCodeDocument = parsedDocument.EditorCodeDocument();

            CodeEditor.CodeDocument codeDocument = new CodeEditor.CodeDocument(verilogRelatedFile);
            if (originalCodeDocument != null) codeDocument.CopyTextOnlyFrom(originalCodeDocument);
            parsedDocument.VerilogDocument = codeDocument;
            this.Document = codeDocument;

            if (verilogRelatedFile is Data.VerilogFile)
            {
                VerilogFile? file = verilogRelatedFile as Data.VerilogFile;
                if (file == null) throw new Exception();
                if (file.SystemVerilog) parsedDocument.SystemVerilog = true;
            }

            if (verilogRelatedFile is Data.VerilogModuleInstance)
            {
                VerilogModuleInstance? file = verilogRelatedFile as Data.VerilogModuleInstance;
                if (file == null) throw new Exception();
                if (file.SystemVerilog) parsedDocument.SystemVerilog = true;
            }

            word = new Verilog.WordScanner(VerilogDocument, parsedDocument, parsedDocument.SystemVerilog);
            word.CancellationToken = token;

            //            System.Diagnostics.Debug.Print("Parser Construct " + sw.ElapsedMilliseconds.ToString());
        }

        /// <summary>
        /// Create Instance Buliding Block Parser
        /// </summary>
        /// <param name="verilogRelatedFile"></param>
        /// <param name="moduleName"></param>
        /// <param name="parameterOverrides"></param>
        /// <param name="parseMode"></param>
        /// <param name="token"></param>
        /// <exception cref="Exception"></exception>
        // create parser with parameter override
        [SetsRequiredMembers]
        public VerilogParser(
            Data.IVerilogRelatedFile verilogRelatedFile,
            string moduleName,
            Dictionary<string, Verilog.Expressions.Expression> parameterOverrides,
            DocumentParser.ParseModeEnum parseMode,
            System.Threading.CancellationToken? token
            ) : base(verilogRelatedFile.ToTextFile(), parseMode, token)
        {

            if (verilogRelatedFile == null) throw new Exception();

            // A missing buffer is not fatal: the instance wrapper delegates its
            // CodeDocument to the source file, and a host that drives the parse
            // itself (the language server) fills that lazily. An empty document is
            // enough to start from - the parser is handed the text separately.
            if (verilogRelatedFile.CodeDocument == null)
            {
                verilogRelatedFile.CreateCodeDocumentForParse();
            }

            this.ParseMode = parseMode;
            CodeEditor2.Data.TextFile? textFile = verilogRelatedFile as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            this.TextFile = textFile;

            bool systemVerilog = false;

            VerilogFile? verilogFile = verilogRelatedFile as VerilogFile;
            if (verilogFile != null && verilogFile.SystemVerilog) systemVerilog = true;

            if (verilogFile == null)
            {
                VerilogModuleInstance? verilogModuleInstance = verilogRelatedFile as VerilogModuleInstance;
                if (verilogModuleInstance != null) verilogFile = verilogModuleInstance.SourceVerilogFile;
                if (verilogModuleInstance != null && verilogModuleInstance.SystemVerilog) systemVerilog = true;
            }
            if (verilogFile == null)
            {
                Data.ImportedPackage? importedPackage = verilogRelatedFile as Data.ImportedPackage;
                if (importedPackage != null) verilogFile = importedPackage.SourceVerilogFile;
                if (importedPackage != null && importedPackage.SystemVerilog) systemVerilog = true;
            }

            if (verilogFile == null) throw new Exception();

            fileRef = new WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile>(verilogFile);
            string key = Verilog.ParsedDocument.KeyGenerator(verilogFile, moduleName, parameterOverrides);

            parsedDocument = new Verilog.ParsedDocument(
                verilogRelatedFile, key, null, DocumentParser.ToVerilogParseMode(parseMode));

            // swap the buffer to a fresh one owned by this file
            // (see the remark in the other constructor: the parse result exposes
            // only the UI-free document contract now)
            CodeEditor.CodeDocument? originalCodeDocument = parsedDocument.EditorCodeDocument();
            CodeEditor.CodeDocument codeDocument = new CodeEditor.CodeDocument(verilogRelatedFile);
            if (originalCodeDocument != null) codeDocument.CopyTextOnlyFrom(originalCodeDocument);
            parsedDocument.VerilogDocument = codeDocument;
            this.Document = codeDocument;

            parsedDocument.SystemVerilog = systemVerilog;
            parsedDocument.Version = verilogRelatedFile.CodeDocument?.Version ?? 0;
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
        public override SystemVerilogCore.Parsing.IParsedDocument ParsedDocument
        {
            get
            {
                return parsedDocument;
            }
        }
        public virtual Verilog.ParsedDocument VerilogParsedDocument
        {
            get
            {
                return parsedDocument;
            }
        }

        //        private Dictionary<string, Verilog.Expressions.Expression> parameterOverrides;
        //        private string targetModuleName = null;

        /// <summary>
        /// The file being parsed, as the UI-free source-file contract.
        ///
        /// <para>
        /// This used to be the editor's concrete <c>Data.VerilogFile</c>, and
        /// <see cref="ParseAsync"/> even asserted the concrete type before parsing.
        /// The parse only hands the file to <c>Root.ParseCreateAsync</c>, which
        /// stores it as <c>IVerilogSourceFile</c>, so the concrete type was never
        /// needed - it just kept the parser bound to the editor assembly.
        /// </para>
        /// </summary>
        private System.WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile> fileRef;
        public SystemVerilogCore.Documents.IVerilogSourceFile? File
        {
            get
            {
                SystemVerilogCore.Documents.IVerilogSourceFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
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

        public override async System.Threading.Tasks.Task ParseAsync(CancellationToken? cancellationToken)
        {
            word.GetFirst();
            word.CancellationToken = cancellationToken;
            // The parse only stores the file (as IVerilogSourceFile) and hands it to
            // the building blocks, so the UI-free contract is enough here; the
            // concrete VerilogFile assertion is gone with the type.
            if (File == null) throw new Exception();

            Root root = await Root.ParseCreateAsync(word, VerilogParsedDocument, File);
        }
    }
}
