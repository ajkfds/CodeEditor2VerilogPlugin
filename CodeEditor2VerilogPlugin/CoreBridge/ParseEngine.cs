
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.BuildingBlocks;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free parse engine.
    /// Drives the real VerilogParser on in-memory text and returns the
    /// resulting Verilog.ParsedDocument without touching the editor UI.
    /// Hosts without an Avalonia dispatcher (LSP server, unit tests) use
    /// this entry point instead of TextFile.PostParse / ParseWorker.
    /// </summary>
    public static class ParseEngine
    {
        /// <summary>
        /// Parse the given SystemVerilog text and return the parsed document.
        /// The parse itself (WordScanner / Root.ParseCreateAsync) is UI-free;
        /// only the editor-side acceptance (color marks, refresh) touches the
        /// UI, and that path is not invoked here.
        /// </summary>
        public static async Task<Verilog.ParsedDocument?> ParseSystemVerilogAsync(
            string text,
            Data.VerilogFile file,
            CancellationToken? cancellationToken = null
            )
        {
            if (text == null) return null;

            // The DocumentParser base constructor copies the text from
            // file.CodeDocument. In UI-less hosts FileCheckAsync has not run,
            // so create the code document here (the CodeDocument setter is
            // protected; CreateCodeDocument is the intended factory).
            if (file.CodeDocument == null)
            {
                System.Reflection.MethodInfo? create = file.GetType().GetMethod(
                    "CreateCodeDocument",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                create?.Invoke(file, null);
            }

            // Register root-level elements into project namespaces, mirroring
            // VerilogFile.AcceptParsedDocumentAsync (which is not executed in
            // UI-less hosts). Without this, cross-object lookups such as
            // Variables.Object.GetSourceClass() (UnitNameSpace.GetFile) cannot
            // resolve class members and every member access reports
            // "unfound object".
            //
            // The parse is run up to two passes: a hierarchical forward
            // reference such as "->> top.e" where the target module "top" is
            // declared later in the same file cannot resolve during the first
            // pass (the module is not yet registered). The first pass registers
            // the building-block names, and the second pass resolves the
            // forward references (same behaviour as the editor's multi-pass
            // ParseHierarchy).
            // first pass
            Verilog.ParsedDocument? parsedDocument = await ParseOnceAsync(text, file, cancellationToken);
            if (parsedDocument == null) return null;

            // The first pass has registered the file's own building blocks into
            // the project namespaces. A hierarchical forward reference such as
            // "->> top.e", where the target module "top" is declared later in
            // the same file, cannot resolve during the first pass (the module is
            // not yet registered) and produces an error. When the first pass
            // reported any error, run a second pass so the forward references
            // resolve (same effect as the editor's multi-pass ParseHierarchy).
            if (parsedDocument.ErrorCount > 0)
            {
                Verilog.ParsedDocument? second = await ParseOnceAsync(text, file, cancellationToken);
                if (second != null) parsedDocument = second;
            }

            return parsedDocument;
        }

        /// <summary>
        /// Parses the given text AS a building-block instance: the target module
        /// is parsed with <paramref name="parameterOverrides"/> applied, exactly
        /// like the editor does when a module instance is opened
        /// (<c>VerilogModuleInstance.CreateDocumentParser</c>).
        ///
        /// <para>
        /// A plain file parse uses the declared default parameter values, so a
        /// module instantiated with <c>#(.P_WIDTH(5))</c> would still be shown
        /// with <c>P_WIDTH = 8</c> (its default) and every width derived from it
        /// would be wrong. The editor opens the INSTANCE, not the file, which is
        /// why its diagnostics / colouring reflect the override; this entry
        /// point gives a UI-less host the same result.
        /// </para>
        ///
        /// <para>
        /// Unlike <see cref="ParseSystemVerilogAsync"/> this does NOT register
        /// the parsed building block into the project name spaces: the file's
        /// plain parse already registered the default-parameter version, and
        /// overwriting it with one instance's override would corrupt the
        /// resolution of every other instance of the same module. The result is
        /// only used for the opened document's diagnostics / colouring.
        /// </para>
        /// </summary>
        public static async Task<Verilog.ParsedDocument?> ParseInstanceAsync(
            string text,
            Data.VerilogFile file,
            string moduleName,
            Dictionary<string, Expression> parameterOverrides,
            CancellationToken? cancellationToken = null
            )
        {
            if (text == null) return null;
            if (string.IsNullOrEmpty(moduleName)) return null;

            // The override constructor requires file.CodeDocument to be set
            // (it snapshots the version). In UI-less hosts FileCheckAsync has
            // not run, so create it here (same as ParseSystemVerilogAsync).
            if (file.CodeDocument == null)
            {
                System.Reflection.MethodInfo? create = file.GetType().GetMethod(
                    "CreateCodeDocument",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                create?.Invoke(file, null);
            }
            if (file.CodeDocument == null) return null;

            pluginVerilog.Parser.VerilogParser parser = new pluginVerilog.Parser.VerilogParser(
                file,
                moduleName,
                parameterOverrides,
                CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse,
                cancellationToken
                );

            CodeEditor2.CodeEditor.CodeDocument sourceDocument = new CodeEditor2.CodeEditor.CodeDocument(file, text);
            parser.Document.CopyTextOnlyFrom(sourceDocument);

            await parser.ParseAsync(cancellationToken ?? CancellationToken.None);

            return parser.VerilogParsedDocument;
        }

        private static async System.Threading.Tasks.Task<Verilog.ParsedDocument?> ParseOnceAsync(
            string text,
            Data.VerilogFile file,
            CancellationToken? cancellationToken
            )
        {
            pluginVerilog.Parser.VerilogParser parser = new pluginVerilog.Parser.VerilogParser(
                file,
                CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse,
                cancellationToken
                );

            // load the target text into the parser's working document.
            // CodeDocument has no public text setter; CopyTextOnlyFrom copies
            // the raw text (and clears color/mark info) from another document.
            CodeEditor2.CodeEditor.CodeDocument sourceDocument = new CodeEditor2.CodeEditor.CodeDocument(file, text);
            parser.Document.CopyTextOnlyFrom(sourceDocument);

            // run the (UI-free) parse
            await parser.ParseAsync(cancellationToken ?? CancellationToken.None);

            Verilog.ParsedDocument? parsedDocument = parser.VerilogParsedDocument;
            if (parsedDocument == null) return null;

            if (parsedDocument.Root != null)
            {
                ProjectProperty projectProperty = file.ProjectProperty;
                foreach (var element in parsedDocument.Root.NamedElements.Values)
                {
                    if (element is Package package)
                    {
                        projectProperty.PackageNameSpace.Register(package.Name, package, file);
                        continue;
                    }

                    if (element is Class class_)
                    {
                        projectProperty.UnitNameSpace.Register(class_.Name, class_, file);
                        continue;
                    }

                    if (element is InterfaceClass interfaceClass)
                    {
                        projectProperty.UnitNameSpace.Register(interfaceClass.Name, interfaceClass, file);
                        continue;
                    }

                    if (element is BuildingBlock buildingBlock)
                    {
                        projectProperty.DefinitionNameSpace.Register(buildingBlock.Name, buildingBlock, file);
                        continue;
                    }

                    projectProperty.UnitNameSpace.Register(element.Name, element, file);
                }
            }

            file.ParsedDocument = parsedDocument;
            return parsedDocument;
        }
    }
}
