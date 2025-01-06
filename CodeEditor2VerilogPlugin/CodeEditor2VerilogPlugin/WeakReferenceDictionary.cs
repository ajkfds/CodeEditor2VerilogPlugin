using AjkAvaloniaLibs.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class WeakReferenceDictionary<K,T> where K:notnull where T: class 
    {
        private Dictionary<K, System.WeakReference<T>> itemRefs = new Dictionary<K, WeakReference<T>>();

        public void Register(K key, T item)
        {
            lock (itemRefs)
            {
                if (itemRefs.ContainsKey(key))
                {
                    T? prevItem;
                    if (itemRefs[key].TryGetTarget(out prevItem))
                    {   // replace item
                        itemRefs[key] = new WeakReference<T>(item);
                        return;
                    }
                    else
                    {
                        // prevItem already lost
                        itemRefs.Remove(key);  // remove lost reference 
                    }
                }

                itemRefs.Add(key, new WeakReference<T>(item));
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

        public List<K> GetMatchedKeyList(Func<T,bool> isMatched)
        {
            List<K> resultKeys = new List<K>();
            List<K> removeKeys = new List<K>();

            foreach (var itemRef in itemRefs)
            {
                T? item;
                if (!itemRefs[itemRef.Key].TryGetTarget(out item))
                {
                    removeKeys.Add(itemRef.Key);
                }else if (isMatched(item))
                {
                    resultKeys.Add(itemRef.Key);
                }
            }

            foreach (var key in removeKeys)
            {
                itemRefs.Remove(key);
            }
            return resultKeys;
        }

        public List<K> KeyList()
        {
            return itemRefs.Keys.ToList();
        }

        public void CleanDictionary()
        {
            lock (itemRefs)
            {
                List<K> removeKeys = new List<K>();

                foreach(var itemRef in itemRefs)
                {
                    T? item;
                    if (!itemRefs[itemRef.Key].TryGetTarget(out item))
                    {
                        removeKeys.Add(itemRef.Key);
                    }

                }

                foreach(var key in removeKeys)
                {
                    itemRefs.Remove(key);
                }
            }
        }
    }
}
