
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
            /// rows. It exists to show that nesting is data: adding
            /// <c>ParentId = Simulation</c> to a row puts it in this submenu.
            /// </summary>
            public const string Simulation = "simulation";

            // ---- rows that used to be hard-coded in the editor -------------
            //
            // NavigatePanelNode.createContextMenu built Add / Delete / Open /
            // CopyPath / Property inline, so VS Code had to re-implement them in
            // hierarchy.js. They are catalog rows now, which is what lets one
            // declaration feed both menus. They stay editor-only until the
            // extension's file operations move onto ClientOperation.

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
            /// Icon asset path for the desktop menu (the editor draws SVG icons;
            /// VS Code resolves its own). Null when the row has no icon.
            /// </summary>
            public string? IconPath { get; init; }

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
            /// Id of the <see cref="IsSubmenu"/> entry this row belongs under,
            /// or null for a top-level row.
            ///
            /// <para>
            /// This is what makes the VS Code context menu hierarchical: VS Code
            /// renders a nested menu from <c>contributes.submenus</c> plus rows
            /// whose <c>submenu</c> property names one, and
            /// <see cref="HierarchyMenuManifest"/> generates both from this
            /// parent/child relation. The editor's NavigatePanel menu is flat, so
            /// nesting is a VS Code-side presentation choice, not a parse
            /// concept.
            /// </para>
            /// </summary>
            public string? ParentId { get; init; }

            /// <summary>
            /// True when the entry only groups its children and is not runnable
            /// itself (the manifest turns it into a submenu instead of a
            /// command).
            /// </summary>
            public bool IsSubmenu { get; init; }
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
                IsSubmenu = true,
                Action = ActionKind.None,
            },
            new Entry
            {
                Id = MenuId.SimulationSetup,
                Label = "Simulation Setup (files)",
                Detail = "list the files / top module the simulator would compile",
                Targets = AppliesTo.File | AppliesTo.Instance,
                Group = 10,
                ParentId = MenuId.Simulation,
                Action = ActionKind.ShowText,
            },
            new Entry
            {
                Id = MenuId.OpenInExplorer,
                Label = "Open in Explorer",
                Detail = "reveal the node's file in the OS file manager",
                Group = 40,
                Action = ActionKind.RevealInOS,
            },
            new Entry
            {
                Id = MenuId.CopyPath,
                Label = "Copy Path",
                Detail = "copy the workspace-relative path",
                Group = 50,
                Action = ActionKind.CopyPath,
            },
            new Entry
            {
                Id = MenuId.CopyInstancePath,
                Label = "Copy Instance Path",
                Detail = "copy the instance path (u_mid.u_leaf)",
                Targets = AppliesTo.Instance,
                Group = 50,
                Action = ActionKind.CopyInstancePath,
            },
            new Entry
            {
                Id = MenuId.ShowProperty,
                Label = "Property",
                Detail = "show the node's parsed properties",
                Targets = AppliesTo.File | AppliesTo.Instance,
                Group = 60,
                Action = ActionKind.ShowProperty,
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
                Hosts = Hosts.Desktop,
                Group = 10,
                IsSubmenu = true,
                Action = ActionKind.None,
            },
            new Entry
            {
                Id = MenuId.AddFolder,
                Label = "Folder",
                Detail = "create an empty directory",
                Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                Hosts = Hosts.Desktop,
                Group = 10,
                ParentId = MenuId.Add,
                Action = ActionKind.CreateFolder,
            },
            new Entry
            {
                Id = MenuId.AddBlankFile,
                Label = "Blank File",
                Detail = "create an empty file",
                Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                Hosts = Hosts.Desktop,
                Group = 10,
                ParentId = MenuId.Add,
                Action = ActionKind.CreateFile,
            },
            new Entry
            {
                Id = MenuId.AddVerilogModule,
                Label = "verilog module",
                Detail = "create a .v file with a module skeleton",
                Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                Hosts = Hosts.Desktop,
                Group = 10,
                ParentId = MenuId.Add,
                Action = ActionKind.CreateSourceFile,
                Argument = "verilog",
                IconPath = "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                IconColor = 0x64C8F0F0,
            },
            new Entry
            {
                Id = MenuId.AddSystemVerilogModule,
                Label = "SystemVerilog module",
                Detail = "create a .sv file with a module skeleton",
                Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                Hosts = Hosts.Desktop,
                Group = 10,
                ParentId = MenuId.Add,
                Action = ActionKind.CreateSourceFile,
                Argument = "systemverilog",
                IconPath = "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                IconColor = 0x64C8F0F0,
            },
            new Entry
            {
                Id = MenuId.AddSystemVerilogInterface,
                Label = "SystemVerilog interface",
                Detail = "create a .sv file with an interface skeleton",
                Targets = AppliesTo.Project | AppliesTo.Folder | AppliesTo.File | AppliesTo.Instance,
                Hosts = Hosts.Desktop,
                Group = 10,
                ParentId = MenuId.Add,
                Action = ActionKind.CreateSourceFile,
                Argument = "interface",
                IconPath = "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                IconColor = 0x64C8F0F0,
            },
            new Entry
            {
                Id = MenuId.DeleteNode,
                Label = "Delete",
                Detail = "delete the node's file or directory",
                Targets = AppliesTo.Folder | AppliesTo.File,
                Hosts = Hosts.Desktop,
                Group = 20,
                Action = ActionKind.DeleteNode,
            },
        };

        /// <summary>
        /// The entries that belong directly under <paramref name="parentId"/>
        /// (null = the top level), in group order.
        /// </summary>
        /// <remarks>
        /// <see cref="All"/> is a flat, group-ordered list because the QuickPick
        /// renders it as one list; the tree of submenus is expressed through
        /// <see cref="Entry.ParentId"/> and walked with this method.
        /// </remarks>
        public static List<Entry> Children(string? parentId)
        {
            List<Entry> result = new List<Entry>();
            lock (entries)
            {
                foreach (Entry entry in entries)
                {
                    if (!string.Equals(entry.ParentId, parentId, StringComparison.Ordinal)) continue;
                    result.Add(entry);
                }
            }
            result.Sort((a, b) =>
            {
                if (a.Group != b.Group) return a.Group.CompareTo(b.Group);
                return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        /// <summary>Every entry, in group order.</summary>
        public static IReadOnlyList<Entry> All
        {
            get
            {
                lock (entries)
                {
                    List<Entry> sorted = new List<Entry>(entries);
                    sorted.Sort((a, b) =>
                    {
                        if (a.Group != b.Group) return a.Group.CompareTo(b.Group);
                        return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
                    });
                    return sorted;
                }
            }
        }

        /// <summary>
        /// Appends an entry (or replaces the one with the same id). Used by
        /// plugins that extend the hierarchy menu; the LSP host serves whatever
        /// is registered here, so the client needs no change.
        /// </summary>
        public static void Register(Entry entry)
        {
            if (entry == null) return;
            if (string.IsNullOrEmpty(entry.Id)) return;
            lock (entries)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Id == entry.Id)
                    {
                        entries[i] = entry;
                        return;
                    }
                }
                entries.Add(entry);
            }
        }

        /// <summary>
        /// Removes a registered entry. Used by the tests and by a plugin that
        /// tears its menu contribution down again.
        /// </summary>
        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (entries)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Id != id) continue;
                    entries.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>The catalog entry for <paramref name="id"/>, or null.</summary>
        public static Entry? Find(string id)
        {
            lock (entries)
            {
                foreach (Entry entry in entries)
                {
                    if (entry.Id == id) return entry;
                }
            }
            return null;
        }
    }
}
