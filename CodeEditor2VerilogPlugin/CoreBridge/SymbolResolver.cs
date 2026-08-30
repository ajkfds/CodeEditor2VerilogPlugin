using System.Collections.Generic;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using SystemVerilogCore;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Helper that resolves symbols in a <see cref="pluginVerilog.Verilog.ParsedDocument"/>
    /// and exposes the result through the <see cref="ISystemVerilogNamedElement"/>
    /// surface. The resolver is intentionally narrow in scope: it returns the
    /// definition site of the identifier at a given character index and the
    /// set of references known to the parser.
    ///
    /// Lookups happen in two stages:
    /// <list type="number">
    /// <item>Local: the parser's namespace tree
    /// (<see cref="NameSpace.GetHierarchyNameSpace"/> +
    /// <see cref="NameSpace.GetNamedElementUpward"/>).</item>
    /// <item>Cross-file: the project-wide
    /// <see cref="ProjectProperty.DefinitionNameSpace"/> and
    /// <see cref="ProjectProperty.PackageNameSpace"/> registries. These are
    /// populated by the parser as it walks every file in the project, so a
    /// jump from one file to another can resolve without the caller having
    /// to know which file holds the declaration.</item>
    /// </list>
    /// </summary>
    internal static class SymbolResolver
    {
        /// <summary>
        /// Returns the definition of the identifier containing
        /// <paramref name="index"/>, or <c>null</c> if the index does not
        /// land on a known symbol.
        /// </summary>
        public static ISystemVerilogNamedElement? FindDefinition(
            pluginVerilog.Verilog.ParsedDocument parsed,
            SystemVerilogFileAdapter file,
            int index)
        {
            (pluginVerilog.Verilog.INamedElement? element, SystemVerilogFileAdapter ownerFile) = Resolve(parsed, file, index);
            if (element == null) return null;
            return NamedElementAdapter.TryCreate(element, ownerFile);
        }

        /// <summary>
        /// Returns the set of references to the symbol that contains
        /// <paramref name="index"/>. The first element is the declaration
        /// itself when it is known. When the declaration is a
        /// <see cref="pluginVerilog.Verilog.DataObjects.DataObject"/> the
        /// full <c>UsedReferences</c>/<c>AssignedReferences</c> list is
        /// returned; for other symbol kinds only the definition is included
        /// (the parser does not currently track references for them).
        ///
        /// Cross-file references: when the declaration lives in a different
        /// file, only the declaration itself is returned. The plugin's
        /// parser does not currently aggregate reference sites across files,
        /// so we cannot list every use site project-wide without additional
        /// plumbing.
        /// </summary>
        public static IReadOnlyList<ISystemVerilogNamedElement> FindReferences(
            pluginVerilog.Verilog.ParsedDocument parsed,
            SystemVerilogFileAdapter file,
            int index)
        {
            (pluginVerilog.Verilog.INamedElement? element, SystemVerilogFileAdapter ownerFile) = Resolve(parsed, file, index);
            if (element == null) return System.Array.Empty<ISystemVerilogNamedElement>();

            if (ownerFile != file)
            {
                // Cross-file declaration. We don't know every use site
                // project-wide, so we return just the declaration.
                ISystemVerilogNamedElement? declaration = NamedElementAdapter.TryCreate(element, ownerFile);
                if (declaration == null) return System.Array.Empty<ISystemVerilogNamedElement>();
                return new[] { declaration };
            }

            // For DataObject declarations we have a real reference list.
            if (element is pluginVerilog.Verilog.DataObjects.DataObject dataObject)
            {
                List<ISystemVerilogNamedElement> references = new List<ISystemVerilogNamedElement>();
                if (dataObject.DefinedReference != null)
                {
                    TryMakeReferenceElement(dataObject, dataObject.DefinedReference, file, references);
                }
                foreach (pluginVerilog.Verilog.WordReference used in dataObject.UsedReferences)
                {
                    TryMakeReferenceElement(dataObject, used, file, references);
                }
                foreach (pluginVerilog.Verilog.WordReference assigned in dataObject.AssignedReferences)
                {
                    TryMakeReferenceElement(dataObject, assigned, file, references);
                }
                return references;
            }

            // For other symbols we don't yet track references; return the
            // declaration only.
            ISystemVerilogNamedElement? declarationOnly = NamedElementAdapter.TryCreate(element, ownerFile);
            if (declarationOnly == null) return System.Array.Empty<ISystemVerilogNamedElement>();
            return new[] { declarationOnly };
        }

        /// <summary>
        /// Resolves the identifier at <paramref name="index"/> using the
        /// parsed document's namespace tree, falling back to the
        /// project-wide registry. Returns the resolved element together
        /// with the file adapter that owns it (which may differ from
        /// <paramref name="file"/> when the declaration lives in another
        /// file).
        /// </summary>
        private static (pluginVerilog.Verilog.INamedElement?, SystemVerilogFileAdapter) Resolve(
            pluginVerilog.Verilog.ParsedDocument parsed,
            SystemVerilogFileAdapter file,
            int index)
        {
            if (parsed.Root == null) return (null, file);

            // Locate the word containing the index. We do not validate the
            // word text because the parser already handles identifier vs.
            // operator ambiguity.
            pluginVerilog.CodeEditor.CodeDocument codeDocument = parsed.CodeDocument;
            codeDocument.GetWord(index, out int wordStart, out int wordLength);
            if (wordLength <= 0) return (null, file);
            if (wordStart < 0 || wordStart >= codeDocument.Length) return (null, file);
            string text = codeDocument.CreateString(wordStart, wordLength);
            if (string.IsNullOrEmpty(text)) return (null, file);

            pluginVerilog.Verilog.IndexReference iref =
                pluginVerilog.Verilog.IndexReference.Create(parsed, codeDocument, wordStart);

            pluginVerilog.Verilog.NameSpace? ns = parsed.Root.GetHierarchyNameSpace(iref);
            if (ns != null)
            {
                pluginVerilog.Verilog.INamedElement? element = ns.GetNamedElementUpward(text);
                if (element != null) return (element, file);
            }

            // Cross-file fallback: look up the symbol in the project-wide
            // definition namespace. This is the path that resolves references
            // like `submodule_instance.submodule_port` or `pkg::MY_CONST`
            // when the declaration is in another file.
            SystemVerilogFileAdapter? crossFile = ResolveCrossFile(parsed, text);
            if (crossFile == null) return (null, file);

            pluginVerilog.Data.IVerilogRelatedFile otherVerilogFile = crossFile.File;
            pluginVerilog.Verilog.ParsedDocument? otherParsed = otherVerilogFile.VerilogParsedDocument;
            if (otherParsed?.Root == null) return (null, file);
            if (!otherParsed.Root.NamedElements.TryGetValue(text, out pluginVerilog.Verilog.INamedElement? crossElement))
            {
                return (null, file);
            }
            return (crossElement, crossFile);
        }

        /// <summary>
        /// Returns the file adapter for the file in which
        /// <paramref name="identifier"/> is defined, or <c>null</c> if the
        /// identifier is not known project-wide.
        /// </summary>
        private static SystemVerilogFileAdapter? ResolveCrossFile(
            pluginVerilog.Verilog.ParsedDocument parsed,
            string identifier)
        {
            pluginVerilog.ProjectProperty? projectProperty = parsed.ProjectProperty;
            if (projectProperty == null) return null;

            // 1. Top-level modules / interfaces / programs / primitives /
            //    checkers / classes (anything stored in DefinitionNameSpace).
            pluginVerilog.Data.IVerilogRelatedFile? declaredFile = projectProperty.DefinitionNameSpace.GetFile(identifier);
            if (declaredFile != null) return new SystemVerilogFileAdapter(declaredFile);

            // 2. Packages (separate registry because they have their own
            //    global namespace).
            declaredFile = projectProperty.PackageNameSpace.GetFile(identifier);
            if (declaredFile != null) return new SystemVerilogFileAdapter(declaredFile);

            return null;
        }

        /// <summary>
        /// Wraps a single reference into a fresh named-element adapter so the
        /// caller can produce a <c>Location</c> from its
        /// <see cref="ISystemVerilogNamedElement.DefinitionRange"/>. The
        /// helper is a no-op when the same word reference has already been
        /// emitted.
        /// </summary>
        private static bool TryMakeReferenceElement(
            pluginVerilog.Verilog.DataObjects.DataObject declaration,
            pluginVerilog.Verilog.WordReference wordReference,
            SystemVerilogFileAdapter file,
            List<ISystemVerilogNamedElement> output)
        {
            int length = wordReference.Length > 0 ? wordReference.Length : declaration.Name.Length;
            for (int i = 0; i < output.Count; i++)
            {
                if (output[i] is ReferenceAdapter existing
                    && existing.StartIndex == wordReference.Index
                    && existing.Length == length)
                {
                    return false;
                }
            }
            output.Add(new ReferenceAdapter(declaration, file, wordReference.Index, length));
            return true;
        }

        /// <summary>
        /// Lightweight read-only <see cref="ISystemVerilogNamedElement"/>
        /// implementation backed by a single word reference. Used to expose
        /// use sites of a <see cref="pluginVerilog.Verilog.DataObjects.DataObject"/>
        /// through the LSP-friendly surface. Exposed as <c>internal</c> so
        /// <see cref="PluginHoverContentProvider"/> can skip the underlying
        /// element lookup.
        /// </summary>
        internal sealed class ReferenceAdapter : ISystemVerilogNamedElement
        {
            public ReferenceAdapter(
                pluginVerilog.Verilog.DataObjects.DataObject declaration,
                SystemVerilogFileAdapter file,
                int startIndex,
                int length)
            {
                Declaration = declaration;
                File = file;
                StartIndex = startIndex;
                Length = length;
            }

            public pluginVerilog.Verilog.DataObjects.DataObject Declaration { get; }
            public int StartIndex { get; }
            public int Length { get; }

            public string Name => Declaration.Name ?? string.Empty;

            public SystemVerilogNamedElementKind Kind
            {
                get
                {
                    if (Declaration is pluginVerilog.Verilog.DataObjects.Nets.Net) return SystemVerilogNamedElementKind.Net;
                    if (Declaration is pluginVerilog.Verilog.DataObjects.Port) return SystemVerilogNamedElementKind.Port;
                    if (Declaration is pluginVerilog.Verilog.DataObjects.Constants.Parameter) return SystemVerilogNamedElementKind.Parameter;
                    if (Declaration is pluginVerilog.Verilog.DataObjects.Constants.Localparam) return SystemVerilogNamedElementKind.LocalParameter;
                    return SystemVerilogNamedElementKind.Variable;
                }
            }

            public SystemVerilogRange? DefinitionRange => new SystemVerilogRange(StartIndex, Length);

            public ISystemVerilogFile? File { get; }

            public ISystemVerilogBuildingBlock? Owner => null;
        }
    }
}
