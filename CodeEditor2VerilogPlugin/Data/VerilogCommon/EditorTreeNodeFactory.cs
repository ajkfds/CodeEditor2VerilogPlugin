using System;
using System.Collections.Generic;
using System.Linq;
using SystemVerilogCore.Tree;
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Verilog.BuildingBlocks;

namespace pluginVerilog.Data.VerilogCommon
{
    /// <summary>
    /// The editor's implementation of the tree builder's node factory.
    ///
    /// <para>
    /// The tree's <b>content</b> - which children a file has, which instantiation
    /// becomes which node, when an existing node can be reused - lives in
    /// <see cref="SystemVerilogCore.Tree.VerilogTreeBuilder"/>. This class only
    /// says <b>what</b> a node is: the editor's own item types
    /// (<see cref="VerilogModuleInstance"/>, <see cref="InterfaceInstance"/>,
    /// <see cref="ImportedPackage"/>, <see cref="VerilogHeaderInstance"/>), which
    /// are files and item-tree entries as well as tree nodes.
    /// </para>
    ///
    /// <para>
    /// <b>Why the factory exists.</b> The builder used to be this file: it walked
    /// the parse result and created the editor's items directly, so a host without
    /// that item tree had to re-implement the rules (the language server did, and
    /// the two drifted). Splitting "which nodes" from "what a node is" is what lets
    /// both hosts walk one rule set.
    /// </para>
    /// </summary>
    internal sealed class EditorTreeNodeFactory : IVerilogTreeNodeFactory
    {
        private readonly IVerilogRelatedFile parent;

        public EditorTreeNodeFactory(IVerilogRelatedFile parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// The child already registered under <paramref name="key"/>, or null.
        /// Reusing it keeps the editor's selection and expansion state, and the
        /// builder only reuses it when the instance key matches.
        /// </summary>
        public IVerilogTreeNode? FindChild(IVerilogTreeNode parentNode, string key)
        {
            if (parentNode is not EditorTreeNode node) return null;
            if (node.Item.Items.TryGetValue(key, out CodeEditor2.Data.Item? found) && found != null)
            {
                return new EditorTreeNode(found);
            }
            return null;
        }

        public IVerilogTreeNode? CreateModuleInstance(
            IVerilogTreeNode parentNode,
            Verilog.Items.ModuleInstantiation instantiation)
        {
            VerilogModuleInstance? instance = VerilogModuleInstance.Create(instantiation);
            if (instance == null) return null;

            CodeEditor2.Data.Item? item = parentNode is EditorTreeNode parentItem ? parentItem.Item : null;
            instance.Parent = item;
            if (item is VerilogModuleInstance parentInstance && parentInstance.ExternalProject)
            {
                instance.ExternalProject = true;
            }
            return new EditorTreeNode(instance);
        }

        public IReadOnlyList<IVerilogTreeNode>? CreateModuleInstanceArray(
            IVerilogTreeNode parentNode,
            Verilog.Items.ModuleInstantiation instantiation)
        {
            List<VerilogModuleInstance>? instances = VerilogModuleInstance.CreateArray(instantiation);
            if (instances == null) return null;

            CodeEditor2.Data.Item? item = parentNode is EditorTreeNode parentItem ? parentItem.Item : null;
            List<IVerilogTreeNode> nodes = new(instances.Count);
            foreach (VerilogModuleInstance instance in instances)
            {
                instance.Parent = item;
                if (item is VerilogModuleInstance parentInstance && parentInstance.ExternalProject)
                {
                    instance.ExternalProject = true;
                }
                nodes.Add(new EditorTreeNode(instance));
            }
            return nodes;
        }

        public IVerilogTreeNode? CreateInterfaceInstance(
            IVerilogTreeNode parentNode,
            Verilog.Items.InterfaceInstance instantiation)
        {
            CodeEditor2.Data.Project? project = EditorProject;
            if (project == null) return null;

            InterfaceInstance? instance = InterfaceInstance.Create(instantiation, project);
            if (instance == null) return null;

            CodeEditor2.Data.Item? item = parentNode is EditorTreeNode parentItem ? parentItem.Item : null;
            instance.Parent = item;
            if (item is InterfaceInstance parentInstance && parentInstance.ExternalProject)
            {
                instance.ExternalProject = true;
            }
            return new EditorTreeNode(instance);
        }

        /// <summary>
        /// The node for a building block a multi-block file lists directly. The
        /// editor models it as an instance of the block itself, which is where the
        /// block's own instantiations nest.
        /// </summary>
        public IVerilogTreeNode? CreateBuildingBlockNode(
            IVerilogTreeNode parentNode,
            BuildingBlock block)
        {
            if (block is not Module module) return null;

            Verilog.Items.ModuleInstantiation instantiation = new()
            {
                BeginIndexReference = module.BeginIndexReference,
                DefinitionReference = module.DefinitionReference,
                Name = module.Name,
                ParameterOverrides = new Dictionary<string, Verilog.Expressions.Expression>(),
                Project = module.Project,
                SourceName = module.Name,
                SourceProjectName = module.Project?.Name ?? "",
            };

            VerilogModuleInstance? instance = VerilogModuleInstance.Create(instantiation);
            if (instance == null) return null;
            instance.InstanceName = module.Name;
            instance.Parent = parentNode is EditorTreeNode parentItem ? parentItem.Item : null;
            return new EditorTreeNode(instance);
        }

        public IVerilogTreeNode? CreateImportedPackage(IVerilogTreeNode parentNode, string packageName)
        {
            CodeEditor2.Data.Project? project = EditorProject;
            if (project == null) return null;

            ImportedPackage? package = ImportedPackage.Create(packageName, project);
            if (package == null) return null;

            CodeEditor2.Data.Item? item = parentNode is EditorTreeNode parentItem ? parentItem.Item : null;
            package.Parent = item;
            if (parent is InstanceTextFile instance && instance.ExternalProject)
            {
                package.ExternalProject = true;
            }
            return new EditorTreeNode(package);
        }

        public IVerilogTreeNode? CreateHeader(
            IVerilogTreeNode parentNode,
            SystemVerilogCore.Documents.IVerilogIncludeFile include)
        {
            if (include is not VerilogHeaderInstance header) return null;

            // The editor's include instance IS the node: it is created by the
            // parser (addIncludeFile) and carries the include's parse result.
            header.Parent = parentNode is EditorTreeNode parentItem ? parentItem.Item : null;
            return new EditorTreeNode(header);
        }

        /// <summary>The editor project the nodes belong to.</summary>
        private CodeEditor2.Data.Project? EditorProject
        {
            get
            {
                if (parent is not CodeEditor2.Data.TextFile textFile) return null;
                return textFile.Project;
            }
        }
    }

