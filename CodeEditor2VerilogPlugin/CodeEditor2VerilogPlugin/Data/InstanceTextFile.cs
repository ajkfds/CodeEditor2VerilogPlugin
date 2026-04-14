using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public virtual bool SystemVerilog { get; }

        public virtual string AbsolutePath { get; }

        public virtual void CheckDirty() { }

    }
}
