using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "alwaysFF" tool: an <c>always @(posedge clk or negedge
    /// rst)</c> block with the clock / reset taken from the ports the parser
    /// marked as clock / reset (SyncContext).
    ///
    /// <para>
    /// The text is built by <see cref="Build"/> here and inserted by
    /// <see cref="SnippetRunner"/>, so the desktop editor and the VS Code
    /// extension insert exactly the same block from one implementation.
    /// </para>
    ///
    /// <para>
    /// The clock and reset are real tab stops (not baked-in text) so the user
    /// can pick a different one: several annotated clocks / resets become a
    /// choice list (<c>${1|CLK_I,CLK2_I|}</c> in VS Code, the popup the desktop
    /// editor already opened). The <c>if(~reset)</c> is a <b>mirror</b> of the
    /// reset tab stop, so editing one updates the other - the desktop editor
    /// used to copy the text with a background task and the VS Code extension
    /// could not do it at all.
    /// </para>
    /// </summary>
    public class AlwaysFFSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AlwaysFFSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.AlwaysFF)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.AlwaysFF).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            CoreBridge.SnippetAdapter.SnippetResult result)
        {
            string indent = SnippetBuilder.GetIndent(parsedDocument, index);

            List<string> clocks = new();
            List<string> resets = new();
            Verilog.BuildingBlocks.BuildingBlock? block = parsedDocument.GetBuildingBlockAt(index);
            if (block is Verilog.IPortNameSpace portNameSpace)
            {
                foreach (Verilog.DataObjects.Port port in portNameSpace.PortsList)
                {
                    if (port.DataObject == null) continue;
                    if (port.DataObject.SyncContext.IsClock && !clocks.Contains(port.Name)) clocks.Add(port.Name);
                    if (port.DataObject.SyncContext.IsReset && !resets.Contains(port.Name)) resets.Add(port.Name);
                }
            }

            CoreBridge.SnippetTemplate template = new CoreBridge.SnippetTemplate();
            template.AppendText(indent + "always @(posedge ");
            template.AddField("clock", clocks.Count > 0 ? clocks[0] : "clock", clocks);
            template.AppendText(" or negedge ");
            int resetField = template.AddField("reset", resets.Count > 0 ? resets[0] : "reset_x", resets);
            template.AppendText(")\n");
            template.AppendText(indent + "begin\n");
            template.AppendText(indent + "\tif(~");
            template.AddMirror(resetField);
            template.AppendText(") begin\n");
            template.AppendText(indent + "\t\t");
            template.AddField("reset-branch", "");
            template.AppendText("\n");
            template.AppendText(indent + "\tend else begin\n");
            template.AppendText(indent + "\t\t");
            template.AddField("else-branch", "");
            template.AppendText("\n");
            template.AppendText(indent + "\tend\n");
            template.AppendText(indent + "end");

            List<CoreBridge.SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new CoreBridge.SnippetAdapter.SnippetEntry
            {
                Label = "alwaysFF",
                Detail = "always @(posedge " + (clocks.Count > 0 ? clocks[0] : "clock")
                    + " or negedge " + (resets.Count > 0 ? resets[0] : "reset_x") + ")",
                Kind = CoreBridge.SnippetAdapter.SnippetKind.Insert,
                Start = index,
                End = index,
                SnippetText = template.ToVscodeSnippet(),
                Fields = fields,
                PlainText = plainText,
            });
        }
    }
}
