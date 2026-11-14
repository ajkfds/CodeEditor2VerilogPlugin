
using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// The RTL Hierarchy node menu, as data.
    ///
    /// <para>
    /// VS Code's <c>contributes.menus."view/item/context"</c> entries are
    /// declared statically in <c>package.json</c>, so an extension host cannot
    /// add a row at run time. That means a menu item written in C# could never
    /// appear unless the client also knew about it in advance - which is the
    /// "adding a menu means editing two repositories" problem.
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
        }

        /// <summary>Which nodes an entry applies to.</summary>
        [Flags]
        public enum AppliesTo
        {
            None = 0,
            Folder = 1,
            File = 2,
            Instance = 4,
            All = Folder | File | Instance,
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
            /// Group number, used for ordering only (the client sorts by it, so
            /// entries from different plugins interleave predictably).
            /// </summary>
            public int Group { get; init; } = 50;
        }

        private static readonly List<Entry> entries = new List<Entry>
        {
            new Entry
            {
                Id = MenuId.SimulationSetup,
                Label = "Simulation Setup (files)",
                Detail = "list the files / top module the simulator would compile",
                Targets = AppliesTo.File | AppliesTo.Instance,
                Group = 10,
            },
            new Entry
            {
                Id = MenuId.OpenInExplorer,
                Label = "Open in Explorer",
                Detail = "reveal the node's file in the OS file manager",
                Group = 40,
            },
            new Entry
            {
                Id = MenuId.CopyPath,
                Label = "Copy Path",
                Detail = "copy the workspace-relative path",
                Group = 50,
            },
            new Entry
            {
                Id = MenuId.CopyInstancePath,
                Label = "Copy Instance Path",
                Detail = "copy the instance path (u_mid.u_leaf)",
                Targets = AppliesTo.Instance,
                Group = 50,
            },
            new Entry
            {
                Id = MenuId.ShowProperty,
                Label = "Property",
                Detail = "show the node's parsed properties",
                Targets = AppliesTo.File | AppliesTo.Instance,
                Group = 60,
            },
        };

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
