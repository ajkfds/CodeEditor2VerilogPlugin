using Avalonia.Input;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Constants;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{


    /// <summary>
    /// dataobjectへの参照を保持するクラス。
    /// packed arrayはDataTypeに含まれ、Unpackerd ArrayはDataObjectに含まれるため、両方の情報を保持する必要がある。
    /// bit-selectやpart-selectが行われた場合、
    /// </summary>
    public class DataObjectReference : Primary
    {


        protected DataObjectReference() { }


        // DataObject名称
        public required string DatObjectName { get; init; }


        // DataObject定義への参照
        public DataObjects.DataObject? OrigainalDataObject = null;
        public List<RangeExpression> RangesFromOriginal { get; set; } = new List<RangeExpression>();

        // 参照先DataObject.DefinedDataObjectの部分Clone
        public DataObjects.DataObject? TargetDataObject = null;


        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (TargetDataObject is Reg || TargetDataObject is Bit || TargetDataObject is Logic)
            {
                label.AppendText(DatObjectName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));
            }
            else if (TargetDataObject is Net)
            {
                label.AppendText(DatObjectName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Net));
            }
            else if (TargetDataObject is DataObjects.Constants.Constants)
            {
                label.AppendText(DatObjectName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Parameter));
            }
            else
            {
                label.AppendText(DatObjectName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));
            }
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append(CreateString());
        }

        public override string CreateString()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
            AppendLabel(label);
            StringBuilder sb = new StringBuilder();
            sb.Append(DatObjectName);
            return label.CreateString();
        }

        public override void AppendRefrencedDataObjects(List<DataObjects.DataObject> referencedObjects)
        {
            if (TargetDataObject == null) return;
            if (!referencedObjects.Contains(TargetDataObject))
            {
                referencedObjects.Add(TargetDataObject);
            }
        }

        public override void AssertAssigned()
        {
            if (OrigainalDataObject == null) return;
            if (OrigainalDataObject.AssignedMap == null) return;
            OrigainalDataObject.AssignedMap.Assert(RangesFromOriginal);
        }


        /// <summary>
        /// create DataObject Reference from DataObject
        /// </summary>
        /// <param name="dataObject"></param>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        public static DataObjectReference Create(DataObjects.DataObject dataObject, NameSpace nameSpace)
        {
            DataObjectReference val = new DataObjectReference()
            {
                DatObjectName = dataObject.Name
            };
            val.TargetDataObject = dataObject;
            if (dataObject is DataObjects.Variables.IntegerVectorValueVariable)
            {
                IntegerVectorValueVariable integerVectorValueVariable = (IntegerVectorValueVariable)dataObject;
                val.BitWidth = integerVectorValueVariable.BitWidth;
            }
            if (dataObject.DefinedReference != null) val.Reference = dataObject.DefinedReference;
            if (dataObject is DataObjects.Constants.Constants)
            {
                val.Constant = true;
            }
            val.SyncContext.PropageteClockDomainFrom(dataObject.SyncContext, val.Reference);
            return val;
        }

        public static DataObjectReference? ParseCreate(WordScanner word, NameSpace nameSpace, INamedElement owner, bool assigned)
        {
            return parseCreate(word, nameSpace, owner, assigned, true);
        }

        // for foreach parse. get DataObject reference w/o range parse
        public static DataObjectReference? ParseCreateWoRange(WordScanner word, NameSpace nameSpace, INamedElement owner, bool assigned)
        {
            return parseCreate(word, nameSpace, owner, assigned, false);
        }

        /// <summary>
        /// Parse and Create DataObject Reference
        /// </summary>
        /// <param name="word"></param>
        /// <param name="nameSpace">nameSpace is reqired for range expression parse</param>
        /// <param name="owner"></param>
        /// <param name="assigned"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static DataObjectReference? parseCreate(WordScanner word, NameSpace nameSpace, INamedElement owner, bool assigned, bool acceptRange)
        {
            if (!owner.NamedElements.ContainsDataObject(word.Text)) return null;
            DataObjects.DataObject originalDataObject = (DataObjects.DataObject)owner.NamedElements[word.Text];

            DataObjectReference val = new DataObjectReference()
            {
                DatObjectName = originalDataObject.Name
            };
            DataObjects.DataObject originalObject = originalDataObject;
            bool partial = false;

            // もともとのdataobject定義を保持
            val.OrigainalDataObject = originalDataObject;

            // TargetDataObjectは部分配列取得のためにCloneされる。(DataTypeも含めDeep Cloneされる)
            val.TargetDataObject = originalDataObject.Clone();
            val.Reference = word.GetReference();

            word.Color(val.TargetDataObject.ColorType);

            // constantの場合は値をコピー
            if (val.TargetDataObject is DataObjects.Constants.Constants)
            {
                DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)val.TargetDataObject;
                val.Constant = true;
                if (constants.Expression.Constant && constants.Expression.Value != null)
                {
                    val.Value = constants.Expression.Value;
                }
                if (constants.Expression.Constant) val.BitWidth = constants.Expression.BitWidth;
            }

            // エラーチェック
            // comment annotationの@discardでdiscardされた後に使用されたエラー
            if (val.TargetDataObject.CommentAnnotation_Discarded)
            {
                word.AddError("Disarded.");
            }
            else
            {
                // 未宣言の変数が使用されたエラー
                if (!word.Prototype && !val.TargetDataObject.Defined)
                {
                    word.AddError("not defined here");
                }
            }
            word.MoveNext();

            // parse unpacked dimensions
            // TargetDataObjectのUnpacked ArrayをDataObjectReferenceに移動させる。
            foreach (var unpackedArray in val.TargetDataObject.UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.TargetDataObject.UnpackedArrays.Clear();

            { // DataObjectReferenceのUnpackedArray数の上限まで、Range表記を処理する。
                int unpackedArrayIndex = 0;
                while (!word.Eof)
                {
                    if (word.Text != "[") break;
                    if (val.UnpackedArrays.Count <= unpackedArrayIndex) break;

                    RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                    if (rangeExpression == null)
                    {
                        return null;
                    }

                    // 部分UnpackedArrayを生成する。
                    partial = true;
                    if (rangeExpression is SingleBitRangeExpression)
                    {
                        SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                        UnPackedArray oldArray = val.UnpackedArrays[unpackedArrayIndex];
                        if (!word.Prototype && oldArray.MaxIndex != null && oldArray.MinIndex != null && singleBitRangeExpression.BitIndex != null)
                        {
                            if (oldArray.MaxIndex < singleBitRangeExpression.BitIndex || oldArray.MinIndex > singleBitRangeExpression.BitIndex)
                            {
                                singleBitRangeExpression.WordReference.AddError("index out of range");
                            }
                        }

                        val.RangesFromOriginal.Add(rangeExpression);
                        //単一のUnpacked Arrayが選択されたので、DataObjectReferenceのUnpackedArrayIndexの次元数を1次元落とす。
                        val.UnpackedArrays.RemoveAt(unpackedArrayIndex);
                    }
                    else
                    {
                        if(rangeExpression is AbsoluteRangeExpression)
                        {
                            AbsoluteRangeExpression absoluteRangeExpression = (AbsoluteRangeExpression)rangeExpression;
                            UnPackedArray oldArray = val.UnpackedArrays[unpackedArrayIndex];
                            if (!word.Prototype)
                            {
                                if(
                                    absoluteRangeExpression.MaxBitIndex != null && absoluteRangeExpression.MinBitIndex != null &&
                                    oldArray.MinIndex != null && oldArray.MaxIndex != null
                                    )
                                {
                                    if(
                                        absoluteRangeExpression.MaxBitIndex < oldArray.MinIndex ||
                                        absoluteRangeExpression.MaxBitIndex > oldArray.MaxIndex ||
                                        absoluteRangeExpression.MinBitIndex < oldArray.MinIndex ||
                                        absoluteRangeExpression.MinBitIndex > oldArray.MaxIndex 
                                        )
                                    {
                                        absoluteRangeExpression.WordReference.AddError("index out of range");
                                    }
                                }
                            }
                        }

                        val.UnpackedArrays[unpackedArrayIndex] = new UnPackedArray(rangeExpression.BitWidth);
                        val.RangesFromOriginal.Add(rangeExpression);
                        unpackedArrayIndex++;
                    }
                }
            }

            // parse packed dimensions
            if (val.TargetDataObject.Packable)
            {
                int packedArrayIndex = 0;
                IPackedDataObject ival = (IPackedDataObject)val.TargetDataObject;

                while (!word.Eof)
                {
                    if (word.Text != "[")
                    {
                        break;
                    }
                    if (ival.PackedDimensions.Count <= packedArrayIndex) break;

                    RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                    if (rangeExpression == null) return null;
                    partial = true;

                    if (rangeExpression is SingleBitRangeExpression)
                    {
                        SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                        PackedArray oldArray = ival.PackedDimensions[packedArrayIndex];
                        if (!word.Prototype && oldArray.MaxIndex != null && oldArray.MinIndex != null && singleBitRangeExpression.BitIndex != null)
                        {
                            if (oldArray.MaxIndex < singleBitRangeExpression.BitIndex || oldArray.MinIndex > singleBitRangeExpression.BitIndex)
                            {
                                singleBitRangeExpression.WordReference.AddError("index out of range");
                            }
                        }
                        ival.PackedDimensions.RemoveAt(packedArrayIndex);
                        val.RangesFromOriginal.Add(rangeExpression);
                    }
                    else
                    {
                        ival.PackedDimensions[packedArrayIndex] = new PackedArray(rangeExpression.BitWidth);
                        packedArrayIndex++;
                        val.RangesFromOriginal.Add(rangeExpression);
                    }
                }
            }

            // partial select
            if (val.TargetDataObject is IPartSelectableDataObject)
            {
                var ival = (IPartSelectableDataObject)val.TargetDataObject;
                if (ival.PartSelectable)
                {
                    IDataType? partSel = ival.ParsePartSelect(word, nameSpace);
                    //                         val.RangesFromOriginal.Add(rangeExpression);

                    if (partSel != null)
                    {
                        val.TargetDataObject = DataObjects.Variables.Variable.Create(originalObject.Name, partSel);
                        val.BitWidth = val.TargetDataObject.BitWidth;
                    }
                }
            }


            while (word.Text == "[" && !word.Eof && originalObject is DataObjects.Variables.String)
            {
                RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                if (rangeExpression is not SingleBitRangeExpression)
                {
                    word.AddError("illegal range");
                    break;
                }
                val.TargetDataObject = DataObjects.Variables.Byte.Create(originalObject.Name, DataObjects.DataTypes.ByteType.Create(false));
                val.RangesFromOriginal.Add(rangeExpression);
                break;
            }

            while (word.Text == "[" && !word.Eof)
            {
                word.AddError("illegal range");
                word.SkipToKeywords(new List<string> { "]", ";" });
                if (word.Text == "]") word.MoveNext();
            }

            //// parse ranges
            //if (word.GetCharAt(0) == '[' && acceptRange)
            //{
            //    if (!parseRange(word, nameSpace, val)) return null;
            //}
            //else
            {   // w/o range
                if (originalDataObject is DataObjects.Variables.IntegerVectorValueVariable || originalDataObject is Net)
                {
                    //var original = dataObject as DataObjects.Variables.IntegerVectorValueVariable;
                    //if (original == null) throw new Exception();
                    val.BitWidth = val.TargetDataObject.BitWidth;
                }
                else if (originalDataObject is DataObjects.Constants.Parameter)
                {
//                    var constants = (DataObjects.Constants.Parameter)originalDataObject;
//                    if (constants.Expression != null)
//                    {
//                        val.Value = constants.Expression.Value;
////                        val.BitWidth = constants.Expression.BitWidth;
//                    }
                }
                else if (originalDataObject is Net)
                {
                    if (((Net)originalDataObject).BitWidth != null) val.BitWidth = ((Net)originalDataObject).BitWidth;
                    else val.BitWidth = 1;
                }
                else if (originalDataObject is DataObjects.Variables.Genvar)
                {
                    val.Constant = true;
                }
            }

            foreach (UnPackedArray unPackedArray in val.UnpackedArrays)
            {
                val.BitWidth = val.BitWidth * unPackedArray.Size;
            }
            if (assigned)
            {
                if(!partial) originalObject.AssignedReferences.Add(val.Reference);
            }
            else
            {
                originalObject.UsedReferences.Add(val.Reference);
            }

            val.SyncContext.PropageteClockDomainFrom(originalObject.SyncContext, val.Reference);
            return val;
        }

        public override SyncContext SyncContext
        {
            get
            {
                if (TargetDataObject == null) return new SyncContext();
                return TargetDataObject.SyncContext;
            }
        }


    }
}
