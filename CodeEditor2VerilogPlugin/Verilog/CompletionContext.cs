using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Items.Generate;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog
{
    /// <summary>
    /// The SystemVerilog partial-parse completion context.
    ///
    /// <para>
    /// Derives from the UI-free contract
    /// (<see cref="SystemVerilogCore.Documents.VerilogCompletionContext"/>), not
    /// from the editor's completion type: the parser only fills plain values and
    /// contract lists, so it no longer references the editor assembly here. The
    /// editor's <c>CodeComplete.CompletionContext</c> derives from the same
    /// contract, which is what lets the editor consume this object directly.
    /// </para>
    /// </summary>
    public class CompletionContext : SystemVerilogCore.Documents.VerilogCompletionContext
    {
        public CompletionContext(
            SystemVerilogCore.Documents.IVerilogSourceFile item,
            Verilog.ParsedDocument parsedDocument,
            int index)
        {

            // Only the contract surface is needed here (length / substring /
            // line lookup), so the editor's concrete document type is not named.
            SystemVerilogCore.Documents.IVerilogTextDocument? codeDocument = item.VerilogTextDocument;
            if (codeDocument == null) return;
            CandidateWord = "";

            this.item = item;
            this.parsedDocument = parsedDocument;
            this.index = index;

            // GetAutoCompleteTarget returns false e.g. when the caret is right after a
            // space/tab (no dot context). Even in that case the namespace / candidate
            // word are resolved by the call itself; do not abort the completion flow,
            // otherwise the partial parse and the AppendAll fallback
            // (appendItemsUpward -> instance / variable name candidates) never run and
            // only keywords appear (candidates without a dot disappeared).
            // Through the settings contract: the walk is the parser's own
            // identifier-chain logic, but the host helper that implements it lives
            // in the data layer, which the parser must not name.
            parsedDocument.ProjectProperty?.GetAutoCompleteTarget(
                item, parsedDocument, index,
                out NameSpace, out NamedElement, out CandidateWord, out CandidateStartIndex);
            if (NameSpace == null && NamedElement == null)
            {
                return;
            }

            int line = codeDocument.GetLineAt(index);
            int lineStartIndex = codeDocument.GetLineStartIndex(line);

            onLineStart = false;
            while (true)
            {
                string lineString = codeDocument.CreateString(lineStartIndex, codeDocument.GetLineLength(line));
                int inlinePosition = CandidateStartIndex - lineStartIndex;
                if (inlinePosition < 0) break;
                if (inlinePosition > lineString.Length) break;

                string headString = lineString.Substring(0, inlinePosition);
                headString = headString.Replace("\t", " ");
                headString = headString.Replace(" ", "");
                if (headString.Length == 0) onLineStart = true;
                break;
            }

            // hierarchical member access context right after the dot (e.g. "MODULE0 inst0."):
            // GetAutoCompleteTarget resolved the member target element (NamedElement) from the
            // identifier chain before the caret. Append its sub-elements directly and skip the
            // partial parse: the partial parse appends generic namespace items (with
            // CandidateWord == "" they all match), which fills AutoCompleteItems and suppresses
            // the AppendAll fallback in AutoComplete.GetAutoCompleteItems, so member candidates
            // never appear right after the dot (they only appeared after typing one more
            // character, when the CandidateWord filter emptied the generic items).
            //
            // This branch is limited to CandidateWord == "" (caret right after the dot).
            // While typing the last identifier of a chain ("inst0.mem" etc.) NamedElement is
            // also non-null, but early return there would skip the partial parse and the
            // AppendAll fallback, so instance / variable name candidates stopped appearing.
            // In that case keep the conventional flow (partial parse -> AppendAll -> member
            // candidates via appendNamedElements with NamedElement != null).
            if (NamedElement != null && CandidateWord == "")
            {
                AppendSubElements(NamedElement);
                return;
            }

            // partial parse
            int parseBlockIndex = 0;

            if (NameSpace == null) return;
            parseBlockIndex = NameSpace.BeginIndexReference.RootIndex;

            IndexReference iref = IndexReference.Create(lineStartIndex, parsedDocument);
            documentRegion = parsedDocument.GetDocumentRegionAt(iref);

            if (documentRegion != null && documentRegion.BeginIndexReference != null)
            {
                parseBlockIndex = documentRegion.BeginIndexReference.RootIndex;
            }
            if (CandidateStartIndex - parseBlockIndex < 1) return;

            string blockText = codeDocument.CreateString(parseBlockIndex, CandidateStartIndex - parseBlockIndex);
            // Throw-away buffer for the partial parse: a contract-only document
            // is enough, so this no longer builds the plugin's CodeDocument
            // (which would pull in the editor's document type).
            SystemVerilogCore.Documents.IVerilogTextDocument document =
                new SystemVerilogCore.Documents.PlainTextDocument(blockText);

            WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog,CandidateStartIndex,this);
            if (documentRegion is Verilog.Items.ModuleInstantiation)
            {
                #pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                Verilog.Items.ModuleInstantiation.ParseAsync(word, NameSpace).GetAwaiter().GetResult();
                #pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }else if(documentRegion is Module module)
            {
                if (word.Text == "module" || word.Text == "macromodule")
                {

                    Module.ParseCreateAsync(word, module.ParameterOverrides, module.Attribute, module.BuildingBlock, item, false).GetAwaiter().GetResult();
                }
            }else if(documentRegion is Verilog.Items.AlwaysConstruct)
            {
                // partial parse of "always ..." statement: propagate completionContext to statements
                if(word.Text == "always" || word.Text == "always_comb" || word.Text == "always_latch" || word.Text == "always_ff")
                {

                    Verilog.Items.AlwaysConstruct.ParseCreate(word, NameSpace);
                }
            }else if(documentRegion is Verilog.Items.InitialConstruct)
            {
                // partial parse of "initial ..." statement: propagate completionContext to statements
                if (word.Text == "initial")
                {

                    Verilog.Items.InitialConstruct.ParseCreate(word, NameSpace);
                }
            }else if(documentRegion is Verilog.Items.FinalConstruct)
            {
                // partial parse of "final ..." statement: propagate completionContext to statements
                if (word.Text == "final")
                {

                    Verilog.Items.FinalConstruct.ParseCreate(word, NameSpace);
                }
            }else if(documentRegion is Verilog.Items.ProgramInstantiation)
            {
                #pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

                Verilog.Items.ProgramInstantiation.ParseAsync(word, NameSpace).GetAwaiter().GetResult();
                #pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }else if(documentRegion is Verilog.Items.InterfaceInstance)
            {

                Verilog.Items.InterfaceInstance.Parse(word, NameSpace);
            }else if(documentRegion is Verilog.Items.BindDirective)
            {
                if (word.Text == "bind")
                {

                    Verilog.Items.BindDirective? bindDirective;
                    Verilog.Items.BindDirective.Parse(word, NameSpace, out bindDirective);
                }
            }else if(documentRegion is Verilog.Items.NetAlias)
            {
                if (word.Text == "alias")
                {

                    Verilog.Items.NetAlias.Parse(word, NameSpace);
                }
            }else if(documentRegion is Verilog.Items.ParameterOverride)
            {
                if (word.Text == "defparam")
                {

                    Verilog.Items.ParameterOverride.Parse(word, NameSpace);
                }
            }else if(documentRegion is Verilog.Statements.CaseStatement)
            {
                // partial parse of case statement: propagate completionContext (via WordScanner)
                // to case expression / case item parse

                Verilog.Statements.CaseStatement.ParseCreate(word, NameSpace, null);
            }else if(documentRegion is Verilog.Statements.WhileStatememt)
            {
                // partial parse of while statement: propagate completionContext (via WordScanner)
                // to condition expression / statement parse

                Verilog.Statements.WhileStatememt.ParseCreate(word, NameSpace, null);
            }else if(documentRegion is Verilog.Statements.RepeatStatement)
            {
                // partial parse of repeat statement: propagate completionContext (via WordScanner)
                // to condition expression / statement parse

                Verilog.Statements.RepeatStatement.ParseCreate(word, NameSpace, null);
            }else if(documentRegion is Verilog.Statements.ConditionalStatement)
            {
                // partial parse of if statement: propagate completionContext (via WordScanner)
                // to condition expression / statement parse

                Verilog.Statements.ConditionalStatement.ParseCreate(word, NameSpace, null);
            }else if(documentRegion is Verilog.Statements.ForStatememt)
            {
                // partial parse of for statement: propagate completionContext (via WordScanner)
                // to for_initialization / expression parse

                Verilog.Statements.ForStatememt.ParseCreate(word, NameSpace, null);
            }else if(documentRegion is NonBlockingAssignment)
            {

                Verilog.Statements.NonBlockingAssignment.ParseCreate(word, NameSpace);
            }else if(documentRegion is BlockingAssignment)
            {

                Verilog.Statements.BlockingAssignment.ParseCreate(word, NameSpace);
            }else if(documentRegion is SequentialBlock)
            {
                if(word.Text == "begin")
                {

                    Verilog.Statements.SequentialBlock.ParseCreate(word, NameSpace, null);
                }
            }
            else if(documentRegion is ParallelBlock)
            {
                if(word.Text == "fork")
                {
                    // partial parse of parallel block: propagate completionContext (via WordScanner)
                    // to statements inside the block

                    Verilog.Statements.ParallelBlock.ParseCreate(word, NameSpace, null);
                }
            }
            else if(documentRegion is Verilog.Statements.NamedSequentialBlock)
            {
                // NamedSequentialBlock ("begin : name") does not derive SequentialBlock;
                // partial parse to propagate completionContext to statements inside the block
                if(word.Text == "begin")
                {

                    Verilog.Statements.SequentialBlock.ParseCreate(word, NameSpace, null);
                }
            }
            else if(documentRegion is Verilog.Statements.NamedParallelBlock)
            {
                // NamedParallelBlock ("fork : name" / "name : fork") does not derive ParallelBlock;
                // partial parse to propagate completionContext to statements inside the block
                if(word.Text == "fork")
                {

                    Verilog.Statements.ParallelBlock.ParseCreate(word, NameSpace, null);
                }
            }
            else if(documentRegion is Verilog.Items.Generate.GenerateBlock)
            {
                if (word.Text == "begin")
                {
                    // partial parse of generate block items: propagate completionContext
                    // (via WordScanner) to udp / module instantiation parse inside the block

                    Verilog.Items.Generate.GenerateBlock.ParseAsync(word, NameSpace).GetAwaiter().GetResult();
                }
            }
            else if(documentRegion is Verilog.Items.GateInstantiation)
            {
                // partial parse of gate instantiation: propagate completionContext (via WordScanner)
                // to gate keyword / terminal parse

                Verilog.Items.GateInstantiation.Parse(word, NameSpace);
            }else if(documentRegion is Verilog.Items.ContinuousAssign)
            {
                if (word.Text == "assign")
                {
                    // partial parse of continuous assign: propagate completionContext (via WordScanner)
                    // to LHS / RHS expression parse (A9)

                    Verilog.Items.ContinuousAssign.ParseCreate(word, NameSpace, this);
                }
            }

            appendMacro((acItem) => true);
        }

        private SystemVerilogCore.Documents.IVerilogSourceFile item { get; init; } = null!;
        private Verilog.ParsedDocument parsedDocument { get; init; } = null!;
        int index { get; init; } = 0;
        private INamedElement? NamedElement;
        private NameSpace? NameSpace;
        private Verilog.Items.IDocumentRegeion? documentRegion = null;
        private int line = 0;
        private int lineStartIndex = 0;
        private bool onLineStart = false;

        public void AppendAll()
        {
            appendNamedElements((acItem) => true);
            appendKeyword((acItem) => true);
            appendSystemFunction((acItem) => true);
            appendSystemTask((acItem) => true);
        }
        public void AppendExpression()
        {
            appendNamedElements((acItem) =>
            {
                if (acItem.Kind == SystemVerilogAutocompleteItemKind.Variable) return true;
                if (acItem.Kind == SystemVerilogAutocompleteItemKind.Function) return true;
                if (acItem.Kind == SystemVerilogAutocompleteItemKind.Module) return true;
                return false;
            });
            appendSystemFunction((acItem) => true);
        }
        public void AppendKeywords(List<string> keywords)
        {
            appendKeyword((acItem) =>
            {
                if (keywords.Contains(acItem.Label)) return true;
                return false;
            });
        }

        // append DataObject (net / variable / parameter) autocomplete items only
        // used for LHS positions such as "assign |" (A9)
        public void AppendDataObjects()
        {
            appendNamedElements((acItem) =>
            {
                if (acItem.Kind == SystemVerilogAutocompleteItemKind.Variable) return true;
                return false;
            });
        }

        // append member (sub-element) autocomplete items of the given element
        // used for "obj." + EOF member name completion (A5)
        public void AppendSubElements(INamedElement element)
        {
            INamedElement? target = element;
            if (target is DataObjects.Variables.Object objElement)
            {
                // Variables.Object is not a NameSpace; resolve its source class
                BuildingBlocks.Class? sourceClass = objElement.GetSourceClass();
                if (sourceClass == null) return;
                target = sourceClass;
            }

            if (target is Verilog.Items.IBuildingBlockInstantiation instTarget)
            {
                // module / interface / program / udp instantiation: resolve the instanced
                // building block (e.g. hierarchical reference "module_instance1." members)
                BuildingBlock? instancedBuildingBlock = instTarget.GetInstancedBuildingBlock();
                if (instancedBuildingBlock == null) return;
                target = instancedBuildingBlock;
            }

            if (target is DataObjects.Variables.VirtualInterface virtualInterfaceTarget)
            {
                Verilog.BuildingBlocks.Interface? sourceInterface = virtualInterfaceTarget.GetSourceInterface();
                if (sourceInterface == null) return;
                target = sourceInterface;
            }

            foreach (INamedElement subElement in target.NamedElements.Values)
            {
                if (CandidateWord != "" && !subElement.Name.StartsWith(CandidateWord)) continue;
                if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                if (AutoCompleteItems.Find(x => x.Label == subElement.Name) != null) continue; // reject duplicated elements

                AutoCompleteItems.Add(new VerilogAutocompleteCandidate(
                    subElement.Name,
                    (byte)subElement.ColorType,
                    KindOf(subElement),
                    ElementIconPath));
            }
        }

        /// <summary>
        /// Maps a declared element to the parser-facing candidate kind, so hosts
        /// can pick their own icon / completion kind without seeing this type.
        /// </summary>
        private static SystemVerilogAutocompleteItemKind KindOf(INamedElement element)
        {
            if (element is NameSpace) return SystemVerilogAutocompleteItemKind.Module;
            if (element is Verilog.DataObjects.DataObject) return SystemVerilogAutocompleteItemKind.Variable;
            if (element is Verilog.DataObjects.Typedef) return SystemVerilogAutocompleteItemKind.Variable;
            if (element is Verilog.Items.ModuleInstantiation) return SystemVerilogAutocompleteItemKind.Module;
            if (element is Verilog.Items.UdpInstantiation) return SystemVerilogAutocompleteItemKind.Module;
            if (element is Verilog.BuiltInMethod) return SystemVerilogAutocompleteItemKind.Task;
            return SystemVerilogAutocompleteItemKind.Keyword;
        }

        private void appendMacro(Func<IVerilogAutocompleteItem, bool> filter)
        {
            if (parsedDocument.ProjectProperty == null) return;
            foreach (string macro in parsedDocument.ProjectProperty.Macros.Keys)
            {
                string macroText = "`" + macro;
                if (!macroText.StartsWith(CandidateWord)) continue;
                AddCandidate(filter, macroText, VerilogColorType.Keyword, SystemVerilogAutocompleteItemKind.Keyword, MacroIconPath);
            }
            foreach (string macro in parsedDocument.Macros.Keys)
            {
                string macroText = "`" + macro;
                if (!macroText.StartsWith(CandidateWord)) continue;
                AddCandidate(filter, macroText, VerilogColorType.Keyword, SystemVerilogAutocompleteItemKind.Keyword, MacroIconPath);
            }
        }

        // Icon assets are host-specific paths; the parser only passes them on.
        private const string MacroIconPath = "CodeEditor2/Assets/Icons/bookmark.svg";
        private const string ElementIconPath = "CodeEditor2/Assets/Icons/tag.svg";

        /// <summary>
        /// Builds a plain candidate and adds it when <paramref name="filter"/>
        /// accepts it.
        ///
        /// <para>
        /// The parser produces
        /// <see cref="SystemVerilogCore.Documents.VerilogAutocompleteCandidate"/>
        /// (label + palette index + kind) instead of the editor's candidate type,
        /// which is what keeps this file free of the editor assembly. Hosts wrap
        /// it in their own presentation object.
        /// </para>
        /// </summary>
        private void AddCandidate(
            Func<IVerilogAutocompleteItem, bool> filter,
            string label,
            VerilogColorType colorType,
            SystemVerilogAutocompleteItemKind kind,
            string? iconPath = null)
        {
            VerilogAutocompleteCandidate candidate =
                new VerilogAutocompleteCandidate(label, (byte)colorType, kind, iconPath);
            if (filter(candidate)) AutoCompleteItems.Add(candidate);
        }

        //private void AppendNameSpace(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        //{
        //    if (NamedElement == null) return;

        //    foreach (INamedElement subElement in NamedElement.NamedElements.Values)
        //    {
        //        if (CandidateWord != "" && !subElement.Name.StartsWith(CandidateWord)) continue;
        //        if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

        //        Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
        //            Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace,
        //            subElement.Name,
        //            CodeDrawStyle.ColorIndex(subElement.ColorType),
        //            Global.CodeDrawStyle.Color(subElement.ColorType),
        //            "CodeEditor2/Assets/Icons/tag.svg"
        //            );
        //        if (filter(acItem)) AutoCompleteItems.Add(acItem);
        //    }
        //}
        private void appendSystemTask(Func<IVerilogAutocompleteItem, bool> filter)
        {
            if (!CandidateWord.StartsWith("$")) return;
            if (parsedDocument.ProjectProperty == null) return;

            foreach (string key in parsedDocument.ProjectProperty.SystemTaskParsers.Keys)
            {
                if (!key.StartsWith(CandidateWord)) continue;
                AddCandidate(filter, key, VerilogColorType.Keyword, SystemVerilogAutocompleteItemKind.Task);
            }
        }
        private void appendSystemFunction(Func<IVerilogAutocompleteItem, bool> filter)
        {
            if (!CandidateWord.StartsWith("$")) return;
            if (parsedDocument.ProjectProperty == null) return;

            foreach (string key in parsedDocument.ProjectProperty.SystemFunctions.Keys)
            {
                if (!key.StartsWith(CandidateWord)) continue;
                AddCandidate(filter, key, VerilogColorType.Keyword, SystemVerilogAutocompleteItemKind.Function);
            }
        }
        public void AppendModuleInstanceSnippets(Func<IVerilogAutocompleteItem, bool> filter)
        {
            // CandidateWord.Length > 1: the candidate list would otherwise show
            // every module in the project after a single keystroke at the start
            // of a module-body line. One character is enough to narrow it.
            if (onLineStart && NameSpace != null && NameSpace.BuildingBlock is Module && CandidateWord.Length > 0 && (documentRegion is Module ||documentRegion is GenerateBlock))
            {
                IVerilogParserSettings? projectProperty = NameSpace.ProjectProperty;
                if (projectProperty == null) throw new Exception();

                List<string> moduleNames = projectProperty.DefinitionNameSpace.GetNameList((x) => { return (x is Module); });
                foreach (string moduleName in moduleNames)
                {
                    // A module name expands to a whole instantiation when accepted,
                    // which the parser cannot express (port connections, tab stops).
                    // The host builds that candidate; a host without the expansion
                    // contributes nothing here.
                    IVerilogAutocompleteItem? candidate =
                        projectProperty.CreateModuleInstanceCandidate(moduleName);
                    if (candidate != null && filter(candidate)) AutoCompleteItems.Add(candidate);
                }
            }

        }
        private void appendKeyword(Func<IVerilogAutocompleteItem, bool> filter)
        {
            // Keywords, including the block-opening ones whose insertion text is a
            // skeleton: the host builds those (see CreateKeywordCandidate), and the
            // rest are plain candidates the parser makes.
            parsedDocument.ProjectProperty?.AppendKeywordCandidates(
                AutoCompleteItems, CandidateWord, CandidateStartIndex, lineStartIndex,
                parsedDocument.SystemVerilog, filter);
        }

        private void appendNamedElements(Func<IVerilogAutocompleteItem, bool> filter)
        {
            if (NamedElement == null)
            {
                if (NameSpace != null)
                {
                    // search upward
                    appendItemsUpward(NameSpace, filter);
                }
            }
            else // sub element
            {
                // append sub-element items
                foreach (INamedElement subElement in NamedElement.NamedElements.Values)
                {
                    if (CandidateWord != "" && !subElement.Name.StartsWith(CandidateWord)) continue;
                    if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

                    AddCandidate(filter, subElement.Name, subElement.ColorType, KindOf(subElement), ElementIconPath);
                }
            }

            return;
        }

        public void appendItemsUpward(NameSpace nameSpace, Func<IVerilogAutocompleteItem, bool> filter)
        {
            foreach (INamedElement subElement in nameSpace.NamedElements.Values)
            {
                if (!subElement.Name.StartsWith(CandidateWord)) continue;
                if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                if (AutoCompleteItems.Find(x => x.Label == subElement.Name) != null) continue;   // reject duplicated elements

                AddCandidate(filter, subElement.Name, subElement.ColorType, KindOf(subElement), ElementIconPath);
            }
            if (nameSpace.Parent != null)
            {
                appendItemsUpward(nameSpace.Parent, filter);
            }

        }
    }
}
