using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
// SnippetBuilder now lives in the parent namespace
// (pluginVerilog.Data.VerilogCommon), which is already in scope here.

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "modport generate" tool: a modport listing every logic
    /// signal of the enclosing interface, with the direction flipped for
    /// signals an existing modport already drives.
    ///
    /// <para>
    /// The text is built by <see cref="Build"/> here; <see cref="SnippetRunner"/>
    /// inserts it and walks the fields.
    /// </para>
    /// </summary>
    public class ModPortSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ModPortSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.ModPort)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.ModPort).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            CoreBridge.SnippetAdapter.SnippetResult result)
        {
            Verilog.BuildingBlocks.BuildingBlock? block = parsedDocument.GetBuildingBlockAt(index);
            if (block is not Verilog.BuildingBlocks.Interface interface_)
            {
                result.Message = "the caret is not inside an interface";
                return;
            }

            // logic signals default to output; a signal an existing modport
            // already drives becomes an input (the same rule as the editor).
            Dictionary<string, Verilog.ModPort.Port.DirectionEnum> directions = new();
            foreach (Verilog.INamedElement element in interface_.NamedElements.Values)
            {
                if (element is Verilog.DataObjects.Variables.Logic)
                {
                    directions[element.Name] = Verilog.ModPort.Port.DirectionEnum.Output;
                }
            }
            if (directions.Count == 0)
            {
                result.Message = "the interface has no logic signal to expose";
                return;
            }

            foreach (Verilog.ModPort modport in interface_.NamedElements.Values.OfType<Verilog.ModPort>())
            {
                foreach (Verilog.ModPort.Port port in modport.Ports.Values)
                {
                    string expName = port.Expression.CreateString();
                    if (!directions.ContainsKey(expName)) continue;
                    if (port.Direction == Verilog.ModPort.Port.DirectionEnum.Output
                        || port.Direction == Verilog.ModPort.Port.DirectionEnum.Inout)
                    {
                        directions[expName] = Verilog.ModPort.Port.DirectionEnum.Input;
                    }
                }
            }

            string indent = SnippetBuilder.GetIndent(parsedDocument, index);
            CoreBridge.SnippetTemplate template = new CoreBridge.SnippetTemplate();
            template.AppendText(indent + "modport ");
            template.AddField("modport-name", "modportname");
            template.AppendText(" (\n");
            bool first = true;
            foreach (KeyValuePair<string, Verilog.ModPort.Port.DirectionEnum> entry in directions)
            {
                if (!first) template.AppendText(",\n");
                template.AppendText(indent + "\t");
                switch (entry.Value)
                {
                    case Verilog.ModPort.Port.DirectionEnum.Input:
                        template.AppendText("input");
                        break;
                    case Verilog.ModPort.Port.DirectionEnum.Inout:
                        template.AppendText("inout");
                        break;
                    default:
                        template.AppendText("output");
                        break;
                }
                template.AppendText("\t." + entry.Key);
                first = false;
            }
            if (!first) template.AppendText("\n");
            template.AppendText(indent + ")");

            List<CoreBridge.SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new CoreBridge.SnippetAdapter.SnippetEntry
            {
                Label = "modport generate",
                Detail = directions.Count + " signal(s)",
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
