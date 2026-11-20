
using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// The navigate-node menu (the right-click menu of the desktop editor's
    /// NavigatePanel and of the VS Code "RTL Hierarchy" view), as data.
    ///
    /// <para>
    /// Both hosts show the same rows, so the list must not live in two places.
    /// This catalog is the single source of truth and each host maps it onto its
    /// own menu API:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>the desktop editor renders it into an Avalonia <c>ContextMenu</c>
    /// (<c>pluginVerilog.NavigatePanel.NavigateNodeMenuRenderer</c>),</item>
    /// <item>the LSP host serves it through <c>rtl/hierarchyMenu</c> and the
    /// extension renders the native VS Code rows.</item>
    /// </list>
    ///
    /// <para>
    /// Rows that only one host can run (the desktop editor owns the LLM agent,
    /// the simulation windows and the input dialogs) are declared with
    /// <see cref="Entry.Hosts"/> so the other host simply does not offer them.
    /// That is what replaces the old per-node-type
    /// <c>CustomizeNavigateNodeContextMenu</c> delegates: a plugin now calls
    /// <see cref="Register"/> instead of subscribing to an Avalonia event.
    /// </para>
    ///
    /// <para>
    /// This catalog is the one list, like <see cref="ToolMenuCatalog"/> for the
    /// editor's Shift+Space menu:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>the LSP host serves it through <c>rtl/hierarchyMenu</c>,</item>
    /// <item>the extension shows it in a QuickPick from a single static
    /// command, so <b>a new entry here appears in VS Code with no client
    /// change at all</b>,</item>
    /// <item>other plugins can append rows with <see cref="Register"/> (for
    /// example a simulation plugin adding "Run Simulation").</item>
    /// </list>
    /// </summary>
    public static class HierarchyMenuCatalog
    {
        /// <summary>
        /// Stable entry ids. They cross the LSP boundary (the client sends the
        /// id back to run the entry), so they must not change.
        /// </summary>
        public static class MenuId
        {
            /// <summary>Files / modules the simulation would compile.</summary>
            public const string SimulationSetup = "simulationSetup";

            /// <summary>Reveal the node's file in the OS file manager.</summary>
            public const string OpenInExplorer = "openInExplorer";

            /// <summary>Copy the workspace-relative path.</summary>
            public const string CopyPath = "copyPath";

            /// <summary>Copy the instance path (instances only).</summary>
            public const string CopyInstancePath = "copyInstancePath";

            /// <summary>Show the node's parsed properties.</summary>
            public const string ShowProperty = "showProperty";

            /// <summary>
            /// Grouping entry (a submenu, not a command) for the simulation
            /// rows. Nesting is the shape of the declaration: a row is in this
            /// submenu because it is written inside its <c>Children</c>.
            /// </summary>
            public const string Simulation = "simulation";

            // ---- rows that used to be hard-coded in the editor -------------
            //
            // NavigatePanelNode.createContextMenu built Add / Delete / Open /
            // CopyPath / Property inline, and the extension re-implemented the
            // file operations in hierarchy.js. They are catalog rows now, so one
            // declaration feeds both menus; the hosts differ only in how they
            // perform the returned ClientOperation (System.IO in the editor,
            // vscode.workspace.fs in the extension).

            /// <summary>Grouping row for the "create a source file" actions.</summary>
            public const string Add = "add";

            public const string AddFolder = "addFolder";
            public const string AddBlankFile = "addBlankFile";
            public const string AddVerilogModule = "addVerilogModule";
            public const string AddSystemVerilogModule = "addSystemVerilogModule";
            public const string AddSystemVerilogInterface = "addSystemVerilogInterface";
            public const string DeleteNode = "deleteNode";
        }

        /// <summary>
        /// Which hosts offer an entry.
        ///
        /// <para>
        /// The desktop editor and VS Code share almost every row, but a few need
        /// a window only one of them owns: the LLM agent, the simulation tab and
        /// the input / confirmation dialogs exist only in the editor, while
        /// "Reveal in Editor" and "Copy Instance Path" are VS Code view actions.
        /// </para>
        /// </summary>
        [Flags]
        public enum Hosts
        {
            None = 0,
            Desktop = 1,
            VsCode = 2,
            Both = Desktop | VsCode,
        }

        /// <summary>
        /// What running an entry does. A string id would have to be matched by
        /// both hosts; an enum keeps the "what" on the C# side and leaves only
        /// "how" (a file-system call, a clipboard write, a dialog) to the host.
        /// </summary>
        public enum ActionKind
        {
            /// <summary>A submenu row: it only groups its children.</summary>
            None,

            /// <summary>Copy the workspace-relative path (or the absolute one).</summary>
            CopyPath,

            /// <summary>Copy the readable instance path ("u_mid.u_leaf").</summary>
            CopyInstancePath,

            /// <summary>Reveal the node's file in the OS file manager.</summary>
            RevealInOS,

            /// <summary>Show the node's parsed properties.</summary>
            ShowProperty,

            /// <summary>Show text the server produced (e.g. the simulation file list).</summary>
            ShowText,

            /// <summary>Create a directory next to the node.</summary>
            CreateFolder,

            /// <summary>Create an empty file next to the node.</summary>
            CreateFile,

            /// <summary>Delete the node's file / directory.</summary>
            DeleteNode,

            /// <summary>Create a Verilog / SystemVerilog skeleton (see <see cref="Entry.Argument"/>).</summary>
            CreateSourceFile,

            /// <summary>
            /// The entry needs a handler only the desktop editor can run. The
            /// catalog row carries the delegate; VS Code never sees the entry
            /// because it is declared <c>Hosts.Desktop</c>.
            /// </summary>
            DesktopHandler,
        }

        /// <summary>Which nodes an entry applies to.</summary>
        [Flags]
        public enum AppliesTo
        {
            None = 0,
            /// <summary>The workspace root (the editor's ProjectNode).</summary>
            Project = 1,
            Folder = 2,
            File = 4,
            Instance = 8,
            /// <summary>An include header instance inside a file.</summary>
            Header = 16,
            /// <summary>An imported package instance inside a file.</summary>
            Package = 32,
            All = Project | Folder | File | Instance | Header | Package,
            /// <summary>The kinds the VS Code tree shows (it has no project node).</summary>
            VsCodeNodes = Folder | File | Instance,
        }

        /// <summary>One row of the hierarchy node menu.</summary>
        public sealed class Entry
        {
            /// <summary>Stable id (one of <see cref="MenuId"/>, or a plugin's own).</summary>
            public string Id { get; init; } = string.Empty;

            /// <summary>Menu label.</summary>
            public string Label { get; init; } = string.Empty;

            /// <summary>One line explaining what the entry does.</summary>
            public string Detail { get; init; } = string.Empty;

            /// <summary>Nodes the entry is offered for.</summary>
            public AppliesTo Targets { get; init; } = AppliesTo.All;

            /// <summary>
            /// Hosts that offer the entry. A row declared
            /// <see cref="Hosts.Desktop"/> is never sent to VS Code, and a row
            /// declared <see cref="Hosts.VsCode"/> never reaches the editor's
            /// context menu.
            /// </summary>
            public Hosts Hosts { get; init; } = Hosts.Both;

            /// <summary>What running the entry does (see <see cref="ActionKind"/>).</summary>
            public ActionKind Action { get; init; } = ActionKind.None;

            /// <summary>
            /// Free-form argument for the action: the source-file template id
            /// (<c>"verilog"</c> / <c>"systemverilog"</c> / <c>"interface"</c>)
            /// for <see cref="ActionKind.CreateSourceFile"/>, or any value a
            /// <see cref="Handler"/> wants.
            /// </summary>
            public string? Argument { get; init; }

            /// <summary>
            /// Icon asset path for the desktop menu (the editor draws SVG
            /// icons). Null when the row has no icon.
            /// </summary>
            /// <remarks>
            /// This is a repository-relative path into the editor's own assets,
            /// so it means nothing to VS Code. Use
            /// <see cref="VsCodeIcon"/> for the extension side.
            /// </remarks>
            public string? IconPath { get; init; }

            /// <summary>
            /// Icon for the VS Code command contribution, or null when the row
            /// has none.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Two forms, matching <c>contributes.commands[].icon</c>:
            /// </para>
            /// <list type="bullet">
            /// <item>a bare icon name that ships in the extension's
            /// <c>icons/</c> folder (<c>"verilog-document"</c>,
            /// <c>"interface"</c>, ...) - the light and dark files are paired
            /// automatically;</item>
            /// <item>a codicon in <c>$(...)</c> form (<c>"$(trash)"</c>) for a
            /// row with no matching asset.</item>
            /// </list>
            /// <para>
            /// Note that VS Code does <b>not</b> draw icons in
            /// <c>view/item/context</c> menus - the schema has no icon property
            /// there and the rows are text only. This icon is what the command
            /// palette and any toolbar placement show.
            /// </para>
            /// </remarks>
            public string? VsCodeIcon { get; init; }

            /// <summary>
            /// Icon colour as <c>0xAARRGGBB</c>. Kept as a plain number so this
            /// file stays free of Avalonia types (the LSP host links it too).
            /// </summary>
            public uint IconColor { get; init; }

            /// <summary>
            /// Runs a <see cref="ActionKind.DesktopHandler"/> entry. The context
            /// carries the node the user right-clicked, so the handler does not
            /// have to read the current selection (which would be wrong when the
            /// menu was built for another node).
            /// </summary>
            public Action<NavigateNodeMenuContext>? Handler { get; init; }

            /// <summary>
            /// Group number, used for ordering only (the client sorts by it, so
            /// entries from different plugins interleave predictably).
            /// </summary>
            public int Group { get; init; } = 50;

            /// <summary>
            /// Rows nested under this one, in display order.
            ///
            /// <para>
            /// Nesting is the shape of the declaration, not a relation
            /// reconstructed from ids: the literal below reads like the menu it
            /// describes, and every consumer (the editor's context menu, the VS
            /// Code manifest, the QuickPick fallback) just walks this list.
            /// Before, a row carried a <c>ParentId</c> and an
            /// <c>IsSubmenu</c> flag, so the tree had to be rebuilt from
            /// scattered strings - and a child written into the wrong menu (a
            /// submenu's children placed in <c>view/item/context</c>) produced a
            /// flat menu with nothing to catch it.
            /// </para>
            ///
            /// <para>
            /// A row with children is a grouping row: it is not runnable itself
            /// and both hosts render it as a submenu / flyout. The list is
            /// mutable only so <see cref="Register"/> and
            /// <see cref="Unregister"/> can extend the catalog; nothing else
            /// should touch it.
            /// </para>
            /// </summary>
            public List<Entry> Children { get; init; } = new List<Entry>();

            /// <summary>
            /// True when the entry only groups its children (see
            /// <see cref="Children"/>). Derived, so it cannot disagree with the
            /// tree the way a separately authored flag could.
            /// </summary>
            public bool IsSubmenu => Children.Count > 0;

            /// <summary>
            /// Extra condition evaluated when a menu is built, or null when the
            /// row is always offered. Used for a row whose usefulness depends on
            /// something only the running host knows - a simulation plugin being
            /// loaded, an LLM having been configured - so a menu does not show an
            /// action that cannot work.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This is <b>not</b> a substitute for <see cref="Hosts"/>:
            /// <see cref="Hosts"/> says which host can ever run the row, and
            /// <see cref="Available"/> refines it at run time. Both must pass.
            /// </para>
            /// <para>
            /// The manifest only asks about a row whose <see cref="Hosts"/>
            /// includes VS Code, and it treats a missing plugin as "not
            /// available" - so a row that needs the Icarus plugin is never
            /// written into <c>package.json</c> even though VS Code could in
            /// principle call it. That is deliberate: the extension has no way
            /// to run it, and a row that always reports "unavailable" is worse
            /// than no row.
            /// </para>
            /// </remarks>
            public Func<bool>? Available { get; init; }

            /// <summary>
            /// Whether the row should be offered right now. A null
            /// <see cref="Available"/> means "always".
            /// </summary>
            public bool IsAvailable()
            {
                try
                {
                    return Available == null || Available();
                }
                catch (Exception)
                {
                    // A predicate that throws must not take the whole menu down;
                    // hide the row instead.
                    return false;
                }
            }
        }

        private static readonly List<Entry> entries = new List<Entry>
        {
            new Entry
            {
                Id = MenuId.Simulation,
                Label = "Simulation",
                Detail = "simulation related actions",
                Targets = AppliesTo.File | AppliesTo.Instance,
                Group = 10,
                Children =
                {
                    new Entry
                    {
                        Id = MenuId.SimulationSetup,
                        Label = "Simulation Setup (files)",
                        Detail = "list the files / top module the simulator would compile",
                        Targets = AppliesTo.File | AppliesTo.Instance,
                        Group = 10,
                        Action = ActionKind.ShowText,
                    },
                },
            },
            new Entry
            {
                Id = MenuId.OpenInExplorer,
                Label = "Open in Explorer",
                Detail = "reveal the node's file in the OS file manager",
                Group = 40,
                Action = ActionKind.RevealInOS,
                IconPath = "CodeEditor2/Assets/Icons/search.svg",
                IconColor = 0x64C8C8FF,
                VsCodeIcon = "$(folder-opened)",
            },
            new Entry
            {
                Id = MenuId.CopyPath,
                Label = "Copy Path",
                Detail = "copy the workspace-relative path",
                Group = 50,
                Action = ActionKind.CopyPath,
                IconPath = "CodeEditor2/Assets/Icons/gear.svg",
                IconColor = 0x64C8C8FF,
                VsCodeIcon = "$(clippy)",
            },
            new Entry
            {
                Id = MenuId.CopyInstancePath,
                Label = "Copy Instance Path",
                Detail = "copy the instance path (u_mid.u_leaf)",
                Targets = AppliesTo.Instance,
                Group = 50,
                Action = ActionKind.CopyInstancePath,
                IconPath = "CodeEditor2/Assets/Icons/gear.svg",
                IconColor = 0x64C8C8FF,
                VsCodeIcon = "$(clippy)",
            },
            new Entry
            {
                Id = MenuId.ShowProperty,
                Label = "Property",
                Detail = "show the node's properties",
                // Every node kind: the editor's inline row offered this on the
                // project, folders, files, instances, headers and packages, and
                // the property form only needs the node's Item (not a parse
                // result). Narrowing this to File | Instance made the row
                // disappear from the other nodes when the catalog replaced the
                // inline rows.
                Targets = AppliesTo.All,
                Group = 60,
                Action = ActionKind.ShowProperty,
                IconPath = "CodeEditor2/Assets/Icons/gear.svg",
                IconColor = 0x64C8C8FF,
                VsCodeIcon = "$(gear)",
            },

            // ---- rows the editor used to build inline ----------------------
            //
            // Declared Hosts.Desktop for now: the extension still implements its
            // own addFolder / addBlankFile / deleteNode in hierarchy.js, so
            // emitting these too would show every row twice. Moving them to
            // Hosts.Both is a one-word change once the extension performs
            // ClientOperation instead.

            new Entry
            {
                Id = MenuId.Add,
                Label = "Add",
                Detail = "create a file or folder next to the node",
                Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                Group = 10,
                Children =
                {
                    new Entry
                    {
                        Id = MenuId.AddFolder,
                        Label = "Folder",
                        Detail = "create an empty directory",
                        Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                        Group = 10,
                        Action = ActionKind.CreateFolder,
                        IconPath = "CodeEditor2/Assets/Icons/folder.svg",
                        IconColor = 0x646496FF,
                        VsCodeIcon = "folder",
                    },
                    new Entry
                    {
                        Id = MenuId.AddBlankFile,
                        Label = "Blank File",
                        Detail = "create an empty file",
                        Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                        Group = 10,
                        Action = ActionKind.CreateFile,
                        IconPath = "CodeEditor2/Assets/Icons/paper2.svg",
                        IconColor = 0x646464FF,
                        VsCodeIcon = "$(new-file)",
                    },
                    new Entry
                    {
                        Id = MenuId.AddVerilogModule,
                        Label = "verilog module",
                        Detail = "create a .v file with a module skeleton",
                        Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                        Group = 10,
                        Action = ActionKind.CreateSourceFile,
                        Argument = "verilog",
                        IconPath = "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                        IconColor = 0x64C8F0F0,
                        VsCodeIcon = "verilog-document",
                    },
                    new Entry
                    {
                        Id = MenuId.AddSystemVerilogModule,
                        Label = "SystemVerilog module",
                        Detail = "create a .sv file with a module skeleton",
                        Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                        Group = 10,
                        Action = ActionKind.CreateSourceFile,
                        Argument = "systemverilog",
                        IconPath = "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                        IconColor = 0x64C8F0F0,
                        VsCodeIcon = "systemverilog-document",
                    },
                    new Entry
                    {
                        Id = MenuId.AddSystemVerilogInterface,
                        Label = "SystemVerilog interface",
                        Detail = "create a .sv file with an interface skeleton",
                        Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                        Group = 10,
                        Action = ActionKind.CreateSourceFile,
                        Argument = "interface",
                        IconPath = "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                        IconColor = 0x64C8F0F0,
                        VsCodeIcon = "systemverilog-document",
                    },
                },
            },
            new Entry
            {
                Id = MenuId.DeleteNode,
                Label = "Delete",
                Detail = "delete the node's file or directory",
                Targets = AppliesTo.Folder | AppliesTo.File,
                Group = 20,
                Action = ActionKind.DeleteNode,
                VsCodeIcon = "$(trash)",
            },
        };

        /// <summary>
        /// The top-level rows, in display order. The tree hangs off each row's
        /// <see cref="Entry.Children"/>, so a host walks it recursively instead
        /// of looking rows up by parent id.
        /// </summary>
        public static List<Entry> Root
        {
            get
            {
                lock (entries)
                {
                    List<Entry> result = new List<Entry>(entries);
                    result.Sort(CompareByGroup);
                    return result;
                }
            }
        }

        /// <summary>
        /// The children of <paramref name="entry"/>, in display order. Empty for
        /// a leaf row.
        /// </summary>
        public static List<Entry> Children(Entry entry)
        {
            if (entry == null || entry.Children.Count == 0) return new List<Entry>();
            List<Entry> result = new List<Entry>(entry.Children);
            result.Sort(CompareByGroup);
            return result;
        }

        /// <summary>
        /// Every row, flattened depth-first in display order. The QuickPick
        /// fallback uses it because it cannot nest; hosts that nest walk
        /// <see cref="Root"/> / <see cref="Children(Entry)"/> instead.
        /// </summary>
        public static IReadOnlyList<Entry> All
        {
            get
            {
                List<Entry> result = new List<Entry>();
                foreach (Entry entry in Root) Flatten(entry, result);
                return result;
            }
        }

        /// <summary>
        /// Walks <paramref name="entry"/> depth-first. Reads
        /// <see cref="Entry.Children"/> directly (no sorting copy) because the
        /// caller already holds the lock.
        /// </summary>
        private static void Flatten(Entry entry, List<Entry> result)
        {
            result.Add(entry);
            List<Entry> children = new List<Entry>(entry.Children);
            children.Sort(CompareByGroup);
            foreach (Entry child in children) Flatten(child, result);
        }

        /// <summary>Ordering used by every host: group number, then label.</summary>
        private static int CompareByGroup(Entry a, Entry b)
        {
            if (a.Group != b.Group) return a.Group.CompareTo(b.Group);
            return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The parent of <paramref name="id"/> in the tree, or null when it is a
        /// top-level row / not found. Needed to label a nested row in a flat
        /// list ("Add > Folder") and to reject running a grouping row.
        /// </summary>
        public static Entry? ParentOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (entries)
            {
                foreach (Entry entry in entries)
                {
                    Entry? found = FindParentIn(entry, id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static Entry? FindParentIn(Entry parent, string id)
        {
            foreach (Entry child in parent.Children)
            {
                if (string.Equals(child.Id, id, StringComparison.Ordinal)) return parent;
                Entry? found = FindParentIn(child, id);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>The row with this id anywhere in the tree, or null.</summary>
        public static Entry? Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Entry entry in All)
            {
                if (string.Equals(entry.Id, id, StringComparison.Ordinal)) return entry;
            }
            return null;
        }

        /// <summary>
        /// Adds a row (or replaces the one with the same id anywhere in the
        /// tree). Used by plugins that extend the hierarchy menu; the LSP host
        /// serves whatever is registered here, so the client needs no change.
        /// </summary>
        /// <param name="entry">The row to add.</param>
        /// <param name="parentId">
        /// Id of the grouping row to add it under, or null for the top level.
        /// Ignored when a row with the same id already exists (it is replaced in
        /// place, keeping its position in the tree).
        /// </param>
        public static void Register(Entry entry, string? parentId = null)
        {
            if (entry == null) return;
            if (string.IsNullOrEmpty(entry.Id)) return;
            lock (entries)
            {
                if (ReplaceInPlace(entries, entry)) return;

                if (string.IsNullOrEmpty(parentId))
                {
                    entries.Add(entry);
                    return;
                }

                Entry? parent = Find(parentId);
                if (parent == null)
                {
                    // A plugin asked for a parent that does not exist. Adding the
                    // row at the top level is better than dropping it silently,
                    // and keeps the menu usable.
                    entries.Add(entry);
                    return;
                }
                parent.Children.Add(entry);
            }
        }

        private static bool ReplaceInPlace(List<Entry> rows, Entry replacement)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].Id, replacement.Id, StringComparison.Ordinal))
                {
                    rows[i] = replacement;
                    return true;
                }
                if (ReplaceInPlace(rows[i].Children, replacement)) return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a registered row from anywhere in the tree. Used by the tests
        /// and by a plugin that tears its menu contribution down again.
        /// </summary>
        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (entries)
            {
                if (RemoveFrom(entries, id)) return;
            }
        }

        private static bool RemoveFrom(List<Entry> rows, string id)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (string.Equals(rows[i].Id, id, StringComparison.Ordinal))
                {
                    rows.RemoveAt(i);
                    return true;
                }
                if (RemoveFrom(rows[i].Children, id)) return true;
            }
            return false;
        }
    }
}
