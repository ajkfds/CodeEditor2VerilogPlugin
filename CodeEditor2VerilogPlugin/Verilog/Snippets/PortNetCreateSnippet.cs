using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "portConnectionCreate" tool: rewrites the module
    /// instantiation under the caret, filling every port connection (and
    /// declaring the nets the connections need).
    ///
    /// <para>
    /// The text is built by <see cref="Build"/> here; <see cref="SnippetRunner"/>
    /// inserts it and walks the fields.
    /// </para>
    /// </summary>
    public class PortConnectionCreateSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public PortConnectionCreateSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.PortConnectionCreate)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.PortConnectionCreate).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            CoreBridge.SnippetAdapter.SnippetResult result)
        {
            Verilog.Items.ModuleInstantiation? instantiation =
                SnippetBuilder.FindInstantiationAt(parsedDocument, index);
            if (instantiation == null)
            {
                result.Message = "the caret is not inside a module instantiation";
                return;
            }
            if (instantiation.LastIndexReference == null) return;

            Verilog.BuildingBlocks.Module? module =
                instantiation.GetInstancedBuildingBlock() as Verilog.BuildingBlocks.Module;
            if (module == null)
            {
                result.Message = "the instantiated module could not be resolved";
                return;
            }

            int start = SnippetBuilder.LastIndex(instantiation.BeginIndexReference);
            int end = SnippetBuilder.LastIndex(instantiation.LastIndexReference) + 1;
            string indent = SnippetBuilder.GetIndent(parsedDocument, start);
            CoreBridge.SnippetTemplate template =
                SnippetBuilder.BuildInstantiationTemplate(module, instantiation.Name, indent, instantiation);

            List<CoreBridge.SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new CoreBridge.SnippetAdapter.SnippetEntry
            {
                Label = "portConnectionCreate",
                Detail = "fill the connections of " + instantiation.Name,
                Kind = CoreBridge.SnippetAdapter.SnippetKind.Replace,
                Start = start,
                End = end,
                SnippetText = template.ToVscodeSnippet(),
                PlainText = plainText,
                Fields = fields,
            });
        }
    }
}
