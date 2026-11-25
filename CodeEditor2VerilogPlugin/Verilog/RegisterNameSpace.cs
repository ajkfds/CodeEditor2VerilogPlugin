using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    /// <summary>
    /// The editor-facing view of a parser name space.
    ///
    /// <para>
    /// The implementation moved to the UI-free parser assembly
    /// (<see cref="SystemVerilogCore.Verilog.RegisterNameSpace{T}"/>), because the
    /// parser is its only consumer and it only needs the
    /// <see cref="SystemVerilogCore.Verilog.IVerilogNameSpaceFile"/> contract from
    /// a file. This class is a thin adapter so the editor keeps its
    /// <c>Data.IVerilogRelatedFile</c> / <c>Data.VerilogFile</c> types at the
    /// call sites:
    /// </para>
    ///
    /// <code>
    /// projectProperty.DefinitionNameSpace.Register(name, block, this);   // this = VerilogFile
    /// Data.IVerilogRelatedFile? file = projectProperty.DefinitionNameSpace.GetFile(name);
    /// </code>
    ///
    /// <para>
    /// <c>Data.VerilogFile</c> implements the name-space file contract by
    /// delegating to its own parse result, so no cast is needed inside the
    /// adapter. A file type that cannot provide the contract (an instance
    /// wrapper, for example) is not registered.
    /// </para>
    /// </summary>
    public class RegisterNameSpace<T> where T : class
    {
        private readonly SystemVerilogCore.Verilog.RegisterNameSpace<T> inner = new();

        /// <summary>
        /// Records that <paramref name="buildingBlockName"/> is declared in
        /// <paramref name="file"/>.
        /// </summary>
        public void Register(string buildingBlockName, T buildingBlock, Data.VerilogFile file)
        {
            // `Data.VerilogFile` implements the UI-free name-space contract; the
            // cast is a no-op in practice and keeps the public signature the
            // editor's call sites already use.
            inner.Register(
                buildingBlockName,
                buildingBlock,
                file as SystemVerilogCore.Verilog.IVerilogNameSpaceFile);
        }

        /// <summary>
        /// The file that declared <paramref name="name"/>, or null.
        /// </summary>
        /// <remarks>
        /// The name space stores the file as the UI-free contract, but the
        /// editor's call sites expect its own file type, so the contract is
        /// narrowed back here. This is the single place where that narrowing
        /// happens.
        /// </remarks>
        public Data.IVerilogRelatedFile? GetFile(string name)
        {
            return inner.GetFile(name) as Data.IVerilogRelatedFile;
        }

        /// <summary>
        /// Every registered name whose declaration matches
        /// <paramref name="isMatched"/>.
        /// </summary>
        public List<string> GetNameList(Func<T, bool> isMatched)
        {
            return inner.GetNameList(isMatched);
        }

        /// <summary>
        /// Resolves <paramref name="name"/> to its declared element, or null.
        /// </summary>
        public T? Get(string name)
        {
            return inner.Get(name);
        }
    }
}
