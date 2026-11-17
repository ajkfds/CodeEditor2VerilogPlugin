using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// The node a navigate-node menu is being built for, described the same way
    /// by both hosts.
    ///
    /// <para>
    /// The desktop editor knows the node as a <c>NavigatePanelNode</c> and VS
    /// Code knows it as a tree item, but every row only needs the same handful of
    /// facts: which kind of node it is, which file it represents, and the paths
    /// the file operations act on. Keeping that in one plain class is what lets
    /// <see cref="NavigateNodeMenuAdapter"/> serve both hosts without knowing
    /// either UI.
    /// </para>
    ///
    /// <para>
    /// <see cref="NavigatePanelNode"/> (the desktop object) is deliberately not
    /// part of this class: the LSP host links the same assembly, so referencing
    /// an Avalonia node here would drag the UI into the headless server. A
    /// desktop-only row receives the node through the handler closure the plugin
    /// registered (see <see cref="HierarchyMenuCatalog.Entry.Handler"/>).
    /// </para>
    /// </summary>
    public sealed class NavigateNodeMenuContext
    {
        /// <summary>Kind of node the menu is for.</summary>
        public HierarchyMenuCatalog.AppliesTo Kind { get; init; } = HierarchyMenuCatalog.AppliesTo.None;

        /// <summary>
        /// The file the node belongs to. For an instance this is the instance's
        /// own parse result (it carries the parameter overrides written at the
        /// instantiation), which is what the simulation setup needs.
        /// </summary>
        public Data.IVerilogRelatedFile? File { get; init; }

        /// <summary>Text shown in the tree for the node.</summary>
        public string? NodeLabel { get; init; }

        /// <summary>Readable instance path ("u_mid.u_leaf"); null for files.</summary>
        public string? InstancePath { get; init; }

        /// <summary>Workspace-relative path ("src/rtl/top.sv"), used by Copy Path.</summary>
        public string? WorkspaceRelativePath { get; init; }

        /// <summary>Absolute path of the node's file.</summary>
        public string? FsPath { get; init; }

        /// <summary>
        /// Absolute directory the node lives in: a folder node is its own
        /// directory, a file / instance uses the directory of its file. This is
        /// where the Add operations create the new item.
        /// </summary>
        public string? DirPath { get; init; }

        /// <summary>
        /// Maps the VS Code node kind string ("folder" / "file" / "instance")
        /// onto the catalog flags. Unknown kinds yield
        /// <see cref="HierarchyMenuCatalog.AppliesTo.None"/>, which makes the
        /// menu empty rather than wrong.
        /// </summary>
        public static HierarchyMenuCatalog.AppliesTo KindFromName(string? nodeKind)
        {
            switch (nodeKind)
            {
                case "project": return HierarchyMenuCatalog.AppliesTo.Project;
                case "folder": return HierarchyMenuCatalog.AppliesTo.Folder;
                case "file": return HierarchyMenuCatalog.AppliesTo.File;
                case "instance": return HierarchyMenuCatalog.AppliesTo.Instance;
                case "header": return HierarchyMenuCatalog.AppliesTo.Header;
                case "package": return HierarchyMenuCatalog.AppliesTo.Package;
                default: return HierarchyMenuCatalog.AppliesTo.None;
            }
        }
    }
}
