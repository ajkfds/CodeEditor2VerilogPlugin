using System.Collections.Generic;
using System.Threading.Tasks;
// Moved out of pluginVerilog.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using pluginVerilog.Verilog;
// SnippetBuilder now lives in the parent namespace
// (pluginVerilog.Data.VerilogCommon), which is already in scope here.

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "moduleInstance" snippet for one named module: the full
    /// instantiation (parameter overrides + one named port connection per
    /// port).
    ///
    /// <para>
    /// The text is built by <see cref="Build"/> here, which the VS Code
    /// extension also uses (through <c>rtl/snippet</c> and the completion
    /// adapter), so the editor and the extension insert the same
    /// instantiation.
    /// </para>
    /// </summary>
    public class ModuleInstanceSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        private readonly string moduleName;

        public ModuleInstanceSnippet(string moduleName) : base(moduleName)
        {
            this.moduleName = moduleName;
            // TryGetSvgBitmap: the snippet is also constructed while PARSING
            // (CompletionContext.AppendModuleInstanceSnippets builds one per
            // module name at the start of a module-body line), and the parser
            // runs in the headless language server too, where Avalonia's asset
            // loader is absent. A throwing icon lookup aborted the whole
            // textDocument/completion request there, so the module-name
            // candidates never reached the client.
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/module.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.ModuleInstance, moduleName).ApplyAsync();
        }

        /// <summary>
        /// Builds the snippet for the caret at <paramref name="index"/>. Without
        /// <paramref name="moduleName"/> the candidate module names are returned
        /// so the host can show a picker.
        /// </summary>
        public static void Build(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            string? moduleName,
            CoreBridge.SnippetAdapter.SnippetResult result)
        {
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null)
            {
                result.Message = "the project has no Verilog property";
                return;
            }

            if (string.IsNullOrEmpty(moduleName))
            {
                // no module chosen yet: hand the candidates to the host
                foreach (string name in projectProperty.DefinitionNameSpace.GetNameList(
                             x => x is Verilog.BuildingBlocks.Module))
                {
                    result.Candidates.Add(name);
                }
                if (result.Candidates.Count == 0) result.Message = "no module found in the project";
                return;
            }

            Verilog.BuildingBlocks.Module? module = SnippetBuilder.ResolveModule(projectProperty, moduleName);
            if (module == null)
            {
                result.Message = "module not found: " + moduleName;
                return;
            }

            string instanceName = SnippetBuilder.MakeUniqueInstanceName(parsedDocument, index, moduleName);
            string indent = SnippetBuilder.GetIndent(parsedDocument, index);
            CoreBridge.SnippetTemplate template =
                SnippetBuilder.BuildInstantiationTemplate(module, instanceName, indent, null);

            List<CoreBridge.SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new CoreBridge.SnippetAdapter.SnippetEntry
            {
                Label = moduleName,
                Detail = "instantiate " + moduleName,
                Kind = CoreBridge.SnippetAdapter.SnippetKind.Insert,
                Start = index,
                End = index,
                SnippetText = template.ToVscodeSnippet(),
                PlainText = plainText,
                Fields = fields,
            });
        }
    }
}
