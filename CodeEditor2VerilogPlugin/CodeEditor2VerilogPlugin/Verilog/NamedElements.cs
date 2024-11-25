using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class NamedElements
    {
        private List<INamedElement> itemList = new List<INamedElement>();
        private Dictionary<string, INamedElement> itemDict = new Dictionary<string, INamedElement>();

        public void Add(string key, INamedElement item)
        {
            if (itemDict.ContainsKey(key)) return;
            itemList.Add(item);
            itemDict.Add(key, item);
        }

        public DataObjects.DataObject? GetDataObject(string identifier)
        {
            if (!itemDict.ContainsKey(identifier)) return null;
            return itemDict[identifier] as DataObjects.DataObject;
        }

        public NameSpace? GetNameSpace(string identifier)
        {
            if (!itemDict.ContainsKey(identifier)) return null;
            return itemDict[identifier] as NameSpace;
        }

        public void Insert(int index, string key, INamedElement item)
        {
            if (itemDict.ContainsKey(key)) return;
            itemList.Insert(index, item);
            itemDict.Add(key, item);
        }

        public void Replace(string key,INamedElement item)
        {
            INamedElement oldItem = itemDict[key];
            int index = itemList.IndexOf(item);
            itemList[index] = item;
            itemDict[key] = item;
        }

        public int IndexOf(INamedElement item)
        {
            return itemList.IndexOf(item);
        }

        public INamedElement this[string key]
        {
            get
            {
                return itemDict[key];
            }
        }

        public INamedElement this[int index]
        {
            get
            {
                return itemList[index];
            }
        }

        public void Remove(string key)
        {
            itemList.Remove(itemDict[key]);
            itemDict.Remove(key);
        }
        public bool ContainsKey(string key)
        {
            return itemDict.ContainsKey(key);
        }

        public bool ContainsValue(INamedElement item)
        {
            return itemDict.ContainsValue(item);
        }

        public void Clear()
        {
            itemList.Clear();
            itemDict.Clear();
        }

        public Dictionary<string, INamedElement>.KeyCollection Keys
        {
            get { return itemDict.Keys; }
        }

        public List<INamedElement> Values
        {
            get
            {
                return itemList;
            }
        }
    }
}
