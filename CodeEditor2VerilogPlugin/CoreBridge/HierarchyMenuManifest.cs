using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Generates the VS Code <c>package.json</c> menu contributions for the RTL
    /// Hierarchy node menu from <see cref="HierarchyMenuCatalog"/>.
    /// </summary>
    public static class HierarchyMenuManifest
    {
        /// <summary>
        /// The <c>viewItem</c> context values the hierarchy nodes carry, mapped
        /// to the catalog's <see cref="HierarchyMenuCatalog.AppliesTo"/>.
        /// </summary>
        private static readonly (string ContextValue, HierarchyMenuCatalog.AppliesTo Target)[] NodeKinds =
        {
            ("rtlFolder", HierarchyMenuCatalog.AppliesTo.Folder),
            ("rtlFile", HierarchyMenuCatalog.AppliesTo.File),
            ("rtlInstance", HierarchyMenuCatalog.AppliesTo.Instance),
        };

        /// <summary>Prefix of every generated VS Code command id.</summary>
        public const string CommandPrefix = "systemverilogLsp.hierarchy.";

        /// <summary>Prefix of every generated VS Code submenu id.</summary>
        public const string SubmenuPrefix = "systemverilogLsp.hierarchySubmenu.";

        /// <summary>The VS Code command id a catalog entry runs through.</summary>
        public static string CommandId(string entryId) => CommandPrefix + entryId;

        /// <summary>The VS Code submenu id for a catalog entry that groups rows.</summary>
        public static string SubmenuId(string entryId) => SubmenuPrefix + entryId;

        /// <summary>
        /// How the generated manifest is meant to be used.
        /// </summary>
        /// <remarks>
        /// The output is written straight into <c>package.json</c> by
        /// <see cref="WriteToPackageJson"/>; there is no manual copy step any
        /// more. VS Code reads <c>contributes</c> once, when the extension
        /// loads, so a catalog change still needs the file rewritten and the
        /// window reloaded - but forgetting the rewrite can no longer happen
        /// silently.
        /// </remarks>
        public const string UsageNote =
            "Generated from CoreBridge/HierarchyMenuCatalog.cs. Rewrite it with " +
            "'dotnet SystemVerilogLanguageServer.dll --write-hierarchy-menu-manifest " +
            "<path to package.json>' (add --check to only verify). Do not edit the " +
            "generated blocks by hand.";

        /// <summary>
        /// The menu the RTL Hierarchy tree's right-click menu is declared in.
        /// </summary>
        public const string ViewItemContextMenu = "view/item/context";

        /// <summary>
        /// Builds the three <c>contributes</c> arrays for the hierarchy menu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// VS Code has no API to add a native context-menu row at run time:
        /// <c>contributes.commands</c>, <c>contributes.submenus</c> and
        /// <c>contributes.menus</c> are read from <c>package.json</c> when the
        /// extension loads. Nesting comes from a row whose <c>submenu</c>
        /// property names a submenu id, and the children of a submenu live in
        /// the menu named after that id. Generating the file keeps the catalog
        /// as the single source of truth: a new entry reaches VS Code by
        /// regenerating, never by hand-editing the extension.
        /// </para>
        /// <para>
        /// The generated text is what <see cref="WriteToPackageJson"/> splices
        /// into <c>package.json</c>, and what the tests compare the committed
        /// file against.
        /// </para>
        /// </remarks>
        public static Manifest Build()
        {
            Manifest manifest = new Manifest();
            AddRows(manifest, HierarchyMenuCatalog.Root, ViewItemContextMenu);
            return manifest;
        }

        /// <summary>
        /// Emits <paramref name="rows"/> into <paramref name="menu"/>, recursing
        /// into each grouping row.
        /// </summary>
        /// <param name="menu">
        /// The <c>contributes.menus</c> key the rows belong to:
        /// <see cref="ViewItemContextMenu"/> at the top level, otherwise the id
        /// of the enclosing submenu.
        /// </param>
        /// <remarks>
        /// Walking the tree is what makes the nesting correct by construction: a
        /// child is emitted into its parent's menu because that is where the
        /// recursion is, not because a <c>ParentId</c> string was matched. The
        /// earlier id-based version put a submenu's children into
        /// <c>view/item/context</c>, which VS Code renders as ordinary rows of
        /// the tree's menu - the "Add entries are flat" bug.
        /// </remarks>
        private static void AddRows(
            Manifest manifest,
            IReadOnlyList<HierarchyMenuCatalog.Entry> rows,
            string menu)
        {
            foreach (HierarchyMenuCatalog.Entry entry in rows)
            {
                if (!OffersToVsCode(entry)) continue;

                if (entry.IsSubmenu)
                {
                    AddSubmenu(manifest, entry, menu);
                    continue;
                }

                AddCommand(manifest, entry);
                foreach (string when in WhenClauses(entry))
                {
                    manifest.AddMenuRow(menu, new ManifestMenuRow
                    {
                        Command = CommandId(entry.Id),
                        When = when,
                        Group = GroupName(entry.Group),
                    });
                }
            }
        }

        /// <summary>
        /// Whether the row is part of the extension manifest. A
        /// <see cref="HierarchyMenuCatalog.Hosts.Desktop"/> row cannot run in VS
        /// Code, so it is never written to <c>package.json</c>.
        /// </summary>
        private static bool OffersToVsCode(HierarchyMenuCatalog.Entry entry)
            => (entry.Hosts & HierarchyMenuCatalog.Hosts.VsCode) != 0;

        /// <summary>
        /// Emits a submenu: its <c>contributes.submenus</c> entry, one row in
        /// <paramref name="parentMenu"/> per node kind that opens it, and the
        /// rows of its children in a menu keyed by the submenu id.
        /// </summary>
        /// <param name="parentMenu">
        /// The menu the flyout-opening row belongs to
        /// (<see cref="ViewItemContextMenu"/> at the top level, otherwise the
        /// id of the enclosing submenu).
        /// </param>
        /// <remarks>
        /// <para>
        /// A submenu's children must be declared in
        /// <c>contributes.menus["&lt;submenu id&gt;"]</c>, NOT in
        /// <c>view/item/context</c>. VS Code fills a flyout from the menu named
        /// after the submenu id; a child left in <c>view/item/context</c> is
        /// rendered as an ordinary row of the tree's context menu, which is why
        /// the Add rows appeared flat next to the "Add" flyout instead of
        /// inside it.
        /// </para>
        /// <para>
        /// The children also keep their <c>when</c> clause: a submenu does not
        /// inherit the parent row's context, so each child re-states
        /// <c>view == ... &amp;&amp; viewItem == ...</c>.
        /// </para>
        /// </remarks>
        private static void AddSubmenu(
            Manifest manifest,
            HierarchyMenuCatalog.Entry submenu,
            string parentMenu)
        {
            manifest.Submenus.Add(new ManifestSubmenu
            {
                Id = SubmenuId(submenu.Id),
                Label = submenu.Label,
            });

            foreach (string when in WhenClauses(submenu))
            {
                manifest.AddMenuRow(parentMenu, new ManifestMenuRow
                {
                    Submenu = SubmenuId(submenu.Id),
                    When = when,
                    Group = GroupName(submenu.Group),
                });
            }

            // The flyout's contents live in the menu named after the submenu id;
            // the recursion is what puts them there.
            AddRows(manifest, HierarchyMenuCatalog.Children(submenu), SubmenuId(submenu.Id));
        }

        /// <summary>
        /// Adds the <c>contributes.commands</c> row. The title is the catalog
        /// label, so the menu shows exactly what C# declares.
        /// </summary>
        private static void AddCommand(Manifest manifest, HierarchyMenuCatalog.Entry entry)
        {
            manifest.Commands.Add(new ManifestCommand
            {
                Command = CommandId(entry.Id),
                Title = entry.Label,
                Category = "RTL Hierarchy",
            });
        }

        /// <summary>
        /// One <c>when</c> clause per node kind the entry applies to. VS Code
        /// has no "one of" operator over context values in a form that also
        /// keeps the row order stable, so a row is emitted per kind.
        /// </summary>
        private static IEnumerable<string> WhenClauses(HierarchyMenuCatalog.Entry entry)
        {
            foreach ((string contextValue, HierarchyMenuCatalog.AppliesTo target) in NodeKinds)
            {
                if ((entry.Targets & target) == 0) continue;
                yield return "view == systemverilogLsp.hierarchy && viewItem == " + contextValue;
            }
        }

        /// <summary>
        /// VS Code groups menu rows by a string; the catalog's numeric group is
        /// turned into a zero-padded name so the order is preserved.
        /// </summary>
        private static string GroupName(int group) => group.ToString("D3") + "_hierarchyMenu";

        /// <summary>
        /// Serializes <see cref="Build"/> as standalone JSON. Kept for
        /// diagnostics (printing the manifest to stdout); the committed
        /// <c>package.json</c> is written by <see cref="WriteToPackageJson"/>.
        /// </summary>
        public static string ToJson()
        {
            Manifest manifest = Build();
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                note = UsageNote,
                commands = manifest.Commands,
                submenus = manifest.Submenus,
                // `menus` is a map of menu name -> rows. The tree's own menu is
                // "view/item/context"; a submenu's contents are keyed by the
                // submenu id, which is how VS Code fills the flyout.
                menus = manifest.Menus,
            }, CreateSerializerOptions());
        }

        /// <summary>
        /// Rewrites the hierarchy-menu blocks of a <c>package.json</c> in place,
        /// or (with <paramref name="write"/> false) only reports whether they
        /// are already current.
        /// </summary>
        /// <param name="path">Path to the extension's <c>package.json</c>.</param>
        /// <param name="write">
        /// <c>true</c> to write the file, <c>false</c> to compare only.
        /// </param>
        /// <remarks>
        /// <para>
        /// Only the blocks this generator owns are touched:
        /// <c>contributes.commands</c> (the rows whose id starts with
        /// <see cref="CommandPrefix"/>), <c>contributes.submenus</c> (all of
        /// them are generated) and <c>contributes.menus</c> (the rows that
        /// reference a generated id, plus the submenu-keyed menus). Everything
        /// else - the extension's own commands, <c>editor/context</c>,
        /// <c>view/title</c>, <c>configuration</c>, ... - is left byte for byte
        /// as it was, which is why this splices text instead of re-serialising
        /// the whole document.
        /// </para>
        /// <para>
        /// This replaces the old "print the JSON and paste it in by hand" step.
        /// Forgetting that paste was invisible: the server was up to date while
        /// the menu silently kept showing the previous rows.
        /// </para>
        /// </remarks>
        public static PackageJsonWriteResult WriteToPackageJson(string path, bool write)
        {
            if (string.IsNullOrEmpty(path))
            {
                return PackageJsonWriteResult.FailedWith("no package.json path given");
            }
            if (!System.IO.File.Exists(path))
            {
                return PackageJsonWriteResult.FailedWith("package.json not found: " + path);
            }

            string original = System.IO.File.ReadAllText(path);

            bool upToDate;
            string updated;
            try
            {
                upToDate = IsUpToDate(original);
                updated = upToDate ? original : SpliceInto(original);
            }
            catch (Exception ex)
            {
                return PackageJsonWriteResult.FailedWith(
                    "could not update " + path + ": " + ex.Message);
            }

            if (upToDate)
            {
                return new PackageJsonWriteResult
                {
                    Changed = false,
                    Message = path + " is already up to date",
                };
            }

            if (!write)
            {
                return new PackageJsonWriteResult
                {
                    Changed = true,
                    Message = path + " is out of date (run with --write-hierarchy-menu-manifest)",
                };
            }

            // Keep the file's line endings: the repository stores LF, and a
            // Windows checkout must not turn the whole file into a diff.
            string newline = original.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            if (newline == "\r\n") updated = updated.Replace("\n", "\r\n");
            System.IO.File.WriteAllText(path, updated);

            return new PackageJsonWriteResult
            {
                Changed = true,
                Message = path + " updated",
            };
        }

        /// <summary>
        /// True when the generated blocks in <paramref name="text"/> already say
        /// what the catalog says.
        /// </summary>
        /// <remarks>
        /// Compares the generated rows as <b>data</b>, not as text: the file is
        /// hand-formatted (2-space indent, its own property order), so a byte
        /// comparison would report a stale file forever and make <c>--check</c>
        /// useless as a build gate. Only the blocks this generator owns are
        /// compared; the extension's own rows are ignored.
        /// </remarks>
        private static bool IsUpToDate(string text)
        {
            Manifest manifest = Build();

            System.Text.Json.Nodes.JsonObject root =
                System.Text.Json.Nodes.JsonNode.Parse(text)?.AsObject()
                ?? throw new InvalidOperationException("package.json is not a JSON object");
            System.Text.Json.Nodes.JsonObject contributes =
                root["contributes"]?.AsObject()
                ?? throw new InvalidOperationException("package.json has no contributes object");

            // commands: the generated subset, in catalog order
            List<string> actualCommands = new List<string>();
            if (contributes["commands"] is System.Text.Json.Nodes.JsonArray commands)
            {
                foreach (System.Text.Json.Nodes.JsonNode? node in commands)
                {
                    if (!IsGeneratedCommand(node)) continue;
                    actualCommands.Add(Signature(node));
                }
            }
            List<string> expectedCommands = new List<string>();
            foreach (ManifestCommand command in manifest.Commands)
            {
                expectedCommands.Add(Signature(System.Text.Json.Nodes.JsonNode.Parse(Serialize(command))));
            }
            if (!SequenceEqual(actualCommands, expectedCommands)) return false;

            // submenus: entirely generated
            List<string> actualSubmenus = new List<string>();
            if (contributes["submenus"] is System.Text.Json.Nodes.JsonArray submenus)
            {
                foreach (System.Text.Json.Nodes.JsonNode? node in submenus)
                {
                    actualSubmenus.Add(Signature(node));
                }
            }
            List<string> expectedSubmenus = new List<string>();
            foreach (ManifestSubmenu submenu in manifest.Submenus)
            {
                expectedSubmenus.Add(Signature(System.Text.Json.Nodes.JsonNode.Parse(Serialize(submenu))));
            }
            if (!SequenceEqual(actualSubmenus, expectedSubmenus)) return false;

            // menus: every generated menu must exist with exactly its rows, and
            // the tree's own menu must carry the generated rows in order
            if (contributes["menus"] is not System.Text.Json.Nodes.JsonObject menus) return false;

            foreach (KeyValuePair<string, List<ManifestMenuRow>> menu in manifest.Menus)
            {
                List<string> actualRows = new List<string>();
                if (menus[menu.Key] is System.Text.Json.Nodes.JsonArray rows)
                {
                    foreach (System.Text.Json.Nodes.JsonNode? node in rows)
                    {
                        // In view/item/context the extension's own rows are
                        // interleaved; only the generated ones are ours.
                        if (menu.Key == ViewItemContextMenu && !IsGeneratedMenuRow(node)) continue;
                        actualRows.Add(Signature(node));
                    }
                }
                List<string> expectedRows = new List<string>();
                foreach (ManifestMenuRow row in menu.Value)
                {
                    expectedRows.Add(Signature(System.Text.Json.Nodes.JsonNode.Parse(Serialize(row))));
                }
                if (!SequenceEqual(actualRows, expectedRows)) return false;
            }

            // a generated submenu that no longer exists in the catalog must be
            // gone from the file too
            foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> menu in menus)
            {
                if (!menu.Key.StartsWith(SubmenuPrefix, StringComparison.Ordinal)) continue;
                if (!manifest.Menus.ContainsKey(menu.Key)) return false;
            }
            return true;
        }

        /// <summary>Order-independent identity of one JSON row.</summary>
        private static string Signature(System.Text.Json.Nodes.JsonNode? node)
        {
            if (node is not System.Text.Json.Nodes.JsonObject obj) return string.Empty;
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> property in obj)
            {
                parts.Add(property.Key + "=" + (property.Value?.ToJsonString() ?? "null"));
            }
            parts.Sort(StringComparer.Ordinal);
            return string.Join(";", parts);
        }

        private static bool SequenceEqual(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        /// <summary>
        /// Replaces the generated blocks in <paramref name="text"/> and returns
        /// the result. Throws when the document does not look like the extension
        /// manifest (a missing key is a bug in the caller, not user input).
        /// </summary>
        private static string SpliceInto(string text)
        {
            Manifest manifest = Build();

            System.Text.Json.Nodes.JsonObject root =
                System.Text.Json.Nodes.JsonNode.Parse(text)?.AsObject()
                ?? throw new InvalidOperationException("package.json is not a JSON object");

            System.Text.Json.Nodes.JsonObject contributes =
                root["contributes"]?.AsObject()
                ?? throw new InvalidOperationException("package.json has no contributes object");

            // ---- build the new values -------------------------------------
            System.Text.Json.Nodes.JsonArray newCommands =
                MergeCommands(contributes["commands"] as System.Text.Json.Nodes.JsonArray, manifest);
            System.Text.Json.Nodes.JsonArray newSubmenus = ToNodeArray(manifest.Submenus);
            System.Text.Json.Nodes.JsonObject newMenus =
                MergeMenus(contributes["menus"] as System.Text.Json.Nodes.JsonObject, manifest);

            // ---- locate the spans in the original text --------------------
            int contributesStart = FindObjectValueStart(text, 0, "contributes")
                ?? throw new InvalidOperationException("could not locate contributes");

            (int commandsStart, int commandsEnd) = FindValueSpan(text, contributesStart, "commands")
                ?? throw new InvalidOperationException("could not locate contributes.commands");
            (int submenusStart, int submenusEnd) = FindValueSpan(text, contributesStart, "submenus")
                ?? throw new InvalidOperationException("could not locate contributes.submenus");
            (int menusStart, int menusEnd) = FindValueSpan(text, contributesStart, "menus")
                ?? throw new InvalidOperationException("could not locate contributes.menus");

            // ---- splice from the LAST span to the first -------------------
            // Removing an earlier span would shift every offset after it, so
            // the edits must run in descending order of start offset. The
            // properties are not in a fixed order in the file (submenus comes
            // before commands today), so sorting is required rather than
            // relying on the declaration order above.
            (int Start, int End, string Replacement)[] edits =
            {
                (commandsStart, commandsEnd, Indent(Serialize(newCommands), 4)),
                (submenusStart, submenusEnd, Indent(Serialize(newSubmenus), 4)),
                (menusStart, menusEnd, Indent(Serialize(newMenus), 4)),
            };
            Array.Sort(edits, (a, b) => b.Start.CompareTo(a.Start));

            StringBuilder sb = new StringBuilder(text);
            foreach ((int start, int end, string replacement) in edits)
            {
                sb.Remove(start, end - start).Insert(start, replacement);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Keeps the extension's own commands and replaces the generated ones
        /// at the position they already occupy, so the diff stays small.
        /// </summary>
        private static System.Text.Json.Nodes.JsonArray MergeCommands(
            System.Text.Json.Nodes.JsonArray? existing,
            Manifest manifest)
        {
            List<System.Text.Json.Nodes.JsonNode?> kept = new List<System.Text.Json.Nodes.JsonNode?>();
            int insertAt = -1;
            if (existing != null)
            {
                foreach (System.Text.Json.Nodes.JsonNode? node in existing)
                {
                    if (IsGeneratedCommand(node))
                    {
                        if (insertAt < 0) insertAt = kept.Count;
                        continue;
                    }
                    kept.Add(node);
                }
            }
            if (insertAt < 0) insertAt = kept.Count;

            List<System.Text.Json.Nodes.JsonNode?> generated =
                ToNodeArray(manifest.Commands).Cast<System.Text.Json.Nodes.JsonNode?>().ToList();
            kept.InsertRange(insertAt, generated);

            System.Text.Json.Nodes.JsonArray result = new System.Text.Json.Nodes.JsonArray();
            foreach (System.Text.Json.Nodes.JsonNode? node in kept)
            {
                result.Add(node?.DeepClone());
            }
            return result;
        }

        /// <summary>
        /// Rebuilds <c>contributes.menus</c>: generated submenu menus first (in
        /// catalog order), then whatever else the file declares. Only the rows
        /// that reference a generated id are replaced inside
        /// <c>view/item/context</c>; the extension's own rows keep their place.
        /// </summary>
        private static System.Text.Json.Nodes.JsonObject MergeMenus(
            System.Text.Json.Nodes.JsonObject? existing,
            Manifest manifest)
        {
            System.Text.Json.Nodes.JsonObject result = new System.Text.Json.Nodes.JsonObject();

            // 1. the generated submenu-keyed menus, in catalog order
            foreach (KeyValuePair<string, List<ManifestMenuRow>> menu in manifest.Menus)
            {
                if (menu.Key == ViewItemContextMenu) continue;
                result[menu.Key] = ToNodeArray(menu.Value);
            }

            // 2. everything else, in the order the file already had it
            if (existing != null)
            {
                foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> menu in existing)
                {
                    // a submenu menu that is no longer generated is dropped
                    if (menu.Key.StartsWith(SubmenuPrefix, StringComparison.Ordinal)) continue;

                    if (menu.Key != ViewItemContextMenu)
                    {
                        result[menu.Key] = menu.Value?.DeepClone();
                        continue;
                    }

                    // the tree's own menu: keep the extension's rows, replace
                    // the generated ones where they were
                    List<System.Text.Json.Nodes.JsonNode?> kept = new List<System.Text.Json.Nodes.JsonNode?>();
                    int insertAt = -1;
                    if (menu.Value is System.Text.Json.Nodes.JsonArray rows)
                    {
                        foreach (System.Text.Json.Nodes.JsonNode? row in rows)
                        {
                            if (IsGeneratedMenuRow(row))
                            {
                                if (insertAt < 0) insertAt = kept.Count;
                                continue;
                            }
                            kept.Add(row);
                        }
                    }
                    if (insertAt < 0) insertAt = kept.Count;
                    kept.InsertRange(insertAt,
                        ToNodeArray(manifest.Menus[ViewItemContextMenu])
                            .Cast<System.Text.Json.Nodes.JsonNode?>().ToList());

                    System.Text.Json.Nodes.JsonArray merged = new System.Text.Json.Nodes.JsonArray();
                    foreach (System.Text.Json.Nodes.JsonNode? node in kept)
                    {
                        merged.Add(node?.DeepClone());
                    }
                    result[ViewItemContextMenu] = merged;
                }
            }

            // a fresh manifest may not have had the key at all
            if (!result.ContainsKey(ViewItemContextMenu))
            {
                result[ViewItemContextMenu] =
                    ToNodeArray(manifest.Menus[ViewItemContextMenu]);
            }
            return result;
        }

        private static bool IsGeneratedCommand(System.Text.Json.Nodes.JsonNode? node)
        {
            string? command = node?["command"]?.GetValue<string>();
            return command != null && command.StartsWith(CommandPrefix, StringComparison.Ordinal);
        }

        private static bool IsGeneratedMenuRow(System.Text.Json.Nodes.JsonNode? node)
        {
            if (IsGeneratedCommand(node)) return true;
            string? submenu = node?["submenu"]?.GetValue<string>();
            return submenu != null && submenu.StartsWith(SubmenuPrefix, StringComparison.Ordinal);
        }

        private static System.Text.Json.Nodes.JsonArray ToNodeArray<T>(IEnumerable<T> items)
        {
            System.Text.Json.Nodes.JsonArray array = new System.Text.Json.Nodes.JsonArray();
            foreach (T item in items)
            {
                array.Add(System.Text.Json.Nodes.JsonNode.Parse(Serialize(item)));
            }
            return array;
        }

        private static System.Text.Json.JsonSerializerOptions CreateSerializerOptions()
        {
            return new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                // Keep `when` clauses readable: the default encoder escapes ' and
                // & ("\u0026\u0026"), which turns every row into noise.
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        private static string Serialize<T>(T value)
            => System.Text.Json.JsonSerializer.Serialize(value, CreateSerializerOptions());

        /// <summary>
        /// Re-indents a serialized value so its first line keeps the caller's
        /// column and every following line gains <paramref name="spaces"/>.
        /// </summary>
        private static string Indent(string json, int spaces)
        {
            string pad = new string(' ', spaces);
            string[] lines = json.Replace("\r\n", "\n").Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) sb.Append(lines[i]);
                else sb.Append('\n').Append(pad).Append(lines[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Finds the start of the value of <paramref name="property"/> inside the
        /// object that begins at <paramref name="objectStart"/>. Used to find
        /// <c>contributes</c> in the document.
        /// </summary>
        private static int? FindObjectValueStart(string text, int objectStart, string property)
        {
            int[]? span = FindPropertyValue(text, objectStart, property);
            return span?[0];
        }

        /// <summary>
        /// Finds the <c>[</c>..<c>]</c> or <c>{</c>..<c>}</c> span of the value of
        /// <paramref name="property"/> in the object at
        /// <paramref name="objectStart"/>.
        /// </summary>
        private static (int Start, int End)? FindValueSpan(string text, int objectStart, string property)
        {
            int[]? span = FindPropertyValue(text, objectStart, property);
            if (span == null) return null;
            return (span[0], span[1]);
        }

        /// <summary>
        /// Returns <c>[valueStart, valueEndExclusive]</c> for
        /// <paramref name="property"/> declared in the object that starts at
        /// <paramref name="objectStart"/>.
        /// </summary>
        /// <remarks>
        /// Walks the object property by property and SKIPS each value, so a
        /// nested object's <c>}</c> cannot be mistaken for the end of the object
        /// being searched. String literals are skipped too, so a
        /// <c>"commands"</c> inside a string never matches.
        /// </remarks>
        private static int[]? FindPropertyValue(string text, int objectStart, string property)
        {
            int i = SkipWhitespace(text, objectStart);
            if (i >= text.Length || text[i] != '{') return null;
            i = SkipWhitespace(text, i + 1);

            while (i < text.Length)
            {
                if (text[i] == '}') return null; // end of this object, key not found
                if (text[i] != '"') return null; // malformed (not a key)

                int keyEnd = SkipString(text, i);
                string key = text.Substring(i + 1, keyEnd - i - 2);

                int colon = SkipWhitespace(text, keyEnd);
                if (colon >= text.Length || text[colon] != ':') return null;

                int valueStart = SkipWhitespace(text, colon + 1);
                if (valueStart >= text.Length) return null;

                int valueEnd = SkipValue(text, valueStart);
                if (valueEnd < 0) return null;

                if (string.Equals(key, property, StringComparison.Ordinal))
                {
                    return new[] { valueStart, valueEnd };
                }

                // move to the next property (past the separating comma)
                i = SkipWhitespace(text, valueEnd);
                if (i < text.Length && text[i] == ',') i = SkipWhitespace(text, i + 1);
            }
            return null;
        }

        /// <summary>Returns the index just past the closing quote.</summary>
        private static int SkipString(string text, int start)
        {
            int i = start + 1;
            while (i < text.Length)
            {
                if (text[i] == '\\') { i += 2; continue; }
                if (text[i] == '"') return i + 1;
                i++;
            }
            return text.Length;
        }

        private static int SkipWhitespace(string text, int start)
        {
            int i = start;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            return i;
        }

        /// <summary>
        /// Returns the index just past the value that starts at
        /// <paramref name="start"/> (bracket/brace matched, string aware).
        /// </summary>
        private static int SkipValue(string text, int start)
        {
            char open = text[start];
            if (open == '"') return SkipString(text, start);

            char close = open switch
            {
                '[' => ']',
                '{' => '}',
                _ => '\0',
            };
            if (close == '\0')
            {
                // scalar (number / true / false / null): read to the delimiter
                int i = start;
                while (i < text.Length && text[i] != ',' && text[i] != '}' && text[i] != ']'
                       && !char.IsWhiteSpace(text[i]))
                {
                    i++;
                }
                return i;
            }

            int depth = 0;
            int j = start;
            while (j < text.Length)
            {
                char c = text[j];
                if (c == '"') { j = SkipString(text, j); continue; }
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0) return j + 1;
                }
                j++;
            }
            return -1;
        }
    }

    /// <summary>Outcome of <see cref="HierarchyMenuManifest.WriteToPackageJson"/>.</summary>
    public sealed class PackageJsonWriteResult
    {
        /// <summary>True when the file differs from (or was written from) the catalog.</summary>
        public bool Changed { get; init; }

        /// <summary>Human readable outcome, printed by the CLI.</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>True when the operation could not run at all.</summary>
        public bool Failed { get; init; }

        public static PackageJsonWriteResult FailedWith(string message)
            => new() { Failed = true, Message = message };
    }

    /// <summary>The generated <c>contributes</c> fragments.</summary>
    public sealed class Manifest
    {
        /// <summary>Rows for <c>contributes.commands</c>.</summary>
        public List<ManifestCommand> Commands { get; } = new();

        /// <summary>Rows for <c>contributes.submenus</c>.</summary>
        public List<ManifestSubmenu> Submenus { get; } = new();

        /// <summary>
        /// Rows keyed by the <c>contributes.menus</c> property they belong to:
        /// <c>view/item/context</c> for the tree's own menu, and
        /// <c>&lt;submenu id&gt;</c> for the contents of a flyout.
        /// </summary>
        /// <remarks>
        /// VS Code reads a flyout's contents from the menu named after the
        /// submenu id. Putting a submenu's children in
        /// <c>view/item/context</c> instead makes them ordinary rows of the
        /// tree's context menu (the flat "Add" rows next to the "Add" flyout).
        /// </remarks>
        public SortedDictionary<string, List<ManifestMenuRow>> Menus { get; } =
            new(StringComparer.Ordinal);

        /// <summary>Appends a row to the menu called <paramref name="menu"/>.</summary>
        public void AddMenuRow(string menu, ManifestMenuRow row)
        {
            if (!Menus.TryGetValue(menu, out List<ManifestMenuRow>? rows))
            {
                rows = new List<ManifestMenuRow>();
                Menus[menu] = rows;
            }
            rows.Add(row);
        }
    }

    /// <summary>One <c>contributes.commands</c> row.</summary>
    public sealed class ManifestCommand
    {
        public string Command { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>One <c>contributes.submenus</c> row.</summary>
    public sealed class ManifestSubmenu
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// One <c>view/item/context</c> row: either a <c>command</c> or a
    /// <c>submenu</c> (VS Code requires exactly one of the two).
    /// </summary>
    public sealed class ManifestMenuRow
    {
        public string? Command { get; set; }
        public string? Submenu { get; set; }
        public string When { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }
}
