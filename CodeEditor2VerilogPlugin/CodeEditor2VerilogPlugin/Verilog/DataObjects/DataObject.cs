using Avalonia;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    public abstract class DataObject : INamedElement
    {
        // #SystemVerilog 2017
        //	net												user-defined-size	4state	v
        //
        //	variable	+ integer_vector_type	+ bit 		user-defined-size	2state	sv
        //										+ logic		user-defined-size	4state  sv
        //										+ reg		user-defined-size	4state	v
        //
        //				+ integer_atom_type		+ byte		8bit signed			2state  sv
        //										+ shortint	16bit signed		2state  sv
        //										+ int		32bit signed		2state  sv
        //										+ longint	64bit signed		2state  sv
        //										+ integer	32bit signed		4state	v
        //										+ time		64bit unsigned		        v
        //
        //            	+ non_integer_type		+ shortreal	                            sv
        //										+ real		                            v
        //										+ realtime	                            v
        //              + struct/union
        //              + enum
        //              + string
        //              + chandle
        //              + virtual(interface)
        //              + class/package
        //              + event
        //              + pos_covergroup
        //              + type_reference
        // 
        [Newtonsoft.Json.JsonIgnore]
        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Normal; } }

        public bool CommentAnnotation_Discarded = false;
        public virtual NamedElements NamedElements { get; } = new NamedElements();


        private WeakReference<NameSpace>? wr_DefinedNameSpace = null;
        [Newtonsoft.Json.JsonIgnore]
        public NameSpace? DefinedNameSpace
        {
            get
            {
                if (wr_DefinedNameSpace == null) return null;
                if(!wr_DefinedNameSpace.TryGetTarget(out NameSpace? nameSpace))
                {
                    return null;
                }
                return nameSpace;
            }
            set
            {
                wr_DefinedNameSpace = new WeakReference<NameSpace>(value);
            }
        }
        public static DataObject Create(string name,DataTypes.IDataType dataType)
        {
            switch (dataType.Type)
            {
                //integer_vector_type::= bit | logic | reg
                case DataTypeEnum.Bit:
                    return Bit.Create(name, dataType);
                case DataTypeEnum.Logic:
                    return Logic.Create(name, dataType);
                case DataTypeEnum.Reg:
                    return Reg.Create(name, dataType);
                //integer_atom_type::= byte | shortint | int | longint | integer | time
                case DataTypeEnum.Byte:
                    return Variables.Byte.Create(name, dataType);
                case DataTypeEnum.Shortint:
                    return Shortint.Create(name, dataType);
                case DataTypeEnum.Int:
                    return Int.Create(name, dataType);
                case DataTypeEnum.Longint:
                    return Longint.Create(name, dataType);
                case DataTypeEnum.Integer:
                    return Integer.Create(name, dataType);
                case DataTypeEnum.Time:
                    return Time.Create(name, dataType);
                //non_integer_type::= "shortreal" | "real" | "realtime"
                case DataTypeEnum.Shortreal:
                    return Shortreal.Create(name, dataType);
                case DataTypeEnum.Real:
                    return Real.Create(name, dataType);
                case DataTypeEnum.Realtime:
                    return Realtime.Create(name, dataType);
                // others
                case DataTypeEnum.Enum:
                    return Variables.Enum.Create(name, dataType);
                case DataTypeEnum.String:
                    return Variables.String.Create(name, dataType);
//                case DataTypeEnum.Chandle:
//                    return Chan.Create(dataType);
//                case DataTypeEnum.Virtual:
//                    return .Create(dataType);
                case DataTypeEnum.Class:
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                    return DataObject.Create(name,dataType);
//                case DataTypeEnum.Interface:
//                    return InterfaceInstance.Create(name,dataType);
                case DataTypeEnum.Event:
                    return Event.Create(name,dataType);
//                case DataTypeEnum.CoverGroup:
//                    return .Create(dataType);
                case DataTypeEnum.Struct:
                    return Struct.Create(name, dataType);
//        TypeReference

            }
            throw new NotImplementedException();
        }

//        [Newtonsoft.Json.JsonIgnore]
        public required string Name { get; init; }
        [Newtonsoft.Json.JsonIgnore]
        public string Comment { set; get; } = "";
        public WordReference? DefinedReference { set; get; } = null;
        public IDataType? DataType;
        public virtual int? BitWidth
        {
            get {
                return DataType?.BitWidth;
            }
        }
        [Newtonsoft.Json.JsonIgnore]
        public List<Arrays.UnPackedArray> UnpackedArrays { get; set; } = new List<Arrays.UnPackedArray>();


        [Newtonsoft.Json.JsonIgnore]
        public List<WordReference> UsedReferences { set; get; } = new List<WordReference>();
        [Newtonsoft.Json.JsonIgnore]
        public List<WordReference> AssignedReferences { set; get; } = new List<WordReference>();
        public int DisposedIndex = -1;

        public SyncContext SyncContext { get; } = new SyncContext();

        public virtual string CreateTypeString()
        {
            if (DataType != null) return DataType.CreateString();
            return "";
        }

        public virtual void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(Name);
            SyncContext.AppendLabel(label);
        }

        public virtual void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {

        }

        public abstract DataObject Clone();
        public abstract DataObject Clone(string name);

        // IInstance
        public virtual Task? GetTask(string identifier)
        {
            return null;
        }
        public virtual Function? GetFunction(string identifier)
        {
            return null;
        }
        public virtual DataObject? GetDataObject(string identifier)
        {
            return null;
        }
        public virtual void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items)
        {
        }


    }
}
