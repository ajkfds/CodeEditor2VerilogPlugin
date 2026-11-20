using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// One editable field (tab stop) of a snippet, positioned in the rendered
    /// plain text.
    ///
    /// <para>
    /// The desktop editor needs <see cref="Start"/> / <see cref="Length"/> to
    /// build its highlights, while VS Code only needs the <c>${n}</c> markers.
    /// Both are produced from the same <see cref="SnippetTemplate"/>, so the
    /// snippet logic exists once (see <see cref="SnippetAdapter"/>).
    /// </para>
    /// </summary>
    public sealed class SnippetField
    {
        /// <summary>1-based tab stop index (shared by mirrors).</summary>
        public int Index { get; init; }

        /// <summary>Logical name (e.g. <c>clock</c>, <c>reset</c>).</summary>
        public string Name { get; init; } = "";

        /// <summary>Text the field holds before the user edits it.</summary>
        public string Default { get; init; } = "";

        /// <summary>
        /// Signals the user may pick from. More than one entry becomes a choice
        /// (<c>${n|a,b|}</c> in VS Code, a popup in the desktop editor).
        /// </summary>
        public IReadOnlyList<string> Candidates { get; init; } = Array.Empty<string>();

        /// <summary>Offset of the rendered default text inside the plain text.</summary>
        public int Start { get; init; }

        /// <summary>Length of the rendered default text (0 for a blank field).</summary>
        public int Length { get; init; }

        /// <summary>
        /// When set, this occurrence repeats an earlier tab stop (VS Code's
        /// <c>${n}</c> mirror). The desktop editor copies the source field's
        /// text here instead of asking the user twice.
        /// </summary>
        public int? MirrorOf { get; init; }

        /// <summary><c>true</c> for a repeated occurrence of an earlier tab stop.</summary>
        public bool IsMirror => MirrorOf != null;
    }

    /// <summary>
    /// Builds a snippet once and renders it for the two hosts.
    ///
    /// <para>
    /// The problem this solves: the desktop editor's snippets
    /// (<c>Verilog/Snippets/*</c>) used <c>{0}</c> placeholders plus highlight
    /// lists, while the VS Code extension needed <c>${n}</c> tab stops. Each
    /// snippet therefore had two text builders and they drifted. A template is
    /// written once and rendered either as a VS Code snippet
    /// (<see cref="ToVscodeSnippet"/>) or as plain text plus field offsets
    /// (<see cref="ToPlainText"/>), so both hosts show exactly the same thing.
    /// </para>
    /// </summary>
    public sealed class SnippetTemplate
    {
        private abstract class Part { }

        private sealed class LiteralPart : Part
        {
            public string Text = "";
        }

        private sealed class FieldPart : Part
        {
            public int Index;
            public bool IsMirror;
        }

        private sealed class FieldDefinition
        {
            public int Index;
            public string Name = "";
            public string Default = "";
            public IReadOnlyList<string> Candidates = Array.Empty<string>();
        }

        private readonly List<Part> parts = new();
        private readonly List<FieldDefinition> definitions = new();

        /// <summary>Appends literal text.</summary>
        public void AppendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            parts.Add(new LiteralPart { Text = text });
        }

        /// <summary>
        /// Appends another template's parts, keeping its field numbering.
        /// </summary>
        /// <remarks>
        /// Used when a snippet has to prepend generated text (the signal
        /// declarations of "Fill Port Connections") to the text built from a
        /// shared template: rebuilding the literal string would drop the tab
        /// stops, and re-adding the fields would renumber them.
        /// </remarks>
        public void Append(SnippetTemplate other)
        {
            if (other == null) return;
            int offset = definitions.Count;
            foreach (FieldDefinition definition in other.definitions)
            {
                definitions.Add(new FieldDefinition
                {
                    Index = definition.Index + offset,
                    Name = definition.Name,
                    Default = definition.Default,
                    Candidates = definition.Candidates,
                });
            }
            foreach (Part part in other.parts)
            {
                if (part is LiteralPart literal)
                {
                    parts.Add(new LiteralPart { Text = literal.Text });
                    continue;
                }
                FieldPart field = (FieldPart)part;
                parts.Add(new FieldPart
                {
                    Index = field.Index + offset,
                    IsMirror = field.IsMirror,
                });
            }
        }

        /// <summary>
        /// Adds a tab stop and returns its index. When
        /// <paramref name="candidates"/> has more than one entry the field
        /// becomes a choice and the first candidate is its default.
        /// </summary>
        public int AddField(string name, string? defaultValue, IReadOnlyList<string>? candidates = null)
        {
            IReadOnlyList<string> list = candidates ?? Array.Empty<string>();
            string text = defaultValue ?? "";
            if (text.Length == 0 && list.Count > 0) text = list[0];

            int index = definitions.Count + 1;
            definitions.Add(new FieldDefinition
            {
                Index = index,
                Name = name,
                Default = text,
                Candidates = list,
            });
            parts.Add(new FieldPart { Index = index, IsMirror = false });
            return index;
        }

        /// <summary>
        /// Repeats an existing tab stop at this position (VS Code mirror). The
        /// desktop editor copies the source field's text here.
        /// </summary>
        public void AddMirror(int fieldIndex)
        {
            parts.Add(new FieldPart { Index = fieldIndex, IsMirror = true });
        }

        /// <summary>
        /// Renders the VS Code snippet body: <c>${1|a,b|}</c> for choices,
        /// <c>${1:default}</c> for a prefilled field, <c>${1}</c> for a blank
        /// one and <c>${n}</c> for a mirror.
        /// </summary>
        public string ToVscodeSnippet()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Part part in parts)
            {
                if (part is LiteralPart literal)
                {
                    sb.Append(EscapeLiteral(literal.Text));
                    continue;
                }

                FieldPart field = (FieldPart)part;
                FieldDefinition definition = definitions[field.Index - 1];
                sb.Append("${").Append(field.Index);
                if (!field.IsMirror)
                {
                    if (definition.Candidates.Count > 1)
                    {
                        sb.Append('|');
                        for (int i = 0; i < definition.Candidates.Count; i++)
                        {
                            if (i > 0) sb.Append(',');
                            sb.Append(EscapeChoice(definition.Candidates[i]));
                        }
                        sb.Append('|');
                    }
                    else if (definition.Default.Length > 0)
                    {
                        sb.Append(':').Append(EscapePlaceholder(definition.Default));
                    }
                }
                sb.Append('}');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Renders the text with every field's default inlined and reports where
        /// each field landed. Used by the desktop editor, which then highlights
        /// the fields and lets the user tab through them.
        /// </summary>
        public string ToPlainText(List<SnippetField> fields)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Part part in parts)
            {
                if (part is LiteralPart literal)
                {
                    sb.Append(literal.Text);
                    continue;
                }

                FieldPart field = (FieldPart)part;
                FieldDefinition definition = definitions[field.Index - 1];
                fields.Add(new SnippetField
                {
                    Index = field.Index,
                    Name = definition.Name,
                    Default = definition.Default,
                    Candidates = definition.Candidates,
                    Start = sb.Length,
                    Length = definition.Default.Length,
                    MirrorOf = field.IsMirror ? field.Index : (int?)null,
                });
                sb.Append(definition.Default);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Escapes the characters that would otherwise be read as snippet
        /// syntax. Without this a <c>$</c> in a comment or a <c>}</c> in a
        /// concatenation would break the tab stops.
        /// </summary>
        private static string EscapeLiteral(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '\\' || c == '$' || c == '}') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Escapes the text of a <c>${n:default}</c> placeholder.</summary>
        private static string EscapePlaceholder(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '\\' || c == '$' || c == '}') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Escapes one entry of a <c>${n|a,b|}</c> choice.</summary>
        private static string EscapeChoice(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '\\' || c == '$' || c == '}' || c == ',' || c == '|') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
