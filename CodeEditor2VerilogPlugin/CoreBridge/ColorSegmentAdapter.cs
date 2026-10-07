
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Converts the editor's per-line color segments (CodeDocument.TextColors,
    /// produced by the real parser) into a flat list of (index, length, type)
    /// color segments that UI-agnostic hosts (LSP semantic tokens) can
    /// consume. The RGB values are mapped back through the plugin's
    /// CodeDrawStyle palette so the resulting types are exactly the ones the
    /// editor would display.
    /// </summary>
    public static class ColorSegmentAdapter
    {
        /// <summary>A flat color segment (document-absolute index range).</summary>
        public struct Segment
        {
            public Segment(int index, int length, CodeDrawStyle.ColorType type)
            {
                Index = index;
                Length = length;
                Type = type;
            }
            public int Index;
            public int Length;
            public CodeDrawStyle.ColorType Type;
        }

        /// <summary>
        /// Walks the parsed document's TextColors line table and yields flat
        /// segments. Colors that do not belong to a recognizable ColorType
        /// (e.g. Normal) are skipped so hosts can layer their own defaults.
        /// </summary>
        public static List<Segment> GetSegments(Verilog.ParsedDocument parsedDocument)
        {
            List<Segment> segments = new List<Segment>();
            CodeEditor.CodeDocument? document = parsedDocument?.CodeDocument;
            if (document == null) return segments;

            CodeEditor2.CodeEditor.CodeDrawStyle drawStyle = document.TextFile?.DrawStyle as CodeEditor2.CodeEditor.CodeDrawStyle
                ?? new CodeDrawStyle();
            CodeDrawStyle pluginDrawStyle = drawStyle as CodeDrawStyle ?? new CodeDrawStyle();

            foreach (KeyValuePair<int, CodeEditor2.CodeEditor.TextDecollation.LineInformation> pair
                in document.TextColors.LineInformation)
            {
                int lineNumber = pair.Key;
                int lineStart = document.GetLineStartIndex(lineNumber);
                foreach (CodeEditor2.CodeEditor.TextDecollation.LineInformation.Color color in pair.Value.Colors)
                {
                    CodeDrawStyle.ColorType type = MatchColorType(color.DrawColor, pluginDrawStyle);
                    if (type == CodeDrawStyle.ColorType.Normal) continue; // default color, host decides
                    segments.Add(new Segment(lineStart + color.Offset, color.Length, type));
                }
            }
            return segments;
        }

        /// <summary>
        /// Maps an Avalonia color back to the plugin palette index by exact
        /// RGB match (the parser only ever writes palette colors).
        /// </summary>
        private static CodeDrawStyle.ColorType MatchColorType(Avalonia.Media.Color color, CodeDrawStyle pluginDrawStyle)
        {
            for (byte i = 0; i < 16; i++)
            {
                Avalonia.Media.Color palette = pluginDrawStyle.Color((CodeDrawStyle.ColorType)i);
                if (palette.R == color.R && palette.G == color.G && palette.B == color.B)
                {
                    return (CodeDrawStyle.ColorType)i;
                }
            }
            return CodeDrawStyle.ColorType.Normal;
        }
    }
}
