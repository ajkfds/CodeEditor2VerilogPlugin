using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog.DataObjects
{
    /// <summary>
    /// Verilog/SystemVerilogのDataObjectを保持するクラス。
    /// AssociativeArray, DynamicArray, Queue, Constants, InterfaceInstance, ModportInstance,Net, Veriableがこれを継承する。
    /// </summary>

    public abstract class DataObject : INamedElement
    {

        /*
        # DataObjectからの継承関係

        DataObject  AssociativeArray
                    DynamicArray
                    Queue
                    Constants   EnumConstants
                                Localparam
                                Parameter
                                Specparam
                    InterfaceInstance
                    ModportInstance
                    Net

                    Variable    Chandle
                                Enum
                                Event
                                Genvar
                                Object
                                Realtime
                                Struct

                                ValueVariable   IntegerAtomVariable         Byte
                                                                            Int
                                                                            Integer
                                                                            Longint
                                                                            Shortint
                                                                            Time
                                        
                                                IntegerVectorValueVariable  Bit
                                                                            Logic
                                                                            Reg

                                                Real
                                                Shortreal
                                                String
                                                UserDefinedVariable
         */








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
        [JsonIgnore]
        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Normal; } }

        public bool CommentAnnotation_Discarded = false;

        // flag to check defined position
        // defined = false @ prototype parse
        // defined changes to true @ definition on actual parse
        public bool Defined = false;
        public virtual NamedElements NamedElements { get; } = new NamedElements();

        public virtual AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }

        private WeakReference<NameSpace>? wr_DefinedNameSpace = null;
        [JsonIgnore]
        public NameSpace? DefinedNameSpace
        {
            get
            {
                if (wr_DefinedNameSpace == null) return null;
                if (!wr_DefinedNameSpace.TryGetTarget(out NameSpace? nameSpace))
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

        /// <summary>
        /// DataTypeで指定したDataObjectを作成する。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static DataObject Create(string name, DataTypes.IDataType dataType)
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
                case DataTypeEnum.Chandle:
                    return Variables.Chandle.Create(name, dataType);
                //                case DataTypeEnum.Virtual:
                //                    return .Create(dataType);
                case DataTypeEnum.Class:
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                    return Variables.Object.Create(name, dataType);
                //                case DataTypeEnum.Interface:
                //                    return InterfaceInstance.Create(name,dataType);
                case DataTypeEnum.Event:
                    return Event.Create(name, dataType);
                //                case DataTypeEnum.CoverGroup:
                //                    return .Create(dataType);
                case DataTypeEnum.Struct:
                    return Struct.Create(name, dataType);
                case DataTypeEnum.UserDefined:
                    return UserDefinedVariable.Create(name, dataType);
                    //        TypeReference

            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// part selectが可能かどうかを表す。
        /// IntegerVactorTypeはdalse,IntegerAtomTypeはtrueになるが、StructやUserDefinedTypeはその参照内容次第でpartSelect可能か同かが変わる。
        /// </summary>
        public virtual bool PartSelectable { get { return false; } }

        /// <summary>
        /// DataObjectの名称
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 定義時に後ろにつけられた説明コメント
        /// </summary>
        public string Comment { set; get; } = "";

        /// <summary>
        /// ドキュメント中の定義位置に対する参照
        /// </summary>
        public WordReference? DefinedReference { set; get; } = null;

        /// <summary>
        /// DataTypeに対する参照。
        /// unpacked arrayはDataObjectに含まれるが、packed arrayはdataTypeの一部であることに注意。
        /// </summary>
        public IDataType? DataType;

        public virtual bool Packable
        {
            get
            {
                if (UnpackedArrays.Count != 0) return false;
                if (DataType == null) return false;
                return DataType.Packable;
            }
        }
        public virtual int? BitWidth
        {
            get
            {
                int? bitWith = null;
                if (DataType != null && DataType.BitWidth != null)
                {
                    bitWith = (int)(DataType.BitWidth);
                    foreach (Arrays.UnPackedArray unPackedArray in UnpackedArrays)
                    {
                        if (unPackedArray.Size == null) return null;
                        bitWith = bitWith * (int)unPackedArray.Size;
                    }
                }
                return bitWith;
            }
        }
        [JsonIgnore]
        public List<Arrays.UnPackedArray> UnpackedArrays { get; set; } = new List<Arrays.UnPackedArray>();


        [JsonIgnore]
        public List<WordReference> UsedReferences { set; get; } = new List<WordReference>();
        [JsonIgnore]
        public List<WordReference> AssignedReferences { set; get; } = new List<WordReference>();
        public int DisposedIndex = -1;

        public ArraysBoolMap? AssignedMap = null;

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
