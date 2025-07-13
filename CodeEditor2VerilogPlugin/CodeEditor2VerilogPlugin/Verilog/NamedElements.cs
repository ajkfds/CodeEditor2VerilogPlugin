using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class NamedElements
    {
        private List<INamedElement> itemList = new List<INamedElement>();
        private Dictionary<string, INamedElement> itemDict = new Dictionary<string, INamedElement>();

        public bool TryGetValue(string key, out INamedElement? namedElement)
        {
            if (!itemDict.TryGetValue(key, out namedElement))
            {
                return false;
            }
            return true;
        }

        public bool TryGetModuleInstantiation(string key, out ModuleInstantiation? moduleInstantiation)
        {
            moduleInstantiation = null;
            if (!itemDict.TryGetValue(key, out INamedElement? namedElement))
            {
                moduleInstantiation = namedElement as ModuleInstantiation;
                return false;
            }
            return true;
        }

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
            int index = itemList.IndexOf(oldItem);
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
        public bool ContainsNameSpace(string key)
        {
            if (!itemDict.ContainsKey(key)) return false;
            if (itemDict[key] is NameSpace) return true;
            return false;
        }
        public bool ContainsModPort(string key)
        {
            if (!itemDict.ContainsKey(key)) return false;
            if (itemDict[key] is ModPort) return true;
            return false;
        }
        public bool ContainsTask(string key)
        {
            if (!itemDict.ContainsKey(key)) return false;
            if (itemDict[key] is Task) return true;
            return false;
        }
        public bool ContainsFunction(string key)
        {
            if (!itemDict.ContainsKey(key)) return false;
            if (itemDict[key] is Function) return true;
            return false;
        }
        public bool ContainsDataObject(string key)
        {
            if (!itemDict.ContainsKey(key)) return false;
            if (itemDict[key] is DataObjects.DataObject) return true;
            return false;
        }
        public bool ContainsIBuldingBlockInstantiation(string key)
        {
            if (!itemDict.ContainsKey(key)) return false;
            if (itemDict[key] is ModuleItems.IBuildingBlockInstantiation) return true;
            return false;
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
