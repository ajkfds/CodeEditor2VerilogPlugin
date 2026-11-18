using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Backwards-compatible facade over <see cref="NavigateNodeMenuAdapter"/>.
    ///
    /// <para>
    /// The RTL Hierarchy node menu and the desktop editor's NavigatePanel context
    /// menu are the same menu, so the implementation moved to
    /// <see cref="NavigateNodeMenuAdapter"/> (which serves both hosts). This class
    /// keeps the old signature for the LSP handler and for anything that still
    /// speaks in terms of a bare file / instance path instead of a
    /// <see cref="NavigateNodeMenuContext"/>.
    /// </para>
    /// </summary>
    public static class HierarchyMenuAdapter
    {
        /// <summary>One row of the menu, with its availability.</summary>
        public sealed class MenuEntry
        {
            public string Id { get; init; } = string.Empty;
            public string Label { get; init; } = string.Empty;
            public string Detail { get; init; } = string.Empty;
            public bool Enabled { get; init; }
            public string? Reason { get; init; }
            public int Group { get; init; }
        }

        /// <summary>The menu for one node.</summary>
        public sealed class MenuResult
        {
            public List<MenuEntry> Entries { get; } = new();
            public string? Message { get; set; }
        }

        /// <summary>What running an entry produced.</summary>
        public sealed class InvokeResult
        {
            public string Title { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string? FsPath { get; set; }
            public string? ClipboardText { get; set; }
            public string? Message { get; set; }
        }

        /// <summary>
        /// Builds the menu for a VS Code hierarchy node.
        /// </summary>
        public static MenuResult Build(string nodeKind, Data.IVerilogRelatedFile? file, string? instancePath)
        {
            return Build(nodeKind, file, null, instancePath, null, null);
        }

        /// <summary>
        /// Builds the menu for a VS Code hierarchy node, with the label / paths
        /// the node descriptor carries.
        /// </summary>
        public static MenuResult Build(
            string nodeKind,
            Data.IVerilogRelatedFile? file,
            string? nodeLabel,
            string? instancePath,
            string? workspaceRelativePath,
            string? fsPath)
        {
            NavigateNodeMenuContext context = new NavigateNodeMenuContext
            {
                Kind = NavigateNodeMenuContext.KindFromName(nodeKind),
                File = file,
                NodeLabel = nodeLabel,
                InstancePath = instancePath,
                WorkspaceRelativePath = workspaceRelativePath,
                FsPath = fsPath,
                DirPath = string.IsNullOrEmpty(fsPath) ? null : System.IO.Path.GetDirectoryName(fsPath),
            };

            NavigateNodeMenuAdapter.MenuResult result =
                NavigateNodeMenuAdapter.Build(context, HierarchyMenuCatalog.Hosts.VsCode);

            MenuResult legacy = new MenuResult { Message = result.Message };
            foreach (NavigateNodeMenuAdapter.MenuEntry entry in result.Entries)
            {
                // The flat list cannot nest, so submenu rows are omitted and the
                // children carry the parent label (the adapter already prefixed
                // it).
                if (entry.IsSubmenu) continue;
                legacy.Entries.Add(new MenuEntry
                {
                    Id = entry.Id,
                    // The QuickPick fallback is flat, so it needs the
                    // parent-prefixed label ("Add › Folder"); the native rows
                    // take the plain label from package.json instead.
                    Label = entry.FlatLabel,
                    Detail = entry.Detail,
                    Enabled = entry.Enabled,
                    Reason = entry.Reason,
                    Group = entry.Group,
                });
            }
            return legacy;
        }

        /// <summary>Runs an entry for a VS Code hierarchy node.</summary>
        public static InvokeResult Invoke(
            string id,
            Data.IVerilogRelatedFile? file,
            string? nodeLabel,
            string? instancePath,
            string? workspaceRelativePath,
            string? fsPath)
        {
            NavigateNodeMenuContext context = new NavigateNodeMenuContext
            {
                Kind = InferKind(instancePath, fsPath),
                File = file,
                NodeLabel = nodeLabel,
                InstancePath = instancePath,
                WorkspaceRelativePath = workspaceRelativePath,
                FsPath = fsPath,
                DirPath = string.IsNullOrEmpty(fsPath) ? null : System.IO.Path.GetDirectoryName(fsPath),
            };

            NavigateNodeMenuAdapter.InvokeResult result =
                NavigateNodeMenuAdapter.Invoke(id, context);

            return new InvokeResult
            {
                Title = result.Title,
                Text = result.Text,
                ClipboardText = result.ClipboardText,
                Message = result.Message,
                FsPath = result.Operation?.Kind == "reveal" ? result.Operation.Path : null,
            };
        }

        /// <summary>
        /// The old API only carried an instance path, so the kind is inferred:
        /// an instance path means an instance node, a file means a file node.
        /// </summary>
        private static HierarchyMenuCatalog.AppliesTo InferKind(string? instancePath, string? fsPath)
        {
            if (!string.IsNullOrEmpty(instancePath)) return HierarchyMenuCatalog.AppliesTo.Instance;
            if (!string.IsNullOrEmpty(fsPath)) return HierarchyMenuCatalog.AppliesTo.File;
            return HierarchyMenuCatalog.AppliesTo.None;
        }
    }
}
