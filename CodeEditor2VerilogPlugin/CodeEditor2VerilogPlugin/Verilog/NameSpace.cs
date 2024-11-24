using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Nets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pluginVerilog.Verilog
{
    public class NameSpace : Item
    {
        protected NameSpace(BuildingBlocks.BuildingBlock buildingBlock,NameSpace parent)
        {
            BuildingBlock = buildingBlock;
            Parent = parent;
        }

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference = null;

        private Dictionary<string, DataObjects.DataObject> variables = new Dictionary<string, DataObjects.DataObject>();
        private Dictionary<string, Net> nets = new Dictionary<string, Net>();
        private Dictionary<string, DataObjects.Typedef> typedefs = new Dictionary<string, DataObjects.Typedef>();
        private Dictionary<string, BuildingBlocks.Class> classes = new Dictionary<string, Class>();

        private Dictionary<string, NameSpace> nameSpaces = new Dictionary<string, NameSpace>();

        public Dictionary<string, DataObjects.DataObject> DataObjects { get { return variables; } }

        public required NameSpace Parent { get; init; }

        private Dictionary<string, DataObjects.Constants.Constants> constants = new Dictionary<string, DataObjects.Constants.Constants>();
        public Dictionary<string, DataObjects.Constants.Constants> Constants { get { return constants; } }

        public Dictionary<string, DataObjects.Typedef> Typedefs { get { return typedefs; } }
        public Dictionary<string,BuildingBlocks.Class> Classes {  get { return classes; } }
        public BuildingBlocks.BuildingBlock BuildingBlock { get; protected set; }
        public Dictionary<string, NameSpace> NameSpaces { get { return nameSpaces;  } }

        public NameSpace GetHierarchyNameSpace(IndexReference iref)
        {
            foreach (Function function in BuildingBlock.Functions.Values)
            {
                if (function.BeginIndexReference == null) continue;
                if (function.LastIndexReference == null) continue;

                if (iref.IsSmallerThan(function.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(function.LastIndexReference)) continue;
                return function.getHierarchyNameSpace2(iref);
            }
            foreach (Task task in BuildingBlock.Tasks.Values)
            {
                if (task.BeginIndexReference == null) continue;
                if (task.LastIndexReference == null) continue;

                if (iref.IsSmallerThan(task.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(task.LastIndexReference)) continue;
                return task.getHierarchyNameSpace2(iref);
            }
            return getHierarchyNameSpace2(iref);
        }
        private NameSpace getHierarchyNameSpace2(IndexReference iref)
        {
            foreach (NameSpace nameSpace in NameSpaces.Values)
            {
                if (nameSpace.BeginIndexReference == null) continue;
                if (nameSpace.LastIndexReference == null) continue;

                if (iref.IsSmallerThan(nameSpace.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(nameSpace.LastIndexReference)) continue;
                return nameSpace.GetHierarchyNameSpace(iref);
            }
            return this;
        }

        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public virtual void AppendAutoCompleteItem( List<AutocompleteItem> items)
        {
            foreach (DataObjects.DataObject variable in DataObjects.Values)
            {
                if(variable is DataObjects.Nets.Net)
                {
                    items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Net));
                }
                else if (variable is DataObjects.Variables.Variable)
                {
                    items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Variable));
                }
                else if (variable is DataObjects.Variables.Object)
                {
                    items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Variable));
                }
                else if (variable is DataObjects.Variables.Time || variable is DataObjects.Variables.Real || variable is DataObjects.Variables.Realtime || variable is DataObjects.Variables.Integer || variable is DataObjects.Variables.Genvar)
                {
                    items.Add(newItem(variable.Name, CodeDrawStyle.ColorType.Variable));
                }
            }

            foreach (DataObjects.Constants.Constants constants in BuildingBlock.Constants.Values)
            {
                items.Add(newItem(constants.Name, CodeDrawStyle.ColorType.Parameter));
            }

            foreach (Function function in BuildingBlock.Functions.Values)
            {
                items.Add(newItem(function.Name, CodeDrawStyle.ColorType.Identifier));
            }

            foreach (Task task in BuildingBlock.Tasks.Values)
            {
                items.Add(newItem(task.Name, CodeDrawStyle.ColorType.Identifier));
            }

            foreach (NameSpace space in NameSpaces.Values)
            {
                if (space.Name == null) System.Diagnostics.Debugger.Break();
                if (space.Name == null) continue;
                items.Add(newItem(space.Name, CodeDrawStyle.ColorType.Identifier));
            }

            if(Parent != null)
            {
                Parent.AppendAutoCompleteItem(items);
            }
        }

        public DataObjects.DataObject? GetDataObject(string identifier)
        {
            if (DataObjects.ContainsKey(identifier))
            {
                return DataObjects[identifier];
            }

            if (Parent != null)
            {
                return Parent.GetDataObject(identifier);
            }
            return null;
        }

        public DataObjects.Constants.Constants? GetConstants(string identifier)
        {
            if (Constants.ContainsKey(identifier))
            {
                return Constants[identifier];
            }

            if (Parent != null)
            {
                return Parent.getConstantsHier(identifier);
            }
            else
            {
                
            }

            return null;
        }

        private DataObjects.Constants.Constants? getConstantsHier(string identifier)
        {
            if (Constants.ContainsKey(identifier))
            {
                return Constants[identifier];
            }

            if (Parent != null)
            {
                return Parent.getConstantsHier(identifier);
            }

            return null;
        }
    }
}
