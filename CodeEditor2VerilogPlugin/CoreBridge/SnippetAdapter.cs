
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free snippet adapter for LSP hosts.
    ///
    /// <para>
    /// The editor's snippets (<c>Verilog/Snippets/*</c>) are interactive: they
    /// drive <c>Controller.CodeEditor</c>, the popup menu and key events, so a
    /// host without an Avalonia dispatcher (LSP server, unit tests) cannot run
    /// them. This adapter keeps the part that decides <b>what text to insert</b>
    /// (port enumeration, parameter overrides, connection generation, port
    /// inversion, modport derivation) and emits it as a VS Code snippet string
    /// with <c>${n:placeholder}</c> tab stops.
    /// </para>
    ///
    /// <para>
    /// The host is then responsible only for the mechanical part: calling
    /// <c>TextEditor.insertSnippet</c> (or replacing a range) with the returned
    /// text. Interactive key handling is not reproduced; the tab stops give the
    /// same "jump between the fields" behaviour.
    /// </para>
    /// </summary>
    public static class SnippetAdapter
    {
        /// <summary>One snippet the host can insert or apply as a replacement.</summary>
        public sealed class SnippetEntry
        {
            /// <summary>Human readable name (shown in the command palette / picker).</summary>
            public string Label { get; init; } = "";

            /// <summary>Short description of what the snippet does.</summary>
            public string? Detail { get; init; }

            /// <summary><c>insert</c> (at the caret) or <c>replace</c> (over a range).</summary>
            public string Kind { get; init; } = SnippetKind.Insert;

            /// <summary>Replacement range start (only meaningful for <c>replace</c>).</summary>
            public int Start { get; init; }

            /// <summary>Replacement range end, exclusive (only for <c>replace</c>).</summary>
            public int End { get; init; }

            /// <summary>VS Code snippet text (<c>${1:name}</c> tab stops).</summary>
            public string SnippetText { get; init; } = "";

            /// <summary>
            /// The same snippet as plain text with every field's default inlined.
            /// The desktop editor inserts this and highlights
            /// <see cref="Fields"/>; the VS Code extension inserts
            /// <see cref="SnippetText"/>. Both come from one
            /// <see cref="SnippetTemplate"/>, so they cannot drift.
            /// </summary>
            public string PlainText { get; init; } = "";

            /// <summary>
            /// Where the editable fields sit inside <see cref="PlainText"/>
            /// (empty for snippets without fields).
            /// </summary>
            public IReadOnlyList<SnippetField> Fields { get; init; } = Array.Empty<SnippetField>();

            /// <summary>
            /// <c>true</c> when the snippet has tab stops, i.e. the user is
            /// expected to fill something in.
            /// </summary>
            public bool HasFields => Fields.Count > 0;
        }

        /// <summary>Result of a snippet request.</summary>
        public sealed class SnippetResult
        {
            /// <summary>Snippets to apply (usually one).</summary>
            public List<SnippetEntry> Snippets { get; } = new();

            /// <summary>
            /// When the request needs a further choice (e.g. which module to
            /// instantiate), the candidate names. The host shows a picker and
            /// re-requests with the chosen name.
            /// </summary>
            public List<string> Candidates { get; } = new();

            /// <summary>Why nothing was produced (shown to the user).</summary>
            public string? Message { get; set; }
        }

        /// <summary>Snippet kinds understood by <see cref="GetSnippets"/>.</summary>
        public static class SnippetKind
        {
            public const string Insert = "insert";
            public const string Replace = "replace";
        }

        /// <summary>Requested snippet names (the <c>kind</c> parameter).</summary>
        public static class SnippetName
        {
            public const string AlwaysFF = "alwaysFF";
            public const string AutoFormat = "autoFormat";
            public const string ModuleInstance = "moduleInstance";
            public const string PortConnectionCreate = "portConnectionCreate";
            public const string PortInvert = "portInvert";
            public const string ModPort = "modport";
        }

        /// <summary>
        /// Builds the snippets for <paramref name="name"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="file">The file being edited (its parse result is used).</param>
        /// <param name="parsedDocument">Parse result of <paramref name="file"/>.</param>
        /// <param name="index">Caret offset in the document.</param>
        /// <param name="name">One of <see cref="SnippetName"/>.</param>
        /// <param name="argument">
        /// Extra argument: the module name for <c>moduleInstance</c> /
        /// <c>portConnectionCreate</c>. Ignored by the other snippets.
        /// </param>
        /// <param name="selectionStart">Selection start (for <c>portInvert</c>).</param>
        /// <param name="selectionEnd">Selection end, exclusive (for <c>portInvert</c>).</param>
        public static SnippetResult GetSnippets(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            string name,
            string? argument,
            int selectionStart,
            int selectionEnd)
        {
            SnippetResult result = new SnippetResult();
            if (file == null || parsedDocument == null)
            {
                result.Message = "the file has not been parsed yet";
                return result;
            }
            if (parsedDocument.Root == null)
            {
                result.Message = "the file has not been parsed yet";
                return result;
            }

            switch (name)
            {
                case SnippetName.AlwaysFF:
                    BuildAlwaysFF(parsedDocument, index, result);
                    break;
                case SnippetName.AutoFormat:
                    BuildAutoFormat(parsedDocument, index, result);
                    break;
                case SnippetName.ModuleInstance:
                    BuildModuleInstance(file, parsedDocument, index, argument, result);
                    break;
                case SnippetName.PortConnectionCreate:
                    BuildPortConnectionCreate(parsedDocument, index, argument, result);
                    break;
                case SnippetName.PortInvert:
                    BuildPortInvert(parsedDocument, selectionStart, selectionEnd, result);
                    break;
                case SnippetName.ModPort:
                    BuildModPort(parsedDocument, index, result);
                    break;
                default:
                    result.Message = "unknown snippet: " + name;
                    break;
            }
            return result;
        }

        // ---------------------- alwaysFF ----------------------

        /// <summary>
        /// The editor's "alwaysFF" snippet: an <c>always @(posedge clk or
        /// negedge rst)</c> block with the clock / reset taken from the ports
        /// the parser marked as clock / reset (SyncContext).
        ///
        /// <para>
        /// The clock and reset are real tab stops (not baked-in text) so the
        /// user can pick a different one: several annotated clocks / resets
        /// become a choice list (<c>${1|CLK_I,CLK2_I|}</c> in VS Code, the
        /// popup the desktop editor already opened). The <c>if(~reset)</c> is a
        /// <b>mirror</b> of the reset tab stop, so editing one updates the
        /// other - the desktop editor used to copy the text with a background
        /// task and the VS Code extension could not do it at all.
        /// </para>
        /// </summary>
        private static void BuildAlwaysFF(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetResult result)
        {
            string indent = GetIndent(parsedDocument, index);

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

            SnippetTemplate template = new SnippetTemplate();
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

            List<SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new SnippetEntry
            {
                Label = "alwaysFF",
                Detail = "always @(posedge " + (clocks.Count > 0 ? clocks[0] : "clock")
                    + " or negedge " + (resets.Count > 0 ? resets[0] : "reset_x") + ")",
                Kind = SnippetKind.Insert,
                Start = index,
                End = index,
                SnippetText = template.ToVscodeSnippet(),
                Fields = fields,
                PlainText = plainText,
            });
        }

        // ---------------------- autoFormat ----------------------

        /// <summary>
        /// The editor's "autoFormat" tool: rewrite the module instantiation
        /// under the caret with <c>ModuleInstantiation.CreateString</c> (one
        /// port connection per line, aligned).
        ///
        /// <para>
        /// This is the same scope as the editor's tool, i.e. <b>only the
        /// instantiation at the caret</b> - not the whole document. The LSP
        /// standard <c>textDocument/formatting</c> is a different feature (it
        /// returns edits for every instantiation in the file), so the tool menu
        /// uses this snippet instead of dispatching to the editor's format
        /// command.
        /// </para>
        /// </summary>
        private static void BuildAutoFormat(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetResult result)
        {
            Verilog.Items.ModuleInstantiation? instantiation =
                FindInstantiationAt(parsedDocument, index);
            if (instantiation == null)
            {
                result.Message = "the caret is not inside a module instantiation";
                return;
            }
            if (instantiation.LastIndexReference == null)
            {
                result.Message = "the instantiation has no known extent";
                return;
            }

            int start = LastIndex(instantiation.BeginIndexReference);
            int end = LastIndex(instantiation.LastIndexReference) + 1;
            string indent = GetIndent(parsedDocument, start);

            // CreateString returns the statement without the trailing
            // semicolon; the replaced range covers it, so keep it.
            string? text = instantiation.CreateString(indent);
            if (string.IsNullOrEmpty(text))
            {
                result.Message = "the instantiation could not be reformatted";
                return;
            }
            if (!text.TrimEnd().EndsWith(";", StringComparison.Ordinal))
            {
                text = text.TrimEnd() + ";";
            }

            result.Snippets.Add(new SnippetEntry
            {
                Label = "autoFormat",
                Detail = "reformat " + instantiation.Name,
                Kind = SnippetKind.Replace,
                Start = start,
                End = end,
                SnippetText = text,
                // No tab stops: the reformatted text is literal (any '$' or
                // '}' in a port expression must not be read as snippet syntax),
                // so the plain text is what both hosts insert.
                PlainText = text,
            });
        }

        // ---------------------- modport ----------------------

        /// <summary>
        /// The editor's "modport generate" snippet: a modport listing every
        /// logic signal of the enclosing interface, with the direction flipped
        /// for signals an existing modport already drives.
        /// </summary>
        private static void BuildModPort(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetResult result)
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

            string indent = GetIndent(parsedDocument, index);
            SnippetTemplate template = new SnippetTemplate();
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

            List<SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new SnippetEntry
            {
                Label = "modport generate",
                Detail = directions.Count + " signal(s)",
                Kind = SnippetKind.Insert,
                Start = index,
                End = index,
                SnippetText = template.ToVscodeSnippet(),
                PlainText = plainText,
                Fields = fields,
            });
        }

        // ---------------------- moduleInstance ----------------------

        /// <summary>
        /// The editor's "moduleInstance" snippet: a full instantiation of the
        /// named module (parameter overrides + one named port connection per
        /// port). Without <paramref name="moduleName"/> the candidate module
        /// names are returned so the host can show a picker.
        /// </summary>
        private static void BuildModuleInstance(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            string? moduleName,
            SnippetResult result)
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

            Verilog.BuildingBlocks.Module? module = ResolveModule(projectProperty, moduleName);
            if (module == null)
            {
                result.Message = "module not found: " + moduleName;
                return;
            }

            string instanceName = MakeUniqueInstanceName(parsedDocument, index, moduleName);
            string indent = GetIndent(parsedDocument, index);
            SnippetTemplate template = BuildInstantiationTemplate(module, instanceName, indent, null);

            List<SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new SnippetEntry
            {
                Label = moduleName,
                Detail = "instantiate " + moduleName,
                Kind = SnippetKind.Insert,
                Start = index,
                End = index,
                SnippetText = template.ToVscodeSnippet(),
                PlainText = plainText,
                Fields = fields,
            });
        }

        // ---------------------- portConnectionCreate ----------------------

        /// <summary>
        /// The editor's "portConnectionCreate" snippet: rewrites the module
        /// instantiation under the caret, filling every port connection (and
        /// declaring the nets the connections need).
        /// </summary>
        private static void BuildPortConnectionCreate(
            Verilog.ParsedDocument parsedDocument,
            int index,
            string? moduleName,
            SnippetResult result)
        {
            Verilog.Items.ModuleInstantiation? instantiation =
                FindInstantiationAt(parsedDocument, index);
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

            int start = LastIndex(instantiation.BeginIndexReference);
            int end = LastIndex(instantiation.LastIndexReference) + 1;
            string indent = GetIndent(parsedDocument, start);
            SnippetTemplate template = BuildInstantiationTemplate(module, instantiation.Name, indent, instantiation);

            List<SnippetField> fields = new();
            string plainText = template.ToPlainText(fields);

            result.Snippets.Add(new SnippetEntry
            {
                Label = "portConnectionCreate",
                Detail = "fill the connections of " + instantiation.Name,
                Kind = SnippetKind.Replace,
                Start = start,
                End = end,
                SnippetText = template.ToVscodeSnippet(),
                PlainText = plainText,
                Fields = fields,
            });
        }

        // ---------------------- portInvert ----------------------

        /// <summary>
        /// The editor's "PortInvert" snippet: flips <c>input</c> / <c>output</c>
        /// and the <c>_I</c> / <c>_O</c> name suffix of every selected port
        /// declaration, preserving the original whitespace and comments.
        /// </summary>
        private static void BuildPortInvert(
            Verilog.ParsedDocument parsedDocument,
            int selectionStart,
            int selectionEnd,
            SnippetResult result)
        {
            pluginVerilog.CodeEditor.CodeDocument? doc = parsedDocument.CodeDocument;
            if (doc == null) return;
            if (selectionStart < 0) selectionStart = 0;
            if (selectionEnd > doc.Length) selectionEnd = doc.Length;
            if (selectionEnd <= selectionStart)
            {
                result.Message = "select the port declarations to invert first";
                return;
            }

            string selected = doc.CreateString(selectionStart, selectionEnd - selectionStart);
            string[] lines = selected.Split('\n');
            List<string> processed = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                processed.Add(InvertPortLine(line));
            }
            string text = string.Join("\n", processed);

            result.Snippets.Add(new SnippetEntry
            {
                Label = "PortInvert",
                Detail = "invert the selected port declarations",
                Kind = SnippetKind.Replace,
                Start = selectionStart,
                End = selectionEnd,
                SnippetText = text,
                // Literal replacement: the inverted declarations contain no tab
                // stops, so both hosts insert the same text.
                PlainText = text,
            });
        }

        /// <summary>
        /// The indentation of the line containing <paramref name="index"/>, so
        /// the inserted text lines up with the surrounding code.
        /// </summary>
        private static string GetIndent(Verilog.ParsedDocument parsedDocument, int index)
        {
            pluginVerilog.CodeEditor.CodeDocument? doc = parsedDocument.CodeDocument;
            if (doc == null) return "";
            if (index < 0) index = 0;
            if (index > doc.Length) index = doc.Length;

            int line = doc.GetLineAt(index);
            int lineStart = doc.GetLineStartIndex(line);
            StringBuilder sb = new StringBuilder();
            for (int i = lineStart; i < doc.Length; i++)
            {
                char ch = doc.GetCharAt(i);
                if (ch != ' ' && ch != '\t') break;
                sb.Append(ch);
            }
            return sb.ToString();
        }

        // ---------------------- helpers ----------------------

        /// <summary>Resolves a module by name from the project's definition namespace.</summary>
        private static Verilog.BuildingBlocks.Module? ResolveModule(
            ProjectProperty projectProperty,
            string moduleName)
        {
            Data.VerilogFile? targetFile =
                projectProperty.DefinitionNameSpace.GetFile(moduleName) as Data.VerilogFile;
            if (targetFile == null) return null;
            if (targetFile.ParsedDocument is not Verilog.ParsedDocument parsed) return null;
            return parsed.Root?.BuildingBlocks.TryGetValue(moduleName, out var block) == true
                ? block as Verilog.BuildingBlocks.Module
                : null;
        }

        /// <summary>
        /// Picks <c>moduleName_0</c>, <c>moduleName_1</c>, ... skipping the names
        /// already used by an instantiation in the enclosing building block.
        /// </summary>
        private static string MakeUniqueInstanceName(
            Verilog.ParsedDocument parsedDocument,
            int index,
            string moduleName)
        {
            Verilog.BuildingBlocks.BuildingBlock? block = parsedDocument.GetBuildingBlockAt(index);
            if (block == null) return moduleName + "_0";

            int count = 0;
            while (block.NamedElements.ContainsIBuldingBlockInstantiation(moduleName + "_" + count))
            {
                count++;
            }
            return moduleName + "_" + count;
        }

        /// <summary>
        /// Builds the instantiation text: parameter overrides, then one named
        /// port connection per port. Ports already connected in
        /// <paramref name="existing"/> keep their expression; the rest get an
        /// <b>empty</b> <c>${n}</c> tab stop so the user types the signal.
        /// </summary>
        /// <remarks>
        /// The editor's own <c>ModuleInstanceSnippet.getReplaceText</c> emits
        /// <c>.port(  )</c> for every port - it never guesses a signal. The
        /// adapter used to fill a placeholder default (<c>${1:clk}</c>), so
        /// accepting the snippet silently wired the port to a signal that may
        /// not exist. The blank tab stop matches the editor.
        /// </remarks>
        private static SnippetTemplate BuildInstantiationTemplate(
            Verilog.BuildingBlocks.Module module,
            string instanceName,
            string indent,
            Verilog.Items.ModuleInstantiation? existing)
        {
            SnippetTemplate template = new SnippetTemplate();

            template.AppendText(module.Name + " ");
            if (module.PortParameterNameList.Count > 0)
            {
                template.AppendText("#(\n");
                bool firstParam = true;
                foreach (string paramName in module.PortParameterNameList)
                {
                    if (!firstParam) template.AppendText(",\n");
                    template.AppendText(indent + "\t." + paramName + "\t( ");
                    if (existing != null && existing.ParameterOverrides.TryGetValue(paramName, out var over))
                    {
                        template.AppendText(over.CreateString());
                    }
                    else if (module.NamedElements.TryGetValue(paramName, out var element)
                             && element is Verilog.DataObjects.Constants.Constants constant
                             && constant.Expression != null)
                    {
                        template.AppendText(constant.Expression.CreateString());
                    }
                    template.AppendText(" )");
                    firstParam = false;
                }
                template.AppendText("\n" + indent + ") ");
            }

            // The instance name is the FIRST tab stop (prefilled with the
            // generated name) so the user can rename it straight away; the port
            // tab stops start after it. Same ordering as the desktop editor.
            template.AppendText("");
            template.AddField("instance-name", instanceName);
            template.AppendText(" (\n");

            bool firstPort = true;
            string? portGroupName = null;
            foreach (Verilog.DataObjects.Port port in module.Ports.Values)
            {
                if (!firstPort) template.AppendText(",\n");

                if (port.PortGroupName != portGroupName)
                {
                    portGroupName = port.PortGroupName;
                    if (!string.IsNullOrEmpty(portGroupName))
                    {
                        template.AppendText(indent + "// " + portGroupName + "\n");
                    }
                }

                template.AppendText(indent + "\t." + port.Name + "\t( ");
                if (existing != null && existing.PortConnection.TryGetValue(port.Name, out var connection))
                {
                    template.AppendText(connection.CreateString());
                }
                else
                {
                    // Blank tab stop: the editor leaves the connection empty
                    // instead of guessing a signal name (see the remarks on
                    // BuildInstantiationText).
                    template.AddField(port.Name, "");
                }
                template.AppendText(" )");
                firstPort = false;
            }
            if (!firstPort) template.AppendText("\n");
            template.AppendText(indent + ");");
            return template;
        }

        /// <summary>
        /// True when the caret sits on a module instantiation, i.e. the tools
        /// that rewrite an instantiation (autoFormat / portConnectionCreate /
        /// autoConnect) can run here. Used by <c>ToolMenuAdapter</c> to decide
        /// whether a menu row is enabled; the lookup itself is the same one the
        /// snippets use, so the menu and the tools never disagree.
        /// </summary>
        public static bool HasModuleInstantiationAt(
            Verilog.ParsedDocument parsedDocument,
            int index)
        {
            return FindInstantiationAt(parsedDocument, index) != null;
        }

        /// <summary>Finds the module instantiation whose range contains <paramref name="index"/>.</summary>
        private static Verilog.Items.ModuleInstantiation? FindInstantiationAt(
            Verilog.ParsedDocument parsedDocument,
            int index)
        {
            Verilog.BuildingBlocks.BuildingBlock? block = parsedDocument.GetBuildingBlockAt(index);
            if (block == null) return null;

            Verilog.IndexReference iref = Verilog.IndexReference.Create(parsedDocument.IndexReference, index);
            foreach (Verilog.Items.IBuildingBlockInstantiation inst in block.GetBuildingBlockInstantiations())
            {
                if (inst is not Verilog.Items.ModuleInstantiation moduleInstantiation) continue;
                if (moduleInstantiation.BeginIndexReference == null) continue;
                if (moduleInstantiation.LastIndexReference == null) continue;
                if (iref.IsSmallerThan(moduleInstantiation.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(moduleInstantiation.LastIndexReference)) continue;
                return moduleInstantiation;
            }
            return null;
        }

        /// <summary>Last index of an IndexReference (the deepest scope offset).</summary>
        private static int LastIndex(Verilog.IndexReference iref)
        {
            IReadOnlyList<int> indexes = iref.Indexes;
            return indexes.Count == 0 ? 0 : indexes[indexes.Count - 1];
        }

        /// <summary>
        /// Flips the direction and the <c>_I</c> / <c>_O</c> suffix of one port
        /// declaration line, keeping the original whitespace and comment. Lines
        /// that are not a port declaration are returned unchanged.
        /// </summary>
        private static string InvertPortLine(string line)
        {
            int indentLength = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t') indentLength++;
                else break;
            }
            string indent = line.Substring(0, indentLength);
            string content = line.Substring(indentLength);

            System.Text.RegularExpressions.Match match = PortPattern.Match(content);
            if (!match.Success) return line;

            string direction = match.Groups["direction"].Value;
            string newDirection = direction == "input" ? "output" : "input";

            string name = match.Groups["name"].Value;
            string newName = name;
            if (name.EndsWith("_I", StringComparison.Ordinal))
            {
                newName = name.Substring(0, name.Length - 2) + "_O";
            }
            else if (name.EndsWith("_O", StringComparison.Ordinal))
            {
                newName = name.Substring(0, name.Length - 2) + "_I";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(indent).Append(newDirection);
            sb.Append(OrSpace(match.Groups["ws1"].Value));

            string qualifiers = match.Groups["qualifiers"].Value;
            if (!string.IsNullOrEmpty(qualifiers)) sb.Append(qualifiers);

            string type = match.Groups["type"].Value;
            if (!string.IsNullOrEmpty(type)) sb.Append(type).Append(OrSpace(match.Groups["ws2"].Value));

            string bitwidth = match.Groups["bitwidth"].Value;
            if (!string.IsNullOrEmpty(bitwidth)) sb.Append(bitwidth).Append(OrSpace(match.Groups["ws3"].Value));

            sb.Append(newName);
            sb.Append(match.Groups["sep"].Value);

            string comment = match.Groups["comment"].Value;
            if (!string.IsNullOrEmpty(comment))
            {
                sb.Append(match.Groups["wsBeforeComment"].Value).Append(comment);
            }
            return sb.ToString();
        }

        private static string OrSpace(string whitespace) =>
            whitespace.Length > 0 ? whitespace : " ";

        /// <summary>
        /// One port declaration line: direction, optional qualifiers / type /
        /// bit width, the name, the trailing separator and an optional comment.
        /// The whitespace groups are captured so the rebuilt line keeps the
        /// original spacing.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex PortPattern =
            new System.Text.RegularExpressions.Regex(
                @"^(?<direction>input|output)(?<ws1>\s+)" +
                @"(?<qualifiers>(?:(?:signed|wire|reg|automatic)\s+)*)" +
                @"(?:(?<type>\w+)(?<ws2>\s+))?" +
                @"(?<bitwidth>\[[^\[\]]*\](?<ws3>\s+))?" +
                @"(?<name>\w+)" +
                @"(?<sep>\s*[;,]?)" +
                @"(?<wsBeforeComment>\s*)" +
                @"(?<comment>//[^\r\n]*)?$",
                System.Text.RegularExpressions.RegexOptions.Compiled);
    }
}
