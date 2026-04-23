using System;

namespace pluginVerilog.Data
{
    public class InstanceTextFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {

        protected InstanceTextFile(CodeEditor2.Data.TextFile sourceTextFile)
        {
            sourceFileRef = new WeakReference<CodeEditor2.Data.TextFile>(sourceTextFile);
        }

        private bool _externalProject = false;
        public bool ExternalProject
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _externalProject;
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
                    _externalProject = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        private System.WeakReference<CodeEditor2.Data.TextFile> sourceFileRef;
        public CodeEditor2.Data.TextFile? SourceTextFile
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    CodeEditor2.Data.TextFile? ret;
                    if (!sourceFileRef.TryGetTarget(out ret)) return null;
                    return ret;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public virtual Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument;
            }
        }

        public virtual ProjectProperty ProjectProperty
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    CodeEditor2.Data.TextFile? sourceFile = SourceTextFile;
                    if (sourceFile == null) throw new Exception("SourceTextFile is null");
                    CodeEditor2.Data.Project? project = sourceFile.Project;
                    if (project == null) throw new Exception("Project is null");
                    ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                    if (projectProperty == null) throw new Exception("ProjectProperty not found");
                    return projectProperty;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public virtual bool SystemVerilog
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    CodeEditor2.Data.TextFile? sourceFile = SourceTextFile;
                    if (sourceFile is IVerilogRelatedFile vFile)
                    {
                        return vFile.SystemVerilog;
                    }
                    return false;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public virtual string AbsolutePath
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    CodeEditor2.Data.TextFile? sourceFile = SourceTextFile;
                    if (sourceFile == null) throw new Exception("SourceTextFile is null");
                    return sourceFile.AbsolutePath;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public virtual void CheckDirty() { }

    }
}
