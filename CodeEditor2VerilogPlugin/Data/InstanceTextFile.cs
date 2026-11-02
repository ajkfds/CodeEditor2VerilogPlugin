using System;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace pluginVerilog.Data
{
    public class InstanceTextFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {

        protected InstanceTextFile(CodeEditor2.Data.TextFile sourceTextFile)
        {
            sourceFileRef = new WeakReference<CodeEditor2.Data.TextFile>(sourceTextFile);
        }
        // Instance array support
        private int _instanceIndex = 0;
        public int InstanceIndex
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _instanceIndex;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _instanceIndex = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        private bool _isArrayInstance = false;
        public bool IsArrayInstance
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _isArrayInstance;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _isArrayInstance = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }
        internal string _instanceName = "";
        public required string InstanceName
        {
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _instanceName = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _instanceName;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public bool ExternalProject { set; get; } = false;
        private System.WeakReference<CodeEditor2.Data.TextFile> sourceFileRef;
        public CodeEditor2.Data.TextFile? SourceTextFile
        {
            get
            {
                CodeEditor2.Data.TextFile? ret;
                if (!sourceFileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }
        public Data.VerilogFile? SourceVerilogFile
        {
            get
            {
                Data.VerilogFile? vFile = SourceTextFile as VerilogFile;
                return vFile;
            }
        }
        public override string Key
        {
            get
            {
                return _getKey();
            }
        }
        /// <summary>
        /// Atomically generates the key from module name and parameter overrides while holding the read lock.
        /// This ensures that the key is consistent - both values are read in a single atomic operation.
        /// </summary>
        internal string _getKey()
        {
            textFileLock.EnterReadLock();
            try
            {
                // Read both values atomically within the same lock acquisition
                return Verilog.ParsedDocument.KeyGenerator(this, _instanceName, _parameterOverrides);
            }
            finally
            {
                textFileLock.ExitReadLock();
            }
        }

        private new CodeEditor2.CodeEditor.ParsedDocument? _parsedDocument;
        public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                Data.VerilogFile? source = SourceVerilogFile;
                CodeEditor2.CodeEditor.ParsedDocument? parsedDocument = source?.GetInstancedParsedDocument(_getKey());

                // 取得するたびに保持しているparsedDocumentを最新のものに置き換える。
                // 不要なparsedDocumentのGC回収を促進するため。
                _parsedDocument = parsedDocument;
                return parsedDocument;
            }
            set
            {
                // ModuleInstanceで保持する。instanceが消えないようにするために。
                _parsedDocument = value;
                // Sourceへの登録はAcceptParseDocument側で行う
            }
        }
        public void CheckDirty()
        {
            Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
            if (vParsedDocument == null) return;
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = CodeDocument;
            if (codeDocument == null) return;
            if (codeDocument.Version != vParsedDocument.Version)
            {
                ReparseRequested = true;
            }
        }
        public override bool ReparseRequested
        {
            get
            {
                Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return true;
                return vParsedDocument.ReparseRequested;
            }
            set
            {
                Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return;
                vParsedDocument.ReparseRequested = value;
            }
        }

        public override async Task<CodeEditor2.CodeEditor.CodeDocument?> GetCodeDocumentAsync()
        {
            if (SourceVerilogFile == null) return null;
            return await SourceVerilogFile.GetCodeDocumentAsync();
        }

        public override CodeEditor2.CodeEditor.CodeDocument? CodeDocument
        {
            get
            {
                return SourceVerilogFile?.CodeDocument;
            }
        }
        internal Dictionary<string, Verilog.Expressions.Expression> _parameterOverrides = new Dictionary<string, Verilog.Expressions.Expression>();
        public required Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides
        {
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _parameterOverrides = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _parameterOverrides;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }


        public virtual ProjectProperty ProjectProperty { get; }
        private bool _systemVerilog = false;
        public bool SystemVerilog
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _systemVerilog;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _systemVerilog = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Saves the source text file that owns this instance's CodeDocument and
        /// then refreshes this instance's navigate panel node.
        /// </summary>
        /// <remarks>
        /// An instance file (module / interface / imported package / header
        /// instance) shares the source file's CodeDocument and delegates saving to
        /// the source file. <see cref="CodeEditor2.Data.TextFile.SaveAsync"/>
        /// refreshes only the navigate panel node of the file it was called on (the
        /// source file), so the instance node kept showing the previous icon — the
        /// dirty (shine) badge stayed visible after Save until the node was
        /// re-selected. Refresh the instance node explicitly here.
        /// </remarks>
        protected async Task SaveSourceTextFileAsync()
        {
            CodeEditor2.Data.TextFile? source = SourceTextFile;
            if (source == null) return;

            await source.SaveAsync();

            // The node is repainted on the UI thread. Saves normally originate from
            // the UI thread (Ctrl+S / File menu) but keep a fallback for callers on
            // a background thread, matching the other instance node updates.
            CodeEditor2.NavigatePanel.NavigatePanelNode? node = NavigatePanelNode;
            if (node == null) return;
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                node.UpdateVisual();
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => node.UpdateVisual());
            }
        }

        public override void PostStatusCheck()
        {
            CodeEditor2.Data.TextFile? source = SourceTextFile;
            if (source != null) source.PostStatusCheck();
        }

        protected override async Task FileCheckAsync()
        {
            await Task.CompletedTask;
        }
        
        public virtual string AbsolutePath { get; }

        public Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument; // no lock is needed. ParsedDocument property is thread safe
            }
        }

    }
}
