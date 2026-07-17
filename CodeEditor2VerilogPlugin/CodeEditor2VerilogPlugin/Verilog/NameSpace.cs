using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Items;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog
{
    public class NameSpace : NamedItem, INamedElement, Items.IRegion
    {
        protected NameSpace(BuildingBlocks.BuildingBlock buildingBlock, NameSpace parent)
        {
            BuildingBlock = buildingBlock;
            Parent = parent;
        }

        [JsonConstructor]
        protected NameSpace()
        {
        }
        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        [JsonIgnore]
        public virtual NamedElements NamedElements { get; } = new NamedElements();

        /// <summary>
        /// Comment-based scope references (from @scope annotation)
        /// Allows accessing elements from other building blocks without explicit instance connection.
        /// </summary>
        public List<CommentScopeReference> CommentScopeReferences { get; } = new List<CommentScopeReference>();

        public IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public List<IRegion> Regions { get; protected set; } = new List<IRegion>();


        public IndexReference? BlockBeginIndexReference = null;

        public NameSpace Parent { get; init; } = null;

        public List<Items.IRegion> Items { get; protected set; } = new List<Items.IRegion>();

        public BuildingBlocks.BuildingBlock BuildingBlock { get; protected set; }

        // ====== VirtualScope support (for @scope annotation) ======

        /// <summary>
        /// True if this NameSpace is a virtual wrapper for another building block,
        /// injected via @scope comment annotation. Virtual scopes have no
        /// associated file and are skipped by SimSetup file collection.
        /// </summary>
        public bool IsVirtualScope { get; init; } = false;

        /// <summary>
        /// The target BuildingBlock this virtual scope wraps. Set when
        /// <see cref="IsVirtualScope"/> is true. The setter exists so that a
        /// VirtualScopeNameSpace created with a null target (because the
        /// referenced building block was not yet parsed at the time of
        /// @scope annotation) can have its target bound later when the
        /// referenced file is parsed and the building block becomes available.
        /// </summary>
        public BuildingBlocks.BuildingBlock? VirtualScopeTarget { get; set; } = null;

        /// <summary>
        /// The originating <see cref="CommentScopeReference"/> if this NameSpace
        /// was created from an @scope annotation.
        /// </summary>
        public CommentScopeReference? SourceCommentScopeReference { get; init; } = null;

        /// <summary>
        /// The name to use when registering this scope in the parent
        /// <see cref="NamedElements"/>. For @scope buildingBlock [instanceName],
        /// this is the instanceName (if specified) or the buildingBlockName.
        /// </summary>
        public string VirtualScopeEntryName { get; init; } = "";

        // ====== end VirtualScope support ======

        public INamedElement? GetNamedElementUpward(string name)
        {
            if (NamedElements.ContainsKey(name)) return NamedElements[name];

            // Search in comment scope references
            var fromScope = GetNamedElementFromCommentScopes(name, out _);
            if (fromScope != null) return fromScope;

            if (Parent == null) return null;
            return Parent.GetNamedElementUpward(name);
        }

        public INamedElement? GetNamedElementUpward(string name, out NameSpace? nameSpace)
        {
            nameSpace = null;
            if (NamedElements.ContainsKey(name))
            {
                nameSpace = this;
                return NamedElements[name];
            }

            // Search in comment scope references
            var fromScope = GetNamedElementFromCommentScopes(name, out NameSpace? scopeNameSpace);
            if (fromScope != null)
            {
                nameSpace = scopeNameSpace;
                return fromScope;
            }

            if (Parent == null) return null;
            return Parent.GetNamedElementUpward(name, out nameSpace);
        }


        public NameSpace GetHierarchyNameSpace(IndexReference iref)
        {
            foreach (INamedElement namedElement in BuildingBlock.NamedElements.Values)
            {
                if (namedElement is Function)
                {
                    Function function = (Function)namedElement;
                    if (function.BeginIndexReference == null) continue;
                    if (function.LastIndexReference == null) continue;

                    if (iref.IsSmallerThan(function.BeginIndexReference)) continue;
                    if (iref.IsGreaterThan(function.LastIndexReference)) continue;
                    return function.GetHierarchyNameSpaceDownward(iref);
                }
                else if (namedElement is Task)
                {
                    Task task = (Task)namedElement;
                    if (task.BeginIndexReference == null) continue;
                    if (task.LastIndexReference == null) continue;

                    if (iref.IsSmallerThan(task.BeginIndexReference)) continue;
                    if (iref.IsGreaterThan(task.LastIndexReference)) continue;
                    return task.GetHierarchyNameSpaceDownward(iref);
                }
            }

            return GetHierarchyNameSpaceDownward(iref);
        }
        public NameSpace GetHierarchyNameSpaceDownward(IndexReference iref)
        {
            foreach (INamedElement element in NamedElements.Values)
            {
                NameSpace? nameSpace = element as NameSpace;
                if (nameSpace == null) continue;
                if (nameSpace.BeginIndexReference == null) continue;
                if (nameSpace.LastIndexReference == null) continue;

                if (iref.IsSmallerThan(nameSpace.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(nameSpace.LastIndexReference)) continue;
                return nameSpace.GetHierarchyNameSpaceDownward(iref);
            }
            return this;
        }

        public virtual List<Items.IBuildingBlockInstantiation> GetBuildingBlockInstantiations()
        {
            List<Items.IBuildingBlockInstantiation> list = new List<Items.IBuildingBlockInstantiation>();
            return getBuildingBlockInstantiations(this, list);
        }
        private List<Items.IBuildingBlockInstantiation> getBuildingBlockInstantiations(NameSpace nameSpace, List<Items.IBuildingBlockInstantiation> list)
        {
            foreach (INamedElement namedElement in nameSpace.NamedElements.Values)
            {
                if (namedElement is Items.IBuildingBlockInstantiation)
                {
                    list.Add((Items.IBuildingBlockInstantiation)namedElement);
                }
                else if (namedElement is NameSpace && !(namedElement is VirtualScopeNameSpace))
                {
                    // Virtual scopes must not be traversed for instantiations.
                    getBuildingBlockInstantiations((NameSpace)namedElement, list);
                }
            }
            return list;
        }


        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public virtual void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            foreach (INamedElement element in NamedElements.Values)
            {
                if (element is DataObjects.DataObject)
                {
                    DataObjects.DataObject variable = (DataObjects.DataObject)element;
                    if (variable is DataObjects.Nets.Net)
                    {
                        items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Net));
                    }
                    else if (variable is DataObjects.Variables.Variable)
                    {
                        items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Variable));
                    }
                    else if (variable is DataObjects.Variables.Object)
                    {
                        items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Variable));
                    }
                    else if (variable is DataObjects.Variables.Time || variable is DataObjects.Variables.Real || variable is DataObjects.Variables.Realtime || variable is DataObjects.Variables.Integer || variable is DataObjects.Variables.Genvar)
                    {
                        items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Variable));
                    }
                }
                else if (element is NameSpace)
                {
                    NameSpace space = (NameSpace)element;
                    if (space.Name == null) System.Diagnostics.Debugger.Break();
                    if (space.Name == null) continue;
                    items.Add(newItem(space.Name, CodeDrawStyle.ColorType.Identifier));

                }
                else if (element is DataObjects.Constants.Constants)
                {
                    DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)element;
                    items.Add(newItem(constants.Name, CodeDrawStyle.ColorType.Parameter));
                }
                else if (element is Function)
                {
                    Function function = (Function)element;
                    items.Add(newItem(function.Name, CodeDrawStyle.ColorType.Identifier));
                }
                else if (element is Task)
                {
                    Task task = (Task)element;
                    items.Add(newItem(task.Name, CodeDrawStyle.ColorType.Identifier));
                }
            }

            if (Parent != null)
            {
                Parent.AppendAutoCompleteItem(items);
            }

            // Also add items from comment scope references (legacy fallback)
            AppendAutoCompleteItemFromCommentScopes(items);
        }

        public DataObjects.Constants.Constants? GetConstants(string identifier)
        {
            if (NamedElements.ContainsKey(identifier))
            {
                INamedElement element = NamedElements[identifier];
                DataObjects.Constants.Constants? constants = element as DataObjects.Constants.Constants;
                return constants;
            }

            if (Parent != null)
            {
                return Parent.getConstantsHier(identifier);
            }
            else
            {

            }
            return null;
        }

        private DataObjects.Constants.Constants? getConstantsHier(string identifier)
        {
            if (NamedElements.ContainsKey(identifier))
            {
                INamedElement element = NamedElements[identifier];
                DataObjects.Constants.Constants? constants = element as DataObjects.Constants.Constants;
                return constants;
            }

            if (Parent != null)
            {
                return Parent.getConstantsHier(identifier);
            }

            return null;
        }

        /// <summary>
        /// Searches for an element in comment scope references.
        /// This allows accessing elements from other building blocks declared via @scope annotation.
        /// </summary>
        /// <param name="name">Element name to search for</param>
        /// <param name="nameSpace">Output: NameSpace where the element was found</param>
        /// <returns>The element if found, null otherwise</returns>
        public INamedElement? GetNamedElementFromCommentScopes(string name, out NameSpace? nameSpace)
        {
            nameSpace = null;

            foreach (var scopeRef in CommentScopeReferences)
            {
                // Try to resolve the scope reference if not already resolved
                if (scopeRef.ResolvedBuildingBlock == null)
                {
                    resolveScopeReference(scopeRef);
                }

                if (scopeRef.ResolvedBuildingBlock != null)
                {
                    // If instance name is specified, look for the instance first
                    if (!string.IsNullOrEmpty(scopeRef.InstanceName))
                    {
                        // Search for instance in the building block
                        foreach (var elem in scopeRef.ResolvedBuildingBlock.NamedElements.Values)
                        {
                            if (elem is Data.VerilogModuleInstance instance && instance.Name == scopeRef.InstanceName)
                            {
                                // Found the instance, now search inside it using ParsedDocument
                                var found = GetElementFromModuleInstance(instance, name, out NameSpace? foundSpace);
                                if (found != null)
                                {
                                    nameSpace = foundSpace;
                                    return found;
                                }
                            }
                        }
                    }
                    else
                    {
                        // No instance name - direct access to building block's namespace
                        var found = scopeRef.ResolvedBuildingBlock.GetNamedElementUpward(name, out NameSpace? foundSpace);
                        if (found != null)
                        {
                            nameSpace = foundSpace;
                            return found;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves a scope reference to get the actual building block.
        /// Public so it can be called from VerilogFile.AcceptParsedDocumentAsync
        /// after all building blocks are registered.
        /// </summary>
        public void resolveScopeReference(CommentScopeReference scopeRef)
        {
            if (BuildingBlock?.File?.ProjectProperty == null) return;

            var projectProperty = BuildingBlock.File.ProjectProperty;

            BuildingBlock? buildingBlock = null;

            if (scopeRef.ParameterOverrides != null && scopeRef.ParameterOverrides.Count > 0)
            {
                // Build a synthetic IBuildingBlockInstantiation-like structure to use
                // the parameterized lookup logic in ProjectProperty.GetInstancedBuildingBlock.
                // For now, GetInstancedBuildingBlock requires an IBuildingBlockInstantiation.
                // Fall back to the base block; parameter overrides for @scope are
                // applied lazily by the VirtualScopeNameSpace wrapper.
                buildingBlock = projectProperty.GetBuildingBlock(scopeRef.BuildingBlockName);

                if (buildingBlock != null)
                {
                    scopeRef.ResolvedBuildingBlock = buildingBlock;
                }
            }
            else
            {
                buildingBlock = projectProperty.GetBuildingBlock(scopeRef.BuildingBlockName);
                scopeRef.ResolvedBuildingBlock = buildingBlock;
            }
        }

        /// <summary>
        /// Gets an element from a VerilogModuleInstance by searching its parsed document.
        /// </summary>
        private INamedElement? GetElementFromModuleInstance(Data.VerilogModuleInstance instance, string name, out NameSpace? foundNameSpace)
        {
            foundNameSpace = null;
            var vParsedDoc = instance.VerilogParsedDocument;
            if (vParsedDoc?.Root == null) return null;

            // Get the module's namespace
            var moduleName = instance.ModuleName;
            if (vParsedDoc.Root.BuildingBlocks.TryGetValue(moduleName, out var module))
            {
                return module.GetNamedElementUpward(name, out foundNameSpace);
            }

            return null;
        }

        /// <summary>
        /// Adds autocomplete items from comment scope references.
        /// </summary>
        public void AppendAutoCompleteItemFromCommentScopes(List<AutocompleteItem> items)
        {
            foreach (var scopeRef in CommentScopeReferences)
            {
                // Try to resolve the scope reference if not already resolved
                if (scopeRef.ResolvedBuildingBlock == null)
                {
                    resolveScopeReference(scopeRef);
                }

                if (scopeRef.ResolvedBuildingBlock != null)
                {
                    // If instance name is specified, add items from the instance
                    if (!string.IsNullOrEmpty(scopeRef.InstanceName))
                    {
                        // Search for instance in the building block
                        foreach (var elem in scopeRef.ResolvedBuildingBlock.NamedElements.Values)
                        {
                            if (elem is Data.VerilogModuleInstance instance && instance.Name == scopeRef.InstanceName)
                            {
                                // Get elements from the instance's parsed document
                                AppendAutoCompleteItemsFromModuleInstance(instance, items);
                            }
                        }
                    }
                    else
                    {
                        // No instance name - add items directly from building block's namespace
                        scopeRef.ResolvedBuildingBlock.AppendAutoCompleteItem(items);
                    }
                }
            }
        }

        /// <summary>
        /// Appends autocomplete items from a VerilogModuleInstance.
        /// </summary>
        private void AppendAutoCompleteItemsFromModuleInstance(Data.VerilogModuleInstance instance, List<AutocompleteItem> items)
        {
            var vParsedDoc = instance.VerilogParsedDocument;
            if (vParsedDoc?.Root == null) return;

            // Get the module's namespace
            var moduleName = instance.ModuleName;
            if (vParsedDoc.Root.BuildingBlocks.TryGetValue(moduleName, out var module))
            {
                module.AppendAutoCompleteItem(items);
            }
        }

        /// <summary>
        /// Resolves and registers all @scope comment references in this NameSpace.
        /// For each reference, a <see cref="VirtualScopeNameSpace"/> is created and
        /// added to <see cref="NamedElements"/> so that it is reachable from
        /// expression parse and autocomplete as if it were a normal sub-namespace.
        /// This should be called AFTER all building blocks in the project have been
        /// registered (i.e., after VerilogFile.AcceptParsedDocumentAsync).
        /// </summary>
        public void ApplyCommentScopeReferences()
        {
            foreach (var scopeRef in CommentScopeReferences)
            {
                if (scopeRef.ResolvedBuildingBlock == null)
                {
                    resolveScopeReference(scopeRef);
                }

                if (scopeRef.ResolvedBuildingBlock == null) continue;

                // Determine the name to register under.
                string entryName = !string.IsNullOrEmpty(scopeRef.InstanceName)
                    ? scopeRef.InstanceName
                    : scopeRef.BuildingBlockName;

                if (string.IsNullOrEmpty(entryName)) continue;

                // Skip if already registered (idempotent)
                if (NamedElements.ContainsKey(entryName))
                {
                    INamedElement? existing = NamedElements[entryName];
                    if (existing is VirtualScopeNameSpace v && v.SourceCommentScopeReference == scopeRef)
                    {
                        continue;
                    }
                }

                var virtualNs = VirtualScopeNameSpace.Create(
                    sourceScopeRef: scopeRef,
                    target: scopeRef.ResolvedBuildingBlock,
                    entryName: entryName,
                    parent: this);

                try
                {
                    if (NamedElements.ContainsKey(entryName))
                    {
                        NamedElements.RemoveKey(entryName);
                    }
                    NamedElements.Add(entryName, virtualNs);
                }
                catch
                {
                    // duplicate-safe ignore
                }
            }
        }
    }
}
