using pluginVerilog.Verilog.BuildingBlocks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog
{
    /// <summary>
    /// A virtual <see cref="NameSpace"/> wrapper that exposes the contents of another
    /// <see cref="BuildingBlock"/> through a single namespace entry. Created from
    /// <c>// @scope</c> comment annotations so that expression parse and autocomplete
    /// can resolve elements across files without requiring an explicit instance
    /// connection.
    ///
    /// Behaviour:
    /// <list type="bullet">
    ///   <item><see cref="NameSpace.IsVirtualScope"/> is true, allowing SimSetup and
    ///         other file-collection code to skip these entries.</item>
    ///   <item>Identifier lookup via <see cref="NameSpace.NamedElements"/> is
    ///         transparently forwarded to the target building block.</item>
    ///   <item>The <see cref="NameSpace.Name"/> used in the parent
    ///         <c>NamedElements</c> is the instance name (if specified) or the
    ///         building block name.</item>
    /// </list>
    /// </summary>
    public class VirtualScopeNameSpace : NameSpace
    {
        protected VirtualScopeNameSpace(BuildingBlocks.BuildingBlock buildingBlock, NameSpace parent):base(buildingBlock,parent)
        {
        }

        public string RelativePath { get; set; }

        /// <summary>
        /// Factory method that creates and fully-initializes a VirtualScopeNameSpace
        /// using object initializer (required by base Item's 'required' members).
        /// </summary>
        public static VirtualScopeNameSpace Create(
            CommentScopeReference sourceScopeRef,
            BuildingBlocks.BuildingBlock target,
            string entryName,
            NameSpace parent)
        {
            // Determine proxy values from the target's owning file.
            string relativePath = $"@virtual/{entryName}";
            CodeEditor2.Data.Project? owningProject = null;
            if (target?.File is Data.IVerilogRelatedFile vFile)
            {
                if (!string.IsNullOrEmpty(vFile.RelativePath))
                    relativePath = vFile.RelativePath;
                owningProject = vFile.Project;
            }
            if (owningProject == null) owningProject = parent?.Project;

            return new VirtualScopeNameSpace(target,parent)
            {
                DefinitionReference = null,
                SourceCommentScopeReference = sourceScopeRef,
                VirtualScopeTarget = target,
                IsVirtualScope = true,
                BuildingBlock = target,
                Parent = parent,
                Name = entryName,
                Project = owningProject!,
                RelativePath = relativePath,
            };
        }

        public override CodeDrawStyle.ColorType ColorType
        {
            get { return CodeDrawStyle.ColorType.Identifier; }
        }

        /// <summary>
        /// Forward NamedElements access to the target building block so that
        /// Primary.searchNameSpace() and similar code can transparently traverse
        /// the virtual scope as if it were a normal namespace.
        /// </summary>
        [JsonIgnore]
        public override NamedElements NamedElements
        {
            get
            {
                if (VirtualScopeTarget != null) return VirtualScopeTarget.NamedElements;
                return base.NamedElements;
            }
        }

        public override List<ModuleItems.IBuildingBlockInstantiation> GetBuildingBlockInstantiations()
        {
            // Virtual scopes must not be traversed for instantiations.
            return new List<ModuleItems.IBuildingBlockInstantiation>();
        }
    }
}
