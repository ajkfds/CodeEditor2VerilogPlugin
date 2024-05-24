using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class WeakReferenceDictionary<T> where T : class
    {
        private Dictionary<string, System.WeakReference<T>> itemRefs = new Dictionary<string, WeakReference<T>>();


        public bool RegisterItem(string name, T item)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(name))
                {
                    T? prevFile;
                    if (!itemRefs[name].TryGetTarget(out prevFile))
                    {
                        itemRefs[name] = new WeakReference<T>(item);
                        return true;
                    }
                    else
                    {
                        if (prevFile == item)
                        {
                            System.Diagnostics.Debugger.Break(); // duplicated register
                            return true;
                        }
                        // other target registered
                        return false;
                    }
                }
                else
                {
                    itemRefs.Add(name, new WeakReference<T>(item));
                    return true;
                }
            }
        }

        public bool RemoveItem(string name, T file)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(name))
                {
                    T? prevFile;
                    if (!itemRefs[name].TryGetTarget(out prevFile))
                    {
                        System.Diagnostics.Debugger.Break(); // already disposed
                        itemRefs.Remove(name);
                        return true;
                    }
                    else
                    {
                        if (prevFile == file)
                        {
                            itemRefs.Remove(name);
                            return true;
                        }
                        // unmatched target file
                        return false;
                    }
                }
                else
                {
                    // no target file
                    return false;
                }
            }
        }

        public bool IsRegisterableItem(string name, T file)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(name))
                {
                    T? prevFile;
                    if (!itemRefs[name].TryGetTarget(out prevFile))
                    {
                        return true;
                    }
                    else
                    {
                        if (prevFile == file)
                        {
                            return false;
                        }
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        public T? GetItem(string name)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(name))
                {
                    T? file;
                    if (!itemRefs[name].TryGetTarget(out file))
                    {
                        return null;
                    }
                    else
                    {
                        return file;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
