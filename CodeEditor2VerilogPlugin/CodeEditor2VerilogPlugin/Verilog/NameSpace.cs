using Avalonia.Input;
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
    public class NameSpace : Item, INamedElement
    {
        protected NameSpace(BuildingBlocks.BuildingBlock buildingBlock,NameSpace parent)
        {
            BuildingBlock = buildingBlock;
            Parent = parent;
        }

        public NamedElements NamedElements { get; } = new NamedElements();

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference = null;

        public required NameSpace Parent { get; init; }

        public BuildingBlocks.BuildingBlock BuildingBlock { get; protected set; }

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
            foreach(INamedElement element in NamedElements.Values)
            {
                NameSpace? nameSpace = element as NameSpace;
                if (nameSpace == null) continue;
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
            foreach (INamedElement element in NamedElements.Values)
            {
                if(element is DataObject)
                {
                    DataObjects.DataObject variable = (DataObjects.DataObject)element;
                    if (variable is DataObjects.Nets.Net)
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

                if(element is NameSpace)
                {
                    NameSpace space = (NameSpace)element;
                    if (space.Name == null) System.Diagnostics.Debugger.Break();
                    if (space.Name == null) continue;
                    items.Add(newItem(space.Name, CodeDrawStyle.ColorType.Identifier));

                }

                if(element is DataObjects.Constants.Constants)
                {
                    DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)element;
                    items.Add(newItem(constants.Name, CodeDrawStyle.ColorType.Parameter));
                }

            }


            foreach (Function function in BuildingBlock.Functions.Values)
            {
                items.Add(newItem(function.Name, CodeDrawStyle.ColorType.Identifier));
            }

            foreach (Task task in BuildingBlock.Tasks.Values)
            {
                items.Add(newItem(task.Name, CodeDrawStyle.ColorType.Identifier));
            }

            if(Parent != null)
            {
                Parent.AppendAutoCompleteItem(items);
            }
        }

        public DataObjects.Constants.Constants? GetConstants(string identifier)
        {
            if (NamedElements.ContainsKey(identifier))
            {
                INamedElement element = NamedElements[identifier];
                DataObjects.Constants.Constants? constants = element as DataObjects.Constants.Constants;
                return constants;
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
            if (NamedElements.ContainsKey(identifier))
            {
                INamedElement element = NamedElements[identifier];
                DataObjects.Constants.Constants? constants = element as DataObjects.Constants.Constants;
                return constants;
            }

            if (Parent != null)
            {
                return Parent.getConstantsHier(identifier);
            }

            return null;
        }
    }
}
