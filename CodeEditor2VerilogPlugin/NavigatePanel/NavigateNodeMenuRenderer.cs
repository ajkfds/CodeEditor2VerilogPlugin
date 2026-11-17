using Avalonia.Controls;
using Avalonia.Media;
using CodeEditor2.NavigatePanel;
using pluginVerilog.CoreBridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace pluginVerilog.NavigatePanel
{
    /// <summary>
    /// Draws the navigate-node menu into an Avalonia <c>ContextMenu</c> from
    /// <see cref="NavigateNodeMenuAdapter"/>.
    ///
    /// <para>
    /// This is the desktop half of the unification: VS Code renders the same
    /// catalog through <c>rtl/hierarchyMenu</c>, so a row is declared once (in
    /// <see cref="HierarchyMenuCatalog"/>) and appears in both hosts. Before this
    /// class existed, <c>NavigatePanelNode.createContextMenu</c> built Add /
    /// Delete / Open / CopyPath / Property inline while plugins subscribed to a
    /// static <c>CustomizeNavigateNodeContextMenu</c> event - so the editor and
    /// VS Code each had their own list.
    /// </para>
    ///
    /// <para>
    /// The renderer only maps rows onto <c>MenuItem</c>s and performs the
    /// resulting operation (a file-system call, a clipboard write, a dialog).
    /// Every decision - which rows apply, whether they are enabled, what a row
    /// creates - stays in the UI-free adapter.
    /// </para>
    /// </summary>
    internal static class NavigateNodeMenuRenderer
    {
        /// <summary>
        /// Rebuilds <paramref name="menu"/> for <paramref name="node"/>. The
        /// caller passes the shared context menu so the same instance is reused
        /// (the editor re-creates it on every selection).
        /// </summary>
        public static void Render(ContextMenu menu, NavigatePanelNode node)
        {
            menu.Items.Clear();
            if (node == null) return;

            NavigateNodeMenuContext context = BuildContext(node);
            NavigateNodeMenuAdapter.MenuResult result =
                NavigateNodeMenuAdapter.Build(context, HierarchyMenuCatalog.Hosts.Desktop);

            // ParentId -> the MenuItem its children are added to. Rows arrive in
            // group order, so a parent is always created before its children.
            Dictionary<string, MenuItem> parents = new Dictionary<string, MenuItem>(StringComparer.Ordinal);

            foreach (NavigateNodeMenuAdapter.MenuEntry entry in result.Entries)
            {
                MenuItem item = CreateMenuItem(entry);

                if (entry.IsSubmenu)
                {
                    parents[entry.Id] = item;
                    AddTo(menu, parents, entry.ParentId, item);
                    continue;
                }

                item.IsEnabled = entry.Enabled;
                if (!string.IsNullOrEmpty(entry.Reason))
                {
                    ToolTip.SetTip(item, entry.Reason);
                }

                string id = entry.Id;
                item.Click += async (_, _) =>
                {
                    try
                    {
                        await RunAsync(id, context, node);
                    }
                    catch (Exception ex)
                    {
                        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                        CodeEditor2.Controller.AppendLog(
                            "# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
                    }
                };

                AddTo(menu, parents, entry.ParentId, item);
            }
        }

        /// <summary>
        /// Adds <paramref name="item"/> under its parent submenu, or at the top
        /// level when the row has none. A missing parent falls back to the top
        /// level rather than dropping the row.
        /// </summary>
        private static void AddTo(
            ContextMenu menu,
            Dictionary<string, MenuItem> parents,
            string? parentId,
            MenuItem item)
        {
            if (!string.IsNullOrEmpty(parentId) && parents.TryGetValue(parentId!, out MenuItem? parent))
            {
                parent.Items.Add(item);
                return;
            }
            menu.Items.Add(item);
        }

        /// <summary>
        /// Builds the editor's menu row. The catalog carries the icon path and a
        /// packed <c>0xAARRGGBB</c> colour so this file (and the catalog) stay
        /// free of Avalonia types on the model side.
        /// </summary>
        private static MenuItem CreateMenuItem(NavigateNodeMenuAdapter.MenuEntry entry)
        {
            if (string.IsNullOrEmpty(entry.IconPath))
            {
                return CodeEditor2.Global.CreateMenuItem(entry.Label, "MenuItem_" + entry.Id);
            }
            return CodeEditor2.Global.CreateMenuItem(
                entry.Label,
                "MenuItem_" + entry.Id,
                entry.IconPath!,
                ToColor(entry.IconColor));
        }

        /// <summary>Unpacks the catalog's <c>0xAARRGGBB</c> colour.</summary>
        private static Color ToColor(uint argb)
        {
            return Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));
        }

        /// <summary>
        /// Runs one row and performs whatever the adapter returned. An entry that
        /// needs a name asks for it first and is then run again with the answer -
        /// the same two-step flow VS Code uses for its input box.
        /// </summary>
        private static async Task RunAsync(
            string id,
            NavigateNodeMenuContext context,
            NavigatePanelNode node)
        {
            NavigateNodeMenuAdapter.InvokeResult result =
                NavigateNodeMenuAdapter.Invoke(id, context);

            if (!string.IsNullOrEmpty(result.Prompt))
            {
                CodeEditor2.Tools.InputWindow window = new CodeEditor2.Tools.InputWindow(
                    result.Title, result.Prompt!);
                await CodeEditor2.Controller.ShowDialogAsync(window);
                if (window.Cancel) return;

                string input = window.InputText.Trim();
                if (input.Length == 0) return;

                result = NavigateNodeMenuAdapter.Invoke(id, context, input);
            }

            if (!string.IsNullOrEmpty(result.Message))
            {
                CodeEditor2.Controller.AppendLog(result.Message, Avalonia.Media.Colors.Red);
                return;
            }

            if (result.Operation != null)
            {
                await ExecuteOperationAsync(result.Operation, node);
                return;
            }

            if (result.ClipboardText != null)
            {
                // Clipboard writes go through the editor view, which owns the
                // TopLevel (a plugin cannot reach the main window directly).
                await CodeEditor2.Controller.CodeEditor.SetClipboardTextAsync(result.ClipboardText!);
                return;
            }

            if (!string.IsNullOrEmpty(result.Text))
            {
                // The adapter already formatted the text; show it in a read-only
                // document tab so a multi-line list stays readable.
                CodeEditor2.Controller.AppendLog(result.Text, Avalonia.Media.Colors.White);
                return;
            }
        }

        /// <summary>
        /// Performs the file-system operation the adapter decided on. The editor
        /// uses <c>System.IO</c>; VS Code does the same work through
        /// <c>vscode.workspace.fs</c> so remote / virtual file systems keep
        /// working.
        /// </summary>
        private static async Task ExecuteOperationAsync(
            NavigateNodeMenuAdapter.ClientOperation operation,
            NavigatePanelNode node)
        {
            if (string.IsNullOrEmpty(operation.Path)) return;

            switch (operation.Kind)
            {
                case "createFolder":
                    Directory.CreateDirectory(operation.Path!);
                    break;

                case "createFile":
                    if (File.Exists(operation.Path))
                    {
                        CodeEditor2.Controller.AppendLog(
                            "! already exist " + operation.Path, Avalonia.Media.Colors.Red);
                        return;
                    }
                    File.WriteAllText(operation.Path!, operation.Content ?? string.Empty);
                    break;

                case "delete":
                    if (Directory.Exists(operation.Path))
                    {
                        if (!operation.Recursive)
                        {
                            Directory.Delete(operation.Path!);
                            break;
                        }
                        Directory.Delete(operation.Path!, true);
                    }
                    else if (File.Exists(operation.Path))
                    {
                        File.Delete(operation.Path!);
                    }
                    break;

                case "reveal":
                    // handled by the caller: no file-system change
                    break;

                default:
                    CodeEditor2.Controller.AppendLog(
                        "unknown menu operation: " + operation.Kind, Avalonia.Media.Colors.Red);
                    return;
            }

            await CodeEditor2.Controller.NavigatePanel.UpdateFolderAsync(node);
        }

        /// <summary>
        /// Describes the editor node in the host-neutral shape the adapter takes.
        /// The <c>NavigatePanelNode</c> itself is not passed down: the LSP host
        /// links the same assembly and must not see Avalonia types.
        /// </summary>
        private static NavigateNodeMenuContext BuildContext(NavigatePanelNode node)
        {
            CodeEditor2.Data.Item? item = node.Item;

            return new NavigateNodeMenuContext
            {
                Kind = ToKind(node),
                File = GetVerilogFile(node),
                NodeLabel = node.Text,
                InstancePath = GetInstancePath(node),
                WorkspaceRelativePath = item?.RelativePath,
                FsPath = ResolveFsPath(node, item),
                DirPath = ResolveDirPath(node, item),
            };
        }

        /// <summary>Maps the node type onto the catalog's <c>AppliesTo</c> flags.</summary>
        private static HierarchyMenuCatalog.AppliesTo ToKind(NavigatePanelNode node)
        {
            if (node is CodeEditor2.NavigatePanel.ProjectNode) return HierarchyMenuCatalog.AppliesTo.Project;
            if (node is CodeEditor2.NavigatePanel.FolderNode) return HierarchyMenuCatalog.AppliesTo.Folder;
            if (node is VerilogModuleInstanceNode) return HierarchyMenuCatalog.AppliesTo.Instance;
            if (node is InterfaceInstanceNode) return HierarchyMenuCatalog.AppliesTo.Instance;
            if (node is VerilogHeaderInstanceNode) return HierarchyMenuCatalog.AppliesTo.Header;
            if (node is ImportedPackageNode) return HierarchyMenuCatalog.AppliesTo.Package;
            if (node is CodeEditor2.NavigatePanel.FileNode) return HierarchyMenuCatalog.AppliesTo.File;
            return HierarchyMenuCatalog.AppliesTo.None;
        }

        /// <summary>
        /// The file whose parse result the rows need. For an instance that is the
        /// instance's own parse (it carries the parameter overrides written at
        /// the instantiation), which is what the simulation setup walks.
        /// </summary>
        private static Data.IVerilogRelatedFile? GetVerilogFile(NavigatePanelNode node)
        {
            if (node is IVerilogNavigateNode verilogNode) return verilogNode.VerilogRelatedFile;
            return node.Item as Data.IVerilogRelatedFile;
        }

        /// <summary>Readable instance path ("u_mid.u_leaf"), or null for files.</summary>
        private static string? GetInstancePath(NavigatePanelNode node)
        {
            if (node is not VerilogModuleInstanceNode instanceNode) return null;

            List<string> segments = new List<string>();
            NavigatePanelNode? current = node;
            while (current != null)
            {
                if (current is VerilogModuleInstanceNode currentInstance)
                {
                    string? name = currentInstance.ModuleInstance?.InstanceName;
                    if (string.IsNullOrEmpty(name)) name = currentInstance.Text;
                    if (!string.IsNullOrEmpty(name)) segments.Insert(0, name);
                }
                current = current.Parent as NavigatePanelNode;
            }
            return segments.Count == 0 ? null : string.Join(".", segments);
        }

        /// <summary>
        /// Absolute path of the node's file. Folder nodes resolve through the
        /// project so a folder row can reveal / delete the directory.
        /// </summary>
        private static string? ResolveFsPath(NavigatePanelNode node, CodeEditor2.Data.Item? item)
        {
            if (node is CodeEditor2.NavigatePanel.FolderNode folderNode && folderNode.Folder != null)
            {
                try
                {
                    return folderNode.Folder.Project.GetAbsolutePath(folderNode.Folder.RelativePath);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            if (item == null) return null;

            try
            {
                return item.Project.GetAbsolutePath(item.RelativePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Directory the Add rows create into: a folder node is its own
        /// directory, a file / instance uses the directory of its file.
        /// </summary>
        private static string? ResolveDirPath(NavigatePanelNode node, CodeEditor2.Data.Item? item)
        {
            string? fsPath = ResolveFsPath(node, item);
            if (string.IsNullOrEmpty(fsPath)) return null;
            if (node is CodeEditor2.NavigatePanel.FolderNode) return fsPath;
            return Path.GetDirectoryName(fsPath);
        }
    }
}
