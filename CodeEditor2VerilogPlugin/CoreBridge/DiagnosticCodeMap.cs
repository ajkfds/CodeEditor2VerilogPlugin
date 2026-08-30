using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Maps parser-emitted <see cref="Verilog.ParsedDocument.Message"/> text
    /// to the LSP-friendly diagnostic <c>code</c> field.
    ///
    /// The plugin does not currently store a code on each message, so we
    /// infer one from a small list of well-known substrings
    /// (<c>"undriven"</c>, <c>"unused"</c>, ...) and fall back to a
    /// normalised slug of the message text. The slug uses a stable prefix
    /// so that client-side rule suppressions can be written against it.
    /// </summary>
    internal static class DiagnosticCodeMap
    {
        private const string Prefix = "verilog";

        // Substring → code. Matched case-insensitively against the message
        // text. Keys are ordered most-specific-first; the first match wins.
        private static readonly (string Needle, string Code)[] KnownSubstrings =
        {
            ("undriven", "verilog/undriven"),
            ("unused", "verilog/unused"),
            ("not defined here", "verilog/undefined"),
            ("undefined", "verilog/undefined"),
            ("not found", "verilog/undefined"),
            ("duplicate", "verilog/duplicate"),
            ("duplicated", "verilog/duplicate"),
            ("already declared", "verilog/duplicate"),
            ("implicit net", "verilog/implicit-net-declaration"),
            ("implicit modport", "verilog/implicit-modport"),
            ("bitwidth", "verilog/assignment-bitwidth-mismatch"),
            ("not all ports connected", "verilog/not-all-ports-connected"),
            ("syntax", "verilog/syntax"),
        };

        public static string FromMessage(string? text)
        {
            if (string.IsNullOrEmpty(text)) return Prefix + "/unknown";

            foreach ((string needle, string code) in KnownSubstrings)
            {
                if (text.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return code;
                }
            }

            return Prefix + "/" + Slug(text);
        }

        private static string Slug(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            bool lastWasDash = false;
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                    lastWasDash = false;
                }
                else if (c == ' ' || c == '_' || c == '-')
                {
                    if (!lastWasDash && sb.Length > 0)
                    {
                        sb.Append('-');
                        lastWasDash = true;
                    }
                }
                // Skip other characters; they are not part of the slug.

                if (sb.Length >= 32) break;
            }

            // Trim trailing dash.
            if (sb.Length > 0 && sb[sb.Length - 1] == '-') sb.Length--;
            if (sb.Length == 0) return "info";
            return sb.ToString();
        }
    }
}
