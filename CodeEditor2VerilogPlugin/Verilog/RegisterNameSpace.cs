using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class RegisterNameSpace<T> where T : class
    {
        // Thread-safe access using ReaderWriterLockSlim
        private readonly System.Threading.ReaderWriterLockSlim accessLock = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.NoRecursion);

        // BuildingBlock -> File Table
        private WeakReferenceDictionary<string, Data.IVerilogRelatedFile> fileTable = new WeakReferenceDictionary<string, Data.IVerilogRelatedFile>();


        public void Register(string buildingBlockName, T buildingBlock, Data.VerilogFile file)
        {
            if (file == null)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                return;
            }

            accessLock.EnterWriteLock();
            try
            {
                fileTable.Register(buildingBlockName, file);
            }
            finally
            {
                accessLock.ExitWriteLock();
            }
        }


        public Data.IVerilogRelatedFile? GetFile(string name)
        {
            accessLock.EnterReadLock();
            try
            {
                return _GetFile(name);
            }
            finally
            {
                accessLock.ExitReadLock();
            }
        }
        private Data.IVerilogRelatedFile? _GetFile(string name)
        {
            return fileTable.GetItem(name);
        }


        public List<string> GetNameList(Func<T, bool> isMatched)
        {
            List<string> results = new List<string>();

            accessLock.EnterReadLock();
            try
            {
                foreach(var key in fileTable.KeyList())
                {
                    T? target = _Get(key);
                    if (target == null) continue; 
                    if(isMatched(target)) results.Add(key);
                }
                return results;
            }
            finally
            {
                accessLock.ExitReadLock();
            }
        }

        public T? Get(string name)
        {
            Data.IVerilogRelatedFile? file = GetFile(name);
            if (file == null) return null;

            if (file.VerilogParsedDocument == null) return null;
            if (file.VerilogParsedDocument.Root == null) return null;

            if (!file.VerilogParsedDocument.Root.NamedElements.ContainsKey(name)) return null;
            return file.VerilogParsedDocument.Root.NamedElements[name] as T;
        }
        private T? _Get(string name)
        {
            Data.IVerilogRelatedFile? file = _GetFile(name);
            if (file == null) return null;

            if (file.VerilogParsedDocument == null) return null;
            if (file.VerilogParsedDocument.Root == null) return null;

            if (!file.VerilogParsedDocument.Root.NamedElements.ContainsKey(name)) return null;
            return file.VerilogParsedDocument.Root.NamedElements[name] as T;
        }
    }
}
