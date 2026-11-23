using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Result of <see cref="HoverGrammar.Write"/> / <see cref="HoverGrammar.Check"/>.
    /// </summary>
    public sealed class HoverGrammarResult
    {
        public bool Failed { get; init; }
        public bool Changed { get; init; }
        public string Message { get; init; } = "";
    }

    /// <summary>
    /// Generates the TextMate grammar the extension uses to colour hover code
    /// blocks, from the parser's own palette and keyword list.
    ///
    /// <para>
    /// A language server cannot colour a hover any other way: VS Code's
    /// markdown sanitizer allows no <c>style</c> / <c>class</c> attribute, so
    /// the only channel is a fenced code block that a TextMate grammar
    /// tokenises. The extension therefore has to contribute a grammar for the
    /// fence language, and the colours in it must be the parser's palette -
    /// otherwise the VS Code hover would drift from the editor's mouse-over
    /// hint. Both inputs live here, in C#: <see cref="CodeDrawStyle"/> for the
    /// colours and <c>Verilog.General.ListOfKeywords</c> for the words.
    /// </para>
    ///
    /// <para>
    /// The grammar only covers what the hover labels contain (keywords, types,
    /// numbers, comments, strings, macros, system tasks, annotations and plain
    /// identifiers) - it is not a general SystemVerilog grammar and is never
    /// used for syntax highlighting in the editor.
    /// </para>
    /// </summary>
    public static class HoverGrammar
    {
        /// <summary>
        /// The TextMate scope of the hover-label grammar.
        ///
        /// <para>
        /// It deliberately does NOT reuse <c>source.systemverilog</c>: a
        /// grammar contributed for the <c>systemverilog</c> language would take
        /// over syntax highlighting from the semantic tokens (which carry the
        /// real parser's colours), which is exactly the regression recorded in
        /// <c>memory/state.md</c> ("カラーリングが pluginVerilog のものではなく
        /// textmate でのカラーリングになっている"). A separate, hover-only scope
        /// keeps TextMate confined to hover code blocks.
        /// </para>
        /// </summary>
        public const string ScopeName = "source.systemverilog.hover";

        /// <summary>
        /// File name of the generated grammar.
        ///
        /// <para>
        /// This is the extension's single grammar file. The old hand-written
        /// grammar lived at the same path but was no longer contributed (syntax
        /// highlighting comes from semantic tokens, which carry the real
        /// parser's colours), so it was dead weight - and a second, differently
        /// coloured grammar next to it would have been a drift source. The
        /// generated one serves the only remaining consumer: hover code blocks.
        /// </para>
        /// </summary>
        public const string FileName = "systemverilog.tmLanguage.json";

        /// <summary>Relative path from the extension root.</summary>
        public const string RelativePath = "syntaxes/" + FileName;

        private const string UsageNote =
            "Generated from CoreBridge/ElementLabelAdapter.cs (palette) and " +
            "Verilog/General.cs (keywords). Rewrite it with " +
            "'dotnet SystemVerilogLanguageServer.dll --write-hover-grammar <extension folder>' " +
            "(add --check to only verify). Do not edit it by hand.";

        /// <summary>Writes or verifies the grammar under <paramref name="extensionRoot"/>.</summary>
        public static HoverGrammarResult Write(string extensionRoot, bool write)
        {
            string path = Path.Combine(extensionRoot, "syntaxes", FileName);
            string expected = Build();

            if (write)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                string? existing = File.Exists(path) ? File.ReadAllText(path) : null;
                if (existing == expected)
                {
                    return new HoverGrammarResult { Message = "hover grammar is already up to date" };
                }
                File.WriteAllText(path, expected, new UTF8Encoding(false));
                return new HoverGrammarResult
                {
                    Changed = true,
                    Message = "wrote " + RelativePath,
                };
            }

            if (!File.Exists(path))
            {
                return new HoverGrammarResult
                {
                    Failed = true,
                    Changed = true,
                    Message = RelativePath + " is missing (run with --write-hover-grammar)",
                };
            }

            string actual = File.ReadAllText(path);
            if (actual == expected)
            {
                return new HoverGrammarResult { Message = "hover grammar is up to date" };
            }

            return new HoverGrammarResult
            {
                Failed = true,
                Changed = true,
                Message = RelativePath + " is out of date (run with --write-hover-grammar)",
            };
        }

        /// <summary>Builds the grammar JSON.</summary>
        public static string Build()
        {
            Dictionary<string, string> palette = ElementLabelAdapter.GetPalette();

            JsonObject keywords = Group("keyword.control.systemverilog", ControlKeywords);
            JsonObject declarations = Group("keyword.other.systemverilog", DeclarationKeywords);
            JsonObject directions = Group("keyword.operator.direction.systemverilog", DirectionKeywords);
            JsonObject types = Group("support.type.systemverilog", TypeKeywords);

            JsonObject grammar = new()
            {
                ["$comment"] = UsageNote,
                ["name"] = "SystemVerilog (hover label)",
                ["scopeName"] = ScopeName,
                // #annotations comes before #comments: a label can put an
                // annotation on its own line (`@sync clk`, `@clock`) and can
                // also carry a trailing `// @sync:clk` comment. TextMate takes
                // the first matching rule, so the annotation has to be tried
                // before the line-comment rule that would swallow it.
                ["patterns"] = new JsonArray(
                    Include("#annotations"),
                    Include("#comments"),
                    Include("#strings"),
                    Include("#numbers"),
                    Include("#system-tasks"),
                    Include("#macros"),
                    Include("#keywords"),
                    Include("#types"),
                    Include("#operators")),
                ["repository"] = new JsonObject
                {
                    ["comments"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray(
                            // The annotation is highlighted inside the comment
                            // too (`// @sync:clk`), matching the editor, which
                            // paints the comment and then re-paints the
                            // annotation tokens on top.
                            new JsonObject
                            {
                                ["name"] = "comment.line.double-slash.systemverilog",
                                ["begin"] = "//",
                                ["end"] = "$",
                                ["patterns"] = new JsonArray(
                                    Include("#annotations")),
                            },
                            new JsonObject
                            {
                                ["name"] = "comment.block.systemverilog",
                                ["begin"] = "/\\*",
                                ["end"] = "\\*/",
                                ["patterns"] = new JsonArray(
                                    Include("#annotations")),
                            }),
                    },
                    ["strings"] = new JsonObject
                    {
                        ["name"] = "string.quoted.double.systemverilog",
                        ["begin"] = "\"",
                        ["end"] = "\"",
                        ["patterns"] = new JsonArray(
                            new JsonObject
                            {
                                ["name"] = "constant.character.escape.systemverilog",
                                ["match"] = "\\\\.",
                            }),
                    },
                    ["numbers"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray(
                            new JsonObject
                            {
                                ["name"] = "constant.numeric.systemverilog",
                                ["match"] = "\\b\\d+('s?[bodh][0-9a-fA-F_xzXZ?]+|'[bodh][0-9a-fA-F_xzXZ?]+|\\d*(\\.\\d+)?)\\b",
                            }),
                    },
                    ["system-tasks"] = new JsonObject
                    {
                        ["name"] = "support.function.systemverilog",
                        ["match"] = "\\$[A-Za-z_][A-Za-z0-9_]*",
                    },
                    ["macros"] = new JsonObject
                    {
                        ["name"] = "entity.name.function.preprocessor.systemverilog",
                        ["match"] = "`[A-Za-z_][A-Za-z0-9_]*",
                    },
                    // Comment annotations (`// @sync:clk`, `// @clock`,
                    // `// @scope ...`) are painted by the editor with
                    // ColorType.CommentAnnotation, which is a different colour
                    // from the surrounding comment. Matching them after
                    // #comments (TextMate takes the first match) would lose to
                    // the line-comment rule, so they are listed first in the
                    // pattern order below.
                    ["annotations"] = new JsonObject
                    {
                        ["name"] = "keyword.other.annotation.systemverilog",
                        ["match"] = "@(sync|clock|scope|project|samesync|reset)\\b",
                    },
                    ["keywords"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray(directions, keywords, declarations),
                    },
                    ["types"] = new JsonObject
                    {
                        ["patterns"] = new JsonArray(types),
                    },
                    ["operators"] = new JsonObject
                    {
                        ["name"] = "keyword.operator.systemverilog",
                        ["match"] = "<=|>=|==|!=|&&|\\|\\||<<|>>|[-+*/%<>=!~&|^?:;,.()\\[\\]{}#]",
                    },
                },
            };

            // The colours: TextMate scope -> palette colour, taken from the
            // parser's palette so the hover and the editor can never drift.
            grammar["settings"] = new JsonArray(
                Scope("keyword.control.systemverilog", palette, "Keyword"),
                Scope("keyword.other.systemverilog", palette, "Keyword"),
                Scope("keyword.operator.direction.systemverilog", palette, "Keyword"),
                Scope("support.type.systemverilog", palette, "Keyword"),
                Scope("keyword.operator.systemverilog", palette, "Normal"),
                Scope("constant.numeric.systemverilog", palette, "Number"),
                Scope("string.quoted.double.systemverilog", palette, "Number"),
                // An escape sequence is drawn with the string's colour; giving
                // it a rule keeps every scope the grammar emits coloured (a
                // scope with no rule silently inherits, which hides mistakes).
                Scope("constant.character.escape.systemverilog", palette, "Number"),
                Scope("comment.line.double-slash.systemverilog", palette, "Comment"),
                Scope("comment.block.systemverilog", palette, "Comment"),
                Scope("keyword.other.annotation.systemverilog", palette, "CommentAnnotation"),
                Scope("support.function.systemverilog", palette, "Variable"),
                Scope("entity.name.function.preprocessor.systemverilog", palette, "Parameter"));

            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return grammar.ToJsonString(options) + "\n";
        }

        private static JsonObject Include(string pattern) =>
            new() { ["include"] = pattern };

        private static JsonObject Group(string scope, IEnumerable<string> words) =>
            new()
            {
                ["name"] = scope,
                ["match"] = "\\b(" + string.Join("|", words) + ")\\b",
            };

        private static JsonObject Scope(
            string scope, Dictionary<string, string> palette, string paletteSlot) =>
            new()
            {
                ["scope"] = scope,
                ["settings"] = new JsonObject
                {
                    ["foreground"] = palette.TryGetValue(paletteSlot, out string? color)
                        ? color
                        : "#D4D4D4",
                },
            };

        /// <summary>
        /// Keywords that appear in hover labels as statements / block keywords.
        /// Kept explicit (rather than reusing the parser's whole keyword list)
        /// because TextMate has no word-boundary aware ordering: a keyword that
        /// is also a type name must be coloured by the more specific scope.
        /// </summary>
        private static readonly string[] ControlKeywords =
        {
            "if", "else", "case", "casex", "casez", "endcase", "default", "for", "foreach",
            "forever", "repeat", "while", "do", "break", "continue", "return", "begin", "end",
            "fork", "join", "join_any", "join_none", "wait", "wait_order", "disable",
            "assert", "assume", "cover", "expect", "property", "endproperty", "sequence",
            "endsequence", "clocking", "endclocking", "randcase", "randsequence",
        };

        /// <summary>
        /// Declaration keywords. Also mirrors <c>Verilog.General.ListOfKeywords</c>
        /// but restricted to the words that can appear in a label.
        /// </summary>
        private static readonly string[] DeclarationKeywords =
        {
            "module", "endmodule", "macromodule", "interface", "endinterface", "package",
            "endpackage", "program", "endprogram", "checker", "endchecker", "primitive",
            "endprimitive", "class", "endclass", "extends", "implements", "virtual", "with",
            "inside", "dist", "iff", "posedge", "negedge", "edge", "always", "always_comb",
            "always_ff", "always_latch", "initial", "final", "assign", "force", "release",
            "deassign", "defparam", "generate", "endgenerate", "genvar", "bind", "alias",
            "import", "export", "extern", "pure", "context", "local", "static", "automatic",
            "typedef", "enum", "struct", "packed", "union", "tagged", "new", "null", "this",
            "super", "unique", "priority", "rand", "randc", "constraint", "solve", "before",
            "cross", "coverpoint", "covergroup", "endgroup", "sample", "timeunit",
            "timeprecision", "timescale", "nettype", "interconnect", "soft", "ref",
            "parameter", "localparam", "specparam", "function", "endfunction", "task",
            "endtask", "input", "output", "inout", "signed", "unsigned", "var", "const",
        };

        private static readonly string[] DirectionKeywords =
        {
            "input", "output", "inout", "ref",
        };

        private static readonly string[] TypeKeywords =
        {
            "bit", "logic", "reg", "wire", "wand", "wor", "tri", "tri0", "tri1", "uwire",
            "supply0", "supply1", "int", "integer", "byte", "shortint", "longint", "time",
            "real", "shortreal", "realtime", "string", "void", "event", "semaphore",
            "mailbox", "process", "chandle",
        };
    }
}
