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
            DataObjects.DataObject dataObject = (DataObjects.DataObject)owner.NamedElements[word.Text];

            DataObjectReference val = new DataObjectReference()
            {
                VariableName = dataObject.Name
            };
            val.DataObject = dataObject;
            //if(val.DataObject is IntegerVectorValueVariable && !word.Prototype)
            //{
            //    val.DataObject = val.DataObject.Clone();
            //}

            val.Reference = word.GetReference();

            word.Color(dataObject.ColorType);

            if (dataObject is DataObjects.Constants.Constants)
            {
                DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)dataObject;
                val.Constant = true;
                if (constants.Expression.Constant && constants.Expression.Value != null)
                {
                    val.Value = constants.Expression.Value;
                }
                if (constants.Expression.Constant) val.BitWidth = constants.Expression.BitWidth;
            }

            if (assigned)
            {
                val.DataObject.AssignedReferences.Add(word.GetReference());
            }
            else
            {
                val.DataObject.UsedReferences.Add(word.GetReference());
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

            // parse dimensions
            foreach (var unpackedArray in dataObject.UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }

            while (!word.Eof && val.Dimensions.Count < dataObject.UnpackedArrays.Count)
            {
                if (word.GetCharAt(0) != '[')
                {
                    break;
                }
                word.MoveNext();    // [
                Expression? exp = Expression.ParseCreate(word, nameSpace);
                if (exp != null) val.Dimensions.Add(exp);
                val.UnpackedArrays.RemoveAt(0);

                if (word.GetCharAt(0) != ']')
                {
                    word.AddError("illegal dimension");
                    break;
                }
                word.MoveNext();    // ]
            }

            // parse ranges
            if (word.GetCharAt(0) == '[' && acceptRange)
            {
                if (!parseRange(word, nameSpace, val)) return null;
            }
            else
            {   // w/o range
                if (dataObject is DataObjects.Variables.IntegerVectorValueVariable)
                {
                    var original = dataObject as DataObjects.Variables.IntegerVectorValueVariable;
                    if (original == null) throw new Exception();
                    val.BitWidth = original.BitWidth;
                }
                else if (dataObject is DataObjects.Constants.Parameter)
                {
                    var constants = (DataObjects.Constants.Parameter)dataObject;
                    if (constants.Expression != null)
                    {
                        val.Value = constants.Expression.Value;
                        val.BitWidth = constants.Expression.BitWidth;
                    }
                }
                else if (dataObject is Net)
                {
                    if (((Net)dataObject).BitWidth != null) val.BitWidth = ((Net)dataObject).BitWidth;
                    else val.BitWidth = 1;
                }
                else if (dataObject is DataObjects.Variables.Genvar)
                {
                    val.Constant = true;
                }
            }

            foreach (UnPackedArray unPackedArray in val.UnpackedArrays)
            {
                val.BitWidth = val.BitWidth * unPackedArray.Size;
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
        private static bool parseRange(WordScanner word, NameSpace nameSpace, DataObjectReference val)
        {
            if (word.Text != "[") throw new Exception();
            word.MoveNext();

            Expression? exp1 = Expression.ParseCreate(word, nameSpace);
            Expression? exp2;
            switch (word.Text)
            {
                case ":":
                    word.MoveNext();
                    exp2 = Expression.ParseCreate(word, nameSpace);
                    if (word.Text != "]")
                    {
                        word.AddError("illegal range");
                        return false;
                    }
                    word.MoveNext();
                    val.RangeExpression = new AbsoluteRangeExpression(exp1, exp2);
                    break;
                case "+:":
                    word.MoveNext();
                    exp2 = Expression.ParseCreate(word, nameSpace);
                    if (word.Text != "]")
                    {
                        word.AddError("illegal range");
                        return false;
                    }
                    word.MoveNext();
                    val.RangeExpression = new RelativePlusRangeExpression(exp1, exp2);
                    break;
                case "-:":
                    word.MoveNext();
                    exp2 = Expression.ParseCreate(word, nameSpace);
                    if (word.Text != "]")
                    {
                        word.AddError("illegal range");
                        return false;
                    }
                    word.MoveNext();
                    val.RangeExpression = new RelativeMinusRangeExpression(exp1, exp2);
                    break;
                case "]":
                    word.MoveNext();
                    val.RangeExpression = new SingleBitRangeExpression(exp1);
                    break;
                default:
                    word.AddError("illegal range/dimension");
                    return false;
            }
            val.BitWidth = val.RangeExpression.BitWidth;
            return true;
        }

    }
}