    /// <summary>
    /// The editor's tree node: the item itself.
    ///
    /// <para>
    /// A node is an item (<see cref="VerilogModuleInstance"/>,
    /// <see cref="InterfaceInstance"/>, <see cref="ImportedPackage"/>,
    /// <see cref="VerilogHeaderInstance"/>, or a file), so this is a thin adapter
    /// over it. The builder reads <see cref="InstanceKey"/> to decide whether an
    /// existing node can be reused and <see cref="ParsedResult"/> to list children.
    /// </para>
    /// </summary>
    internal sealed class EditorTreeNode : IVerilogTreeNode
    {
        private readonly CodeEditor2.Data.Item item;

        public EditorTreeNode(CodeEditor2.Data.Item item)
        {
            this.item = item;
        }

        /// <summary>The editor item this node stands for.</summary>
        public CodeEditor2.Data.Item Item => item;

        public VerilogTreeNodeKind Kind =>
            item switch
            {
                ImportedPackage => VerilogTreeNodeKind.ImportedPackage,
                VerilogHeaderInstance => VerilogTreeNodeKind.Header,
                InterfaceInstance => VerilogTreeNodeKind.InterfaceInstance,
                VerilogModuleInstance => VerilogTreeNodeKind.ModuleInstance,
                _ => VerilogTreeNodeKind.File,
            };

        public string Name => item.Name;

        public string RelativePath => item.RelativePath;

        /// <summary>
        /// The parse-cache key of an instance (module name plus parameter
        /// overrides), or null for a node that is not an instance.
        /// </summary>
        public string? InstanceKey =>
            item switch
            {
                VerilogModuleInstance module => module.Key,
                InterfaceInstance iface => iface.Key,
                _ => null,
            };

        public SystemVerilogCore.Parsing.IParsedDocument? ParsedResult =>
            (item as IVerilogRelatedFile)?.VerilogParsedDocument;

        /// <summary>
        /// The node's children. Read through the item's own collection, which the
        /// editor's UpdateAsync fills; the builder writes through the factory
        /// instead of this list, so this is only used for the reuse check.
        /// </summary>
        public IReadOnlyList<IVerilogTreeNode> Children =>
            item.Items.GetSnapShot().Select(child => (IVerilogTreeNode)new EditorTreeNode(child)).ToList();
    }
}
