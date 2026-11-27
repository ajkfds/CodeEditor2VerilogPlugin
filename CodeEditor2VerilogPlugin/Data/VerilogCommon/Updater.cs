using Avalonia.Threading;
using CodeEditor2.Data;
using SystemVerilogCore.Tree;
using System;
using System.Collections.Generic;
using System.Threading;

namespace pluginVerilog.Data.VerilogCommon
{
    /// <summary>
    /// Refreshes an editor item's children from its parse result.
    ///
    /// <para>
    /// <b>The content is not decided here.</b> Which children a file has, which
    /// instantiation becomes which node, when an existing node can be reused - all
    /// of that lives in <see cref="SystemVerilogCore.Tree.VerilogTreeBuilder"/>,
    /// so the language server walks the same rules without an item tree. This class
    /// only applies the result to the editor's own collection: it builds the nodes
    /// through <see cref="EditorTreeNodeFactory"/>, swaps them into
    /// <c>Item.Items</c> atomically, and disposes the ones that were replaced.
    /// </para>
    ///
    /// <para>
    /// Before, the rules lived here too (they were this file), and the language
    /// server had its own copy - which drifted. Splitting them means the editor's
    /// NavigatePanel and the VS Code RTL Hierarchy cannot disagree.
    /// </para>
    /// </summary>
    public static class Updater
    {
        /// <summary>
        /// Update the Items member of this object according to the parse result.
        /// </summary>
        /// <param name="item">The item to refresh.</param>
        /// <param name="semaphore">Serialises the swap against concurrent updates.</param>
        public static async System.Threading.Tasks.Task UpdateAsync(IVerilogRelatedFile item, SemaphoreSlim semaphore)
        {
            // run on background thread to avoid blocking UI
            if (Dispatcher.UIThread.CheckAccess())
            {
                await System.Threading.Tasks.Task.Run(async () => await UpdateAsync(item, semaphore));
                return;
            }

            Item? parent = item as Item;

            // doesn't have appropriate parsed data for this item
            if (parent == null || item.VerilogParsedDocument == null || item.VerilogParsedDocument.Root == null)
            {
                // dispose all sub items - use atomic update to prevent partial state
                item.Items.Clear(out List<Item> clearedItems);
                // Dispose after clearing to avoid deadlock
                foreach (Item subItem in clearedItems)
                {
                    subItem.Dispose();
                }
                return;
            }

            // The builder decides the children; this host supplies the nodes.
            IReadOnlyList<IVerilogTreeNode> nodes = VerilogTreeBuilder.BuildChildren(
                new EditorTreeNode(parent),
                new EditorTreeNodeFactory(item),
                BuildingBlockNameOf(item));

            // Atomic swap: dispose old items and replace with new ones in a single
            // operation.
            List<Item> oldItems = new();
            await semaphore.WaitAsync();
            try
            {
                oldItems = item.Items.GetSnapShot();

                Dictionary<string, Item> newSubItems = new();
                foreach (IVerilogTreeNode node in nodes)
                {
                    if (node is not EditorTreeNode editorNode) continue;
                    Item child = editorNode.Item;
                    newSubItems[child.Name] = child;
                    oldItems.Remove(child);
                }

                item.Items.ReplaceTo(newSubItems);
            }
            finally
            {
                semaphore.Release();
            }

            // Dispose the items that were replaced.
            foreach (Item oldItem in oldItems)
            {
                oldItem.Dispose();
            }
        }

        /// <summary>
        /// The building block an instance item was parsed as, or null for a file
        /// (a file lists its own blocks).
        /// </summary>
        private static string? BuildingBlockNameOf(IVerilogRelatedFile item)
        {
            return item switch
            {
                VerilogModuleInstance module => module.InstanceName,
                InterfaceInstance iface => iface.InstanceName,
                _ => null,
            };
        }
    }
}
