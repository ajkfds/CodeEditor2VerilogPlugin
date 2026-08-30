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
    /// The implementation reuses the parser's own lookup logic
    /// (<see cref="NameSpace.GetHierarchyNameSpace"/> and
    /// <see cref="NameSpace.GetNamedElementUpward"/>) so that the result
    /// matches what the editor itself would jump to.
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
            (pluginVerilog.Verilog.INamedElement? element, _) = Resolve(parsed, file, index);
            if (element == null) return null;
            return NamedElementAdapter.TryCreate(element, file);
        }

        /// <summary>
        /// Returns the set of references to the symbol that contains
        /// <paramref name="index"/>. The first element is the declaration
        /// itself when it is known. When the declaration is a
        /// <see cref="pluginVerilog.Verilog.DataObjects.DataObject"/> the
        /// full <c>UsedReferences</c>/<c>AssignedReferences</c> list is
        /// returned; for other symbol kinds only the definition is included
        /// (the parser does not currently track references for them).
        /// </summary>
        public static IReadOnlyList<ISystemVerilogNamedElement> FindReferences(
            pluginVerilog.Verilog.ParsedDocument parsed,
            SystemVerilogFileAdapter file,
            int index)
        {
            (pluginVerilog.Verilog.INamedElement? element, _) = Resolve(parsed, file, index);
            if (element == null) return System.Array.Empty<ISystemVerilogNamedElement>();

            // For DataObject declarations we have a real reference list.
            if (element is pluginVerilog.Verilog.DataObjects.DataObject dataObject)
            {
                List<ISystemVerilogNamedElement> references = new List<ISystemVerilogNamedElement>();
                if (dataObject.DefinedReference != null)
                {
                    if (TryMakeReferenceElement(dataObject, dataObject.DefinedReference, file, references))
                    {
                        // definition included
                    }
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
            ISystemVerilogNamedElement? declaration = NamedElementAdapter.TryCreate(element, file);
            if (declaration == null) return System.Array.Empty<ISystemVerilogNamedElement>();
            return new[] { declaration };
        }

        /// <summary>
        /// Resolves the identifier at <paramref name="index"/> using the
        /// parsed document's namespace tree. Returns the resolved element
        /// together with the namespace that contained the index.
        /// </summary>
        private static (pluginVerilog.Verilog.INamedElement?, pluginVerilog.Verilog.NameSpace?) Resolve(
            pluginVerilog.Verilog.ParsedDocument parsed,
            SystemVerilogFileAdapter file,
            int index)
        {
            if (parsed.Root == null) return (null, null);

            // Locate the word containing the index. We do not validate the
            // word text because the parser already handles identifier vs.
            // operator ambiguity.
            pluginVerilog.CodeEditor.CodeDocument codeDocument = parsed.CodeDocument;
            codeDocument.GetWord(index, out int wordStart, out int wordLength);
            if (wordLength <= 0) return (null, null);
            if (wordStart < 0 || wordStart >= codeDocument.Length) return (null, null);
            string text = codeDocument.CreateString(wordStart, wordLength);
            if (string.IsNullOrEmpty(text)) return (null, null);

            pluginVerilog.Verilog.IndexReference iref =
                pluginVerilog.Verilog.IndexReference.Create(parsed, codeDocument, wordStart);

            pluginVerilog.Verilog.NameSpace? ns = parsed.Root.GetHierarchyNameSpace(iref);
            if (ns == null) return (null, null);

            pluginVerilog.Verilog.INamedElement? element = ns.GetNamedElementUpward(text);
            return (element, ns);
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
            // Skip duplicate entries. A single source line is often added to
            // both UsedReferences and AssignedReferences, so we have to
            // dedupe by (index, length).
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
        /// through the LSP-friendly surface.
        /// </summary>
        private sealed class ReferenceAdapter : ISystemVerilogNamedElement
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
