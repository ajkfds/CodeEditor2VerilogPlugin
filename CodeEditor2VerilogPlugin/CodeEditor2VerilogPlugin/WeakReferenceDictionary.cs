using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace pluginVerilog
{
    public class WeakReferenceDictionary<K, T> where K : notnull where T : class
    {
        private ConcurrentDictionary<K, System.WeakReference<T>> itemRefs = new ConcurrentDictionary<K, WeakReference<T>>();

        public int Count { get { return itemRefs.Count; } }
        public void Register(K key, T item)
        {
            WeakReference < T > weakRef = new WeakReference<T>(item);
            itemRefs.AddOrUpdate(key, weakRef, (k, old) => { return weakRef; });
        }

        public bool Remove(K key)
        {
            return itemRefs.TryRemove(key, out _);
        }

        public bool HasItem(K key)
        {
            return itemRefs.ContainsKey(key);
        }

        public T? GetItem(K key)
        {
            if(!itemRefs.TryGetValue(key, out var weakRef))
            {
                return null;
            }
            T? item;
            if (weakRef.TryGetTarget(out item))
            {
                return item;
            }
            else
            {
                itemRefs.TryRemove(key, out _);
                return null;
            }
        }

        public List<K> GetMatchedKeyList(Func<T, bool> isMatched)
        {
            List<K> resultKeys = new List<K>();

            foreach (var weakRefKvp in itemRefs)
            {
                T? item;
                if (!weakRefKvp.Value.TryGetTarget(out item))
                {
                    itemRefs.TryRemove(weakRefKvp.Key, out _);
                }
                else if (isMatched(item))
                {
                    resultKeys.Add(weakRefKvp.Key);
                }

            }
            return resultKeys;
        }

        public List<K> KeyList()
        {
            return itemRefs.Keys.ToList();
        }

        public void CleanDictionary()
        {
            foreach (var weakRefKvp in itemRefs)
            {
                T? item;
                if (!weakRefKvp.Value.TryGetTarget(out item))
                {
                    itemRefs.TryRemove(weakRefKvp.Key, out _);
                }
            }
        }
    }
}
