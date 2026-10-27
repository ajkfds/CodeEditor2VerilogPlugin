using System;
using System.Threading.Tasks;


namespace pluginVerilog.Data
{
    public class InstanceTextFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {

        protected InstanceTextFile(CodeEditor2.Data.TextFile sourceTextFile)
        {
            sourceFileRef = new WeakReference<CodeEditor2.Data.TextFile>(sourceTextFile);
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

        public virtual Verilog.ParsedDocument? VerilogParsedDocument { get; }

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

        public virtual void CheckDirty() { }

    }
}
