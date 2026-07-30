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
        ///
        /// If <see cref="NameSpace.VirtualScopeTarget"/> is not yet bound
        /// (because the referenced building block lives in a different file
        /// whose parse has not yet completed), this getter attempts a
        /// late-binding lookup via the <see cref="ProjectProperty"/> obtained
        /// from the <see cref="NameSpace.SourceCommentScopeReference"/> or
        /// from the parent namespace's Project, and updates
        /// <see cref="NameSpace.VirtualScopeTarget"/> if a building block is
        /// now available. This is the key fix that allows parse-time access
        /// (e.g. `wire aa = inst0.SIG;` after `// @scope MY_MOD inst0`) to
        /// resolve correctly even on the first parse, when the target
        /// BuildingBlock may not yet have been registered.
        /// </summary>
        [JsonIgnore]
        public override NamedElements NamedElements
        {
            get
            {
                if (VirtualScopeTarget == null)
                {
                    // Attempt late binding of VirtualScopeTarget from the
                    // registered building blocks. This handles the case where
                    // the @scope annotation was parsed before the referenced
                    // building block's file completed parsing, or where a
                    // subsequent reparse is required to bind the target.
                    BuildingBlocks.BuildingBlock? resolved = null;
                    if (SourceCommentScopeReference != null)
                    {
                        // First, reuse an already-resolved building block if
                        // present on the scope reference itself.
                        if (SourceCommentScopeReference.ResolvedBuildingBlock != null)
                        {
                            resolved = SourceCommentScopeReference.ResolvedBuildingBlock;
                        }
                        else
                        {
                            // Fall back to a lookup via ProjectProperty. The
                            // ProjectProperty is reachable either from this
                            // wrapper's Parent (the @scope's owning
                            // namespace's BuildingBlock.File.ProjectProperty)
                            // or from the Project field on this wrapper.
                            CodeEditor2.Data.ProjectProperty? projectProperty = null;
                            if (Parent is BuildingBlock parentBlock
                                && parentBlock.File is Data.IVerilogRelatedFile parentFile
                                && parentFile.ProjectProperty != null)
                            {
                                projectProperty = parentFile.ProjectProperty;
                            }
                            else if (Project is CodeEditor2.Data.Project pj
                                && pj.ProjectProperties != null
                                && pj.ProjectProperties.Count > 0)
                            {
                                // The Project carries a dictionary of
                                // ProjectProperty objects keyed by ID. The
                                // first available property is sufficient for a
                                // building block lookup; the per-ProjectProperty
                                // routing used by IVerilogRelatedFile already
                                // gives the right one when available above.
                                foreach (var pp in pj.ProjectProperties.Values)
                                {
                                    if (pp != null)
                                    {
                                        projectProperty = pp;
                                        break;
                                    }
                                }
                            }
                            if (projectProperty != null)
                            {
                                // The base ProjectProperty class does not
                                // expose GetBuildingBlock; the Verilog
                                // plugin's ProjectProperty does. Cast to the
                                // plugin type to access the lookup. If the
                                // ProjectProperty instance is not the Verilog
                                // one, the cast will fail and we'll leave
                                // resolved = null, which simply means we
                                // can't late-bind the target.
                                if (projectProperty is pluginVerilog.ProjectProperty verilogPP)
                                {
                                    resolved = verilogPP.GetBuildingBlock(
                                        SourceCommentScopeReference.BuildingBlockName);
                                    if (resolved != null)
                                    {
                                        SourceCommentScopeReference.ResolvedBuildingBlock = resolved;
                                    }
                                }
                            }
                        }
                    }

                    if (resolved != null)
                    {
                        // Late-bind the target so that subsequent accesses
                        // can take the fast path.
                        VirtualScopeTarget = resolved;
                        BuildingBlock = resolved;
                    }
                }

                if (VirtualScopeTarget != null) return VirtualScopeTarget.NamedElements;
                return base.NamedElements;
            }
        }

        public override List<Items.IBuildingBlockInstantiation> GetBuildingBlockInstantiations()
        {
            // Virtual scopes must not be traversed for instantiations.
            return new List<Items.IBuildingBlockInstantiation>();
        }

        /// <summary>
        /// Late-binds the target building block on this virtual scope, if
        /// not already bound. Used by the @scope annotation parser on
        /// subsequent reparses, after the referenced building block has
        /// finally been registered in the project.
        ///
        /// This is a no-op if the target is already non-null or the new
        /// target is null, so it's safe to call unconditionally.
        /// </summary>
        public void UpdateTarget(BuildingBlocks.BuildingBlock? newTarget)
        {
            if (newTarget == null) return;
            if (VirtualScopeTarget != null) return;
            VirtualScopeTarget = newTarget;
            BuildingBlock = newTarget;
        }
    }
}
