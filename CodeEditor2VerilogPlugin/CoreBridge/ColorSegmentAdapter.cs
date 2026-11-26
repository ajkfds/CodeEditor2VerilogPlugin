
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Converts the parser's colour spans into a flat list of
    /// (index, length, type) segments that UI-agnostic hosts (LSP semantic
    /// tokens) can consume.
    ///
    /// <para>
    /// This used to walk the editor's concrete document
    /// (<c>CodeDocument.TextColors</c>) and map Avalonia colours back to the
    /// palette by exact RGB match, which forced every consumer - including the
    /// language server - to reference the editor assembly and Avalonia. The
    /// parser already writes palette indices
    /// (<see cref="SystemVerilogCore.Documents.IVerilogTextDocument.SetColorAt"/>),
    /// so the contract exposes them directly and no colour matching is needed.
    /// </para>
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
        /// Returns the parser's colour spans as flat segments. The default
        /// colour (Normal) is skipped so hosts can layer their own.
        /// </summary>
        public static List<Segment> GetSegments(Verilog.ParsedDocument parsedDocument)
        {
            List<Segment> segments = new List<Segment>();
            if (parsedDocument == null) return segments;

            foreach (SystemVerilogCore.Documents.VerilogColorSpan span
                     in parsedDocument.VerilogDocument.GetColorSpans())
            {
                CodeDrawStyle.ColorType type = (CodeDrawStyle.ColorType)span.ColorIndex;
                if (type == CodeDrawStyle.ColorType.Normal) continue; // default color, host decides
                segments.Add(new Segment(span.Index, span.Length, type));
            }
            return segments;
        }
    }
}
