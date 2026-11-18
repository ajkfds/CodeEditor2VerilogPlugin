using System;
using System.Collections.Generic;
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

        /// <summary>How the generated manifest is meant to be used.</summary>
        public const string UsageNote =
            "Generated from CoreBridge/HierarchyMenuCatalog.cs. Run " +
            "'dotnet run --project CodeEditor2VerilogPlugin/CodeEditor2VerilogPlugin " +
            "-- --emit-hierarchy-menu' (or the LSP server with --hierarchy-menu-manifest) " +
            "and copy the printed JSON into RtlEditorVscodeExtension/package.json. " +
            "Do not edit the generated blocks by hand.";

        /// <summary>
        /// Builds the three <c>contributes</c> arrays for the hierarchy menu.
        /// </summary>
        /// <remarks>
        /// VS Code has no API to add a native context-menu row at run time:
        /// <c>contributes.commands</c>, <c>contributes.submenus</c> and
        /// <c>contributes.menus."view/item/context"</c> are read from
        /// <c>package.json</c> when the extension loads. Nesting comes from a
        /// row whose <c>submenu</c> property names a submenu id, which is why
        /// the catalog's parent/child relation has to be flattened into this
        /// file. Generating it keeps the catalog as the single source of truth:
        /// a new entry reaches VS Code by regenerating, never by hand-editing
        /// the extension.
        /// </remarks>
        /// <summary>
        /// The menu the RTL Hierarchy tree's right-click menu is declared in.
        /// </summary>
        public const string ViewItemContextMenu = "view/item/context";

        public static Manifest Build()
        {
            Manifest manifest = new Manifest();

            foreach (HierarchyMenuCatalog.Entry entry in Children(null))
            {
                if (entry.IsSubmenu)
                {
                    // The row that OPENS the flyout belongs to the menu we are
                    // currently filling; its children go into their own menu.
                    AddSubmenu(manifest, entry, ViewItemContextMenu);
                    continue;
                }
                AddCommand(manifest, entry);
                foreach (string when in WhenClauses(entry))
                {
                    manifest.AddMenuRow(ViewItemContextMenu, new ManifestMenuRow
                    {
                        Command = CommandId(entry.Id),
                        When = when,
                        Group = GroupName(entry.Group),
                    });
                }
            }
            return manifest;
        }

        /// <summary>
        /// The catalog rows VS Code can run. A row declared
        /// <see cref="HierarchyMenuCatalog.Hosts.Desktop"/> is never written into
        /// <c>package.json</c>: the extension could not run it (it needs the
        /// editor's windows), and emitting it would add a row that does nothing.
        /// </summary>
        private static List<HierarchyMenuCatalog.Entry> Children(string? parentId)
        {
            List<HierarchyMenuCatalog.Entry> result = new List<HierarchyMenuCatalog.Entry>();
            foreach (HierarchyMenuCatalog.Entry entry in HierarchyMenuCatalog.Children(parentId))
            {
                if ((entry.Hosts & HierarchyMenuCatalog.Hosts.VsCode) == 0) continue;
                result.Add(entry);
            }
            return result;
        }

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

            foreach (HierarchyMenuCatalog.Entry child in Children(submenu.Id))
            {
                if (child.IsSubmenu)
                {
                    AddSubmenu(manifest, child, SubmenuId(submenu.Id));
                    continue;
                }
                AddCommand(manifest, child);
                foreach (string when in WhenClauses(child))
                {
                    manifest.AddMenuRow(SubmenuId(submenu.Id), new ManifestMenuRow
                    {
                        Command = CommandId(child.Id),
                        When = when,
                        Group = GroupName(child.Group),
                    });
                }
            }
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
        /// Serializes <see cref="Build"/> as the JSON an editor pastes into
        /// <c>package.json</c>. Property names are camelCased to match VS Code's
        /// manifest schema, and the output is indented so a diff shows exactly
        /// which row changed.
        /// </summary>
        public static string ToJson()
        {
            Manifest manifest = Build();
            System.Text.Json.JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                // The output is pasted into package.json by a human, so keep it
                // readable: the default encoder escapes ' and & ("\u0026\u0026"),
                // which makes the `when` clauses unreadable and diffs noisy.
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                note = UsageNote,
                commands = manifest.Commands,
                submenus = manifest.Submenus,
                // `menus` is a map of menu name -> rows. The tree's own menu is
                // "view/item/context"; a submenu's contents are keyed by the
                // submenu id, which is how VS Code fills the flyout.
                menus = manifest.Menus,
            }, options);
        }
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
