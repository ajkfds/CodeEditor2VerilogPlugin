
using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free builder for the RTL Hierarchy node menu (the VS Code counterpart
    /// of the editor's NavigatePanel right-click menu).
    ///
    /// <para>
    /// The rows come from <see cref="HierarchyMenuCatalog"/>, so the list lives
    /// on the C# side and a new entry appears in VS Code without touching the
    /// extension. This adapter adds the two things a static
    /// <c>package.json</c> menu cannot express: <b>which rows apply to the
    /// selected node</b> and <b>what each row produces</b>.
    /// </para>
    ///
    /// <para>
    /// Nothing here needs an Avalonia dispatcher, so it runs in the headless
    /// LSP host. The results are plain text the client displays (a QuickPick or
    /// an information dialog); the client performs no analysis of its own.
    /// </para>
    /// </summary>
    public static class HierarchyMenuAdapter
    {
        /// <summary>One row of the menu, with its availability.</summary>
        public sealed class MenuEntry
        {
            /// <summary>Stable id (one of <see cref="HierarchyMenuCatalog.MenuId"/>).</summary>
            public string Id { get; init; } = string.Empty;

            /// <summary>Menu label.</summary>
            public string Label { get; init; } = string.Empty;

            /// <summary>One line explaining what the entry does.</summary>
            public string Detail { get; init; } = string.Empty;

            /// <summary><c>false</c> when the entry cannot run for this node.</summary>
            public bool Enabled { get; init; }

            /// <summary>Why the entry cannot run (only set when disabled).</summary>
            public string? Reason { get; init; }

            /// <summary>Ordering group (see <see cref="HierarchyMenuCatalog.Entry.Group"/>).</summary>
            public int Group { get; init; }
        }

        /// <summary>The menu for one node.</summary>
        public sealed class MenuResult
        {
            /// <summary>Rows, already filtered to the ones that apply to the node.</summary>
            public List<MenuEntry> Entries { get; } = new();

            /// <summary>Why the menu is empty (only set when it is).</summary>
            public string? Message { get; set; }
        }

        /// <summary>What running an entry produced.</summary>
        public sealed class InvokeResult
        {
            /// <summary>Title for the client's dialog.</summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>Body text (may be multi-line; the client only displays it).</summary>
            public string Text { get; set; } = string.Empty;

            /// <summary>
            /// A file the client should open / reveal, when the entry has one
            /// (absolute path). Null for pure text results.
            /// </summary>
            public string? FsPath { get; set; }

            /// <summary>Clipboard text, when the entry copies something.</summary>
            public string? ClipboardText { get; set; }

            /// <summary>Why the entry could not run.</summary>
            public string? Message { get; set; }
        }

        /// <summary>
        /// Builds the menu for a node.
        /// </summary>
        /// <param name="nodeKind">"folder" / "file" / "instance" (see the hierarchy node kind).</param>
        /// <param name="file">The file the node belongs to, when the server could resolve it.</param>
        /// <param name="instancePath">Readable instance path (instances only).</param>
        public static MenuResult Build(string nodeKind, Data.IVerilogRelatedFile? file, string? instancePath)
        {
            MenuResult result = new MenuResult();

            HierarchyMenuCatalog.AppliesTo target = ToTarget(nodeKind);
            if (target == HierarchyMenuCatalog.AppliesTo.None)
            {
                result.Message = "no menu for this node";
                return result;
            }

            foreach (HierarchyMenuCatalog.Entry entry in HierarchyMenuCatalog.All)
            {
                if ((entry.Targets & target) == 0) continue;
                // A submenu is a grouping row: the native VS Code menu renders
                // it as a flyout (see HierarchyMenuManifest), and a QuickPick
                // cannot nest, so listing it here would offer a row that does
                // nothing. Its children are listed instead, prefixed with the
                // parent label.
                if (entry.IsSubmenu) continue;

                string? reason = GetDisabledReason(entry, file, instancePath);
                result.Entries.Add(new MenuEntry
                {
                    Id = entry.Id,
                    Label = BuildQuickPickLabel(entry),
                    Detail = entry.Detail,
                    Enabled = reason == null,
                    Reason = reason,
                    Group = entry.Group,
                });
            }
            return result;
        }

        /// <summary>
        /// Label shown in the QuickPick. A nested row is prefixed with its
        /// parent's label, because the flat list has no other way to show the
        /// hierarchy the native menu renders as a submenu.
        /// </summary>
        private static string BuildQuickPickLabel(HierarchyMenuCatalog.Entry entry)
        {
            if (string.IsNullOrEmpty(entry.ParentId)) return entry.Label;
            HierarchyMenuCatalog.Entry? parent = HierarchyMenuCatalog.Find(entry.ParentId!);
            if (parent == null) return entry.Label;
            return parent.Label + " › " + entry.Label;
        }

        private static HierarchyMenuCatalog.AppliesTo ToTarget(string nodeKind)
        {
            switch (nodeKind)
            {
                case "folder": return HierarchyMenuCatalog.AppliesTo.Folder;
                case "file": return HierarchyMenuCatalog.AppliesTo.File;
                case "instance": return HierarchyMenuCatalog.AppliesTo.Instance;
                default: return HierarchyMenuCatalog.AppliesTo.None;
            }
        }

        /// <summary>
        /// Returns why <paramref name="entry"/> cannot run for the node, or null
        /// when it can. Only the entries that need a parse result are checked
        /// here; the ones the client performs itself (reveal / copy) are always
        /// available.
        /// </summary>
        private static string? GetDisabledReason(
            HierarchyMenuCatalog.Entry entry,
            Data.IVerilogRelatedFile? file,
            string? instancePath)
        {
            switch (entry.Id)
            {
                case HierarchyMenuCatalog.MenuId.SimulationSetup:
                    if (file == null) return "the file could not be resolved";
                    if (file.VerilogParsedDocument?.Root == null) return "the file has not been parsed yet";
                    return null;

                case HierarchyMenuCatalog.MenuId.CopyInstancePath:
                    return string.IsNullOrEmpty(instancePath)
                        ? "this node has no instance path"
                        : null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Runs the entry for a node and returns what the client should show.
        /// </summary>
        /// <param name="id">The entry id (from <see cref="MenuResult.Entries"/>).</param>
        /// <param name="file">The file the node belongs to (null when unresolved).</param>
        /// <param name="nodeLabel">Node label, used in the dialog titles.</param>
        /// <param name="instancePath">Readable instance path (instances only).</param>
        /// <param name="workspaceRelativePath">Workspace-relative path (for Copy Path).</param>
        /// <param name="fsPath">Absolute path of the node's file (for reveal / copy).</param>
        public static InvokeResult Invoke(
            string id,
            Data.IVerilogRelatedFile? file,
            string? nodeLabel,
            string? instancePath,
            string? workspaceRelativePath,
            string? fsPath)
        {
            InvokeResult result = new InvokeResult();

            HierarchyMenuCatalog.Entry? entry = HierarchyMenuCatalog.Find(id);
            if (entry == null)
            {
                result.Message = "unknown menu entry: " + id;
                return result;
            }
            result.Title = entry.Label;

            // A submenu only groups rows; it is never invoked (the native menu
            // renders it as a flyout, and Build() does not offer it).
            if (entry.IsSubmenu)
            {
                result.Message = entry.Label + " is a submenu; pick one of its entries";
                return result;
            }

            switch (id)
            {
                case HierarchyMenuCatalog.MenuId.SimulationSetup:
                    BuildSimulationSetup(file, nodeLabel, result);
                    break;

                case HierarchyMenuCatalog.MenuId.CopyPath:
                    result.ClipboardText = workspaceRelativePath ?? fsPath;
                    if (string.IsNullOrEmpty(result.ClipboardText)) result.Message = "the node has no path";
                    break;

                case HierarchyMenuCatalog.MenuId.CopyInstancePath:
                    result.ClipboardText = instancePath;
                    if (string.IsNullOrEmpty(result.ClipboardText)) result.Message = "the node has no instance path";
                    break;

                case HierarchyMenuCatalog.MenuId.OpenInExplorer:
                    result.FsPath = fsPath;
                    if (string.IsNullOrEmpty(result.FsPath)) result.Message = "the node has no file";
                    break;

                case HierarchyMenuCatalog.MenuId.ShowProperty:
                    result.Text = BuildPropertyText(nodeLabel, instancePath, workspaceRelativePath, fsPath, file);
                    break;

                default:
                    // An entry registered by a plugin: nothing to run here.
                    result.Message = "no handler for menu entry: " + id;
                    break;
            }
            return result;
        }

        /// <summary>
        /// The simulation file list: the top module plus every file the
        /// simulator would compile, and the include paths.
        ///
        /// <para>
        /// This is <see cref="Data.SimulationSetup"/> - the same collection the
        /// desktop editor's Run Simulation uses - so the VS Code view shows the
        /// real answer rather than a second implementation. Instance nodes are
        /// passed as the instance file (not the base file), because only the
        /// instance parse carries the parameter overrides written at the
        /// instantiation.
        /// </para>
        /// </summary>
        private static void BuildSimulationSetup(
            Data.IVerilogRelatedFile? file,
            string? nodeLabel,
            InvokeResult result)
        {
            if (file == null)
            {
                result.Message = "the file could not be resolved";
                return;
            }

            Verilog.ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument?.Root == null)
            {
                result.Message = "the file has not been parsed yet";
                return;
            }

            // Pick the building block this node represents: the instance's own
            // module when known, otherwise the first non-class block (the same
            // rule SimulationSetup.Create uses for a plain file).
            string? buildingBlockName = nodeLabel;
            if (string.IsNullOrEmpty(buildingBlockName) ||
                !parsedDocument.Root.BuildingBlocks.ContainsKey(buildingBlockName))
            {
                buildingBlockName = null;
                foreach (Verilog.BuildingBlocks.BuildingBlock block in parsedDocument.Root.BuildingBlocks.Values)
                {
                    if (block is Verilog.BuildingBlocks.Class) continue;
                    if (block is Verilog.BuildingBlocks.InterfaceClass) continue;
                    if (block is Verilog.BuildingBlocks.Package) continue;
                    buildingBlockName = block.Name;
                    break;
                }
            }

            if (buildingBlockName == null)
            {
                result.Message = "no module / interface found in this file";
                return;
            }

            Data.SimulationSetup? setup;
            try
            {
                setup = Data.SimulationSetup.Create(file, buildingBlockName);
            }
            catch (Exception ex)
            {
                result.Message = "the simulation setup could not be collected: " + ex.Message;
                return;
            }

            if (setup == null)
            {
                result.Message = "the simulation setup could not be collected for " + buildingBlockName;
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("top: ").Append(setup.TopName ?? buildingBlockName).Append('\n');

            List<string> files = new List<string>();
            foreach (Data.IVerilogRelatedFile target in setup.GetAllFiles())
            {
                string? path = Data.SimulationSetup.GetAbsolutePath(target) ?? target.RelativePath;
                if (!string.IsNullOrEmpty(path)) files.Add(path);
            }
            sb.Append('\n').Append("files (").Append(files.Count).Append("):\n");
            foreach (string path in files) sb.Append("  ").Append(path).Append('\n');

            List<string> includePaths = setup.GetAllIncludePaths();
            if (includePaths.Count > 0)
            {
                sb.Append('\n').Append("include paths (").Append(includePaths.Count).Append("):\n");
                foreach (string path in includePaths) sb.Append("  ").Append(path).Append('\n');
            }

            if (setup.UnfoundModules.Count > 0)
            {
                sb.Append('\n').Append("unresolved (").Append(setup.UnfoundModules.Count).Append("):\n");
                foreach (string name in setup.UnfoundModules) sb.Append("  ").Append(name).Append('\n');
            }

            result.Text = sb.ToString();
        }

        private static string BuildPropertyText(
            string? nodeLabel,
            string? instancePath,
            string? workspaceRelativePath,
            string? fsPath,
            Data.IVerilogRelatedFile? file)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("label: ").Append(nodeLabel ?? "").Append('\n');
            sb.Append("path: ").Append(workspaceRelativePath ?? "").Append('\n');
            sb.Append("file: ").Append(fsPath ?? "").Append('\n');
            if (!string.IsNullOrEmpty(instancePath))
            {
                sb.Append("instance path: ").Append(instancePath).Append('\n');
            }

            Verilog.ParsedDocument? parsedDocument = file?.VerilogParsedDocument;
            if (parsedDocument != null)
            {
                sb.Append("errors: ").Append(parsedDocument.ErrorCount).Append('\n');
                sb.Append("warnings: ").Append(parsedDocument.WarningCount).Append('\n');
            }
            return sb.ToString();
        }
    }
}
