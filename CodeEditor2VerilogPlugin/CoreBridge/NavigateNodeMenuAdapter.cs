using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free builder / runner for the navigate-node menu (the desktop editor's
    /// NavigatePanel right-click menu and the VS Code "RTL Hierarchy" node menu).
    ///
    /// <para>
    /// The rows come from <see cref="HierarchyMenuCatalog"/>, so the list lives on
    /// the C# side. This adapter adds the two things a static <c>package.json</c>
    /// menu cannot express: <b>which rows apply to the selected node</b> and
    /// <b>what each row produces</b>. Both hosts call the same two methods and
    /// only differ in how they render a row and how they perform the resulting
    /// file-system / clipboard operation.
    /// </para>
    ///
    /// <para>
    /// Nothing here needs an Avalonia dispatcher, so it runs in the headless LSP
    /// host. The results are plain data: text to display, text to copy, a path to
    /// reveal, or a <see cref="ClientOperation"/> the host executes.
    /// </para>
    /// </summary>
    public static class NavigateNodeMenuAdapter
    {
        /// <summary>One row of the menu, with its availability.</summary>
        public sealed class MenuEntry
        {
            /// <summary>Stable id (one of <see cref="HierarchyMenuCatalog.MenuId"/>).</summary>
            public string Id { get; init; } = string.Empty;

            /// <summary>
            /// Menu label, exactly as the catalog declares it ("Folder",
            /// "Copy Path"). A host that renders nesting itself (the editor
            /// nests MenuItems, VS Code renders a flyout from package.json)
            /// must use this one: prefixing the parent here made the desktop
            /// menu read "Add › Folder" even though it already shows the row
            /// inside an "Add" submenu.
            /// </summary>
            public string Label { get; init; } = string.Empty;

            /// <summary>
            /// Label for a <b>flat</b> list, where nesting cannot be drawn: a
            /// nested row is prefixed with its parent's label ("Add › Folder").
            /// Only the QuickPick fallback uses this.
            /// </summary>
            public string FlatLabel { get; init; } = string.Empty;

            /// <summary>One line explaining what the entry does.</summary>
            public string Detail { get; init; } = string.Empty;

            /// <summary><c>false</c> when the entry cannot run for this node.</summary>
            public bool Enabled { get; init; }

            /// <summary>Why the entry cannot run (only set when disabled).</summary>
            public string? Reason { get; init; }

            /// <summary>Ordering group (see <see cref="HierarchyMenuCatalog.Entry.Group"/>).</summary>
            public int Group { get; init; }

            /// <summary>Parent submenu id, or null for a top-level row.</summary>
            public string? ParentId { get; init; }

            /// <summary>True when the row only groups its children.</summary>
            public bool IsSubmenu { get; init; }

            /// <summary>Icon asset path (desktop menu); null when the row has none.</summary>
            public string? IconPath { get; init; }

            /// <summary>Icon colour as <c>0xAARRGGBB</c>.</summary>
            public uint IconColor { get; init; }

            /// <summary>
            /// The row needs a window only the desktop editor owns, so an LSP host
            /// can never run it. Declared through
            /// <see cref="HierarchyMenuCatalog.Entry.Hosts"/>, so a VS Code build
            /// does not normally see these rows at all; kept on the wire for
            /// clients that ask for them anyway.
            /// </summary>
            public bool DesktopOnly { get; init; }
        }

        /// <summary>The menu for one node.</summary>
        public sealed class MenuResult
        {
            /// <summary>Rows, already filtered to the ones that apply to the node.</summary>
            public List<MenuEntry> Entries { get; } = new();

            /// <summary>Why the menu is empty (only set when it is).</summary>
            public string? Message { get; set; }
        }

        /// <summary>
        /// A file-system / clipboard operation the host performs. The catalog
        /// decides <b>what</b> to do and <b>where</b>; the host only executes it,
        /// because VS Code has to go through <c>vscode.workspace.fs</c> (remote and
        /// virtual file systems, watcher integration) while the editor uses
        /// <c>System.IO</c>.
        /// </summary>
        public sealed class ClientOperation
        {
            /// <summary>
            /// <c>"createFolder"</c> / <c>"createFile"</c> / <c>"delete"</c> /
            /// <c>"reveal"</c>.
            /// </summary>
            public string Kind { get; init; } = string.Empty;

            /// <summary>Absolute path the operation acts on.</summary>
            public string? Path { get; init; }

            /// <summary>Initial content for <c>createFile</c>.</summary>
            public string? Content { get; init; }

            /// <summary>Delete directories recursively.</summary>
            public bool Recursive { get; init; }
        }

        /// <summary>What running an entry produced.</summary>
        public sealed class InvokeResult
        {
            /// <summary>Title for the client's dialog.</summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>Body text (may be multi-line; the client only displays it).</summary>
            public string Text { get; set; } = string.Empty;

            /// <summary>Clipboard text, when the entry copies something.</summary>
            public string? ClipboardText { get; set; }

            /// <summary>Why the entry could not run.</summary>
            public string? Message { get; set; }

            /// <summary>
            /// Set when the entry needs a value from the user (a new folder name,
            /// a new file name, a new module name). The host shows an input box
            /// and calls <see cref="Invoke"/> again with
            /// <see cref="PromptArgument"/> as the <c>argument</c>.
            /// </summary>
            public string? Prompt { get; set; }

            /// <summary>The file-system operation the host should perform.</summary>
            public ClientOperation? Operation { get; set; }
        }

        /// <summary>
        /// Builds the menu for a node.
        /// </summary>
        /// <param name="context">The node (kind + paths + file).</param>
        /// <param name="host">
        /// Which host is asking. Rows declared for the other host are skipped, so
        /// "Run Simulation" never shows up in VS Code and "Reveal in Editor" never
        /// shows up in the editor.
        /// </param>
        public static MenuResult Build(NavigateNodeMenuContext context, HierarchyMenuCatalog.Hosts host)
        {
            MenuResult result = new MenuResult();
            if (context == null)
            {
                result.Message = "no node given";
                return result;
            }

            HierarchyMenuCatalog.AppliesTo target = context.Kind;
            if (target == HierarchyMenuCatalog.AppliesTo.None)
            {
                result.Message = "no menu for this node";
                return result;
            }

            // Rows arrive as a tree, flattened depth-first with ParentId filled
            // in. The editor re-nests them by ParentId (its ContextMenu is
            // rebuilt per node) and the VS Code manifest turns each grouping row
            // into a flyout, so both hosts get the same shape from one walk.
            foreach (HierarchyMenuCatalog.Entry entry in HierarchyMenuCatalog.All)
            {
                if ((entry.Hosts & host) == 0) continue;
                if ((entry.Targets & target) == 0) continue;

                // A row can be unavailable right now (a plugin that owns the
                // action is not loaded). A grouping row inherits that from its
                // children: showing an empty submenu is worse than hiding it.
                if (!IsOffered(entry, host, target)) continue;

                string? reason = entry.IsSubmenu ? null : GetDisabledReason(entry, context);
                result.Entries.Add(new MenuEntry
                {
                    Id = entry.Id,
                    Label = entry.Label,
                    FlatLabel = BuildQuickPickLabel(entry),
                    Detail = entry.Detail,
                    Enabled = reason == null,
                    Reason = reason,
                    Group = entry.Group,
                    ParentId = HierarchyMenuCatalog.ParentOf(entry.Id)?.Id,
                    IsSubmenu = entry.IsSubmenu,
                    IconPath = entry.IconPath,
                    IconColor = entry.IconColor,
                    DesktopOnly = (entry.Hosts & HierarchyMenuCatalog.Hosts.VsCode) == 0,
                });
            }
            return result;
        }

        /// <summary>
        /// Whether the row should be listed at all: its host and node filters
        /// pass, and it is available right now.
        /// </summary>
        /// <remarks>
        /// A grouping row is offered when at least one of its children is, so an
        /// "Add" submenu never opens onto nothing. The host / target filters are
        /// re-applied to the children because a child can be narrower than its
        /// parent (a submenu offered for files and folders with a child that
        /// only makes sense for folders).
        /// </remarks>
        private static bool IsOffered(
            HierarchyMenuCatalog.Entry entry,
            HierarchyMenuCatalog.Hosts host,
            HierarchyMenuCatalog.AppliesTo target)
        {
            if ((entry.Hosts & host) == 0) return false;
            if ((entry.Targets & target) == 0) return false;
            if (!entry.IsAvailable()) return false;

            if (!entry.IsSubmenu) return true;

            foreach (HierarchyMenuCatalog.Entry child in HierarchyMenuCatalog.Children(entry))
            {
                if (IsOffered(child, host, target)) return true;
            }
            return false;
        }

        /// <summary>
        /// Label shown in a flat list (<see cref="MenuEntry.FlatLabel"/>). A
        /// nested row is prefixed with its parent's label, because a QuickPick
        /// has no other way to show the hierarchy the native menu renders as a
        /// submenu. <see cref="MenuEntry.Label"/> stays unprefixed for hosts
        /// that nest for real.
        /// </summary>
        private static string BuildQuickPickLabel(HierarchyMenuCatalog.Entry entry)
        {
            HierarchyMenuCatalog.Entry? parent = HierarchyMenuCatalog.ParentOf(entry.Id);
            return parent == null ? entry.Label : parent.Label + " › " + entry.Label;
        }

        /// <summary>
        /// Returns why <paramref name="entry"/> cannot run for the node, or null
        /// when it can. Only the entries that need a parse result or a path are
        /// checked here; rows that are always meaningful return null.
        /// </summary>
        private static string? GetDisabledReason(
            HierarchyMenuCatalog.Entry entry,
            NavigateNodeMenuContext context)
        {
            switch (entry.Action)
            {
                case HierarchyMenuCatalog.ActionKind.ShowText:
                    if (context.File == null) return "the file could not be resolved";
                    if (context.File.VerilogParsedDocument?.Root == null) return "the file has not been parsed yet";
                    return null;

                case HierarchyMenuCatalog.ActionKind.CopyInstancePath:
                    return string.IsNullOrEmpty(context.InstancePath)
                        ? "this node has no instance path"
                        : null;

                case HierarchyMenuCatalog.ActionKind.RevealInOS:
                case HierarchyMenuCatalog.ActionKind.CopyPath:
                case HierarchyMenuCatalog.ActionKind.CreateFolder:
                case HierarchyMenuCatalog.ActionKind.CreateFile:
                case HierarchyMenuCatalog.ActionKind.CreateSourceFile:
                    if (string.IsNullOrEmpty(context.FsPath) && string.IsNullOrEmpty(context.DirPath))
                    {
                        return "this node has no file";
                    }
                    return null;

                case HierarchyMenuCatalog.ActionKind.DeleteNode:
                    if (string.IsNullOrEmpty(context.FsPath)) return "this node has no file";
                    if (context.Kind == HierarchyMenuCatalog.AppliesTo.Project) return "the project root cannot be deleted";
                    return null;

                case HierarchyMenuCatalog.ActionKind.ShowProperty:
                    return context.File == null ? "the file could not be resolved" : null;

                case HierarchyMenuCatalog.ActionKind.DesktopHandler:
                    return entry.Handler == null ? "this entry has no handler" : null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Runs the entry for a node and returns what the host should do.
        /// </summary>
        /// <param name="id">The entry id (from <see cref="MenuResult.Entries"/>).</param>
        /// <param name="context">The node the menu was built for.</param>
        /// <param name="argument">
        /// The value the user typed after a <see cref="InvokeResult.Prompt"/>, or
        /// the entry's own <see cref="HierarchyMenuCatalog.Entry.Argument"/> when
        /// the host passes nothing.
        /// </param>
        public static InvokeResult Invoke(
            string id,
            NavigateNodeMenuContext context,
            string? argument = null)
        {
            InvokeResult result = new InvokeResult();

            HierarchyMenuCatalog.Entry? entry = HierarchyMenuCatalog.Find(id);
            if (entry == null)
            {
                result.Message = "unknown menu entry: " + id;
                return result;
            }
            result.Title = entry.Label;

            if (entry.IsSubmenu)
            {
                result.Message = entry.Label + " is a submenu; pick one of its entries";
                return result;
            }

            switch (entry.Action)
            {
                case HierarchyMenuCatalog.ActionKind.ShowText:
                    // The only ShowText row today is the simulation file list.
                    BuildSimulationSetup(context, result);
                    break;

                case HierarchyMenuCatalog.ActionKind.CopyPath:
                    result.ClipboardText = context.WorkspaceRelativePath ?? context.FsPath;
                    if (string.IsNullOrEmpty(result.ClipboardText)) result.Message = "the node has no path";
                    break;

                case HierarchyMenuCatalog.ActionKind.CopyInstancePath:
                    result.ClipboardText = context.InstancePath;
                    if (string.IsNullOrEmpty(result.ClipboardText)) result.Message = "the node has no instance path";
                    break;

                case HierarchyMenuCatalog.ActionKind.RevealInOS:
                    if (string.IsNullOrEmpty(context.FsPath))
                    {
                        result.Message = "the node has no file";
                        break;
                    }
                    result.Operation = new ClientOperation { Kind = "reveal", Path = context.FsPath };
                    break;

                case HierarchyMenuCatalog.ActionKind.ShowProperty:
                    result.Text = BuildPropertyText(context);
                    break;

                case HierarchyMenuCatalog.ActionKind.CreateFolder:
                    // the argument is the folder name the user typed
                    BuildCreateFolder(context, argument, result);
                    break;

                case HierarchyMenuCatalog.ActionKind.CreateFile:
                    // the argument is the file name the user typed
                    BuildCreateFile(context, argument, null, result);
                    break;

                case HierarchyMenuCatalog.ActionKind.CreateSourceFile:
                    // the entry's own Argument is the template ("verilog" /
                    // "systemverilog" / "interface"); the caller's argument is
                    // the module name the user typed. Passing the template as the
                    // name would create "verilog.sv".
                    BuildCreateFile(context, argument, entry.Argument, result);
                    break;

                case HierarchyMenuCatalog.ActionKind.DeleteNode:
                    if (string.IsNullOrEmpty(context.FsPath))
                    {
                        result.Message = "the node has no file";
                        break;
                    }
                    result.Operation = new ClientOperation
                    {
                        Kind = "delete",
                        Path = context.FsPath,
                        Recursive = context.Kind == HierarchyMenuCatalog.AppliesTo.Folder,
                    };
                    break;

                case HierarchyMenuCatalog.ActionKind.DesktopHandler:
                    if (entry.Handler == null)
                    {
                        result.Message = "this entry has no handler";
                        break;
                    }
                    try
                    {
                        entry.Handler(context);
                    }
                    catch (Exception ex)
                    {
                        result.Message = ex.Message;
                    }
                    break;

                default:
                    result.Message = "no handler for menu entry: " + id;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Asks the host for a name (the first step of CreateFolder /
        /// CreateFile / CreateSourceFile) or performs the operation when
        /// <paramref name="name"/> is already known.
        /// </summary>
        private static void BuildCreateFolder(
            NavigateNodeMenuContext context,
            string? name,
            InvokeResult result)
        {
            string? directory = context.DirPath;
            if (string.IsNullOrEmpty(directory))
            {
                result.Message = "the node has no directory";
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                result.Prompt = "New folder name";
                return;
            }
            result.Operation = new ClientOperation
            {
                Kind = "createFolder",
                Path = System.IO.Path.Combine(directory, name),
            };
        }

        /// <summary>
        /// Asks the host for a name, then creates the file. The content is built
        /// here so the editor and VS Code write byte-identical skeletons.
        /// </summary>
        private static void BuildCreateFile(
            NavigateNodeMenuContext context,
            string? name,
            string? template,
            InvokeResult result)
        {
            string? directory = context.DirPath;
            if (string.IsNullOrEmpty(directory))
            {
                result.Message = "the node has no directory";
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                result.Prompt = template == null
                    ? "New file name"
                    : "New " + TemplateDisplayName(template) + " name";
                return;
            }

            string extension = template == null ? string.Empty : TemplateExtension(template);
            string fileName = extension.Length == 0 || name.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase)
                ? name
                : name + "." + extension;

            result.Operation = new ClientOperation
            {
                Kind = "createFile",
                Path = System.IO.Path.Combine(directory, fileName),
                Content = template == null ? string.Empty : BuildSourceSkeleton(template, name),
            };
        }

        /// <summary>Human readable name for the input prompt.</summary>
        private static string TemplateDisplayName(string template)
        {
            switch (template)
            {
                case "verilog": return "verilog module";
                case "systemverilog": return "system verilog module";
                case "interface": return "system verilog interface";
                default: return template;
            }
        }

        /// <summary>File extension of a source template.</summary>
        private static string TemplateExtension(string template)
            => template == "verilog" ? "v" : "sv";

        /// <summary>
        /// The skeleton written into a new file. Matches what the editor's
        /// NavigatePanelMenu used to write, so a file created from VS Code is
        /// indistinguishable from one created in the editor.
        /// </summary>
        private static string BuildSourceSkeleton(string template, string name)
        {
            StringBuilder sb = new StringBuilder();
            switch (template)
            {
                case "interface":
                    sb.Append("interface ").Append(name).Append(";\n");
                    sb.Append('\n');
                    sb.Append("endinterface\n");
                    break;
                default:
                    sb.Append("`timescale 1ns / 1ps\n");
                    sb.Append('\n');
                    sb.Append("module ").Append(name).Append(";\n");
                    sb.Append('\n');
                    sb.Append("endmodule\n");
                    break;
            }
            return sb.ToString();
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
        private static void BuildSimulationSetup(NavigateNodeMenuContext context, InvokeResult result)
        {
            Data.IVerilogRelatedFile? file = context.File;
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
            string? buildingBlockName = context.NodeLabel;
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

        /// <summary>
        /// The node's parsed properties, as text. One implementation for both
        /// hosts: the editor shows it in a dialog, VS Code opens it in a
        /// read-only document.
        /// </summary>
        private static string BuildPropertyText(NavigateNodeMenuContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("label: ").Append(context.NodeLabel ?? "").Append('\n');
            sb.Append("path: ").Append(context.WorkspaceRelativePath ?? "").Append('\n');
            sb.Append("file: ").Append(context.FsPath ?? "").Append('\n');
            if (!string.IsNullOrEmpty(context.InstancePath))
            {
                sb.Append("instance path: ").Append(context.InstancePath).Append('\n');
            }

            Verilog.ParsedDocument? parsedDocument = context.File?.VerilogParsedDocument;
            if (parsedDocument != null)
            {
                sb.Append("errors: ").Append(parsedDocument.ErrorCount).Append('\n');
                sb.Append("warnings: ").Append(parsedDocument.WarningCount).Append('\n');
            }
            return sb.ToString();
        }
    }
}
