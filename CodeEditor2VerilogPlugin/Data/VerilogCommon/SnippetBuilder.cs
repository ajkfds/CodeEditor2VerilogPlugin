using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.Data.VerilogCommon
{
    /// <summary>
    /// Helpers shared by the snippet implementations
    /// (<c>AlwaysFFSnippet</c>, <c>AutoFormatSnippet</c>,
    /// <c>ModuleInstanceSnippet</c>, <c>PortConnectionCreateSnippet</c>,
    /// <c>PortInvertSnippet</c>, <c>ModPortSnippet</c>).
    ///
    /// <para>
    /// These used to live in <c>CoreBridge.SnippetAdapter</c>, which made the
    /// adapter hold the snippet logic itself. The snippets are the
    /// implementation, so each one now owns its <c>Build</c> method and only the
    /// genuinely shared parts are here. Everything is UI free (no Avalonia
    /// dispatcher, no editor), so the headless language server can call it.
    /// </para>
    ///
    /// <para>
    /// <b>Why this is not under <c>Verilog/</c>.</b> The parser folder is being
    /// moved into <c>SystemVerilogCore</c>, and everything left in it must not
    /// reach back into the plugin. This file builds a
    /// <see cref="CoreBridge.SnippetTemplate"/>, which is a plugin-side wire
    /// shape, so keeping it in <c>Verilog/</c> would have kept the parser folder
    /// depending on the plugin. It is a consumer of the parse tree (like the
    /// editor snippets next to it), not part of the parser.
    /// </para>
    /// </summary>
    public static class SnippetBuilder
    {
        /// <summary>
        /// The indentation of the line containing <paramref name="index"/>, so
        /// the inserted text lines up with the surrounding code.
        /// </summary>
        public static string GetIndent(Verilog.ParsedDocument parsedDocument, int index)
        {
            SystemVerilogCore.Documents.IVerilogTextDocument? doc = parsedDocument.VerilogDocument;
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
        public static Verilog.Items.ModuleInstantiation? FindInstantiationAt(
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
        public static int LastIndex(Verilog.IndexReference iref)
        {
            IReadOnlyList<int> indexes = iref.Indexes;
            return indexes.Count == 0 ? 0 : indexes[indexes.Count - 1];
        }

        /// <summary>Resolves a module by name from the project's definition namespace.</summary>
        public static Verilog.BuildingBlocks.Module? ResolveModule(
            ProjectProperty projectProperty,
            string moduleName)
        {
            // No cast to the editor's concrete file type: the definition
            // namespace already returns the parser-facing file contract.
            Data.IVerilogRelatedFile? targetFile =
                projectProperty.DefinitionNameSpace.GetEditorFile(moduleName);
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
        public static string MakeUniqueInstanceName(
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
        /// <paramref name="existing"/> keep their expression.
        /// </summary>
        /// <param name="enclosing">
        /// The building block the text is inserted into, or <c>null</c> for
        /// "blank connections".
        /// <para>
        /// The two tools that share this formatting differ in exactly one
        /// respect, which is why this is a parameter and not a second method:
        /// </para>
        /// <list type="bullet">
        /// <item><b>Insert Module Instance</b> (<c>null</c>) writes
        /// <c>.port(  )</c> for every port - it never guesses a signal, so the
        /// user fills them in;</item>
        /// <item><b>Fill Port Connections</b> (the enclosing module) derives a
        /// signal name from each unconnected port and declares it when the
        /// enclosing module does not have it yet, so accepting the tool leaves a
        /// usable instantiation instead of a wall of empty connections.</item>
        /// </list>
        /// </param>
        /// <remarks>
        /// The derived name is the port name with the <c>_I</c> / <c>_O</c> /
        /// <c>_IO</c> direction suffix removed and the rest lower-cased
        /// (<c>CLK_I</c> -&gt; <c>clk</c>), and the declaration reuses the port's
        /// own type. That is what the editor's original
        /// <c>PortConnectionCreateSnippet.CreateString</c> did; it was lost when
        /// the text generation moved into this shared helper, leaving every
        /// connection blank.
        /// </remarks>
        public static CoreBridge.SnippetTemplate BuildInstantiationTemplate(
            Verilog.BuildingBlocks.Module module,
            string instanceName,
            string indent,
            Verilog.Items.ModuleInstantiation? existing,
            Verilog.BuildingBlocks.BuildingBlock? enclosing = null)
        {
            CoreBridge.SnippetTemplate template = new CoreBridge.SnippetTemplate();
            StringBuilder declarations = new StringBuilder();
            HashSet<string> declaredNames = new HashSet<string>(System.StringComparer.Ordinal);

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
                else if (enclosing != null)
                {
                    // Derive the signal name from the port the way the editor
                    // always did (see the remarks above) and declare it when the
                    // enclosing module does not have it yet.
                    string signal = ConnectionName(port.Name);
                    template.AppendText(signal);
                    AppendSignalDeclaration(declarations, enclosing, signal, port, declaredNames);
                }
                else
                {
                    // Blank tab stop: "Insert Module Instance" never guesses a
                    // signal, so the user fills every connection in.
                    template.AddField(port.Name, "");
                }
                template.AppendText(" )");
                firstPort = false;
            }
            if (!firstPort) template.AppendText("\n");
            // ");" closes the port list opened by " (\\n" above AND terminates
            // the statement. Dropping the ")" (it was lost when this helper
            // moved out of SnippetAdapter) produced an instantiation whose
            // parenthesis never closed: the inserted text no longer parsed, so
            // the instance disappeared from the hierarchy and re-running the
            // tool on it failed.
            template.AppendText(indent + ");");

            if (declarations.Length > 0)
            {
                // The declarations must precede the statement they are used by,
                // so they are prepended. They are literal text (no tab stops).
                CoreBridge.SnippetTemplate combined = new CoreBridge.SnippetTemplate();
                combined.AppendText(declarations.ToString());
                combined.Append(template);
                return combined;
            }
            return template;
        }

        /// <summary>
        /// Signal name the editor derives for an unconnected port: the port name
        /// with the direction suffix removed and the rest lower-cased
        /// (<c>CLK_I</c> -&gt; <c>clk</c>, <c>DATA_O</c> -&gt; <c>data</c>).
        /// </summary>
        /// <remarks>
        /// The suffix list is the one the original
        /// <c>PortConnectionCreateSnippet</c> used (<c>_I</c> / <c>_O</c> /
        /// <c>_IO</c>, compared case-insensitively).
        /// </remarks>
        public static string ConnectionName(string portName)
        {
            string name = portName;
            foreach (string suffix in DirectionSuffixes)
            {
                if (name.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                }
            }
            return name.ToLowerInvariant();
        }

        /// <summary>
        /// Direction suffixes stripped from a port name to make a signal name.
        /// </summary>
        private static readonly string[] DirectionSuffixes = { "_I", "_O", "_IO" };

        /// <summary>
        /// Appends <c>wire [..] name;</c> for <paramref name="signal"/> when
        /// <paramref name="enclosing"/> does not already declare it, using the
        /// port's own type so the connection is width-correct.
        /// </summary>
        /// <remarks>
        /// The type comes from the port's data object: a net keeps its type, a
        /// packed vector becomes a <c>wire</c> carrying the same packed
        /// dimensions, and anything else reuses its declaration text. The
        /// original editor implementation did the same three cases.
        /// </remarks>
        private static void AppendSignalDeclaration(
            StringBuilder declarations,
            Verilog.BuildingBlocks.BuildingBlock enclosing,
            string signal,
            Verilog.DataObjects.Port port,
            HashSet<string> declaredNames)
        {
            if (enclosing.NamedElements.ContainsKey(signal)) return;
            if (!declaredNames.Add(signal)) return;
            if (port.DataObject == null) return;

            Verilog.DataObjects.DataObject source = port.DataObject;
            string typeText;
            if (source is Verilog.DataObjects.Nets.Net net)
            {
                Verilog.DataObjects.Nets.Net declared = Verilog.DataObjects.Nets.Net.Create(
                    signal, Verilog.DataObjects.Nets.Net.NetTypeEnum.Wire, net.DataType?.Clone());
                typeText = declared.CreateTypeString();
            }
            else if (source is Verilog.DataObjects.Variables.IntegerVectorValueVariable vector)
            {
                Verilog.DataObjects.Nets.Net declared = Verilog.DataObjects.Nets.Net.Create(
                    signal,
                    Verilog.DataObjects.Nets.Net.NetTypeEnum.Wire,
                    Verilog.DataObjects.DataTypes.LogicType.Create(false, null));
                foreach (Verilog.DataObjects.Arrays.PackedArray dimension in vector.PackedDimensions)
                {
                    declared.PackedDimensions.Add(dimension.Clone());
                }
                typeText = declared.CreateTypeString();
            }
            else
            {
                typeText = source.CreateTypeString();
            }

            // CreateTypeString already ends with a space ("wire " / "wire  [7:0] "),
            // so the name is appended directly. A tab here produced
            // "wire \tclk;", which does not line up with the hand-written
            // declarations around it.
            declarations.Append(typeText);
            if (!typeText.EndsWith(" ", System.StringComparison.Ordinal))
            {
                declarations.Append(' ');
            }
            declarations.Append(signal);
            foreach (Verilog.DataObjects.Arrays.UnPackedArray dimension in source.UnpackedArrays)
            {
                declarations.Append(dimension.CreateString());
            }
            declarations.Append(";\n");
        }

        /// <summary>
        /// Flips the direction and the <c>_I</c> / <c>_O</c> suffix of one port
        /// declaration line, keeping the original whitespace and comment. Lines
        /// that are not a port declaration are returned unchanged.
        /// </summary>
        public static string InvertPortLine(string line)
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
            if (name.EndsWith("_I", System.StringComparison.Ordinal))
            {
                newName = name.Substring(0, name.Length - 2) + "_O";
            }
            else if (name.EndsWith("_O", System.StringComparison.Ordinal))
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
