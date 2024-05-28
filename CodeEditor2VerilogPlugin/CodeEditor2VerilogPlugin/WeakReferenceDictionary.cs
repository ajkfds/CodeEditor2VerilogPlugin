using AjkAvaloniaLibs.Contorls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class WeakReferenceDictionary<K,T> : IEnumerable<T> where K:notnull where T: class 
    {
        private Dictionary<K, System.WeakReference<T>> itemRefs = new Dictionary<K, WeakReference<T>>();

        public bool Register(K key, T item)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(key))
                {
                    T? prevItem;
                    if (itemRefs[key].TryGetTarget(out prevItem))
                    {   // replace item
                        itemRefs[key] = new WeakReference<T>(item);
                        return true;
                    }
                    else
                    {
                        // prevItem already lost
                        itemRefs.Remove(key);  // remove lost reference 
                    }
                }

                itemRefs.Add(key, new WeakReference<T>(item));
                return true;
            }
        }

        public bool Remove(K key)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(key))
                {
                    itemRefs.Remove(key);
                    return true;
                }
                else
                {
                    // no target file
                    return false;
                }
            }
        }

        public bool HasItem(K key)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(key))
                {
                    T? prevFile;
                    if (itemRefs[key].TryGetTarget(out prevFile))
                    {
                        return true;
                    }
                    else
                    {
                        itemRefs.Remove(key);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public T? GetItem(K key)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(key))
                {
                    T? item;
                    if (itemRefs[key].TryGetTarget(out item))
                    {
                        return item;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)itemRefs).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)itemRefs).GetEnumerator();
        }

        public List<K> KeyList()
        {
            return itemRefs.Keys.ToList();
        }

    }
}
