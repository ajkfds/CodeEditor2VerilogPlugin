using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Arrays;
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
    public class DataObjectReference : Primary
    {
        protected DataObjectReference() { }
        public required string VariableName { get; init; }
        public RangeExpression? RangeExpression { get; protected set; }
        public List<Expression> Dimensions = new List<Expression>();
        public DataObjects.DataObject? DataObject = null;
        public string NameSpaceText = "";
        public List<DataObjects.Arrays.UnPackedArray> UnpackedArrays { get; set; } = new List<DataObjects.Arrays.UnPackedArray>();


        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            //label.AppendText(NameSpaceText, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));

            if (DataObject is Reg || DataObject is Bit || DataObject is Logic)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));
            }
            else if (DataObject is Net)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Net));
            }
            else if (DataObject is DataObjects.Constants.Constants)
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Parameter));
            }
            else
            {
                label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));
            }

            if (RangeExpression != null)
            {
                label.AppendText(" ");
                label.AppendLabel(RangeExpression.GetLabel());
            }
            foreach (Expression expression in Dimensions)
            {
                label.AppendText(" [");
                label.AppendLabel(expression.GetLabel());
                label.AppendText("]");
            }
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append(CreateString());
        }

        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(NameSpaceText);
            sb.Append(VariableName);
            if (RangeExpression != null)
            {
                sb.Append(RangeExpression.CreateString());
            }
            foreach (Expression expression in Dimensions)
            {
                sb.Append(" [");
                sb.Append(expression.CreateString());
                sb.Append("]");
            }
            return sb.ToString();
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
                VariableName = dataObject.Name
            };
            val.DataObject = dataObject;
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
                VariableName = originalDataObject.Name
            };
            DataObjects.DataObject originalObject = originalDataObject;
            bool partial = false;

            val.DataObject = originalDataObject.Clone();
            val.Reference = word.GetReference();

            word.Color(val.DataObject.ColorType);

            if (val.DataObject is DataObjects.Constants.Constants)
            {
                DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)val.DataObject;
                val.Constant = true;
                if (constants.Expression.Constant && constants.Expression.Value != null)
                {
                    val.Value = constants.Expression.Value;
                }
                if (constants.Expression.Constant) val.BitWidth = constants.Expression.BitWidth;
            }


            if (val.DataObject.CommentAnnotation_Discarded)
            {
                word.AddError("Disarded.");
            }
            else
            {
                if (!word.Prototype && !val.DataObject.Defined)
                {
                    word.AddError("not defined here");
                }
            }

            word.MoveNext();

            // parse unpacked dimensions
            foreach (var unpackedArray in val.DataObject.UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.DataObject.UnpackedArrays.Clear();

            {
                int unpackedArrayIndex = 0;
                while (!word.Eof)
                {
                    if (word.Text != "[")
                    {
                        break;
                    }
                    if (val.UnpackedArrays.Count <= unpackedArrayIndex) break;

                    RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                    if (rangeExpression == null) return null;

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
                        unpackedArrayIndex++;
                    }
                }
            }

            // parse packed dimensions
            if (val.DataObject is IntegerVectorValueVariable)
            {
                int packedArrayIndex = 0;
                IntegerVectorValueVariable ival = (IntegerVectorValueVariable)val.DataObject;

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
                        if(!word.Prototype && oldArray.MaxIndex != null && oldArray.MinIndex != null && singleBitRangeExpression.BitIndex != null)
                        {
                            if( oldArray.MaxIndex < singleBitRangeExpression.BitIndex || oldArray.MinIndex > singleBitRangeExpression.BitIndex)
                            {
                                singleBitRangeExpression.WordReference.AddError("index out of range");
                            }
                        }
                        ival.PackedDimensions.RemoveAt(packedArrayIndex);
                    }
                    else
                    {
                        ival.PackedDimensions[packedArrayIndex] = new PackedArray(rangeExpression.BitWidth);
                        packedArrayIndex++;
                    }
                }
            }
            else if(val.DataObject is IntegerAtomVariable)
            {
                IntegerAtomVariable ival = (IntegerAtomVariable)val.DataObject;

                while (!word.Eof)
                {
                    if (word.Text != "[")
                    {
                        break;
                    }

                    RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                    if (rangeExpression == null) return null;
                    partial = true;

                    if (rangeExpression is SingleBitRangeExpression)
                    {
                        SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                        if(!word.Prototype && singleBitRangeExpression.BitIndex != null)
                        {
                            if(singleBitRangeExpression.BitIndex<0 || singleBitRangeExpression.BitIndex>= val.DataObject.BitWidth)
                            {
                                singleBitRangeExpression.WordReference.AddError("index out of range");
                            }
                        }

                        List<PackedArray> packedDimensions = new List<PackedArray>();
                        packedDimensions.Add(new PackedArray(1));
                        val.DataObject = DataObjects.Variables.Logic.Create(ival.Name, DataObjects.DataTypes.IntegerVectorType.Create(DataObjects.DataTypes.DataTypeEnum.Logic, false, packedDimensions));
                        break;
                    }
                    else
                    {
                        List<PackedArray> packedDimensions = new List<PackedArray>();
                        packedDimensions.Add(new PackedArray(rangeExpression.BitWidth));
                        val.DataObject = DataObjects.Variables.Logic.Create(ival.Name, DataObjects.DataTypes.IntegerVectorType.Create(DataObjects.DataTypes.DataTypeEnum.Logic, false, packedDimensions));
                        break;
                    }
                }
            }
            else if (val.DataObject is Net)
            {
                int packedArrayIndex = 0;
                Net ival = (Net)val.DataObject;

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
                    }
                    else
                    {
                        if(rangeExpression is AbsoluteRangeExpression)
                        {
                            AbsoluteRangeExpression absoluteRangeExpression = (AbsoluteRangeExpression)rangeExpression;
                            PackedArray oldArray = ival.PackedDimensions[packedArrayIndex];
                            if (!word.Prototype)
                            {
                                if (
                                    absoluteRangeExpression.MaxBitIndex != null && absoluteRangeExpression.MinBitIndex != null &&
                                    oldArray.MinIndex != null && oldArray.MaxIndex != null
                                    )
                                {
                                    if (
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
                        ival.PackedDimensions[packedArrayIndex] = new PackedArray(rangeExpression.BitWidth);
                        packedArrayIndex++;
                    }
                }
            }

            while(word.Text == "[" && !word.Eof && originalObject is DataObjects.Variables.String)
            {
                RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                if (rangeExpression is not SingleBitRangeExpression)
                {
                    word.AddError("illegal range");
                    break;
                }
                val.DataObject = DataObjects.Variables.Byte.Create(originalObject.Name, DataObjects.DataTypes.IntegerAtomType.Create(DataObjects.DataTypes.DataTypeEnum.Byte, false));
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
                    val.BitWidth = val.DataObject.BitWidth;
                }
                else if (originalDataObject is DataObjects.Constants.Parameter)
                {
                    var constants = (DataObjects.Constants.Parameter)originalDataObject;
                    if (constants.Expression != null)
                    {
                        val.Value = constants.Expression.Value;
                        val.BitWidth = constants.Expression.BitWidth;
                    }
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

            return val;
        }

        public override SyncContext SyncContext
        {
            get
            {
                if (DataObject == null) return new SyncContext();
                return DataObject.SyncContext;
            }
        }

        private void parsePackedDimension(WordScanner word, NameSpace nameSpace, DataObjectReference val)
        {
            foreach (var unpackedArray in val.DataObject.UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.DataObject.UnpackedArrays.Clear();

            {
                int unpackedArrayIndex = 0;
                while (!word.Eof)
                {
                    if (word.Text != "[")
                    {
                        break;
                    }
                    if (val.UnpackedArrays.Count <= unpackedArrayIndex) break;

                    RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
                    if (rangeExpression == null) return;

//                    partial = true;
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
                        val.UnpackedArrays.RemoveAt(unpackedArrayIndex);
                    }
                    else
                    {
                        if (rangeExpression is AbsoluteRangeExpression)
                        {
                            AbsoluteRangeExpression absoluteRangeExpression = (AbsoluteRangeExpression)rangeExpression;
                            UnPackedArray oldArray = val.UnpackedArrays[unpackedArrayIndex];
                            if (!word.Prototype)
                            {
                                if (
                                    absoluteRangeExpression.MaxBitIndex != null && absoluteRangeExpression.MinBitIndex != null &&
                                    oldArray.MinIndex != null && oldArray.MaxIndex != null
                                    )
                                {
                                    if (
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
                        unpackedArrayIndex++;
                    }
                }
            }
        }

    }
}
